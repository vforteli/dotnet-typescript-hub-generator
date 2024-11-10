using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using TypescriptModelGenerator;

namespace TypescriptHubGenerator;

public record HubFiles(Dictionary<string, string> TypeFiles, string HubFile);

public static class HubGenerator
{
    private const string Import = """import type { {{typeName}} } from "./types/{{typeName}}";""";

    /// <summary>
    /// Create typescript client from hub
    /// </summary>
    /// <param name="hubType"></param>
    /// <returns></returns>
    public static HubFiles CreateFromHub(Type hubType)
    {
        if (!typeof(Hub).IsAssignableFrom(hubType))
        {
            throw new ArgumentException($"Type '{hubType.Name}' is not a subclass of Hub.");
        }

        if (hubType.BaseType == null)
        {
            throw new ArgumentNullException("Hub is not generic... nothing to do here...");
        }

        var types = new Dictionary<string, string>();

        var callbackMethods = hubType.BaseType.GenericTypeArguments
            .First()
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
            .Select((m) => CreateCallback(m, types))
            .ToImmutableList();

        var hubMethodStrings = hubType
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
            .Select((m) => CreateMethod(m, types))
            .ToImmutableList();


        var imports = callbackMethods
            .SelectMany(o => o.imports)
            .Concat(hubMethodStrings.SelectMany(o => o.imports))
            .DistinctBy(o => o.Name)
            .Select(o => Import.Replace("{{typeName}}", o.Name))
            .ToImmutableList();

        return new HubFiles(types,
            CreateHubClient(
                $"{hubType.Name}Client",
                string.Join("\n\n", hubMethodStrings.Select(o => o.method)),
                string.Join("\n\n", callbackMethods.Select(o => o.callbacks)),
                string.Join("\n", imports)));
    }


    /// <summary>
    /// Create the callback functions for a Hub, ie the functions to be called on the client side
    /// </summary>
    private static (ImmutableList<ComplexType> imports, string callbacks) CreateCallback(MethodInfo method,
        Dictionary<string, string> processedTypes)
    {
        const string callbackTemplate =
            """
              add{{methodName}}Handler(callback: ({{invokeParameters}}) => void): void {
                this.connection.on("{{callbackName}}", callback);
              }
            
              remove{{methodName}}Handler(callback: ({{invokeParameters}}) => void): void {
                this.connection.off("{{callbackName}}", callback);
              }
            """;

        var parameters = method.GetParameters()
            .Select(o =>
                new KeyValuePair<string, TsType>(
                    o.Name?.ToCamelCase() ??
                    throw new ArgumentNullException("Cannot have parameters without name here..."),
                    TypeScriptModelGenerator.ParseParameterInfo(o, processedTypes)))
            .ToImmutableList();

        var parametersString = string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}"));

        var imports = parameters.Select(o => o.Value).OfType<ComplexType>().ToImmutableList();

        return (imports, callbackTemplate
            .Replace("{{methodName}}", method.Name)
            .Replace("{{callbackName}}", method.Name.ToCamelCase())
            .Replace("{{invokeParameters}}", parametersString));
    }


    /// <summary>
    /// Create the invokable hub methods
    /// </summary>
    private static (ImmutableList<ComplexType> imports, string method) CreateMethod(MethodInfo method,
        Dictionary<string, string> processedTypes)
    {
        const string methodTemplate =
            """
              async {{methodName}}({{methodParameters}}) {
                await this.connection.invoke{{returnTypeParameter}}({{invokeParameters}});
              }
            """;

        var methodName = method.Name.ToCamelCase();

        var parameters = method.GetParameters()
            .Select(o =>
                new KeyValuePair<string, TsType>(
                    o.Name?.ToCamelCase() ??
                    throw new ArgumentNullException("Cannot have parameters without name here..."),
                    TypeScriptModelGenerator.ParseParameterInfo(o, processedTypes)))
            .ToImmutableList();

        var parametersString = string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}"));

        var imports = parameters.Select(o => o.Value).OfType<ComplexType>().ToList();

        var invokeParameters = new List<string> { $"\"{methodName}\"" }.Concat(parameters.Select(o => o.Key));

        var invokeParametersString = string.Join(", ", invokeParameters);

        // so, should we treat all references types as nullable since this cannot be determined...
        var returnType = method.ReturnType.BaseType == typeof(Task)
            ? TypeScriptModelGenerator.ParseTypeRecursively(method.ReturnType.GenericTypeArguments.First(),
                processedTypes, false)
            : null;

        if (returnType is ComplexType complexType)
        {
            imports.Add(complexType);
        }

        var methodTypeScript = methodTemplate
            .Replace("{{methodParameters}}", parametersString)
            .Replace("{{methodName}}", methodName)
            .Replace("{{invokeParameters}}", invokeParametersString)
            .Replace("{{returnTypeParameter}}", returnType != null ? $"<{returnType.Name}>" : "");

        return (imports.ToImmutableList(), methodTypeScript);
    }


    /// <summary>
    /// Create the actual hub client
    /// </summary>
    private static string CreateHubClient(string hubName, string methods, string callbacks, string imports)
    {
        const string hubClientTemplate =
            """
            import type { HubConnection } from "@microsoft/signalr";

            {{imports}}

            export class {{hubClientName}} {
              readonly connection: HubConnection;
            
              constructor(hubConnection: HubConnection) {
                this.connection = hubConnection;
              }

            {{methods}}

            {{callbacks}}
            }

            """;

        return hubClientTemplate
            .Replace("{{hubClientName}}", hubName)
            .Replace("{{imports}}", imports)
            .Replace("{{methods}}", methods)
            .Replace("{{callbacks}}", callbacks);
    }


    public static string CreateReactContext(string hubName)
    {
        var hubVariableName = hubName.ToCamelCase();
        const string contextTemplate =
            """
            import { HubConnection, HubConnectionState } from "@microsoft/signalr";
            import { createContext, ReactNode, useEffect, useRef } from "react";
            import { {{hubName}} } from "./{{hubName}}";

            export type {{hubName}}ContextProviderProps = {
              children: ReactNode;
              hubConnection: HubConnection | (() => HubConnection);
            };

            export const {{hubName}}Context = createContext<{ hub: {{hubName}} } | undefined>(undefined);

            export const {{hubName}}ContextProvider = ({ children, hubConnection }: {{hubName}}ContextProviderProps) => {
              const connection = typeof hubConnection === "function" ? hubConnection() : hubConnection;
            
              const {{hubVariableName}} = useRef(new {{hubName}}(connection));
            
              useEffect(() => {
                if ({{hubVariableName}}.current.connection.state === HubConnectionState.Disconnected) {
                  {{hubVariableName}}.current.connection.start().catch((err) => console.error(err));
                }
              }, [{{hubVariableName}}.current.connection.state]);
            
              return <{{hubName}}Context.Provider value={{ hub: {{hubVariableName}}.current }}>{children}</{{hubName}}Context.Provider>;
            };

            """;

        return contextTemplate
            .Replace("{{hubName}}", hubName)
            .Replace("{{hubVariableName}}", hubVariableName);
    }


    public static string CreateReactContextHook(string hubName)
    {
        var hubVariableName = hubName.ToCamelCase();
        const string contextHookTemplate =
            """
            import { useContext } from "react";
            import { {{hubName}}Context } from "./{{hubName}}Context";

            export const use{{hubName}} = () => {
              const context = useContext({{hubName}}Context);
            
              if (context === undefined) {
                throw Error("Context undefined? Forgot a provider somewhere?");
              }
            
              return context;
            };

            """;

        return contextHookTemplate.Replace("{{hubName}}", hubName);
    }
}