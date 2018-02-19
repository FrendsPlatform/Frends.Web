# Frends.Web
FRENDS Tasks for HTTP REST requests.

- [Installing](#installing)
- [Tasks](#tasks)
    - [RestRequest](#restrequest)
    - [HttpRequest](#httprequest)
	- [HttpRequestBytes](#httprequestbytes)    
- [License](#license)
- [Building](#building)
- [Contributing](#contributing)

Installing
==========

You can install the task via FRENDS UI Task view or you can find the nuget package from the following nuget feed
`https://www.myget.org/F/frends/api/v2`

Tasks
=====

## RestRequest

The Web.RestRequest task is meant for simple JSON REST requests.  It only accepts a JSON response, and always add the `Accept : application/json` header. If you need more options, please use the HttpRequest task.

If the response is not valid JSON, the task will throw an JsonReaderException with the following message: "Unable to read response message as json: {response}". The task will still accept a response with an empty bodu, though, and return an empty string in that case.

Input:

| Property          | Type                               | Description                                             | Example                                   |
|-------------------|----------------------------------------|---------------------------------------------------------|-------------------------------------------|
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.  |  `Post`  |
| Url               | string                                 | The URL with protocol and path to call.  | `https://foo.example.org/path/to` `https://foo.example.org/path/to?Id=14` |
| Message           | string                             | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}` |
| Headers           | Array{Name: string, Value: string} | List of HTTP headers to be added to the request. Setting charset parameter encodes message.    | `Name = Content-Type, Value = application/json` |

Options:

| Property                         | Type                               | Description                                                         |
|----------------------------------|------------------------------------|---------------------------------------------------------------------|
| Authentication                   | Enum(None, Basic, Windows,WindowsIntegratedSecurity, OAuth, ClientCertificate ) | Different options for authentication for the HTTP request.   |
| Connection Timeout Seconds       | int                                | Timeout in seconds to be used for the connection and operation. Default is 30 seconds. |
| Follow Redirects                 | bool                               | If FollowRedirects is set to false, all responses with an HTTP status code from 300 to 399 is returned to the application. Default is true.|
| Allow Invalid Certificate        | bool                               | Do not throw an exception on certificate error. Setting this to true is discouraged in production. |
| Throw Exception On ErrorResponse | bool                               | Throw a WebException if return code of request is not successful.  |
| AllowInvalidResponseContentTypeCharSet | bool                         | Some Api's return faulty content-type charset header. If set to true this overrides the returned charset. |
| Username                         | string                             | This field is available for Basic- and Windows Authentication. If Windows Authentication is selected Username needs to be of format domain\username. Basic authentication will add a base64 encoded Authorization header consisting of Username and password fields. |
| Password                         | string                             | This field is available for Basic- and Windows Authentication.  |
| Token                            | string                             | Token to be used in an OAuth request. The token will be added as a Authentication header. `Authorization Bearer '{Token}'` |
| CertificateThumbprint            | string                             | This field is used with Client Certificate Authentication. The certificate needs to be found in Cert\CurrentUser\My store on the agent running the process |

Result:

| Property          | Type                      | Description          |
|-------------------|---------------------------|----------------------|
| Body              | JToken                    | Response body        |
| Headers           | Dictionary<string,string> | Response headers     |
| StatusCode        | int                       | Response status code | 


## HttpRequest
HttpRequest is a generic HTTP request task. It accepts any type of string response, e.g. application/xml or text/plain

Input:

| Property          | Type                               | Description                                                                | Example                                   |
|-------------------|----------------------------------------|------------------------------------------------------------------------|-------------------------------------------|
| Message           | string                                 | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}`             |
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.                                                | `Post`                                    |
| Url               | string                                 | The URL with protocol and path to call                                 | `https://foo.example.org/path/to?Id=14`   |
| Headers           | Array{Name: string, Value: string}     | List of HTTP headers to be added to the request.                       | `Name = Content-Type, Value = application/json` |

Options:

| Property                         | Type                               | Description                                                         |
|----------------------------------|------------------------------------|---------------------------------------------------------------------|
| Authentication                   | Enum(None, Basic, Windows,WindowsIntegratedSecurity, OAuth, ClientCertificate) | Options for authentication for the HTTP request. |
| Connection Timeout Seconds       | int                                | Timeout in seconds to be used for the connection and operation. Default is 30 seconds. |
| Follow Redirects                 | bool                               | If FollowRedirects is set to false, all responses with an HTTP status code from 300 to 399 is returned to the application. Default is true.|
| Allow Invalid Certificate        | bool                               | Do not throw an exception on certificate error. Setting this to true is discouraged in production. |
| Throw Exception On ErrorResponse | bool                               | Throw a WebException if return code of request is not successful.  |
| AllowInvalidResponseContentTypeCharSet | bool                         | Some Api's return faulty content-type charset header. If set to true this overrides the returned charset. |
| Username                         | string                             | This field is available for Basic- and Windows Authentication. If Windows Authentication is selected Username needs to be of format domain\username. Basic authentication will add a base64 encoded Authorization header consisting of Username and password fields. |
| Password                         | string                             | This field is available for Basic- and Windows Authentication.  |
| Token                            | string                             | Token to be used in an OAuth request. The token will be added as a Authentication header. `Authorization Bearer '{Token}'` |
| CertificateThumbprint            | string                             | This field is used with Client Certificate Authentication. The certificate needs to be found in Cert\CurrentUser\My store on the agent running the process |

Result:

| Property          | Type                      | Description             |
|-------------------|---------------------------|-------------------------|
| Body              | string                    | Response body as string |
| Headers           | Dictionary<string,string> | Response headers        |
| StatusCode        | int                       | Response status code    | 

## HttpRequestBytes
HttpRequest is a generic HTTP request task that returns the raw response body as a byte array.

Input and Options parameters are the same as with the [HttpRequest](#httprequest) task.

Result:

| Property          | Type                                         | Description                                                                                                                   |
|-------------------|----------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| BodyBytes         | byte[]                                       | Response body as a byte array                                                                                                 |
| ContentType		| System.Net.Http.Headers.MediaTypeHeaderValue | The parsed media type header from the response, so you can e.g. get the media type string by `#result.ContentType.MediaType`  |
| Headers           | Dictionary<string,string>                    | Response headers, with multiple values for the same header combined by semicolons (;)                                         |
| StatusCode        | int                                          | Response status code                                                                                                          | 

License
=======
This project is licensed under the MIT License - see the LICENSE file for details

Building
========

Clone a copy of the repo

`git clone https://github.com/FrendsPlatform/Frends.Web.git`

Restore dependencies

`nuget restore frends.web`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.Web.Tests\bin\Release\Frends.Web.Tests.dll`

Create a nuget package

`nuget pack nuspec/Frends.Web.nuspec`

Contributing
============
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!
