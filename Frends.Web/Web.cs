
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aws4RequestSigner;
#pragma warning disable 1573

// ReSharper disable InconsistentNaming
#pragma warning disable 1591

namespace Frends.Web
{
    public enum Method
    {
        GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS, CONNECT
    }

    public enum Authentication
    {
        None, Basic, WindowsAuthentication, WindowsIntegratedSecurity, OAuth, ClientCertificate, AWSAssumeRole
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
        [DisplayFormat(DataFormatString = "Text")]
        public string Url { get; set; }

        /// <summary>
        /// The message to be sent with the request.
        /// </summary>
        [UIHint(nameof(Method), "", Method.POST, Method.DELETE, Method.PATCH, Method.PUT)]
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
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.WindowsAuthentication, Authentication.Basic)]
        public string Username { get; set; }

        [PasswordPropertyText]
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.WindowsAuthentication, Authentication.Basic)]
        public string Password { get; set; }

        /// <summary>
        /// Bearer token to be used for request. Token will be added as Authorization header.
        /// </summary>
        [PasswordPropertyText]
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.OAuth)]
        public string Token { get; set; }

        /// <summary>
        /// Thumbprint for using client certificate authentication.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Access key ID to use in the request.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        public String AccessKeyID { get; set; }

        /// <summary>
        /// The secret access key used in the request.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        [PasswordPropertyText]
        public String SecretAccessKey { get; set; }

        /// <summary>
        /// The temporary session token recieved with the request.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        [PasswordPropertyText]
        public String SessionToken { get; set; }

        /// <summary>
        /// Expiration date time to be used with the request.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        public String Expiration { get; set; }

        /// <summary>
        /// The AWS service name which can be found in the first part of the URL of the service.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("execute-api")]
        public String Service { get; set; }

        /// <summary>
        /// The region against which authentication and the request is made. These should all match.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.AWSAssumeRole)]
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("eu-west-1")]
        public String RegionEndpoint { get; set; }

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
        public Dictionary<string, string> Headers { get; set; }
        public int StatusCode { get; set; }
    }

    public class HttpByteResponse
    {
        public byte[] BodyBytes { get; set; }
        public double BodySizeInMegaBytes => Math.Round((BodyBytes?.Length / (1024 * 1024d) ?? 0), 3);
        public MediaTypeHeaderValue ContentType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int StatusCode { get; set; }
    }

    public class RestResponse
    {
        public object Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int StatusCode { get; set; }
    }

    public class Web
    {
        /// <summary>
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#RestRequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: JToken Body. Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> RestRequest([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            using (var handler = new WebRequestHandler())
            {
                cancellationToken.ThrowIfCancellationRequested();
                handler.SetHandleSettingsBasedOnOptions(options);

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

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
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#HttpRequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string Body, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpRequest([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
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

        /// <summary>
        /// HTTP request with byte return type
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#HttpRequestBytes
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string BodyBytes, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpRequestBytes([PropertyTab]Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            using (var handler = new WebRequestHandler())
            {
                cancellationToken.ThrowIfCancellationRequested();
                handler.SetHandleSettingsBasedOnOptions(options);

                using (var httpClient = new HttpClient(handler))
                {
                    var responseMessage = await GetHttpRequestResponseAsync(httpClient, input, options, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = new HttpByteResponse()
                    {
                        BodyBytes = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false),
                        ContentType = responseMessage.Content.Headers.ContentType,
                        StatusCode = (int)responseMessage.StatusCode,
                        Headers = GetResponseHeaderDictionary(responseMessage.Headers, responseMessage.Content.Headers)
                    };

                    if (!responseMessage.IsSuccessStatusCode && options.ThrowExceptionOnErrorResponse)
                    {
                        throw new WebException($"Request to '{input.Url}' failed with status code {(int)responseMessage.StatusCode}.");
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

            //Ignore case for headers and key comparison
            var headerDict = input.Headers.ToDictionary(key => key.Name, value => value.Value, StringComparer.InvariantCultureIgnoreCase);

            //Check if Content-Type exists and is set and valid
            var contentTypeIsSetAndValid = false;
            MediaTypeWithQualityHeaderValue validContentType = null;
            if (headerDict.TryGetValue("content-type", out string contentTypeValue))
                contentTypeIsSetAndValid = MediaTypeWithQualityHeaderValue.TryParse(contentTypeValue, out validContentType);

            using (HttpContent content = contentTypeIsSetAndValid ?
                new StringContent(input.Message ?? "", Encoding.GetEncoding(validContentType.CharSet ?? Encoding.UTF8.WebName)) :
                new StringContent(input.Message ?? ""))
            {
                //Clear default headers
                content.Headers.Clear();
                foreach (var header in headerDict)
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
                    Content = input.Method != Method.GET ? content : null
                };

                switch (options.Authentication)
                {
                    case Authentication.AWSAssumeRole:
                        AWS4RequestSigner helper = new AWS4RequestSigner(options.AccessKeyID, options.SecretAccessKey);
                        request.Headers.Add("X-Amz-Security-Token", options.SessionToken);
                        request = helper.Sign(request, options.Service, options.RegionEndpoint).Result;
                        break;
                }

                var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (options.AllowInvalidResponseContentTypeCharSet)
                {
                    response.Content.Headers.ContentType.CharSet = null;
                }
                return response;
            }
        }

        private static object TryParseRequestStringResultAsJToken(string response)
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
                case Authentication.ClientCertificate:
                    handler.ClientCertificates.Add(GetCertificate(options.CertificateThumbprint));
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

        internal static X509Certificate2 GetCertificate(string thumbprint)
        {
            thumbprint = Regex.Replace(thumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var signingCert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                {
                    throw new FileNotFoundException($"Certificate with thumbprint: '{thumbprint}' not found in current user cert store.");
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
