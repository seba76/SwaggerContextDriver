using LINQPad.Extensibility.DataContext;
using Microsoft.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SwaggerContextDriver
{
    class SchemaBuilder
    {
        internal static List<ExplorerItem> GetSchemaAndBuildAssembly(string driverLocation, ConnectionProperties props, AssemblyName name, ref string nameSpace, ref string typeName)
        {
            var document = SwaggerDocument.FromUrlAsync(props.Uri).Result;

            // Compile the code into the assembly, using the assembly name provided:
            BuildAssemblySingleClientFromOpId(document, driverLocation, name, nameSpace, ref typeName);

            //// Use the schema to populate the Schema Explorer:
            List<ExplorerItem> schema = GetSchemaSingleClient(document, typeName);

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

        private static List<ExplorerItem> GetSchemaMultiClient(SwaggerDocument document, string typeName)
        {
            var items = new List<ExplorerItem>();
            var root = new ExplorerItem("Api", ExplorerItemKind.CollectionLink, ExplorerIcon.Box);
            root.Children = new List<ExplorerItem>();
            items.Add(root);
            foreach (var op in document.Operations)
            {
                var tok = op.Operation.OperationId.Split(new[] { '_' }, 2);
                var ei = new ExplorerItem(FirstCharToUpper(tok.Length == 2 ? tok[1] : tok[0]) + "Async", ExplorerItemKind.Schema, ExplorerIcon.StoredProc);
                ei.ToolTipText = op.Operation.Summary;
                if (tok.Length == 2)
                {
                    ei.DragText = "(new Api." + tok[0] + "Client())." + ei.Text + "()";
                }
                else
                {
                    ei.DragText = "(new Api.Client())." + ei.Text + "()";
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

                root.Children.Add(ei);
            }

            return items;
        }

        private static void BuildAssemblyMultiClientFromOpId(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName)
        {
            typeName = "Api";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",
                OperationNameGenerator = new MultipleClientsFromOperationIdOperationNameGenerator(), // this is default
                CSharpGeneratorSettings =
                {
                    Namespace = nameSpace
                },
            };

            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var code = generator.GenerateFile();
            code = code.Replace(nameSpace + "\n{", nameSpace + "\n{" + " public class " + typeName + "{ ");
            code += " }";
            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
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


        private static void BuildAssemblySingleClientFromOpId(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName)
        {
            typeName = "Client";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",                
                OperationNameGenerator = new SingleClientFromOperationIdOperationNameGenerator(),
                CSharpGeneratorSettings =
                {                    
                    Namespace = nameSpace
                },
            };

            var generator = new SwaggerToCSharpClientGenerator(document, settings);
            
            var code = generator.GenerateFile();
            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
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

        private static void BuildAssemblySingleClientFromPathSegOp(SwaggerDocument document, string location, AssemblyName name, string nameSpace, ref string typeName)
        {
            typeName = "Client";
            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",
                OperationNameGenerator = new SingleClientFromPathSegmentsOperationNameGenerator(),
                CSharpGeneratorSettings =
                {
                    Namespace = nameSpace
                },
            };

            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var code = generator.GenerateFile();
            CompilerResults results;
            var assemblyNames = new List<string>()
            {
                "System.dll",
                "System.Core.dll",
                "System.Xml.dll",
                "System.Runtime.Serialization.dll",
                "System.Net.Http.dll",
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
