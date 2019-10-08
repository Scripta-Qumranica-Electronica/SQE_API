# This Dockerfile builds the HTTP container.
# Note that you need to supply .. as the build context when building an image
FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY ./sqe-http-server/sqe-http-server.csproj ./sqe-http-server/
COPY ./sqe-api-service/sqe-api-service.csproj ./sqe-api-service/
COPY ./sqe-database-access/sqe-database-access.csproj ./sqe-database-access/
COPY ./sqe-dto/sqe-dto.csproj ./sqe-dto/

RUN dotnet restore "sqe-http-server/sqe-http-server.csproj"

WORKDIR /src/sqe-http-server

COPY ./sqe-http-server /src/sqe-http-server
COPY ./sqe-api-service /src/sqe-api-service
COPY ./sqe-database-access /src/sqe-database-access
COPY ./sqe-dto /src/sqe-dto

RUN dotnet build "sqe-http-server.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "sqe-http-server.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

# startup.sh picks up settings in environment variables and inserts them
# into appsettings.json before the program runs.
COPY ./startup.sh .
ENTRYPOINT ["/app/startup.sh", "dotnet", "sqe-http-server.dll"]