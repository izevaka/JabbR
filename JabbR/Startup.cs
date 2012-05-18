using System.IO;
using System.Web.Hosting;
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
                .UseStatic(BasePath())
                .RunHttpServer(GlobalConfiguration.Configuration)
                //.RunSignalR()
                ;
        }

        private static string BasePath()
        {
            return HostingEnvironment.IsHosted 
                ? HostingEnvironment.MapPath("~") 
                : Directory.GetCurrentDirectory();
        }
    }
}