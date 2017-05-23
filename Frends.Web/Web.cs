
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading;
using Frends.Tasks.Attributes;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#pragma warning disable 1573

// ReSharper disable InconsistentNaming
#pragma warning disable 1591

namespace Frends.Web
{
    public enum Method
    {
        Get,Post,Put,Patch,Delete
    }

    public enum Authentication
    {
        None, Basic, WindowsAuthentication, WindowsIntegratedSecurity, OAuth
    }

    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Input
    {
        /// <summary>
        /// The HTTP Method to be used with the request.
        /// </summary>
        public Method Method { get; set; }

        /// <summary>
        /// The URL with protocol and path. You can include query parameters directly in the url. 
        /// </summary>
        [DefaultValue("https://example.org/path/to")]
        [DefaultDisplayType(DisplayType.Text)]
        public string Url { get; set; }

        /// <summary>
        /// The message to be sent with the request.
        /// </summary>
        [ConditionalDisplay(nameof(Method), Method.Post, Method.Delete, Method.Patch, Method.Put)]
        public string Message { get; set; }

        /// <summary>
        /// List of HTTP headers to be added to the request. 
        /// </summary>
        public Header[] Headers { get; set; }
    }

    public class Options
    {
        /// <summary>
        /// Method of authenticating request
        /// </summary>
        public Authentication Authentication { get; set; }

        /// <summary>
        /// If WindowsAuthentication is selected you should use domain\username
        /// </summary>
        [ConditionalDisplay(nameof(Frends.Web.Authentication), Authentication.WindowsAuthentication, Authentication.Basic)]
        public string Username { get; set; }

        [PasswordPropertyText]
        [ConditionalDisplay(nameof(Frends.Web.Authentication), Authentication.WindowsAuthentication, Authentication.Basic)]
        public string Password { get; set; }

        /// <summary>
        /// Bearer token to be used for request. Token will be added as Authorization header.
        /// </summary>
        [PasswordPropertyText]
        [ConditionalDisplay(nameof(Frends.Web.Authentication), Authentication.OAuth)]
        public string Token { get; set; }

        /// <summary>
        /// Timeout in seconds to be used for the connection and operation.
        /// </summary>
        [DefaultValue(30)]
        public int ConnectionTimeoutSeconds { get; set; }

        /// <summary>
        /// If FollowRedirects is set to false, all responses with an HTTP status code from 300 to 399 is returned to the application.
        /// </summary>
        [DefaultValue(true)]
        public bool FollowRedirects { get; set; }

        /// <summary>
        /// Do not throw an exception on certificate error. 
        /// </summary>
        public bool AllowInvalidCertificate { get; set; }

        /// <summary>
        /// Some Api's return faulty content-type charset header. This setting overrides the returned charset.
        /// </summary>
        public bool AllowInvalidResponseContentTypeCharSet { get; set; }
        /// <summary>
        /// Throw exception if return code of request is not successfull
        /// </summary>
        public bool ThrowExceptionOnErrorResponse { get; set; }
    }

    public class HttpResponse
    {
        public string Body { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public int StatusCode { get; set; }
    }

    public class RestResponse
    {
        public JToken Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int StatusCode { get; set; }
    }

    public class Web
    {
        /// <summary>
        /// For a more detailed documentation see: https://bitbucket.org/hiqfinland/frends.web#markdown-header-webrestrequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: JToken Body. Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> RestRequest([CustomDisplay(DisplayOption.Tab)] Input input, [CustomDisplay(DisplayOption.Tab)] Options options, CancellationToken cancellationToken)
        {
            using (var handler = new WebRequestHandler())
            {
                cancellationToken.ThrowIfCancellationRequested();
                handler.SetHandleSettingsBasedOnOptions(options);

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept","application/json");

                    var responseMessage = await GetHttpRequestResponseAsync(httpClient, input, options, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var response = new RestResponse
                    {
                        Body = TryParseRequestStringResultAsJToken(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)),
                        StatusCode = (int)responseMessage.StatusCode,
                        Headers = GetResponseHeaderDictionary(responseMessage.Headers, responseMessage.Content.Headers)
                    };

                    if (!responseMessage.IsSuccessStatusCode && options.ThrowExceptionOnErrorResponse)
                    {
                        throw new WebException($"Request to '{input.Url}' failed with status code {(int)responseMessage.StatusCode}. Response body: {response.Body}");
                    }

                    return response;
                }
            }
        }

        /// <summary>
        /// For a more detailed documentation see: https://bitbucket.org/hiqfinland/frends.web#markdown-header-webhttprequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string Body, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpRequest([CustomDisplay(DisplayOption.Tab)] Input input, [CustomDisplay(DisplayOption.Tab)] Options options, CancellationToken cancellationToken)
        {
            using (var handler = new WebRequestHandler())
            {
                cancellationToken.ThrowIfCancellationRequested();
                handler.SetHandleSettingsBasedOnOptions(options);

                using (var httpClient = new HttpClient(handler))
                {
                    var responseMessage = await GetHttpRequestResponseAsync(httpClient, input, options, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

               
                    var response = new HttpResponse()
                    {
                        Body = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false),
                        StatusCode = (int)responseMessage.StatusCode,
                        Headers = GetResponseHeaderDictionary(responseMessage.Headers, responseMessage.Content.Headers)
                    };

                    if (!responseMessage.IsSuccessStatusCode && options.ThrowExceptionOnErrorResponse)
                    {
                        throw new WebException($"Request to '{input.Url}' failed with status code {(int)responseMessage.StatusCode}. Response body: {response.Body}");
                    }

                    return response;
                }
            }
        }

        //Combine response- and responsecontent header to one dictionary
        private static Dictionary<string, string> GetResponseHeaderDictionary(HttpResponseHeaders responseMessageHeaders, HttpContentHeaders contentHeaders)
        {
            var responseHeaders = responseMessageHeaders.ToDictionary(h => h.Key, h => string.Join(";", h.Value));
            var allHeaders = contentHeaders.ToDictionary(h => h.Key, h => string.Join(";", h.Value));
            responseHeaders.ToList().ForEach(x => allHeaders[x.Key] = x.Value);
            return allHeaders;
        }

        private static async Task<HttpResponseMessage> GetHttpRequestResponseAsync(HttpClient httpClient, Input input, Options options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (options.Authentication == Authentication.Basic || options.Authentication == Authentication.OAuth)
            {
                switch (options.Authentication)
                {
                    case Authentication.Basic:
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}")));
                        break;
                    case Authentication.OAuth:
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                            options.Token);
                        break;
                }
            }

            //Do not automtically set expect 100-continue response header
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            httpClient.Timeout = TimeSpan.FromSeconds(Convert.ToDouble(options.ConnectionTimeoutSeconds));

            using (HttpContent content = new StringContent(input.Message ?? ""))
            {
                //Clear default headers
                content.Headers.Clear();
                foreach (var header in input.Headers.ToDictionary(key => key.Name, value => value.Value))
                {
                    var requestHeaderAddedSuccessfully = httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    if (!requestHeaderAddedSuccessfully)
                    {
                        //Could not add to request headers try to add to content headers
                        var contentHeaderAddedSuccessfully = content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        if (!contentHeaderAddedSuccessfully)
                        {
                            Trace.TraceWarning($"Could not add header {header.Key}:{header.Value}");
                        }
                    }
                }

                var request = new HttpRequestMessage(new HttpMethod(input.Method.ToString()), new Uri(input.Url))
                {
                    Content = input.Method != Method.Get ? content : null
                };

                var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (options.AllowInvalidResponseContentTypeCharSet)
                {
                    response.Content.Headers.ContentType.CharSet = null;
                }
                return response;
            }
        }

        private static JToken TryParseRequestStringResultAsJToken(string response)
        {
            try
            {
                return string.IsNullOrWhiteSpace(response) ? new JValue("") : JToken.Parse(response);
            }
            catch (JsonReaderException)
            {
                throw new JsonReaderException($"Unable to read response message as json: {response}");
            }
        }
    }

    public static class Extensions
    {
        internal static void SetHandleSettingsBasedOnOptions(this WebRequestHandler handler, Options options)
        {
            switch (options.Authentication)
            {
                case Authentication.WindowsIntegratedSecurity:
                    handler.UseDefaultCredentials = true;
                    break;
                case Authentication.WindowsAuthentication:
                    var domainAndUserName = options.Username.Split('\\');
                    if (domainAndUserName.Length != 2)
                    {
                        throw new ArgumentException($@"Username needs to be 'domain\username' now it was '{options.Username}'");
                    }
                    handler.Credentials = new NetworkCredential(domainAndUserName[1], options.Password, domainAndUserName[0]);
                    break;
            }

            handler.AllowAutoRedirect = options.FollowRedirects;

            if (options.AllowInvalidCertificate)
            {
                handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
            }
            //Allow all endpoint types
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
        }
    }
}
