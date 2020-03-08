# SQE_API

[master](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/master/): [![Build Status](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API.svg?branch=master)](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API) [development](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/development/): [![Build Status](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API.svg?branch=development)](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API) [staging](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/staging/): [![Build Status](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API.svg?branch=staging)](https://travis-ci.org/Scripta-Qumranica-Electronica/SQE_API)

[master](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/master/): [![Coverage Status](https://coveralls.io/repos/github/Scripta-Qumranica-Electronica/SQE_API/badge.svg?branch=master)](https://coveralls.io/github/Scripta-Qumranica-Electronica/SQE_API?branch=master) [development](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/development/): [![Coverage Status](https://coveralls.io/repos/github/Scripta-Qumranica-Electronica/SQE_API/badge.svg?branch=development)](https://coveralls.io/github/Scripta-Qumranica-Electronica/SQE_API?branch=development) [staging](https://github.com/Scripta-Qumranica-Electronica/SQE_API/tree/staging/): [![Coverage Status](https://coveralls.io/repos/github/Scripta-Qumranica-Electronica/SQE_API/badge.svg?branch=staging)](https://coveralls.io/github/Scripta-Qumranica-Electronica/SQE_API?branch=staging)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Scripta-Qumranica-Electronica/SQE_API/blob/master/LICENSE.txt)

This repository contains the Asp.Net Core web API for the [SQE project](https://www.qumranica.org/). It exposes access to the [SQE database](https://github.com/Scripta-Qumranica-Electronica/SQE_Database) via the HTTP protocol and the realtime SignalR transport.

## Getting Started with the API

### Prerequisites
A running instance of the [SQE database](https://github.com/Scripta-Qumranica-Electronica/SQE_Database). The database can be easily spun up using the prebuilt [dockerhub image](https://hub.docker.com/repository/docker/qumranica/sqe-database). This can be done with `docker run` or as part of a `docker-compose.yml` file (see the example [docker-compose.yml](https://github.com/Scripta-Qumranica-Electronica/SQE_API/blob/integration-tests/docker/deploy/docker-compose.yml)). The SQE API does rely on using a compatible version of the SQE database (check [docker-compose.yml](https://github.com/Scripta-Qumranica-Electronica/SQE_API/blob/integration-tests/docker/deploy/docker-compose.yml) under `services` → `sqe-database` → `image` for the proper version to use).

### Running the API

The SQE_API project is generally intended to be run from docker images. For quick startup and for production deployment, you can use a prebuilt docker container from our docker hub [repository](https://hub.docker.com/repository/docker/qumranica/sqe-http-api). The docker container can be run either with `docker run` or with `docker-compose`, which is preferred. 

#### Setup

The container can receive several environment variables to configure project settings. You can set these as environment variables using the -e switch and `docker run`, or within a docker-compose file (see the example [docker-compose.yml](./docker/deploy/docker-compose.yml)). The possible settings are as follows:

Database connection settings (with default values):

*   MYSQL_ROOT_PASSWORD=none
*   MYSQL_DATABASE=SQE_DEV
*   MYSQL_HOST=sqe-database
*   MYSQL_PORT=3306
*   MYSQL_USER=sqe_user
*   MYSQL_PASSWORD=mysecretpw

Opt out of dotnet telemetry if you wish:

*   DOTNET_CLI_TELEMETRY_OPTOUT=1

Setup email acount for user management emails:

*   MAILER_EMAIL_ADDRESS
*   MAILER_EMAIL_USERNAME
*   MAILER_EMAIL_PASSWORD
*   MAILER_EMAIL_SMTP_URL
*   MAILER_EMAIL_SMTP_PORT
*   MAILER_EMAIL_SMTP_SECURITY (The options are "None", "Auto", "SslOnConnect", "StartTls", and "StartTlsWhenAvailable", the default setting is "StartTlsWhenAvailable")
*   WEBSITE_HOST (the url of the current running instance of [ScrollEditor](https://github.com/Scripta-Qumranica-Electronica/ScrollEditor))

Provide some random string as a secret that is used to generate the JWT's

*   SQE_API_SECRET

Set logging levels, valid values are: Verbose, Debug, Information, Warning, Error, Fatal

* API_LOGLEVEL (Log level for API generated messages)
* DOTNET_LOGLEVEL (Log level for dotnet core messages and Microsoft tooling)
* SYSTEM_LOGLEVEL (Log level for system level messages)

Set Redis SignalR backplane settings for horizontal scaling of the realtime API (disabled by default)

* USE_REDIS (Use a Redis backplane to horizontally scale the realtime API)
* REDIS_HOST
* REDIS_PORT
* REDIS_PASSWORD

Set server options
* Http_Server (Run the API HTTP server in addition to the SignalR realtime server)

These environment variables automatically rewrite the settings `sqe-http-api/appsettings.json` when starting up the docker container. (The docker container runs a small [startup script](./startup.sh) every time it starts up, which reads the evironment variables and injects them into the settings file.)

If you are running the API directly from these source files, probably for development purposes, then you can insert your settings in sqe-http-api/appsettings.json before running the project.

This project has a custom githook in .githooks that will ensure no sensitive data from `docker-compose.yml` or `sqe-http-api/appsettings.json` is transmitted to GitHub when pushing commits. If you want to benefit from this, you must run `git config core.hooksPath .githooks` in the project's root to enable the checks.

### Typescript Interoperability

The data returned by the SQE API is formatted according to strictly structured data transfer objects. Typescript definitions for all data transfer objects are automatically generated from the C# code and are provide in a single file [`ts-dtos/sqe-dtos.ts`](./ts-dtos/sqe-dtos.ts). If you wish to use the SignalR transport, a convenience class is available in [`ts-dtos/sqe-signalr.ts`](./ts-dtos/sqe-signalr.ts), from which `SignalRUtilities` can be imported.  That class receives a `HubConnection` in its constructor and can then be used to call all available server side methods, which are conveniently documented in JSDoc.

# Development

Since this project is fully opensources, we appreciate any community contributions. The project is complex, and relies upon a good understanding of the SQE database structure, since we use custom SQL queries with [Dapper](https://github.com/StackExchange/Dapper) for all database interactions. In addition, to support both HTTP and SignalR requests in a consistent way with a single codebase, a helper project was required `sqe-realtime-hub-builder`.

## Project Structure

The complete solution consists of several individual projects:

* [sqe-api-server](./sqe-api-server) (The web server)
* [sqe-database-access](./sqe-database-access) (The code for database interactions)
* [sqe-dto](./sqe-dto) (Class definitions for data transfer objects)
* [sqe-api-test](./sqe-api-test) (Integration tests for the SQE API)
* [sqe-realtime-hub-builder](./sqe-realtime-hub-builder) (Code that uses Roslyn code analysis to autogenerate SignalR hub methods that mirror the HTTP endpoints)
* [GenerateTypescriptDTOs](./Utilities/GenerateTypescriptDTOs) (Code to generate typescript DTO's from the classes in `sqe-dto`)
* [GenerateTypescriptInterfaces](./Utilities/GenerateTypescriptInterfaces) (Code to generate a typescript convenience class for SignalR hub methods in `sqe-api-server/RealtimeHubs`)

The `sqe-api-server` project houses the `HttpControllers`, which receive HTTP requests. These controllers are thin methods that must do nothing more that route each request to a `services` in `Services`, then returns the response. The `sqe-realtime-hub-builder` uses the Roslyn compiler to analyze the `HttpControllers`; it validates that the endpoints in each controller conform to our architectural standards and then it creates corresponding SignalR Hub methods for them. This way the HTTP server and realyime SignalR server are kept in sync with each other. 

The `Services` are called directly by both the HTTP controllers and by the SignalR Hub methods. These services process the request and its accomanying data by making any necessary calls to the data repositories in `sqe-database-access` and then packaging the response for delivery to the client. They also use the SignalR functionality to broadcast all data mutation requests to the relevant clients (this is done regardless of whether the mutation request originated from an HTTP or SignalR client).

The heavy lifting of database access is accomplished in the `sqe-database-access` project.  This project uses `repositories` to build queries from a set of query string builders in `Queries`.  All user data mutations can use the `helper` service `DatabaseWriter`, which wraps an array of database mutation requests into recorded transactions.  The `repositories` may wrap responses in one of the `Models` (which Dapper uses to serialize database responses) or may return the data in a simpler form to the `Services` in the `sqe-api-server`.

We have used Dapper to simplify data access in the `repositories`.  It is beneficial to use the Async versions of its database access functions.  In fact, it is best practice to use async/await throughout the project.  It is also best practice to supply an actual class to the Dapper functions, rather than leaving generic return type as `object`; it can be nice to place a `Return` class inside each `query` builder class corresponding to the anticipated return of the query built there, this ensures proper type mapping and helps other coders using intellisense.

### The data flow (and structure)

|User|
|----|
HTTP request (variables in path and JSON payload) / SignalR requests (variables in method call)

|Controller/Hub|
|----|
Controllers (and the autogenerated Hub methods) should route requests directly to `services` (passing necessary variables) and directly return the response (preferably in a single liner, e.g., `return await _service.ProcessRequestAsync(routeParam, bodyObject);`).  They must _**not**_ perform any other logic.

|Service|
|----|
Parses any input DTO's and sends data requests to the low level database `repositories`.

|Repository|
|----|
Builds SQL queries from the `queries` string builders. Performs reads and writes and sends the data/status back to the `service`.

|Service|
|----|
Recieves data from the `repository` and packages it into a `DTO` for the `controller` or `hub method`.

|Controller/Hub|
|----|
Sends the `DTO` response back to the `user`.

|User|
|----|
Receives the `DTO` corresponding to the HTTP/SignalR request.

## Security

The HTTP API uses JWT to maintain security.  This means authentication need only occur once, and will persist for future visits to the site without need for another login.  The JWT is an encrypted set of assertions that is sent in the `Authorize` header of every request as a `Bearer` token (using HTTP2 allows the header to be sent once for all requests in a single session).  For us the JWT stores little more than the user_id and name:

```JSON
{
  "unique_name": "test",
  "nameid": "5",
  "nbf": 1555051740,
  "exp": 1555656540,
  "iat": 1555051740
}
```
Users with an invalid JWT are immediately sent a 401 `Error: Unauthorized`.  The `controllers` should usually pass this information on by means of the `_userService.GetCurrentUserObjectAsync` convenience method, which can be used for all manner of permission checking..

## Dependency injection

Things fail silently when you don't properly setup dependency injections.  If you create a new `repository`, for instance, you must register it in `Startup.cs` under the method `ConfigureServices(IServiceCollection services)`.  Then you can inject it into your `service` classes with:

```C#
public interface IArtefactService
    {
        Task<ArtefactDTO> GetArtefact(uint scrollVersionId);
    }

    public class ArtefactService : IArtefactService
    {
		IArtefactRepository _artefactRepository; // Set type for injected dependency
		
		public ArtefactService(IArtefactRepository artefactRepository)
		{
		    _artefactRepository = artefactRepository; // Inject it in class constructor
		}
		
		public Task<ArtefactDTO> GetArtefact(uint scrollVersionId)
		{
			// return "something using _artefactRepository";
		}
	}
}
```

## Integration tests

The integration tests require a properly set up, compatible version of the SQE database to be running (check [docker-compose.yml](https://github.com/Scripta-Qumranica-Electronica/SQE_API/blob/integration-tests/docker/deploy/docker-compose.yml) under `services` → `sqe-database` → `image` for the proper version to use).

Tests are run from the `*Test.cs` files at the root of the `sqe-api-test` project.  The tests use Xunit, and each one should be a subclass of `WebControllerTest`.  As a subclass of `WebControllerTest` each test class will have access to a variable `_client`, which is an HttpClient that can be used for requests to the API's HTTP server.  It will also have a `StartConnectionAsync` method that will return a HubConnection for SignalR API requests.

Each method tagged with `[Fact]` in these testing classes will be run and all `Assert` statements will need to pass for the test to complete successfully. 

The best way to run the tests is to set up the details of each API endpoint in the ApiRequests class.  That class uses partials and its functions will be spread across several files in the `ApiRequests` folder.  Please refer to them for further documentation and the basic structure. The top level classes will be either Get, Post, Put, or Delete. Within those each endpoint has its own class (a subclass of RequestObject<Tinput, Toutput>), which should be given the same name as the endpoint path, but with _ instead of / (see below) and using Pascal case (e.g., `v1/editions/{editionId}/add-editor-request` becomes `V1_Editions_EditionId_AddEditorRequest`).

Each subclassed `RequestObject` can be passed to the `Send` function in the `Request` Helper class, for example:
```c#
var arts = new ApiRequests.ApiRequests.Get.V1_Editions_EditionId_Artefacts(894);
var (httpStatus, httpResponse, realtimeResponse, listenerResponse) = await Request.Send(arts, http: _client, 
    realtime: StartConnectionAsync, listener: false, auth: true, user1: null, user2: null, shouldSucceed: true,
    deterministic: true);
```
See the documentation in `Request.Send` for further information about all its options. 

Each test will consist of one or more calls to `Request.Send` along with an analysis of the response using Assert.  It can be advantageous to build helper functions for common actions that are reused in several testing scripts.

TODO: add more documentation here