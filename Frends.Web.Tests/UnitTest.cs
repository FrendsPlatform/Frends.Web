using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Sdk;

namespace Frends.Web.Tests
{

    public static class TestDataSource
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { (Func<Input, Options, CancellationToken, Task<object>>)(async (x,y,c) => await Web.HttpRequest(x, y, c)) };
                yield return new object[] { (Func<Input, Options, CancellationToken, Task<object>>)(async (x, y, c) => await Web.RestRequest(x, y, c)) };
            }
        }

    }

    public class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly MockHttpMessageHandler _mockHttpMessageHandler;

        public MockHttpClientFactory(MockHttpMessageHandler mockHttpMessageHandler)
        {
            _mockHttpMessageHandler = mockHttpMessageHandler;
        }
        public HttpClient CreateClient(Options options)
        {
           return _mockHttpMessageHandler.ToHttpClient();

        }
    }

    public class UnitTest
    {
        private const string BasePath = "http://localhost:9191";

        private readonly MockHttpMessageHandler _mockHttpMessageHandler;

        public UnitTest()
        {
            _mockHttpMessageHandler = new MockHttpMessageHandler();
            Web.ClearClientCache();
            Web.ClientFactory = new MockHttpClientFactory(_mockHttpMessageHandler);
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

        [Theory]
        [MemberData(nameof(TestDataSource.TestCases), MemberType = typeof(TestDataSource))]
        public async Task RequestTestGetWithParameters(
            Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";
            var dict = new Dictionary<string, string>()
            {
                {"foo", "bar"},
                {"bar", "foo"}
            };

            _mockHttpMessageHandler.When($"{BasePath}/endpoint").WithQueryString(dict)
                .Respond("application/json", expectedReturn);


            var input = GetInputParams(url: "http://localhost:9191/endpoint?foo=bar&bar=foo");
            var options = new Options {ConnectionTimeoutSeconds = 60};

            var result = (dynamic) await requestFunc(input, options, CancellationToken.None);

            Assert.Contains("FooBar", result.Body.ToString());
        }

        [Theory]
        [MemberData(nameof(TestDataSource.TestCases), MemberType = typeof(TestDataSource))]
        public async Task RequestShuldThrowExceptionIfOptionIsSetAsync(
            Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _mockHttpMessageHandler.When($"{BasePath}/endpoint")
                .Respond(HttpStatusCode.InternalServerError, "application/json", expectedReturn);

            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options {ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = true};

            var ex = await Assert.ThrowsAsync<WebException>(async () =>
                await requestFunc(input, options, CancellationToken.None));
            Assert.Contains("FooBar", ex.Message);
        }

        [Theory]
        [MemberData(nameof(TestDataSource.TestCases), MemberType = typeof(TestDataSource))]
        public async Task RequestShouldNotThrowIfOptionIsNotSet(
            Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            _mockHttpMessageHandler.When($"{BasePath}/endpoint")
                .Respond(HttpStatusCode.InternalServerError, "application/json", expectedReturn);


            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options {ConnectionTimeoutSeconds = 60, ThrowExceptionOnErrorResponse = false};

            var result = (dynamic) await requestFunc(input, options, CancellationToken.None);
            Assert.Contains("FooBar", result.Body.ToString());
        }

        [Theory]
        [MemberData(nameof(TestDataSource.TestCases), MemberType = typeof(TestDataSource))]
        public async Task RequestShouldAddBasicAuthHeaders(
            Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";

            var input = new Input
            {
                Method = Method.GET,
                Url = " http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options
            {
                ConnectionTimeoutSeconds = 60,
                ThrowExceptionOnErrorResponse = true,
                Authentication = Authentication.Basic,
                Username = "Foo",
                Password = "Bar"
            };
            var sentAuthValue =
                "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}"));

            _mockHttpMessageHandler.Expect($"{BasePath}/endpoint").WithHeaders("Authorization", sentAuthValue)
                .Respond("application/json", expectedReturn);

            var result = (dynamic) await requestFunc(input, options, CancellationToken.None);

            _mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [MemberData(nameof(TestDataSource.TestCases), MemberType = typeof(TestDataSource))]
        public async Task RequestShouldAddOAuthBearerHeader(
            Func<Input, Options, CancellationToken, Task<object>> requestFunc)
        {
            const string expectedReturn = @"'FooBar'";


            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options
            {
                ConnectionTimeoutSeconds = 60,
                Authentication = Authentication.OAuth,
                Token = "fooToken"
            };

            _mockHttpMessageHandler.Expect($"{BasePath}/endpoint").WithHeaders("Authorization", "Bearer fooToken")
                .Respond("application/json", expectedReturn);
            await requestFunc(input, options, CancellationToken.None);
            _mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task RequestShouldAddClientCertificate()
        {
            const string thumbprint = "ABCD";
            var input = new Input
            {
                Method = Method.GET,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[0],
                Message = ""
            };
            var options = new Options
            {
                ConnectionTimeoutSeconds = 60,
                ThrowExceptionOnErrorResponse = true,
                Authentication = Authentication.ClientCertificate,
                CertificateThumbprint = thumbprint
            };

            Web.ClientFactory = new HttpClientFactory();

            var ex = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await Web.RestRequest(input, options, CancellationToken.None));

            Assert.Contains($"Certificate with thumbprint: '{thumbprint}' not", ex.Message);
        }

        [Fact]
        public async Task RestRequestBodyReturnShouldBeOfTypeJToken()
        {
            dynamic dyn = new
            {
                Foo = "Bar"
            };
            string output = JsonConvert.SerializeObject(dyn);


            var input = new Input
                {Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = ""};
            var options = new Options
                {ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken"};

            _mockHttpMessageHandler.When(input.Url)
                .Respond("application/json", output);

            var result = (RestResponse) await Web.RestRequest(input, options, CancellationToken.None);
            var resultBody = result.Body as JToken;
            Assert.Equal(new JValue("Bar"), resultBody["Foo"]);
        }

        [Fact]
        public async Task RestRequestShouldNotThrowIfReturnIsEmpty()
        {
            var input = new Input
                {Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = ""};
            var options = new Options
                {ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken"};

            _mockHttpMessageHandler.When(input.Url)
                .Respond("application/json", String.Empty);

            var result = (RestResponse) await Web.RestRequest(input, options, CancellationToken.None);

            Assert.Equal(new JValue(""), result.Body);
        }

        [Fact]
        public async Task RestRequestShouldThrowIfReturnIsNotValidJson()
        {
            var input = new Input
                {Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = ""};
            var options = new Options
                {ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken"};

            _mockHttpMessageHandler.When(input.Url)
                .Respond("application/json", "<fail>failbar<fail>");
            var ex = await Assert.ThrowsAsync<JsonReaderException>(async () =>
                await Web.RestRequest(input, options, CancellationToken.None));

            Assert.Equal("Unable to read response message as json: <fail>failbar<fail>", ex.Message);
        }

        [Fact]
        public async Task HttpRequestBodyReturnShouldBeOfTypeString()
        {
            const string expectedReturn = "<foo>BAR</foo>";
     
            var input = new Input
                {Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = ""};
            var options = new Options {ConnectionTimeoutSeconds = 60};

            _mockHttpMessageHandler.When(input.Url)
                .Respond("text/plain", expectedReturn);
            var result = (HttpResponse) await Web.HttpRequest(input, options, CancellationToken.None);

            Assert.Equal(expectedReturn, result.Body);
        }
        
        [Fact]
        public async Task HttpRequestBytesReturnShoulReturnEmpty()
        {
            var input = new Input { Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = "" };
            var options = new Options { ConnectionTimeoutSeconds = 60 };

            _mockHttpMessageHandler.When(input.Url)
                .Respond("application/octet-stream", String.Empty);

            var result = (HttpByteResponse)await Web.HttpRequestBytes(input, options, CancellationToken.None);
            Assert.Equal(0, result.BodySizeInMegaBytes);
            Assert.Empty(result.BodyBytes);
        }

        [Fact]
        public async Task HttpRequestBytesShouldBeAbleToReturnBinary()
        {

            var testFileUriPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase),
                "TestFiles\\frends_favicon.png");
            string localTestFilePath = new Uri(testFileUriPath).LocalPath;
            var input = new Input
                {Method = Method.GET, Url = "http://localhost:9191/endpoint", Headers = new Header[0], Message = ""};
            var options = new Options {ConnectionTimeoutSeconds = 60};

            var actualFileBytes = File.ReadAllBytes(localTestFilePath);
            _mockHttpMessageHandler.When(input.Url)
                .Respond("image/png", new MemoryStream(actualFileBytes));

            var result = (HttpByteResponse) await Web.HttpRequestBytes(input, options, CancellationToken.None);

            Assert.NotEmpty(result.BodyBytes);

        
            Assert.Equal(actualFileBytes, result.BodyBytes);
        }

        [Fact]
        public async Task PatchShouldComeThroug()
        {
            var message = "והצ";

            var input = new Input
            {
                Method = Method.PATCH,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[] { },
                Message = message
            };
            var options = new Options { ConnectionTimeoutSeconds = 60 };

            _mockHttpMessageHandler.Expect(new HttpMethod("PATCH"), input.Url).WithContent(message)
                .Respond("text/plain", "foo והצ");

            await Web.HttpRequest(input, options, CancellationToken.None);

            _mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task RequestShouldSetEncodingWithContentTypeCharsetIgnoringCase()
        {
            var codePageName = "iso-8859-1";
            var requestMessage = "והצ!";
            var expectedContentType = $"text/plain; charset={codePageName}";

            var contentType = new Header { Name = "cONTENT-tYpE", Value = expectedContentType };
            var input = new Input
            {
                Method = Method.POST,
                Url = "http://localhost:9191/endpoint",
                Headers = new Header[1] { contentType },
                Message = requestMessage
            };
            var options = new Options { ConnectionTimeoutSeconds = 60 };

            _mockHttpMessageHandler.Expect(HttpMethod.Post, input.Url).WithHeaders("cONTENT-tYpE", expectedContentType).WithContent(requestMessage)
                .Respond("text/plain", "foo והצ");


            var result = (HttpResponse)await Web.HttpRequest(input, options, CancellationToken.None);

            _mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task SendBytesShouldPassExactBytes()
        {

            var expectedString = "Tהה on se odotettu stringi!צײצצִ";
            var bytes = Encoding.UTF8.GetBytes(expectedString);
            var input = new ByteInput
            {
                Method = SendMethod.POST,
                Url = "http://localhost:9191/data",
                Headers = new[]
                {
                    new Header {Name = "Content-Type", Value = "text/plain; charset=utf-8"},
                    new Header() {Name = "Content-Length", Value = bytes.Length.ToString()}
                },
                ContentBytes = bytes
            };

            var options = new Options
            { ConnectionTimeoutSeconds = 60, Authentication = Authentication.OAuth, Token = "fooToken" };

            _mockHttpMessageHandler.Expect(HttpMethod.Post, input.Url).WithHeaders("Content-Type", "text/plain; charset=utf-8").WithContent(expectedString)
                .Respond("text/plain", "foo והצ");

            var result = (HttpResponse)await Web.HttpSendBytes(input, options, CancellationToken.None);

            _mockHttpMessageHandler.VerifyNoOutstandingExpectation();
        }
    }
}
