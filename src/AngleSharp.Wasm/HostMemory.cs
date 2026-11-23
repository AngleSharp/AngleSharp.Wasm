namespace AngleSharp.Wasm;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Wasmtime;

/// <summary>
/// Gives the linear memories of WebAssembly (Wasmtime's host memory creator) arrays of .NET,
/// pinned, so that a script's ArrayBuffer of a memory (memory.buffer) is the memory itself, as
/// in browsers, instead of a copy. Growing a memory moves it to a new array, like a browser
/// gives a new ArrayBuffer for it then.
/// </summary>
internal static class HostMemory
{
    private const string Library = "wasmtime";

    private static readonly Dictionary<IntPtr, LinearMemory> Memories = new();
    private static readonly NewMemoryCallback NewMemoryDelegate = NewMemory;
    private static readonly GetMemoryCallback GetMemoryDelegate = GetMemory;
    private static readonly GrowMemoryCallback GrowMemoryDelegate = GrowMemory;
    private static readonly FinalizerCallback FinalizeDelegate = Finalize;

    /// <summary>
    /// Configures Wasmtime to create its memories here: they may move as they grow (nothing is
    /// reserved up front, there are no guard pages, so accesses are checked explicitly).
    /// </summary>
    /// <returns>True if the memories are arrays of .NET, false if Wasmtime keeps its own.</returns>
    public static bool TryConfigure(Config config)
    {
        try
        {
            var property = typeof(Config).GetProperty("NativeHandle", BindingFlags.Instance | BindingFlags.NonPublic);

            if (property?.GetValue(config) is not SafeHandle handle)
            {
                return false;
            }

            config.WithMemoryMayMove(true)
                .WithMemoryGuardSize(0)
                .WithMemoryReservationForGrowth(0)
                .WithMemoryInitCopyOnWrite(false);

            var native = handle.DangerousGetHandle();
            wasmtime_config_memory_reservation_set(native, 0);
            var creator = new MemoryCreatorStruct
            {
                Env = IntPtr.Zero,
                NewMemory = Marshal.GetFunctionPointerForDelegate(NewMemoryDelegate),
                Finalizer = IntPtr.Zero,
            };
            wasmtime_config_host_memory_creator_set(native, ref creator);
            return true;
        }
        catch (Exception exception) when (exception is EntryPointNotFoundException or DllNotFoundException or TargetInvocationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the array holding the memory at the given address, if it is one of the arrays here
    /// and just as long as the memory.
    /// </summary>
    public static byte[]? Find(IntPtr address, long length)
    {
        lock (Memories)
        {
            return Memories.TryGetValue(address, out var memory) && memory.Data.LongLength == length && memory.Size == length
                ? memory.Data
                : null;
        }
    }

    private static IntPtr NewMemory(IntPtr env, IntPtr type, UIntPtr minimum, UIntPtr maximum, UIntPtr reserved, UIntPtr guard, IntPtr result)
    {
        try
        {
            var size = checked((long)minimum.ToUInt64());
            var capacity = Math.Max(size, checked((long)reserved.ToUInt64()));
            var memory = new LinearMemory(Allocate(capacity), size);
            var handle = GCHandle.Alloc(memory);
            Register(memory);
            Marshal.StructureToPtr(new LinearMemoryStruct
            {
                Env = GCHandle.ToIntPtr(handle),
                GetMemory = Marshal.GetFunctionPointerForDelegate(GetMemoryDelegate),
                GrowMemory = Marshal.GetFunctionPointerForDelegate(GrowMemoryDelegate),
                Finalizer = Marshal.GetFunctionPointerForDelegate(FinalizeDelegate),
            }, result, false);
            return IntPtr.Zero;
        }
        catch (Exception exception)
        {
            return wasmtime_error_new($"The memory could not be created: {exception.Message}");
        }
    }

    private static IntPtr GetMemory(IntPtr env, out UIntPtr size, out UIntPtr capacity)
    {
        var memory = (LinearMemory)GCHandle.FromIntPtr(env).Target!;
        size = (UIntPtr)(ulong)memory.Size;
        capacity = (UIntPtr)(ulong)memory.Data.LongLength;
        return memory.Address;
    }

    private static IntPtr GrowMemory(IntPtr env, UIntPtr newSize)
    {
        try
        {
            var memory = (LinearMemory)GCHandle.FromIntPtr(env).Target!;
            var size = checked((long)newSize.ToUInt64());

            if (size > memory.Data.LongLength)
            {
                // a new array (zeroed) with what there is, as an array can't grow
                var data = Allocate(size);
                Buffer.BlockCopy(memory.Data, 0, data, 0, checked((int)memory.Size));

                lock (Memories)
                {
                    Memories.Remove(memory.Address);
                    memory.Data = data;
                    memory.Size = size;
                    Memories[memory.Address] = memory;
                }
            }
            else
            {
                lock (Memories)
                {
                    memory.Size = size;
                }
            }

            return IntPtr.Zero;
        }
        catch (Exception exception)
        {
            return wasmtime_error_new($"The memory could not grow: {exception.Message}");
        }
    }

    private static void Finalize(IntPtr env)
    {
        var handle = GCHandle.FromIntPtr(env);

        if (handle.Target is LinearMemory memory)
        {
            lock (Memories)
            {
                Memories.Remove(memory.Address);
            }
        }

        handle.Free();
    }

    private static void Register(LinearMemory memory)
    {
        lock (Memories)
        {
            Memories[memory.Address] = memory;
        }
    }

    // pinned, so the address stays; at least a byte, as an empty array has no address
    private static byte[] Allocate(long length) =>
        GC.AllocateArray<byte>(checked((int)Math.Max(1, length)), pinned: true);

    private sealed class LinearMemory
    {
        public LinearMemory(byte[] data, long size)
        {
            Data = data;
            Size = size;
        }

        public byte[] Data { get; set; }

        public long Size { get; set; }

        public IntPtr Address => Marshal.UnsafeAddrOfPinnedArrayElement(Data, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryCreatorStruct
    {
        public IntPtr Env;
        public IntPtr NewMemory;
        public IntPtr Finalizer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinearMemoryStruct
    {
        public IntPtr Env;
        public IntPtr GetMemory;
        public IntPtr GrowMemory;
        public IntPtr Finalizer;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NewMemoryCallback(IntPtr env, IntPtr type, UIntPtr minimum, UIntPtr maximum, UIntPtr reserved, UIntPtr guard, IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetMemoryCallback(IntPtr env, out UIntPtr size, out UIntPtr capacity);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GrowMemoryCallback(IntPtr env, UIntPtr newSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FinalizerCallback(IntPtr env);

    [DllImport(Library)]
    private static extern void wasmtime_config_host_memory_creator_set(IntPtr config, ref MemoryCreatorStruct creator);

    [DllImport(Library)]
    private static extern void wasmtime_config_memory_reservation_set(IntPtr config, ulong size);

    [DllImport(Library)]
    private static extern IntPtr wasmtime_error_new([MarshalAs(UnmanagedType.LPUTF8Str)] string message);
}
