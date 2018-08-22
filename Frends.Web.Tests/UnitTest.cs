using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpMock;
using HttpMock.Verify.NUnit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.Web.Tests
{
    [TestFixture]
    public class UnitTest
    {
        private const string BasePath = "http://localhost:9191";

        private IHttpServer _stubHttp;

        [SetUp]
        public void Setup()
        {
            _stubHttp = HttpMockRepository.At(BasePath);
        }
        private static IEnumerable<Func<Input, Options, CancellationToken, Task<object>>> TestCases
        {
            get
            {
                yield return Web.HttpRequest;
                yield return Web.RestRequest;
            }
        }

        private Input GetInputParams(Method method = Method.GET, string url = BasePath, string message = "",
            params Header[] headers)
        {
            return new Input
            {
                Method = method,
                Url = url,
                Headers = headers,
                Message = message
            };
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task RequestTestGetWithParameters(Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";
            var dict = new Dictionary<string, string>()
            {
                { "foo", "bar"},
                { "bar", "foo"}
            };

            _stubHttp.Stub(x => x.Get("/endpoint").WithParams(dict))
               .Return(expectedReturn)
               .OK();

            var input = GetInputParams(url: "http://localhost:9191/endpoint?foo=bar&bar=foo");
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (dynamic)await requestFunc(input, options, CancellationToken.None);

            Assert.That(result.Body.ToString(), Does.Contain("FooBar"));
        }

        [TestCaseSource(nameof(TestCases))]
        public void RequestShuldThrowExceptionIfOptionIsSet(Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(expectedReturn)
               .WithStatus(HttpStatusCode.InternalServerError);

            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options { ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = true };

            var ex = Assert.ThrowsAsync<WebException>(async () => await requestFunc(input, options, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("FooBar"));
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task RequestShouldNotThrowIfOptionIsNotSet(Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(expectedReturn)
               .WithStatus(HttpStatusCode.InternalServerError);

            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options { ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = false };

            var result = (dynamic)await requestFunc(input, options, CancellationToken.None);
            Assert.That(result.Body.ToString(), Does.Contain("FooBar"));
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task RequestShouldAddBasicAuthHeaders(Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(expectedReturn)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = true, Authentication = Authentication.Basic, Username = "Foo", Password = "Bar" };
            await Web.RestRequest(input, options, CancellationToken.None);

            var requestHeaders = _stubHttp.AssertWasCalled(called => called.Get("/endpoint")).LastRequest().RequestHead;
            var recievedAuthValue = requestHeaders.Headers["Authorization"];
            var sentAuthValue = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));

            Assert.That(recievedAuthValue, Is.EqualTo(sentAuthValue));
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task RequestShouldAddOAuthBearerHeader(Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(expectedReturn)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };
            await Web.RestRequest(input, options, CancellationToken.None);

            var requestHeaders = _stubHttp.AssertWasCalled(called => called.Get("/endpoint")).LastRequest().RequestHead;
            var recievedAuthValue = requestHeaders.Headers["Authorization"];

            Assert.That(recievedAuthValue, Is.EqualTo("Bearer fooToken"));
        }

        [Test]
        public void RequestShouldAddClientCertificate()
        {
            const string expectedReturn = @"'FooBar'";

            _stubHttp.Stub(x => x.Get("/endpoint"))
                .Return(expectedReturn)
                .OK();
            const string thumbprint = "ABCD";
            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = true, Authentication = Authentication.ClientCertificate, CertificateThumbprint = thumbprint };


            var ex = Assert.ThrowsAsync<FileNotFoundException>(async () => await Web.RestRequest(input, options, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain($"Certificate with thumbprint: '{thumbprint}' not"));
        }

        [Test]
        public async Task RestRequestBodyReturnShouldBeOfTypeJToken()
        {
            dynamic dyn = new
            {
                Foo = "Bar"
            };
            string output = JsonConvert.SerializeObject(dyn);
            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(output)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };
            var result = (RestResponse)await Web.RestRequest(input, options, CancellationToken.None);
            var resultBody = result.Body as JToken;
            Assert.That(resultBody["Foo"], Is.EqualTo(new JValue("Bar")));
        }


        [Test]
        public async Task RestRequestIfResetResponseContentTypeCharsetShouldNoterror()
        {
            dynamic dyn = new
            {
                Foo = "Bar"
            };
            string output = JsonConvert.SerializeObject(dyn);
            _stubHttp.Stub(x => x.Get("/endpoint")).AddHeader("content-type", "application/json;charset=utf8")
               .Return(output)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 30, AllowInvalidResponseContentTypeCharSet = true };
            var result = (RestResponse)await Web.RestRequest(input, options, CancellationToken.None);
            var resultBody = result.Body as JToken;
            Assert.That(resultBody["Foo"], Is.EqualTo(new JValue("Bar")));
        }

        [Test]
        public async Task RestRequestShouldNotThrowIfReturnIsEmpty()
        {
            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(string.Empty)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };
            var result = (RestResponse)await Web.RestRequest(input, options, CancellationToken.None);

            Assert.That(result.Body, Is.EqualTo(new JValue("")));
        }

        [Test]
        public async Task RestRequestShouldNotThrowIfReturnIsEmptyAndContentTypeValidationTurnedOff()
        {
            _stubHttp.Stub(x => x.CustomVerb("/endpoint", "PATCH"))
                .Return("")
                .WithStatus(HttpStatusCode.Accepted);

            var input = new Input
            {
                Method = Method.PATCH,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options
            {
                ConnectionTimeoutSeconds = 60,
                AllowInvalidResponseContentTypeCharSet = true
            };
            var result = (RestResponse)await Web.RestRequest(input, options, CancellationToken.None);

            Assert.That(result.Body, Is.EqualTo(new JValue("")));
        }

        [Test]
        public void RestRequestShouldThrowIfReturnIsNotValidJson()
        {

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return("<fail>failbar<fail>")
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };
            var ex = Assert.ThrowsAsync<JsonReaderException>(async () => await Web.RestRequest(input, options, CancellationToken.None));
            Assert.That(ex.Message, Is.EqualTo("Unable to read response message as json: <fail>failbar<fail>"));
        }

        [Test]
        public async Task HttpRequestBodyReturnShouldBeOfTypeString()
        {
            const string expectedReturn = "<foo>BAR</foo>";

            _stubHttp.Stub(x => x.Get("/endpoint"))
               .Return(expectedReturn)
               .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (HttpResponse)await Web.HttpRequest(input, options, CancellationToken.None);
            Assert.That(result.Body, Is.EqualTo(expectedReturn));
        }

        [Test]
        public async Task HttpRequestBytesReturnShoulReturnEmpty()
        {
            _stubHttp.Stub(x => x.Get("/endpoint"))
                .Return(string.Empty)
                .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60 };

            var result = (HttpByteResponse)await Web.HttpRequestBytes(input, options, CancellationToken.None);
            Assert.That(result.BodySizeInMegaBytes, Is.EqualTo(0));
            Assert.That(result.BodyBytes, Is.Empty);
        }

        [Test]
        public async Task HttpRequestBytesShouldBeAbleToReturnBinary()
        {
            var testFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles\\frends_favicon.png");

            _stubHttp.Stub(x => x.Get("/endpoint"))
                .ReturnFile(testFilePath)
                .AsContentType("image/png")
                .OK();

            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (HttpByteResponse)await Web.HttpRequestBytes(input, options, CancellationToken.None);

            Assert.That(result.BodyBytes, Is.Not.Empty);

            var actualFileBytes = File.ReadAllBytes(testFilePath);
            Assert.That(result.BodyBytes, Is.EqualTo(actualFileBytes));
        }

        [Test]
        public async Task PatchShouldComeThroug()
        {
            var message = "åäö";
            _stubHttp.Stub(x => x.CustomVerb("/endpoint", "PATCH"))
                .AsContentType($"text/plain; charset=UTF-8")
                .Return("foo åäö")
                .OK();

            var input = new Input { Method = Method.PATCH, Url = "http://localhost:9191/endpoint", Headers = new Header[] { }, Message = message };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            await Web.HttpRequest(input, options, CancellationToken.None);
            var requestBody = _stubHttp.AssertWasCalled(called => called.CustomVerb("PATCH", "/endpoint")).LastRequest().Body;

            Assert.That(requestBody, Is.EqualTo(message));
        }

        [Test]
        public async Task RequestShouldSetEncodingWithContentTypeCharsetIgnoringCase()
        {
            var codePageName = "iso-8859-1";
            var requestMessage = "åäö!";
            var utf8ByteArray = Encoding.UTF8.GetBytes(requestMessage);
            var expectedContentType = $"text/plain; charset={codePageName}";

            _stubHttp.Stub(x => x.Post("/endpoint"))
                .AsContentType($"text/plain; charset={codePageName}")
                .Return("foo åäö")
                .OK();

            var contentType = new Header { Name = "cONTENT-tYpE", Value = expectedContentType };
            var input = new Input { Method = Method.POST, Url = "http://localhost:9191/endpoint", Headers = new Header[1] { contentType }, Message = requestMessage };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (HttpResponse)await Web.HttpRequest(input, options, CancellationToken.None);
            var request = _stubHttp.AssertWasCalled(called => called.Post("/endpoint")).LastRequest();
            var requestHead = request.RequestHead;
            var requestBodyByteArray = Encoding.GetEncoding(codePageName).GetBytes(request.Body);
            var requestContentType = requestHead.Headers["cONTENT-tYpE"];

            //Casing should not affect setting header.
            Assert.That(requestContentType, Is.EqualTo(expectedContentType));
            //HttpMock does not handle other encodings well, so we expect the array to be be malformed.
            Assert.That(requestBodyByteArray, Is.Not.EqualTo(utf8ByteArray));
        }

        [Test]
        public async Task RequestShouldHaveUTF8BodyContentWithMalformedContentType()
        {
            var message = "åäö";
            var utf8ByteArray = Encoding.UTF8.GetBytes(message);
            var expectedContentType = "malformed";

            _stubHttp.Stub(x => x.Post("/endpoint"))
                .AsContentType($"text/plain; charset=utf-8")
                .Return("foo åäö")
                .OK();

            var contentType = new Header { Name = "content-type", Value = expectedContentType };
            var input = new Input { Method = Method.POST, Url = "http://localhost:9191/endpoint", Headers = new Header[1] { contentType }, Message = message };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (HttpResponse)await Web.HttpRequest(input, options, CancellationToken.None);
            var requestBody = _stubHttp.AssertWasCalled(called => called.Post("/endpoint")).LastRequest().Body;
            var requestBodyByteArray = Encoding.UTF8.GetBytes(requestBody);

            Assert.That(requestBodyByteArray, Is.EqualTo(utf8ByteArray));
        }

        [Test]
        public async Task RequestShouldFallbackToUTF8WithNoCharset()
        {
            var message = "åäö";
            var utf8ByteArray = Encoding.UTF8.GetBytes(message);
            var expectedContentType = "application/json";

            _stubHttp.Stub(x => x.Post("/endpoint"))
                .AsContentType($"text/plain; charset=utf-8")
                .Return("foo åäö")
                .OK();

            var contentType = new Header { Name = "content-type", Value = expectedContentType };
            var input = new Input { Method = Method.POST, Url = "http://localhost:9191/endpoint", Headers = new Header[1] { contentType }, Message = message };
            var options = new Options { ConnectionTimeoutSeconds = 60 };
            var result = (HttpResponse)await Web.HttpRequest(input, options, CancellationToken.None);
            var requestBody = _stubHttp.AssertWasCalled(called => called.Post("/endpoint")).LastRequest().Body;
            var requestBodyByteArray = Encoding.UTF8.GetBytes(requestBody);

            Assert.That(requestBodyByteArray, Is.EqualTo(utf8ByteArray));
        }

        [Test]
        public async Task SendBytesShouldPassExactBytes()
        {
            _stubHttp.Stub(x => x.Post("/data"))
                .Return("OK")
                .OK();

            var expectedString = "Tää on se odotettu stringi!öÖööÄ";
            var bytes = Encoding.UTF8.GetBytes(expectedString);
            var input = new ByteInput
            {
                Method = SendMethod.POST,
                Url = "http://localhost:9191/data",
                Headers = new []
                {
                    new Header {Name= "Content-Type", Value = "text/plain; charset=utf-8"},
                    new Header() {Name="Content-Length", Value = bytes.Length.ToString()}
                },
                ContentBytes = bytes
            };

            var options = new Options { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };
            var result = (HttpResponse) await Web.HttpSendBytes(input, options, CancellationToken.None);

            Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));

            var sentBody = _stubHttp.AssertWasCalled(called => called.Post("/data")).LastRequest().Body;
            Assert.That(sentBody, Is.EqualTo(expectedString));
        }
    }
}
