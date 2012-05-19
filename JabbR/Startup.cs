using Gate.Adapters.AspNetWebApi;
using Gate.Middleware;
using JabbR.App_Start;
using JabbR.Infrastructure;
using Owin;
using SignalR.Hosting.Owin;

namespace JabbR
{
    /// <summary>
    /// Initialization method when self hosted on ASP.NET via OWIN
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Default entry-point
        /// </summary>
        public void Configuration(IAppBuilder builder)
        {
            Bootstrapper.PreAppStart();

            // Create a new IoC scope for each http request
            builder.UsePerRequestScope();

            // Execute the default.aspx template for root requests
            builder.UseHomePage();

            // Serve static files relative to the web app base directory
            builder.UseStatic(".");

            // Enable SignalR on the default url with the given IoC container
            builder.UseSignalR(GlobalConfiguration.Resolver);

            // Enable WebAPI with the given configuration
            builder.RunHttpServer(GlobalConfiguration.Configuration);
        }
    }
}