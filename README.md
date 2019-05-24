# SQE_API

Asp.Net Core web API for the [SQE project](https://www.qumranica.org/). 

## Project structure

The overall project (or solution) uses several distinct components in two separate cs projects to process web requests.  The `sqe-http-api` project houses the `controllers`, which receive HTTP requests and route them to the `services`, which are responsible for managing the data access task and formatting the response using a `DTO`.  We still need to add a project with the `sqe-signalr-api`.

The heavy lifting of database access is accomplished in the `data-access` project.  This project uses `repositories` to build queries from a set of query string builders in `queries`.  All user data mutations can use the `helper` service `DatabaseWriter`, which wraps an array of database mutates in recorded transactions.  The `repositories` may wrap responses in a `model` or may return the data in a simpler form to the `services`.

We have used Dapper to simplify data access in the `repositories`.  It is beneficial to use the Async versions of its database access functions.  In fact, it is best practice to use async/await throughout the project.  It is also best practice to supply an actual class to the Dapper functions, rather than leaving generic return type as `object`, it is nice to place a `Return` class inside each `query` builder class corresponding to the anticipated return of the query built there, this ensures proper type mapping and helps other coders using intellisense.

### The data flow (and structure)

|User|
|----|
HTTP request (variables in path and JSON payload)

|Controller|
|----|
Controllers should route requests to `services` (passing necessary variables).  They should _not_ perform any other logic.

|Service|
|----|
Sends data requests to the low level database `repositories`.

|Repository|
|----|
Builds SQL queries from the `queries` string builders. Performs reads and writes and sends the data/status back to the `service`.

|Service|
|----|
Recieves data from the `repository` and packages it into a `DTO` for the `controller`.

|Controller|
|----|
Sends the `DTO` response back to the `user`.

|User|
|----|
Receives the `DTO` corresponding to the HTTP request.

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
Users with an invalid JWT are immediately sent 401 `Error: Unauthorized`.  The `controllers` can use this JWT to inject the user_id into any request with the convenience `IUserService.GetCurrentUserId()`.

## Dependency injection

Things fail silently when you don't properly setup dependency injections.  If you create a new `repository`, for instance, you must register it in `Startup.cs` under the function `ConfigureServices(IServiceCollection services)`.  Then you can inject it into your `service` classes with:

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


TODO: add more documentation here