using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JabbR.Handlers
{
    public class ApiResponseWriter : IApiResponseWriter
    {
        private HttpContextBase _Context;

        public ApiResponseWriter(HttpContextBase _Context)
        {
            // TODO: Complete member initialization
            this._Context = _Context;
        }

        public void WriteBadRequest(string message)
        {
            WriteError(400, "Bad request", message);
        }

        public void WriteResponseObejct(object responseObject)
        {
            WriteResponseObejct(responseObject, null);
        }

        public void WriteResponseObejct(object responseObject, string filenamePrefix)
        {
            var request = _Context.Request;
            var response = _Context.Response;
            var json = Serialize(responseObject);
            var data = Encoding.UTF8.GetBytes(json);

            _Context.Response.ContentType = "application/json";
            _Context.Response.ContentEncoding = Encoding.UTF8;

            var routeData = _Context.Request.RequestContext.RouteData.Values;
            var formatName = (string)routeData["format"];

            bool downloadFile = false;
            Boolean.TryParse(_Context.Request["download"], out downloadFile);

            switch (formatName)
            {
                case "json":
                    {
                        if (downloadFile && filenamePrefix != null)
                        {
                            _Context.Response.Headers["Content-Disposition"] = "attachment; filename=\"" + filenamePrefix + ".json\"";
                        }
                        _Context.Response.BinaryWrite(data);
                        break;
                    }

                default:
                    {
                        WriteBadRequest("format not supported.");
                        break;
                    }
            }

        }

        public void WriteNotFound(string message)
        {
            WriteError(404, "Not found", message);
        }

        private void WriteError(int statusCode, string description, string message)
        {
            _Context.Response.TrySkipIisCustomErrors = true;
            _Context.Response.StatusCode = statusCode;
            _Context.Response.StatusDescription = description;
            _Context.Response.Write(Serialize(new ClientError { Message = message }));
        }

        private string Serialize(object value)
        {
            var resolver = new CamelCasePropertyNamesContractResolver();
            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver
            };

            settings.Converters.Add(new IsoDateTimeConverter());

            return JsonConvert.SerializeObject(value, Formatting.Indented, settings);
        }


        private class ClientError
        {
            public string Message { get; set; }
        }
    }

}