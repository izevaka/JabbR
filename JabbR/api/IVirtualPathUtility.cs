using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Handlers
{
    public interface IVirtualPathUtility
    {
        string ToAbsolute(string relativePath);
    }
}
