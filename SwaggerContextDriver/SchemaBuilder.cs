using LINQPad.Extensibility.DataContext;
using Microsoft.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace SwaggerContextDriver
{
    class SchemaBuilder
    {
        internal static SwaggerDocument DownloadDefinition(string uri, ConnectionProperties props, ICredentials credentials = null)
        {
            var client = new WebClient();

            if (credentials != null) client.Credentials = credentials;

            using (ExecutionContext.SuppressFlow())
            {
                var swaggerDefinition = client.DownloadString(uri);
                return SwaggerDocument.FromJsonAsync(swaggerDefinition).Result;
            }
        }

        internal static List<ExplorerItem> GetSchemaAndBuildAssembly(string driverLocation, ConnectionProperties props, AssemblyName name, ref string nameSpace, ref string typeName)
        {
            List<ExplorerItem> schema = new List<ExplorerItem>();

            var uri = new Uri(props.Uri);
            SwaggerDocument document = null;
            if (uri.Scheme == "file")
            {
                document = SwaggerDocument.FromFileAsync(uri.LocalPath).Result;
            }
            else if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                if (props.AuthOption == AuthenticationType.None)
                    document = DownloadDefinition(props.Uri, props);
                else if (props.AuthOption == AuthenticationType.Basic)
                    document = DownloadDefinition(props.Uri, props, new NetworkCredential(props.UserName, props.Password, props.Domain));
                else if (props.AuthOption == AuthenticationType.CurrentUser)
                    document = DownloadDefinition(props.Uri, props, CredentialCache.DefaultNetworkCredentials);
                else
                    throw new NotSupportedException("Authentication method not supported.");

                if (document.BaseUrl.StartsWith("/") && document.BasePath.StartsWith("/"))
                {
                    var t = document.BaseUrl;                    
                    document.BasePath = uri.Scheme + "://" + uri.Host + ":" + uri.Port + document.BaseUrl;
                    System.Diagnostics.Debug.WriteLine("Changing BaseUrl from '{0}' to '{1}'", t, document.BasePath);
                }

                if (string.IsNullOrEmpty(document.Host))
                {
                    document.Host = uri.Host;
                    System.Diagnostics.Debug.WriteLine("Host was null, setting it to '{0}'", document.Host);
                }
            }

            switch (props.GenOption)
            {
                case GeneratorType.SingleClientFromOperatinoId:

                    // Compile the code into the assembly, using the assembly name provided:
                    BuildAssemblySingleClientFromOpId(document, driverLocation, name, nameSpace, ref typeName, props);

                    // Use the schema to populate the Schema Explorer:
                    schema = GetSchemaSingleClient(document, typeName);
                    break;
                case GeneratorType.SingleClientFromPathSegment:

                    // Compile the code into the assembly, using the assembly name provided:
                    BuildAssemblySingleClientFromPathSegOp(document, driverLocation, name, nameSpace, ref typeName, props);

                    // Use the schema to populate the Schema Explorer:
                    //schema = GetSchemaSingleClientPath(document, typeName);
                    schema = GetSchemaViaReflection(name, nameSpace, typeName);
                    break;
                case GeneratorType.MultipleClientsFromOperationId:

                    // Compile the code into the assembly, using the assembly name provided:
                    BuildAssemblyMultiClientFromOpId(document, driverLocation, name, nameSpace, ref typeName, props);

                    // Use the schema to populate the Schema Explorer:
                    schema = GetSchemaMultiClient(document, typeName);
                    break;
            }

            return schema;
        }

        private static List<ExplorerItem> GetSchemaSingleClient(SwaggerDocument document, string typeName)
        {
            var items = new List<ExplorerItem>();
            foreach (var op in document.Operations)
            {
                var ei = new ExplorerItem(FirstCharToUpper(op.Operation.OperationId) + "Async", ExplorerItemKind.Schema, ExplorerIcon.StoredProc);
                ei.ToolTipText = op.Operation.Summary;
                ei.DragText = FirstCharToUpper(op.Operation.OperationId) + "Async" + "()";

                if (op.Operation.Parameters != null && op.Operation.Parameters.Count > 0)
                {
                    var para = new List<ExplorerItem>();
                    foreach (var p in op.Operation.Parameters)
                    {
                        var name = p.Name;
                        if (p.IsAnyType == false)
                        {
                            if (p.IsRequired)
                            {
                                name += string.Format("({0})", p.Type);
                            }
                            else
                            {
                                name += string.Format("({0}?)", p.Type);
                            }
                        }

                        var t = new ExplorerItem(name, ExplorerItemKind.Parameter, ExplorerIcon.Parameter);
                        t.ToolTipText = p.Description;
                        para.Add(t);
                    }

                    if (para.Count > 0) ei.Children = para;
                }

                items.Add(ei);
            }

            return items;
        }

        private static List<ExplorerItem> GetSchemaSingleClientPath(SwaggerDocument document, string typeName)
        {
            var items = new List<ExplorerItem>();
            foreach (var op in document.Operations)
            {
                var ei = new ExplorerItem(FirstCharToUpper(op.Operation.OperationId.Replace(".get", "").Replace(".", "")) + "Async", ExplorerItemKind.Schema, ExplorerIcon.StoredProc);
                ei.ToolTipText = op.Operation.Summary;
                ei.DragText = FirstCharToUpper(op.Operation.OperationId) + "Async" + "()";

                if (op.Operation.Parameters != null && op.Operation.Parameters.Count > 0)
                {
                    var para = new List<ExplorerItem>();
                    foreach (var p in op.Operation.Parameters)
                    {
                        var name = p.Name;
                        if (p.IsAnyType == false)
                        {
                            if (p.IsRequired)
                            {
                                name += string.Format("({0})", p.Type);
                            }
                            else
                            {
                                name += string.Format("({0}?)", p.Type);
                            }
                        }

                        var t = new ExplorerItem(name, ExplorerItemKind.Parameter, ExplorerIcon.Parameter);
                        t.ToolTipText = p.Description;
                        para.Add(t);
                    }

                    if (para.Count > 0) ei.Children = para;
                }

                items.Add(ei);
            }

            return items;
        }

        private static List<ExplorerItem> GetSchemaMultiClient(SwaggerDocument document, string typeName)
        {
            var items = new List<ExplorerItem>();
            foreach (var op in document.Operations)
            {
                var tok = op.Operation.OperationId.Split(new[] { '_' }, 2);
                var ei = new ExplorerItem(FirstCharToUpper(tok.Length == 2 ? tok[1] : tok[0]) + "Async", ExplorerItemKind.Schema, ExplorerIcon.StoredProc);
                ei.ToolTipText = op.Operation.Summary;
                if (tok.Length == 2)
                {
                    ei.DragText = ei.Text + "()";
                }
                else
                {
                    ei.DragText = ei.Text + "()";
                }

                if (op.Operation.Parameters != null && op.Operation.Parameters.Count > 0)
                {
                    var para = new List<ExplorerItem>();
                    foreach (var p in op.Operation.Parameters)
                    {
                        var name = p.Name;
                        if (p.IsAnyType == false)
                        {
                            if (p.IsRequired)
                            {
                                name += string.Format("({0})", p.Type);
                            }
                            else
                            {
                                name += string.Format("({0}?)", p.Type);
                            }
                        }

                        var t = new ExplorerItem(name, ExplorerItemKind.Parameter, ExplorerIcon.Parameter);
                        t.ToolTipText = p.Description;
                        para.Add(t);
                    }

                    if (para.Count > 0) ei.Children = para;
                }

                items.Add(ei);
            }

            return items;
        }

        private static List<ExplorerItem> GetSchemaViaReflection(AssemblyName assembly, string nameSpace, string typeName)
        {
            var items = new List<ExplorerItem>();

            Assembly testAssembly = Assembly.LoadFile(assembly.CodeBase);

            // get type of class Calculator from just loaded assembly
            Type calcType = testAssembly.GetType(nameSpace + "." + typeName);

            var methods = calcType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var op in methods)
            {
                if (op.IsSpecialName) continue;

                var ei = new ExplorerItem(op.Name, ExplorerItemKind.Schema, ExplorerIcon.StoredProc);
                ei.DragText = op.Name + "()";

                var param = op.GetParameters();
                if (param != null && param.Length > 0)
                {
                    var para = new List<ExplorerItem>();
                    foreach (var p in param)
                    {
                        var name = p.Name;
                        if (p.IsOptional)
                        {
                            name += string.Format("({0}?)", p.ParameterType.Name);
                        }
                        else
                        {
                            name += string.Format("({0})", p.ParameterType.Name);
                        }

                        var t = new ExplorerItem(name, ExplorerItemKind.Parameter, ExplorerIcon.Parameter);
                        para.Add(t);
                    }

                    if (para.Count > 0) ei.Children = para;
                }

                items.Add(ei);
            }

            var pr = calcType.GetProperties();
            foreach (var op in pr)
            {
                if (op.IsSpecialName) continue;

                var name = op.Name;
                if (op.CanRead && op.CanWrite) name += "{ get; set; }";
                else if (op.CanRead) name += "{ get; }";
                else if (op.CanWrite) name += "{ set; }";

                var ei = new ExplorerItem(op.Name, ExplorerItemKind.Schema, ExplorerIcon.Table);
                ei.DragText = op.Name;
                ei.ToolTipText = name;

                items.Add(ei);
            }

            return items;
        }

        private static void BuildAssemblyMultiClientFromOpId(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName, ConnectionProperties props)
        {
            typeName = "Client";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",
                OperationNameGenerator = new MultipleClientsFromOperationIdOperationNameGenerator(), // this is default
                CSharpGeneratorSettings =
                {
                    Namespace = nameSpace
                },
            };

            if (props.InjectHttpClient)
            {
                settings.ClientBaseClass = "SwaggerContextDriverExtension.MyClient";
                settings.UseHttpClientCreationMethod = true;
                settings.DisposeHttpClient = props.DisposeHttpClient;
            }

            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var code = "";

            code = AddHttpClientInjectCode(code, props, "SwaggerContextDriverExtension");

            code += "\n\n";
            code += generator.GenerateFile();
            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
                "LINQPad.exe",
                "System.ComponentModel.DataAnnotations.dll",
            };

            assemblyNames.Add(Path.Combine(location, "Newtonsoft.Json.dll"));

            using (var codeProvider = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }))
            {
                var options = new CompilerParameters(
                    assemblyNames.ToArray(),
                    name.CodeBase,
                    true);
                results = codeProvider.CompileAssemblyFromSource(options, code);
            }

            if (results.Errors.Count > 0)
            {
                throw new Exception
                    ("Cannot compile typed context: " + results.Errors[0].ErrorText + " (line " + results.Errors[0].Line + ")");
            }
        }

        private static void BuildAssemblySingleClientFromOpId(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName, ConnectionProperties props)
        {
            typeName = "Client";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                GenerateClientClasses = true,
                GenerateOptionalParameters = true,

                ClassName = "{controller}Client",
                OperationNameGenerator = new SingleClientFromOperationIdOperationNameGenerator(),
                CSharpGeneratorSettings =
                {
                    Namespace = nameSpace
                },
            };

            if (props.InjectHttpClient)
            {
                settings.DisposeHttpClient = false;
                settings.ClientBaseClass = nameSpace + ".MyClient";
                settings.UseHttpClientCreationMethod = true;
                settings.DisposeHttpClient = props.DisposeHttpClient;
            }

            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var code = generator.GenerateFile();

            code = AddHttpClientInjectCode(code, props, nameSpace);

            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
                "LINQPad.exe",
                "System.ComponentModel.DataAnnotations.dll",
            };

            assemblyNames.Add(Path.Combine(location, "Newtonsoft.Json.dll"));

            using (var codeProvider = new CSharpCodeProvider())
            {
                var options = new CompilerParameters(
                    assemblyNames.ToArray(),
                    name.CodeBase,
                    true);
                results = codeProvider.CompileAssemblyFromSource(options, code);
            }

            if (results.Errors.Count > 0)
                throw new Exception
                    ("Cannot compile typed context: " + results.Errors[0].ErrorText + " (line " + results.Errors[0].Line + ")");
        }

        private static void BuildAssemblySingleClientFromPathSegOp(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName, ConnectionProperties props)
        {
            typeName = "Client";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",

                OperationNameGenerator = new SingleClientFromPathSegmentsOperationNameGenerator(),
                GenerateOptionalParameters = true,
                CSharpGeneratorSettings =
                {
                    Namespace = nameSpace
                },
            };

            if (props.InjectHttpClient)
            {
                settings.DisposeHttpClient = false;
                settings.ClientBaseClass = nameSpace + ".MyClient";
                settings.UseHttpClientCreationMethod = true;
                settings.DisposeHttpClient = props.DisposeHttpClient;
            }

            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var code = generator.GenerateFile();

            code = AddHttpClientInjectCode(code, props, nameSpace);

            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
                "LINQPad.exe",
                "System.ComponentModel.DataAnnotations.dll",
            };

            assemblyNames.Add(Path.Combine(location, "Newtonsoft.Json.dll"));

            using (var codeProvider = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } }))
            {
                var options = new CompilerParameters(
                    assemblyNames.ToArray(),
                    name.CodeBase,
                    true);
                results = codeProvider.CompileAssemblyFromSource(options, code);
            }

            if (results.Errors.Count > 0)
                throw new Exception
                    ("Cannot compile typed context: " + results.Errors[0].ErrorText + " (line " + results.Errors[0].Line + ")");
        }

        private static string AddHttpClientInjectCode(string code, ConnectionProperties props, string nameSpace)
        {
            if (props.InjectHttpClient)
            {
                code += @"
namespace " + nameSpace + @"
{
    public class MyClient
    {
        public LINQPad.Extensibility.DataContext.IConnectionInfo driverConnectionInfo { get; set; }

        public System.Net.Http.HttpClient HttpClient { get; set; }

        public async System.Threading.Tasks.Task<System.Net.Http.HttpClient> CreateHttpClientAsync(System.Threading.CancellationToken cancellationToken)
        {
            if (HttpClient == null)
            {
                var httpClientHandler = new System.Net.Http.HttpClientHandler();
                var driverData = driverConnectionInfo.DriverData;
                var authType = (string)driverData.Element(""AuthenticationType"");
                var userName = (string)driverData.Element(""UserName"");
                var password = (string)driverData.Element(""Password"");
                var domain = (string)driverData.Element(""Domain"");

                if (authType == ""None"")
                    httpClientHandler.Credentials = null;
                else if (authType == ""Basic"")
                    httpClientHandler.Credentials = new System.Net.NetworkCredential(userName, password, domain);
                else if (authType == ""CurrentUser"")
                    httpClientHandler.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                else
                    throw new System.NotSupportedException(""Authentication method specified in the Connection Dialog is not supported."");

                HttpClient = new System.Net.Http.HttpClient(httpClientHandler);
            }

            return HttpClient;
        }
    }
}
";
            }

            return code;
        }

        private static string FirstCharToUpper(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            else if (text.Length == 1)
            {
                return text.ToUpperInvariant();
            }

            return Char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
