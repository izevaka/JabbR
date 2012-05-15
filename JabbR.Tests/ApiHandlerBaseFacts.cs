using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using JabbR.Handlers;
using System.Web;
using Moq;

namespace JabbR.Test
{
    public class TestApiHandler : ApiHandlerBase
    {
        public override void Process()
        {
        }
    }

    public class ApiHandlerBaseFacts
    {
        public class Constructor
        {
            TestApiHandler _Handler;
            public Constructor()
            {
                _Handler = new TestApiHandler();
            }

            [Fact]
            public void ShouldContextToNull()
            {
                Assert.Null(_Handler.Context);
            }
            [Fact]
            public void ShouldWriterToNull()
            {
                Assert.Null(_Handler.Writer);
            }
        }

        public class ProcessRequest
        {
            TestApiHandler _Handler;
            public ProcessRequest()
            {
                _Handler = new TestApiHandler();
                var httpWorkerMock = new Mock<HttpWorkerRequest>() { DefaultValue = DefaultValue.Mock };
                
                httpWorkerMock.Setup(h => h.GetRawUrl()).Returns("/blah");

                var testContext = new HttpContext(httpWorkerMock.Object);
                ((IHttpHandler)_Handler).ProcessRequest(testContext);
            }

            [Fact]
            public void ShouldCreateContext()
            {
                Assert.NotNull(_Handler.Context);
            }
        }
    }
}
