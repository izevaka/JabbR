using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web.Http;
using Gate.Adapters.AspNetWebApi;
using Gate.Middleware;
using JabbR.App_Start;
using Owin;
using SignalR.Hosting.Owin;

namespace JabbR
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            GlobalConfiguration.Configuration = new HttpConfiguration();

            Bootstrapper.PreAppStart();

            builder
                .Use<AppDelegate>(Sanity)
                .UseHomePage()
                .UseStatic(TransitionalGlue.BasePath())
                .UseSignalRHubs("/signalr", TransitionalGlue.Resolver)
                .RunHttpServer(GlobalConfiguration.Configuration);
        }

        private AppDelegate Sanity(AppDelegate app)
        {
            return
                (env, result, fault) =>
                {
                    var watcher = new Watcher();
                    watcher.Call(env);
                    app(
                        env,
                        (status, headers, body) =>
                        {
                            watcher.Result(status, headers, body);
                            result(
                                status,
                                headers,
                                (write, flush, end, cancellationToken) =>
                                {
                                    watcher.Body(cancellationToken);
                                    body(
                                        write,
                                        flush,
                                        ex =>
                                        {
                                            watcher.End(ex);
                                            end(ex);
                                        },
                                        cancellationToken);
                                });
                        },
                        ex =>
                        {
                            watcher.Fault(ex);
                            fault(ex);
                        });
                };
        }

        class Watcher
        {
            private Timer _timer;
            private bool _resultCalled;
            private bool _faultCalled;
            private bool _bodyCalled;
            private bool _endCalled;

            private IDictionary<string, object> _env;
            private string _status;
            private IDictionary<string, IEnumerable<string>> _headers;
            private BodyDelegate _body;
            private Exception _faultException;
            private Exception _endException;

            public void Call(IDictionary<string, object> env)
            {
                _env = env;
                _timer = new Timer(OnTimer, null, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(-1));
            }

            private void OnTimer(object state)
            {
                TraceAssert(_faultCalled || _resultCalled, "fault or result not called in time");
                if (_resultCalled)
                {
                    TraceAssert(_bodyCalled, "body not called in time with result");
                }
                _timer.Dispose();
            }

            public void Result(string status, IDictionary<string, IEnumerable<string>> headers, BodyDelegate body)
            {
                TraceAssert(!_faultCalled, "result called after fault called");
                TraceAssert(!_resultCalled, "result may only be called once");
                _resultCalled = true;
                _status = status;
                _headers = headers;
                _body = body;
            }

            public void Fault(Exception exception)
            {
                TraceAssert(!_resultCalled, "fault called after result called");
                TraceAssert(!_faultCalled, "fault may only be called once");
                _faultCalled = true;
                _faultException = exception;
            }

            public void Body(CancellationToken cancellationToken)
            {
                TraceAssert(_resultCalled, "result must be called before response body");
                TraceAssert(!_bodyCalled, "body may only be called once");
                _bodyCalled = true;
            }

            public void End(Exception exception)
            {
                TraceAssert(_resultCalled, "result must be called before body end");
                TraceAssert(_bodyCalled, "body must be called before body end");
                TraceAssert(!_endCalled, "end may only be called once");
                _endCalled = true;
                _endException = exception;
            }

            private void TraceAssert(bool condition, string message)
            {
                if (condition) return;

                object value;
                if (_env.TryGetValue("host.TraceOutput", out value) && value is TextWriter)
                {
                    ((TextWriter)value).WriteLine(message);
                }
            }
        }
    }
}