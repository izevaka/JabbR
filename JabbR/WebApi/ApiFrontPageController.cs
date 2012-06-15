using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.WebApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace JabbR.WebApi
{
    public class ApiFrontPageController : ApiController
    {
        private IVirtualPathUtility _VirtualPathUtilityWrapper;
        private IApplicationSettings _AppSettings;

        public ApiFrontPageController(IVirtualPathUtility virtualPathUtilityWrapper, IApplicationSettings appSettings)
        {
            _VirtualPathUtilityWrapper = virtualPathUtilityWrapper;
            _AppSettings = appSettings;
        }
        /// <summary>
        /// Returns an absolute URL (including host and protocol) that corresponds to the relative path passed as an argument.
        /// </summary>
        /// <param name="sitePath">Path within the aplication, may contain ~ to denote the application root</param>
        /// <returns>A URL that corresponds to requested path using host and protocol of the request</returns>
        public string ToAbsoluteUri(string sitePath)
        {
            var path = _VirtualPathUtilityWrapper.ToAbsolute(sitePath);

            return Request.FormatResourceUri(path);
        }

        public HttpResponseMessage GetFrontPage()
        {
            var responseData = new ApiFrontpageModel
            {
                Auth = new AuthApiModel
                {
                    JanrainAppId = _AppSettings.AuthAppId,
                    AuthUri = ToAbsoluteUri("~/Auth/Login.ashx")
                },
                MessagesUri = ToAbsoluteUri(GetMessagesPath())
            };
            return Request.CreateJabbrSuccessMessage(HttpStatusCode.OK, responseData);
        }
        private string GetMessagesPath() {
            //hardcoded for now, needs a better place - i.e. some sort of constants.cs. 
            //Alternatively there might be a better way to do that in WebAPI
            return "/api/v1/messages/{room}/{format}";
        }
    }
}