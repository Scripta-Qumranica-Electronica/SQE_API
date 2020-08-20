using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CaseExtensions;

namespace GenerateTestRequestObjects
{
    public static class Writers
    {
        public static async Task WriteEndpointsAsync(ApiRequestsDescription parsedControllerMethods,
            StreamWriter outputFile)
        {
            foreach (var httpVerb in parsedControllerMethods.requests.OrderBy(x => x.Key.ToString()).Select(x => x.Key))
            {
                await outputFile.WriteLineAsync(@$"
            public static partial class {httpVerb.ToString().ToLower().ToPascalCase()}
            {{
        ");
                if (!parsedControllerMethods.requests.TryGetValue(httpVerb, out var endpoints)) continue;
                foreach (var endpoint in endpoints)
                {
                    var listener = string.IsNullOrEmpty(endpoint.ListenerType)
                        ? ""
                        : $"ListenerMethod = \"{endpoint.listenerName}\";";
                    var endpointInputType = endpoint.IType == null ? "EmptyInput" : endpoint.IType;
                    var endpointOutputType = endpoint.OType == null ? "EmptyOutput" : endpoint.OType;
                    var endpointListenerType = httpVerb.ToString().ToLowerInvariant() switch
                    {
                        "get" => "EmptyOutput",
                        "post" => string.IsNullOrEmpty(endpoint.ListenerType) ? "EmptyOutput" : endpoint.ListenerType,
                        "put" => string.IsNullOrEmpty(endpoint.ListenerType) ? "EmptyOutput" : endpoint.ListenerType,
                        "delete" => string.IsNullOrEmpty(endpoint.ListenerType) ? "EmptyOutput" : endpoint.ListenerType,
                        _ => throw new Exception("Unsupported http verb")
                    };
                    var methodName = endpoint.HttpString.Replace("\"", "")
                        .ToPascalCase()
                        .Replace("/", "_")
                        .Replace("{", "")
                        .Replace("}", "");
                    if (endpoint.bodyParams != null)
                        endpoint.bodyParams.ParamName = "payload";
                    var constructorParams = new List<ParameterDescription>()
                        .Concat(endpoint.routeParams)
                        .Concat(new List<ParameterDescription> { endpoint.bodyParams })
                        .Concat(endpoint.queryParams)
                        .Where(x => x != null);
                    var optionals = "";
                    foreach (var qp in endpoint.queryParams)
                    {
                        var firstQp = endpoint.queryParams.First() == qp;

                        optionals += $@"
                            + (_{qp.ParamName.ToCamelCase()} != null ? $""{(firstQp ? "?" : "&")}{
                            (qp.ParamType.Contains("List<")
                             || qp.ParamType.Contains("Enumerable<")
                             || qp.ParamType.Contains("[")
                                ? $"{qp.ParamName}={{string.Join(\",\", _{qp.ParamName.ToCamelCase()})}}\""
                                : $"{qp.ParamName}={{_{qp.ParamName.ToCamelCase()}}}\"")} : """")";
                    }

                    await outputFile.WriteLineAsync($@"
                public class {methodName}
                : RequestObject<{endpointInputType}, {endpointOutputType}, {endpointListenerType}>
                {{
                    {string.Join("\n", constructorParams.Select(x => $"private readonly {x.ParamType} _{x.ParamName.ToCamelCase()};"))}

                    {endpoint.comments}
                    public {methodName}({string.Join(", ", constructorParams.Select(x => $"{x.ParamType} {x.ParamName}{(endpoint.queryParams.Contains(x) ? " = null" : "")}"))}) 
                        {(constructorParams.Any(x => x.ParamName == "payload") ? ": base(payload)" : "")} 
                    {{  
                        {string.Join("\n", constructorParams.Select(x => $"_{x.ParamName.ToCamelCase()} = {x.ParamName};"))}
                        {listener}
                    }}

                    protected override string HttpPath()
                    {{
                        return RequestPath{string.Join("", endpoint.routeParams.Select(x =>
                        $".Replace(\"/{x.ParamName.ToKebabCase()}\", $\"/{{_{x.ParamName.ToCamelCase()}.ToString()}}\")"))}{optionals};
                    }}

                    public override Func<HubConnection, Task<T>> SignalrRequest<T>()
                    {{
                        return signalR => signalR.InvokeAsync<T>(SignalrRequestString(){(constructorParams.Count() > 0 ? ", " : "")}{string.Join(", ", constructorParams.Select(x => $"_{x.ParamName.ToCamelCase()}"))});
                    }}
                    
                    {(endpoint.routeParams.Any(x => x.ParamName.ToLowerInvariant() == "editionid") ? @"public override uint? GetEditionId()
                    {{
                        return _editionId;
                    }}" : "")}
                }}");
                }

                await outputFile.WriteLineAsync("\t}");
            }
        }
    }
}