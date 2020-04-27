using System;
using System.Collections.Concurrent;
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

#pragma warning disable 1573 // disable warning for missing documentation for internal cancellation tokens

// ReSharper disable InconsistentNaming
#pragma warning disable 1591

namespace Frends.Web
{
    public enum Method
    {
        GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS, CONNECT
    }

    /// <summary>
    /// Allowed methods for sending content
    /// </summary>
    public enum SendMethod
    {
        POST, PUT, PATCH, DELETE
    }

    public enum Authentication
    {
        None, Basic, WindowsAuthentication, WindowsIntegratedSecurity, OAuth, ClientCertificate
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
        /// The message text to be sent with the request.
        /// </summary>
        [UIHint(nameof(Method), "", Method.POST, Method.DELETE, Method.PATCH, Method.PUT)]
        public string Message { get; set; }

        /// <summary>
        /// List of HTTP headers to be added to the request.
        /// </summary>
        public Header[] Headers { get; set; }
    }

    public class ByteInput
    {
        /// <summary>
        /// The HTTP Method to be used with the request.
        /// </summary>
        [DefaultValue(SendMethod.POST)]
        public SendMethod Method { get; set; }

        /// <summary>
        /// The URL with protocol and path. You can include query parameters directly in the url.
        /// </summary>
        [DefaultValue("https://example.org/path/to")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Url { get; set; }

        /// <summary>
        /// The content to send as byte array
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        public byte[] ContentBytes { get; set; }

        /// <summary>
        /// List of HTTP headers to be added to the request.
        /// </summary>
        public Header[] Headers { get; set; }
    }

    public class Options : IEquatable<Options>
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
        /// Specifies where the Client Certificate should be loaded from.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        [DefaultValue(CertificateSource.CertificateStore)]
        public CertificateSource ClientCertificateSource { get; set; }

        /// <summary>
        /// Path to the Client Certificate when using a file as the Certificate Source, pfx (pkcs12) files are recommended. For other supported formats, see
        /// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2collection.import?view=netframework-4.7.1
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        public string ClientCertificateFilePath { get; set; }

        /// <summary>
        /// Client certificate bytes as a base64 encoded string when using a string as the Certificate Source , pfx (pkcs12) format is recommended. For other supported formates, see
        /// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2collection.import?view=netframework-4.7.1
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        public string ClientCertificateInBase64 { get; set; }

        /// <summary>
        /// Key phrase (password) to access the certificate data when using a string or file as the Certificate Source
        /// </summary>
        [PasswordPropertyText]
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        public string ClientCertificateKeyPhrase { get; set; }

        /// <summary>
        /// Thumbprint for using client certificate authentication.
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        public string CertificateThumbprint { get; set; }
        
        /// <summary>
        /// Should the entire certificate chain be loaded from the certificate store and included in the request. Only valid when using Certificate Store as the Certificate Source 
        /// </summary>
        [UIHint(nameof(Frends.Web.Authentication), "", Authentication.ClientCertificate)]
        [DefaultValue(true)]
        public bool LoadEntireChainForCertificate { get; set; }

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

        /// <summary>
        /// If set to false, cookies must be handled manually. Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool AutomaticCookieHandling { get; set; } = true;



        public bool Equals(Options other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Authentication == other.Authentication &&
                   string.Equals(Username, other.Username) &&
                   string.Equals(Password, other.Password) &&
                   string.Equals(Token, other.Token) &&
                   string.Equals(CertificateThumbprint, other.CertificateThumbprint) &&
                   ClientCertificateSource == other.ClientCertificateSource &&
                   ClientCertificateInBase64 == other.ClientCertificateInBase64 &&
                   ClientCertificateFilePath == other.ClientCertificateFilePath &&
                   ConnectionTimeoutSeconds == other.ConnectionTimeoutSeconds &&
                   FollowRedirects == other.FollowRedirects &&
                   AllowInvalidCertificate == other.AllowInvalidCertificate &&
                   AllowInvalidResponseContentTypeCharSet == other.AllowInvalidResponseContentTypeCharSet &&
                   ThrowExceptionOnErrorResponse == other.ThrowExceptionOnErrorResponse &&
                   AutomaticCookieHandling == other.AutomaticCookieHandling;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Options)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Authentication;
                hashCode = (hashCode * 397) ^ (Username != null ? Username.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Password != null ? Password.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Token != null ? Token.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ClientCertificateSource.GetHashCode();
                hashCode = (hashCode * 397) ^ (CertificateThumbprint != null ? CertificateThumbprint.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ClientCertificateInBase64 != null ? ClientCertificateInBase64.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ClientCertificateFilePath != null ? ClientCertificateFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ConnectionTimeoutSeconds;
                hashCode = (hashCode * 397) ^ FollowRedirects.GetHashCode();
                hashCode = (hashCode * 397) ^ AllowInvalidCertificate.GetHashCode();
                hashCode = (hashCode * 397) ^ AllowInvalidResponseContentTypeCharSet.GetHashCode();
                hashCode = (hashCode * 397) ^ ThrowExceptionOnErrorResponse.GetHashCode();
                hashCode = (hashCode * 397) ^ AutomaticCookieHandling.GetHashCode();
                return hashCode;
            }
        }
    }

    public enum CertificateSource
    {
        CertificateStore,
        File,
        String
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

    public interface IHttpClientFactory
    {
        HttpClient CreateClient(Options options);
    }
    public class HttpClientFactory : IHttpClientFactory
    {

        public HttpClient CreateClient(Options options)
        {
            var handler = new HttpClientHandler();
            handler.SetHandlerSettingsBasedOnOptions(options);
            return new HttpClient(handler);
        }
    }

    public class Web
    {
        private static readonly ConcurrentDictionary<Options, HttpClient> ClientCache = new ConcurrentDictionary<Options, HttpClient>();

        public static void ClearClientCache()
        {
            ClientCache.Clear();
        }
        // For tests
        public static IHttpClientFactory ClientFactory = new HttpClientFactory();

        /// <summary>
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#RestRequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: JToken Body. Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> RestRequest([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClientForOptions(options);
            var headers = GetHeaderDictionary(input.Headers);
            using (var content = GetContent(input, headers))
            {
                var responseMessage = await GetHttpRequestResponseAsync(
                        httpClient,
                        input.Method.ToString(),
                        input.Url,
                        content,
                        headers,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var response = new RestResponse
                {
                    Body = TryParseRequestStringResultAsJToken(await responseMessage.Content.ReadAsStringAsync()
                        .ConfigureAwait(false)),
                    StatusCode = (int)responseMessage.StatusCode,
                    Headers = GetResponseHeaderDictionary(responseMessage.Headers, responseMessage.Content.Headers)
                };

                if (!responseMessage.IsSuccessStatusCode && options.ThrowExceptionOnErrorResponse)
                {
                    throw new WebException(
                        $"Request to '{input.Url}' failed with status code {(int)responseMessage.StatusCode}. Response body: {response.Body}");
                }

                return response;

            }
        }

        private static HttpClient GetHttpClientForOptions(Options options)
        {

            return ClientCache.GetOrAdd(options, (opts) =>
            {
                // might get called more than once if e.g. many process instances execute at once,
                // but that should not matter much, as only one client will get cached
                var httpClient = ClientFactory.CreateClient(options);
                httpClient.SetDefaultRequestHeadersBasedOnOptions(opts);

                return httpClient;
            });
        }

        /// <summary>
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#HttpRequest
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string Body, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpRequest([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClientForOptions(options);
            var headers = GetHeaderDictionary(input.Headers);

            using (var content = GetContent(input, headers))
            {
                var responseMessage = await GetHttpRequestResponseAsync(
                        httpClient,
                        input.Method.ToString(),
                        input.Url,
                        content,
                        headers,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var response = new HttpResponse()
                {
                    Body = responseMessage.Content != null ? await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null,
                    StatusCode = (int)responseMessage.StatusCode,
                    Headers = GetResponseHeaderDictionary(responseMessage.Headers, responseMessage.Content?.Headers)
                };

                if (!responseMessage.IsSuccessStatusCode && options.ThrowExceptionOnErrorResponse)
                {
                    throw new WebException(
                        $"Request to '{input.Url}' failed with status code {(int)responseMessage.StatusCode}. Response body: {response.Body}");
                }

                return response;
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
            var httpClient = GetHttpClientForOptions(options);
            var headers = GetHeaderDictionary(input.Headers);

            using (var content = GetContent(input, headers))
            {
                var responseMessage = await GetHttpRequestResponseAsync(
                        httpClient,
                        input.Method.ToString(),
                        input.Url,
                        content,
                        headers,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);

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

        /// <summary>
        /// HTTP POST or other send with byte content, e.g. for files or gzipped content
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#HttpRequestBytes
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string Body, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpSendBytes([PropertyTab]ByteInput input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClientForOptions(options);
            var headers = GetHeaderDictionary(input.Headers);

            using (var content = GetContent(input))
            {
                var responseMessage = await GetHttpRequestResponseAsync(
                        httpClient,
                        input.Method.ToString(),
                        input.Url,
                        content,
                        headers,
                        options,
                        cancellationToken)
                    .ConfigureAwait(false);

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

        /// <summary>
        /// HTTP request with byte content and byte return type
        /// For a more detailed documentation see: https://github.com/FrendsPlatform/Frends.Web#HttpSendAndReceiveBytes
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options">Optional parameters with default values</param>
        /// <returns>Object with the following properties: string BodyBytes, Dictionary(string,string) Headers. int StatusCode</returns>
        public static async Task<object> HttpSendAndReceiveBytes([PropertyTab]ByteInput input, [PropertyTab] Options options, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClientForOptions(options);
            var headers = GetHeaderDictionary(input.Headers);

            using (var content = GetContent(input))
            {
                var responseMessage = await GetHttpRequestResponseAsync(
                    httpClient,
                    input.Method.ToString(),
                    input.Url,
                    content,
                    headers,
                    options,
                    cancellationToken)
                  .ConfigureAwait(false);

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

        // Combine response- and responsecontent header to one dictionary
        private static Dictionary<string, string> GetResponseHeaderDictionary(HttpResponseHeaders responseMessageHeaders, HttpContentHeaders contentHeaders)
        {
            var responseHeaders = responseMessageHeaders.ToDictionary(h => h.Key, h => string.Join(";", h.Value));
            var allHeaders = contentHeaders?.ToDictionary(h => h.Key, h => string.Join(";", h.Value)) ?? new Dictionary<string, string>();
            responseHeaders.ToList().ForEach(x => allHeaders[x.Key] = x.Value);
            return allHeaders;
        }

        private static IDictionary<string, string> GetHeaderDictionary(IEnumerable<Header> headers)
        {
            //Ignore case for headers and key comparison
            return headers.ToDictionary(key => key.Name, value => value.Value, StringComparer.InvariantCultureIgnoreCase);
        }

        private static async Task<HttpResponseMessage> GetHttpRequestResponseAsync(
            HttpClient httpClient, string method, string url,
            HttpContent content, IDictionary<string, string> headers,
            Options options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Only POST, PUT, PATCH and DELETE can have content, otherwise the HttpClient will fail
            var isContentAllowed = Enum.TryParse(method, ignoreCase: true, result: out SendMethod _);

            var request = new HttpRequestMessage(new HttpMethod(method), new Uri(url))
            {
                Content = isContentAllowed ? content : null,
            };

            //Clear default headers
            content.Headers.Clear();
            foreach (var header in headers)
            {
                var requestHeaderAddedSuccessfully = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (!requestHeaderAddedSuccessfully && request.Content != null)
                {
                    //Could not add to request headers try to add to content headers
                    // this check is probably not needed anymore as the new HttpClient does not seem fail on malformed headers
                    var contentHeaderAddedSuccessfully = content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    if (!contentHeaderAddedSuccessfully)
                    {
                        Trace.TraceWarning($"Could not add header {header.Key}:{header.Value}");
                    }
                }
            }

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException canceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Cancellation is from outside -> Just throw 
                    throw;
                }

                // Cancellation is from inside of the request, mostly likely a timeout
                throw new Exception("HttpRequest was canceled, most likely due to a timeout.", canceledException);
            }
            

            // this check is probably not needed anymore as the new HttpClient does not fail on invalid charsets
            if (options.AllowInvalidResponseContentTypeCharSet && response.Content.Headers?.ContentType != null)
            {
                response.Content.Headers.ContentType.CharSet = null;
            }
            return response;

        }

        private static HttpContent GetContent(Input input, IDictionary<string, string> headers)
        {
            //Check if Content-Type exists and is set and valid
            var contentTypeIsSetAndValid = false;
            MediaTypeWithQualityHeaderValue validContentType = null;
            if (headers.TryGetValue("content-type", out string contentTypeValue))
            {
                contentTypeIsSetAndValid = MediaTypeWithQualityHeaderValue.TryParse(contentTypeValue, out validContentType);
            }

            return contentTypeIsSetAndValid
                ? new StringContent(input.Message ?? "", Encoding.GetEncoding(validContentType.CharSet ?? Encoding.UTF8.WebName))
                : new StringContent(input.Message ?? "");
        }

        private static HttpContent GetContent(ByteInput input)
        {
            return new ByteArrayContent(input.ContentBytes);
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
        internal static void SetHandlerSettingsBasedOnOptions(this HttpClientHandler handler, Options options)
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
                        throw new ArgumentException(
                            $@"Username needs to be 'domain\username' now it was '{options.Username}'");
                    }

                    handler.Credentials =
                        new NetworkCredential(domainAndUserName[1], options.Password, domainAndUserName[0]);
                    break;
                case Authentication.ClientCertificate:
                    handler.ClientCertificates.AddRange(GetCertificates(options));
                    break;
            }

            handler.AllowAutoRedirect = options.FollowRedirects;
            handler.UseCookies = options.AutomaticCookieHandling;

            if (options.AllowInvalidCertificate)
            {
                handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            }
        }

        internal static void SetDefaultRequestHeadersBasedOnOptions(this HttpClient httpClient, Options options)
        {
            if (options.Authentication == Authentication.Basic || options.Authentication == Authentication.OAuth)
            {
                switch (options.Authentication)
                {
                    case Authentication.Basic:
                        httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue(
                                "Basic",
                                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}")));
                        break;
                    case Authentication.OAuth:
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
                        break;
                }
            }

            //Do not automatically set expect 100-continue response header
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json");
            httpClient.Timeout = TimeSpan.FromSeconds(Convert.ToDouble(options.ConnectionTimeoutSeconds));
        }

        private static X509Certificate[] GetCertificates(Options options)
        {
            X509Certificate2[] certificates;

            switch (options.ClientCertificateSource)
            {
                case CertificateSource.CertificateStore:
                    var thumbprint = options.CertificateThumbprint;
                    certificates = GetCertificatesFromStore(thumbprint, options.LoadEntireChainForCertificate);
                    break;
                case CertificateSource.File:
                    certificates = GetCertificatesFromFile(options.ClientCertificateFilePath, options.ClientCertificateKeyPhrase);
                    break;
                case CertificateSource.String:
                    certificates = GetCertificatesFromString(options.ClientCertificateInBase64, options.ClientCertificateKeyPhrase);
                    break;
                default:
                    throw new Exception("Unsupported Certificate source");
            }

            return certificates.Cast<X509Certificate>().ToArray();
        }

        private static X509Certificate2[] GetCertificatesFromString(string certificateContentsBase64, string keyPhrase)
        {
            var certificateBytes = Convert.FromBase64String(certificateContentsBase64);

            return LoadCertificatesFromBytes(certificateBytes, keyPhrase);
        }

        private static X509Certificate2[] LoadCertificatesFromBytes(byte[] certificateBytes, string keyPhrase)
        {
            var collection = new X509Certificate2Collection();

            if (!string.IsNullOrEmpty(keyPhrase))
            {
                collection.Import(certificateBytes, keyPhrase, X509KeyStorageFlags.PersistKeySet);
            }
            else
            {
                collection.Import(certificateBytes, null, X509KeyStorageFlags.PersistKeySet);
            }
            return collection.Cast<X509Certificate2>().OrderByDescending(c => c.HasPrivateKey).ToArray();

        }

        private static X509Certificate2[] GetCertificatesFromFile(string clientCertificateFilePath, string keyPhrase)
        {
            return LoadCertificatesFromBytes(File.ReadAllBytes(clientCertificateFilePath), keyPhrase);
        }

        private static X509Certificate2[] GetCertificatesFromStore(string thumbprint,
            bool loadEntireChain)
        {
            thumbprint = Regex.Replace(thumbprint, @"[^\da-zA-z]", string.Empty).ToUpper();
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var signingCert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                {
                    throw new FileNotFoundException(
                        $"Certificate with thumbprint: '{thumbprint}' not found in current user cert store.");
                }

                var certificate = signingCert[0];


                if (!loadEntireChain)
                {
                    return new [] {certificate};
                }

                var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.Build(certificate);

                // include the whole chain
                var certificates = chain
                    .ChainElements.Cast<X509ChainElement>()
                    .Select(c => c.Certificate)
                    .OrderByDescending(c => c.HasPrivateKey)
                    .ToArray();

                return certificates;
            }
            finally
            {
                store.Close();
            }
        }
    }
}
