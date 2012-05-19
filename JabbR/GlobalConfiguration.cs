using System.Web.Http;
using System.Web.Routing;
using SignalR.Ninject;

namespace JabbR
{
    /// <summary>
    /// Global information between Bootstrapper and Startup.
    /// 
    /// This should eventually disappear - but as long as OWIN support is on a branch
    /// this helps keep the number of changes to Bootstrapper to a minimum.
    /// </summary>
    public static class GlobalConfiguration
    {
        private static readonly HttpConfiguration HttpConfiguration = new HttpConfiguration();

        public static HttpConfiguration Configuration { get { return HttpConfiguration; } }

        public static NinjectDependencyResolver Resolver { get; set; }

        public static void MapHubs(this RouteCollection routes, NinjectDependencyResolver resolver)
        {
            Resolver = resolver;
        }

        public static void MapHttpRoute(this RouteCollection routes, string name, string routeTemplate)
        {
        }
    }
}
