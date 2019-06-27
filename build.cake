var target = Argument("target", "Default");
var projectName = "AngleSharp.Wasm";
var solutionName = "AngleSharp.Wasm";
var frameworks = new Dictionary<String, String>
{
    { "netstandard2.0", "netstandard2.0" },
};

#load tools/anglesharp.cake

RunTarget(target);
