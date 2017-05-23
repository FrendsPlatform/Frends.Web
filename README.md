- [Task documentation](#task-documentation)
   - [Web.RestRequest](#web.restrequest)
     - [Input](#input)
     - [Options](#options)
     - [Result](#result)
   - [Web.HttpRequest](#web.httprequest)
     - [Input](#input)
     - [Options](#options)
     - [Result](#result)
# Task documentation

## Web.RestRequest
This task will always add a Accept : application/json header. The task only accepts a json response. If you want to do more customized implementations please use HttpRequest task.
If the response is not valid json the task will throw an JsonReaderException with the following message: "Unable to read response message as json: {response}". The task will still accept a response that is empty and will return a empty string for body
### Input
| Property          | Type                               | Description                                             | Example                                   |
|-------------------|----------------------------------------|---------------------------------------------------------|-------------------------------------------|
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.  |  `Post`  |
| Url               | string                                 | The URL with protocol and path to call.  | `https://foo.example.org/path/to` `https://foo.example.org/path/to?Id=14` |
| Message           | string                             | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}` |
| Headers           | Array{Name: string, Value: string} | List of HTTP headers to be added to the request.     | `Name = Content-Type, Value = application/json` |

### Options
| Property                         | Type                               | Description                                                         |
|----------------------------------|------------------------------------|---------------------------------------------------------------------|
| Authentication                   | Enum(None, Basic, Windows,WindowsIntegratedSecurity, OAuth ) | Different options for authentication for the HTTP request.   |
| Connection Timeout Seconds       | int                                | Timeout in seconds to be used for the connection and operation. Default is 30 seconds. |
| Follow Redirects                 | bool                               | If FollowRedirects is set to false, all responses with an HTTP status code from 300 to 399 is returned to the application. Default is true.|
| Allow Invalid Certificate        | bool                               | Do not throw an exception on certificate error. Setting this to true is discouraged in production. |
| Throw Exception On ErrorResponse | bool                               | Throw a WebException if return code of request is not successful.  |
| AllowInvalidResponseContentTypeCharSet | bool                         | Some Api's return faulty content-type charset header. If set to true this overrides the returned charset. |
| Username                         | string                             | This field is available for Basic- and Windows Authentication. If Windows Authentication is selected Username needs to be of format domain\username. Basic authentication will add a base64 encoded Authorization header consisting of Username and password fields. |
| Password                         | string                             | This field is available for Basic- and Windows Authentication.  |
| Token                            | string                             | Token to be used in an OAuth request. The token will be added as a Authentication header. `Authorization Bearer '{Token}'` |

### Result
The result will be a Object with the following properties:

JToken Body - Response Body

Dictionary<string,string> Headers - Response Headers

int StatusCode - Response status code

## Web.HttpRequest
HttpRequest accepts any type of string response. E.g. xml and csv

### Input
| Property          | Type                               | Description                                             | Example                                   |
|-------------------|----------------------------------------|---------------------------------------------------------|-------------------------------------------|
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.  |  `Post`  |
| Url               | string                                 | The URL with protocol and path to call | `https://foo.example.org/path/to` `https://foo.example.org/path/to?Id=14` |
| Message           | string                             | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}` |
| Headers           | Array{Name: string, Value: string} | List of HTTP headers to be added to the request.     | `Name = Content-Type, Value = application/json` |

### Options
| Property                         | Type                               | Description                                                         |
|----------------------------------|------------------------------------|---------------------------------------------------------------------|
| Authentication                   | Enum(None, Basic, Windows,WindowsIntegratedSecurity, OAuth ) | Options for authentication for the HTTP request.   |
| Connection Timeout Seconds       | int                                | Timeout in seconds to be used for the connection and operation. Default is 30 seconds. |
| Follow Redirects                 | bool                               | If FollowRedirects is set to false, all responses with an HTTP status code from 300 to 399 is returned to the application. Default is true.|
| Allow Invalid Certificate        | bool                               | Do not throw an exception on certificate error. Setting this to true is discouraged in production. |
| Throw Exception On ErrorResponse | bool                               | Throw a WebException if return code of request is not successful.  |
| AllowInvalidResponseContentTypeCharSet | bool                         | Some Api's return faulty content-type charset header. If set to true this overrides the returned charset. |
| Username                         | string                             | This field is available for Basic- and Windows Authentication. If Windows Authentication is selected Username needs to be of format domain\username. Basic authentication will add a base64 encoded Authorization header consisting of Username and password fields. |
| Password                         | string                             | This field is available for Basic- and Windows Authentication.  |
| Token                            | string                             | Token to be used in an OAuth request. The token will be added as a Authentication header. `Authorization Bearer '{Token}'` |

### Result
The result will be a Object with the following properties:

string Body - Response Body

Dictionary<string,string> Headers - Response Headers

int StatusCode - Response status code
