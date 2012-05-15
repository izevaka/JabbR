using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using JabbR.Infrastructure;
using JabbR.Handlers;
using Moq;
using System.Web;
using System.IO;
using System.Web.Routing;
using JabbR.Services;

namespace JabbR.Test
{
    public class AuthApiHandlerFacts
    {
        public class Process
        {
            AuthApiHandler _Handler;
            Mock<IApiResponseWriter> _ResponseWriterMock;
            Mock<HttpContextBase> _ContextMock;
            Mock<HttpRequestBase> _RequestMock;
            Mock<IVirtualPathUtility> _VirtualPathMock;
            Mock<IApplicationSettings> _AppSettingsMock;

            public Process()
            {
                _ResponseWriterMock = new Mock<IApiResponseWriter>();
                _VirtualPathMock = new Mock<IVirtualPathUtility>();
                _AppSettingsMock = new Mock<IApplicationSettings>();
                
                _Handler = new AuthApiHandler(_VirtualPathMock.Object, _AppSettingsMock.Object);
                
                _ContextMock = new Mock<HttpContextBase>();
                _RequestMock = new Mock<HttpRequestBase>();
                _ContextMock.Setup(c=>c.Request).Returns(_RequestMock.Object);

                _Handler.Writer = _ResponseWriterMock.Object;
                _Handler.Context = _ContextMock.Object;

                _VirtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");
                _RequestMock.Setup(r => r.Url).Returns(new Uri("http://example.com/api"));
            }

            [Fact]
            public void ShouldOutputAuthEndpoint()
            {
                object responseData = null;
                _ResponseWriterMock.Setup(rw => rw.WriteResponseObejct(It.IsAny<object>())).Callback<object>(o => { responseData = o; });
                
                _Handler.Process();

                Assert.Equal("http://example.com/Auth/Login.ashx", ((ApiResponse)responseData).Auth.AuthUri);
            }

            [Fact]
            public void ShouldOutputAppId()
            {
                _AppSettingsMock.Setup(asm => asm.AuthAppId).Returns("theAppId");

                object responseData = null;
                _ResponseWriterMock.Setup(rw => rw.WriteResponseObejct(It.IsAny<object>())).Callback<object>(o => { responseData = o; });

                _Handler.Process();

                Assert.Equal("theAppId", ((ApiResponse)responseData).Auth.JanrainAppId);
            }
        }
    }
}
