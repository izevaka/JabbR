using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Routing;
using Jurassic.Compiler;
using Ninject.Activation;
using Owin;
using SignalR.Ninject;

namespace JabbR
{
    public static class TransitionalGlue
    {
        public static string BasePath()
        {
            return HostingEnvironment.IsHosted
                       ? HostingEnvironment.MapPath("~")
                       : Directory.GetCurrentDirectory();
        }

        public static void MapHubs(this RouteCollection routes, NinjectDependencyResolver resolver)
        {
            Resolver = resolver;
        }

        public static void MapHttpRoute(this RouteCollection routes, string name, string routeTemplate)
        {
        }

        public static IAppBuilder UseHomePage(this IAppBuilder builder)
        {
            var viewType = PseudoWebForms.Compile(Path.Combine(BasePath(), "default.aspx"));

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


        public static NinjectDependencyResolver Resolver { get; set; }


        public static object RequestScopeAccessor(IContext context)
        {
            var scope = CallContext.LogicalGetData("Jabbr.TransitionalGlue.Scope");
            return scope;
        }

        public static IAppBuilder UseRequestScope(this IAppBuilder builder, Func<Action> scopeFactory)
        {
            return builder.Use<AppDelegate>(
                app => (env, result, fault) =>
                {
                    new Gate.Request(env).CallDisposed.Register(scopeFactory());
                    app(env, result, fault);
                });
        }

        public static IDisposable CreateScope()
        {
            var prior = CallContext.LogicalGetData("Jabbr.TransitionalGlue.Scope");
            CallContext.LogicalSetData("Jabbr.TransitionalGlue.Scope", new object());
            return prior == null
                ? new Disposable(() => CallContext.FreeNamedDataSlot("Jabbr.TransitionalGlue.Scope"))
                : new Disposable(() => CallContext.LogicalSetData("Jabbr.TransitionalGlue.Scope", prior));
        }

        class Disposable : IDisposable
        {
            private Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, () => { }).Invoke();
            }
        }
    }

    public static class GlobalConfiguration
    {
        public static HttpConfiguration Configuration { get; set; }

    }

    public static class PseudoWebForms
    {
        public abstract class View
        {
            public Gate.Request Request { get; set; }
            public Gate.Response Response { get; set; }

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

        public static Type Compile(string filePath)
        {
            var defaultAspx = File.ReadAllText(filePath);

            using (var writer = new StringWriter())
            {
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

                writer.Write(@"
using System;
public class View : JabbR.PseudoWebForms.View
{
    public override void RenderView()
    {
");
                foreach (var segment in Segments(defaultAspx))
                {
                    switch (segment.Item1)
                    {
                        case "literal":
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
                        case "<%":
                            writer.WriteLine(segment.Item2);
                            break;
                        case "<%=":
                            writer.Write("Write(");
                            writer.Write(segment.Item2);
                            writer.WriteLine(");");
                            break;
                        case "<%:":
                            writer.Write("WriteEncoded(");
                            writer.WriteLine(segment.Item2);
                            writer.WriteLine(");");
                            break;
                    }
                }
                writer.Write(@"
    }
}
");

                var source = writer.ToString();

                var codeDomProvider = CodeDomProvider.CreateProvider("cs");

                var forceToLoad = new[]
                                      {
                                          typeof (JabbR.GlobalConfiguration),
                                          typeof (System.Configuration.ConfigurationManager),
                                          typeof (SquishIt.Framework.Bundle),
                                      };
                var options = new CompilerParameters(
                        AppDomain.CurrentDomain.GetAssemblies()
                            .Where(x => !x.IsDynamic)
                            .Select(x => x.Location)
                            .ToArray());


                var results = codeDomProvider.CompileAssemblyFromSource(options, source);
                return results.CompiledAssembly.GetType("View");
            }
        }


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
