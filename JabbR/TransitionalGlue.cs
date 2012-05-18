using System;
using System.Web.Http;
using System.Web.Routing;
using SignalR.Ninject;

namespace JabbR
{
    public static class GlobalConfiguration
    {
        public static HttpConfiguration Configuration { get; set; }

        public static void MapHubs(this RouteCollection routes, NinjectDependencyResolver resolver)
        {
        }

        public static void MapHttpRoute(this RouteCollection routes, string name, string routeTemplate)
        {
        }
    }
}
