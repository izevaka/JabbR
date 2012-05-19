using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Gate;
using Owin;
using SquishIt.Framework;

namespace JabbR
{
    /// <summary>
    /// Very tactical, and almost certainly temporary, mechanism to execute the default.aspx template.
    /// A middleware that can execute Razor pages will almost certainly replace this some day.
    /// </summary>
    public static class PseudoWebForms
    {
        public static IAppBuilder UseHomePage(this IAppBuilder builder)
        {
            var viewType = Compile(Path.Combine(Directory.GetCurrentDirectory(), "default.aspx"));

            return builder.Use<AppDelegate>(
                app => (env, result, fault) =>
                           {
                               var req = new Gate.Request(env);
                               if (req.Path == "/" || req.Path.Equals("/Default.aspx", StringComparison.OrdinalIgnoreCase))
                               {
                                   var view = (PseudoWebForms.View)Activator.CreateInstance(viewType);
                                   view.Request = req;
                                   view.Response = new Gate.Response(result) { ContentType = "text/html" };
                                   view.RenderView();
                                   view.Response.End();
                                   return;
                               }
                               app(env, result, fault);
                           });
        }

        /// <summary>
        /// Base class for default.aspx that exposes enough of what it expects to find.
        /// </summary>
        public abstract class View
        {
            public Request Request { get; set; }
            public Response Response { get; set; }

            public abstract void RenderView();

            public void Write(string value)
            {
                Response.Write(value);
            }
            public void WriteLiteral(string value)
            {
                Write(value);
            }
            public void WriteEncoded(string value)
            {
                Write(HttpUtility.HtmlEncode(value));
            }


            public string ResolveClientUrl(string path)
            {
                if (path.StartsWith("~/", StringComparison.Ordinal))
                {
                    return Request.PathBase + path.Substring(1);
                }
                return path;
            }
        }

        /// <summary>
        /// Parses the template, emits code, compiles as an assembly, and returns the resulting loaded type.
        /// </summary>
        public static Type Compile(string filePath)
        {
            // Reads all text from the file path
            var defaultAspx = File.ReadAllText(filePath);

            using (var writer = new StringWriter())
            {
                // run through segments looking or additional namespace, if any
                foreach (var segment in Segments(defaultAspx))
                {
                    switch (segment.Item1)
                    {
                        case "<%@":
                            {
                                var ns = segment.Item2.Trim();
                                if (ns.StartsWith("Import namespace=\"", StringComparison.InvariantCultureIgnoreCase) &&
                                    ns.EndsWith("\""))
                                {
                                    ns = ns.Substring("Import namespace=\"".Length,
                                                      ns.Length - "Import namespace=\"".Length - 1);
                                    writer.Write("using ");
                                    writer.Write(ns);
                                    writer.Write(";");
                                }
                                break;
                            }
                    }
                }

                // simple top of file
                writer.Write(@"
using System;
public class View : JabbR.PseudoWebForms.View
{
    public override void RenderView()
    {
");
                // run through each segment and emit code
                foreach (var segment in Segments(defaultAspx))
                {
                    switch (segment.Item1)
                    {
                        case "<%": // raw code snippet
                            writer.WriteLine(segment.Item2);
                            break;
                        case "<%=": // unencoded html output
                            writer.Write("Write(");
                            writer.Write(segment.Item2);
                            writer.WriteLine(");");
                            break;
                        case "<%:": // encoded html output
                            writer.Write("WriteEncoded(");
                            writer.WriteLine(segment.Item2);
                            writer.WriteLine(");");
                            break;
                        case "literal": // plain text/html to output
                            writer.Write("WriteLiteral(\"");
                            writer.Write(segment.Item2
                                             .Replace("\\", "\\\\")
                                             .Replace("\"", "\\\"")
                                             .Replace("\r", "\\r")
                                             .Replace("\n", "\\n")
                                             .Replace("\t", "\\t")
                                );
                            writer.WriteLine("\");");
                            break;
                    }
                }
                writer.Write(@"
    }
}
");

                var source = writer.ToString();

                var codeDomProvider = CodeDomProvider.CreateProvider("cs");

                // This ensures some crucial assemblies will appear in the 
                // following call to .GetAssemblies()
                var forceToLoad = new[]
                                      {
                                          typeof (GlobalConfiguration),
                                          typeof (ConfigurationManager),
                                          typeof (Bundle),
                                      };

                // Every assembly in the current domain that is not dynamic
                // should be passed to the compiler
                var options = new CompilerParameters(
                    AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => !x.IsDynamic)
                        .Select(x => x.Location)
                        .ToArray());

                // compile source to assembly, and return resulting type
                var results = codeDomProvider.CompileAssemblyFromSource(options, source);
                return results.CompiledAssembly.GetType("View");
            }
        }

        /// <summary>
        /// Given source code text, split it up into individual segments.
        /// Each segment is a tuple of Item1: segment type, and Item2: template text
        /// </summary>
        static IEnumerable<Tuple<string, string>> Segments(string text)
        {
            var scan = 0;
            while (scan != text.Length)
            {
                var sequences = new[] { "<%@", "<%:", "<%=", "<%" };

                var next = sequences
                    .Select(sequence => new { startIndex = text.IndexOf(sequence, scan, StringComparison.InvariantCulture), sequence })
                    .Where(x => x.startIndex != -1)
                    .OrderBy(x => x.startIndex)
                    .Select(hit => new { startIndex = hit.startIndex, endIndex = text.IndexOf("%>", hit.startIndex, StringComparison.InvariantCulture), sequence = hit.sequence })
                    .FirstOrDefault(x => x.endIndex != -1);

                if (next == null)
                {
                    yield return Tuple.Create("literal", text.Substring(scan));
                    break;
                }
                yield return Tuple.Create("literal", text.Substring(scan, next.startIndex - scan));
                yield return Tuple.Create(next.sequence, text.Substring(next.startIndex + next.sequence.Length, next.endIndex - next.startIndex - next.sequence.Length));
                scan = next.endIndex + 2;
            }
        }
    }
}