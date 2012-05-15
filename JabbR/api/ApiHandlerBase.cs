using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Handlers
{
    public abstract class ApiHandlerBase : IHttpHandler
    {
        public IApiResponseWriter Writer { get; set; }
        public HttpContextBase Context { get; set; }

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            Context = new HttpContextWrapper(context);
            Writer = new ApiResponseWriter(Context);

            Process();
        }

        public abstract void Process();
    }
}