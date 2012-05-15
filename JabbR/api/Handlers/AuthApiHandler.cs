using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Configuration;
using JabbR.Infrastructure;
using System.Web.Routing;
using JabbR.Services;

namespace JabbR.Handlers
{
    public class AuthApiHandler : ApiHandlerBase
    {
        private IVirtualPathUtility _VirtualPathUtilityWrapper;
        private IApplicationSettings _AppSettings;

        public AuthApiHandler(IVirtualPathUtility virtualPathUtilityWrapper, IApplicationSettings appSettings)
        {
            _VirtualPathUtilityWrapper = virtualPathUtilityWrapper;
            _AppSettings = appSettings;
        }
        /// <summary>
        /// Returns an absolute URL (including host and protocol) that corresponds to the relative path passed as an argument.
        /// </summary>
        /// <param name="sitePath">Path within the aplication, may contain ~ to denote the application root</param>
        /// <returns>A URL that corresponds to requested path using host and protocol of the request</returns>
        public string ToAbsoluteUrl(string sitePath)
        {
            var requestUri = Context.Request.Url;
            var path = _VirtualPathUtilityWrapper.ToAbsolute(sitePath);

            return requestUri.GetLeftPart(UriPartial.Authority) + path;
        }

        public override void Process()
        {
            var responseData = new ApiResponse
            {
                Auth = new AuthApiResponse
                {
                    JanrainAppId = _AppSettings.AuthAppId,
                    AuthUri = ToAbsoluteUrl("~/Auth/Login.ashx")
                },
                MessagesUri = ToAbsoluteUrl(GetMessagesUrl())
            };
            Writer.WriteResponseObejct(responseData);
        }
        private string GetMessagesUrl() {
            //hardcoded for now, needs a better place - i.e. some sort of constants.cs. 
            //Alternatively there might be a better way to do that in WebAPI
            return "/api/v1/messages/{room}/{format}";
        }
    }
}