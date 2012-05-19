using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Ninject.Activation;
using Ninject.Syntax;
using Owin;

namespace JabbR.Infrastructure
{
    public static class RequestScope
    {
        /// <summary>
        /// Extension method to Ninject binding builder. 
        /// Instructs the binding to use the RequestScope.Accessor method to determine
        /// which object should govern resolved item's lifetime.
        /// </summary>
        public static IBindingNamedWithOrOnSyntax<T> InJabbrRequestScope<T>(this IBindingInSyntax<T> binding)
        {
            return binding.InScope(Accessor);
        }

        /// <summary>
        /// Extension method to OWIN IAppBuilder.
        /// Creates a new scope object as a request arrives, and registers a callback on the
        /// request's disposal token to dispose of it.
        /// </summary>
        public static IAppBuilder UsePerRequestScope(this IAppBuilder builder)
        {
            return builder.Use<AppDelegate>(
                app => (env, result, fault) =>
                {
                    var scope = Create();
                    new Gate.Request(env).CallDisposed.Register(scope.Dispose);
                    app(env, result, fault);
                });
        }

        /// <summary>
        /// Returns the scope object in the current execution context
        /// </summary>
        private static object Accessor(IContext context)
        {
            return CallContext.LogicalGetData("JabbR.Infrastructure.RequestScope");
        }


        /// <summary>
        /// Associates a new scope object with the current execution context, and 
        /// returns a disposable to revert that operation
        /// </summary>
        public static IDisposable Create()
        {
            var prior = CallContext.LogicalGetData("JabbR.Infrastructure.RequestScope");
            CallContext.LogicalSetData("JabbR.Infrastructure.RequestScope", new object());
            return prior == null
                ? new Disposable(() => CallContext.FreeNamedDataSlot("JabbR.Infrastructure.RequestScope"))
                : new Disposable(() => CallContext.LogicalSetData("JabbR.Infrastructure.RequestScope", prior));
        }

        /// <summary>
        /// An IDisposable implementation that invokes a given action
        /// </summary>
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
}