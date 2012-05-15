using System;
using System.Collections.Generic;
using System.Linq;

namespace JabbR.Handlers
{
    public interface IApiResponseWriter
    {
        void WriteBadRequest(string message);
        void WriteResponseObejct(object responseObject);
        void WriteResponseObejct(object responseObject, string filenamePrefix);
        void WriteNotFound(string message);
    }
}
