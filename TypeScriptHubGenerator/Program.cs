using System.CommandLine;
using TypescriptHubGenerator;

var assemblyPathOption = new Option<string>(
        name: "--assembly-path",
        description: "Path to the assembly to generate the typescript for")
    { IsRequired = true, };
assemblyPathOption.AddAlias("-f");

var outputFolderOption = new Option<string>(
        name: "--output-folder",
        description: "Path to the output folder")
    { IsRequired = true, };
outputFolderOption.AddAlias("-o");

var createReactContextOption = new Option<bool>(
        name: "--create-react-context",
        description: "Generate react context and hook")
    { IsRequired = false, };

var rootCommand = new RootCommand("Generate TypeScript hub client");
rootCommand.AddOption(assemblyPathOption);
rootCommand.AddOption(outputFolderOption);
rootCommand.AddOption(createReactContextOption);
rootCommand.SetHandler(
    async (assemblyPath, outputFolder, createReactContext) =>
    {
        var assemblyPathAbsolute = Path.IsPathRooted(assemblyPath)
            ? assemblyPath
            : Path.Combine(Environment.CurrentDirectory, assemblyPath);

        var outputFolderAbsolut = Path.IsPathRooted(outputFolder)
            ? outputFolder
            : Path.Combine(Environment.CurrentDirectory, outputFolder);

        await HubGenerator.CreateHubFilesAsync(assemblyPathAbsolute, outputFolder, createReactContext);
    },
    assemblyPathOption, outputFolderOption, createReactContextOption);

return rootCommand.InvokeAsync(args).Result;