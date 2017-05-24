- [Frends.Web](#frends.web)
   - [Installing](#installing)
   - [Building](#building)
   - [Contributing](#contributing)
   - [Documentation](#documentation)
     - [Web.RestRequest](#web.restrequest)
       - [Input](#input)
       - [Options](#options)
       - [Result](#result)
     - [Web.HttpRequest](#web.httprequest)
       - [Input](#input)
       - [Options](#options)
       - [Result](#result)
   - [License](#license)
       
# Frends.Web
FRENDS Task for Web and HTTP related adapters.

## Installing
You can install the task via FRENDS UI Task view or you can find the nuget package from the following nuget feed
`https://www.myget.org/F/frends/api/v2`

## Building
Ensure that you have `https://www.myget.org/F/frends/api/v2` added to your nuget feeds

Clone a copy of the repo

`git clone https://github.com/FrendsPlatform/Frends.Web.git`

Restore dependencies

`nuget restore frends.web`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.Web.Tests\bin\Release\Frends.Web.Tests.dll`

Create a nuget package

`nuget pack nuspec/Frends.Web.nuspec`

## Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes
NOTE: Be sure to merge the latest from "upstream" before making a pull request!

## Documentation

### Web.RestRequest
This task will always add a Accept : application/json header. The task only accepts a json response. If you want to do more customized implementations please use HttpRequest task.
If the response is not valid json the task will throw an JsonReaderException with the following message: "Unable to read response message as json: {response}". The task will still accept a response that is empty and will return a empty string for body
#### Input
| Property          | Type                               | Description                                             | Example                                   |
|-------------------|----------------------------------------|---------------------------------------------------------|-------------------------------------------|
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.  |  `Post`  |
| Url               | string                                 | The URL with protocol and path to call.  | `https://foo.example.org/path/to` `https://foo.example.org/path/to?Id=14` |
| Message           | string                             | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}` |
| Headers           | Array{Name: string, Value: string} | List of HTTP headers to be added to the request.     | `Name = Content-Type, Value = application/json` |

#### Options
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

#### Result
The result will be a Object with the following properties:

JToken Body - Response Body

Dictionary<string,string> Headers - Response Headers

int StatusCode - Response status code

### Web.HttpRequest
HttpRequest accepts any type of string response. E.g. xml and csv

#### Input
| Property          | Type                               | Description                                             | Example                                   |
|-------------------|----------------------------------------|---------------------------------------------------------|-------------------------------------------|
| Method            | Enum(Get, Post, Put, Patch, Delete)    | Http method of request.  |  `Post`  |
| Url               | string                                 | The URL with protocol and path to call | `https://foo.example.org/path/to` `https://foo.example.org/path/to?Id=14` |
| Message           | string                             | The message to be sent with the request. Not used for Get requests     | `{"Name" : "Adam", "Age":42}` |
| Headers           | Array{Name: string, Value: string} | List of HTTP headers to be added to the request.     | `Name = Content-Type, Value = application/json` |

#### Options
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

#### Result
The result will be a Object with the following properties:

string Body - Response Body

Dictionary<string,string> Headers - Response Headers

int StatusCode - Response status code

## License

This project is licensed under the MIT License - see the LICENSE file for details

