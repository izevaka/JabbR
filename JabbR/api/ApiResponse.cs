using System;
using System.Collections.Generic;
using System.Linq;

namespace JabbR.Handlers
{
    public class ApiResponse
    {
        public AuthApiResponse Auth { get; set; }
        public string MessagesUri { get; set; }
    }
}
