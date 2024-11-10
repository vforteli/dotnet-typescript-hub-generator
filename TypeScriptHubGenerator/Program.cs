using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using TypescriptHubGenerator;

var assemblyPath = "";
var outputFolder = "";

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine("Assembly not found?! Check path and ensure it has been built");
    Environment.Exit(1);
}

var hubTypes = Assembly.LoadFile(assemblyPath)
    .GetTypes()
    .Where(t => t.BaseType?.BaseType ==
                typeof(Hub)); // this will find generic hubs with an interface defined for operations


Directory.CreateDirectory(outputFolder);

foreach (var hubType in hubTypes)
{
    var hubFiles = HubGenerator.CreateFromHub(hubType);
    var hubClientName = $"{hubType.Name}Client";

    await File.WriteAllTextAsync(Path.Combine(outputFolder, $"{hubClientName}.ts"), hubFiles.HubFile);

    Directory.CreateDirectory(Path.Combine(outputFolder, "types"));

    foreach (var file in hubFiles.TypeFiles)
    {
        await File.WriteAllTextAsync(Path.Combine(outputFolder, "types", $"{file.Key}.ts"), file.Value);
    }

    var contextFile = HubGenerator.CreateReactContext(hubClientName);
    await File.WriteAllTextAsync(Path.Combine(outputFolder, $"{hubClientName}Context.tsx"), contextFile);

    var contextHookFile = HubGenerator.CreateReactContextHook(hubClientName);
    await File.WriteAllTextAsync(Path.Combine(outputFolder, $"{hubClientName}ContextHook.tsx"), contextHookFile);
}


Console.WriteLine("Done...");