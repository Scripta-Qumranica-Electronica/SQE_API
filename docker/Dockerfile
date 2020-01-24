# This Dockerfile builds the HTTP container.
# Note that you need to supply .. as the build context when building an image
FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY ./sqe-api-server/sqe-api-server.csproj ./sqe-api-server/
COPY ./sqe-database-access/sqe-database-access.csproj ./sqe-database-access/
COPY ./sqe-dto/sqe-dto.csproj ./sqe-dto/
COPY ./sqe-realtime-hub-builder/sqe-realtime-hub-builder.csproj ./sqe-realtime-hub-builder/

RUN dotnet restore "sqe-api-server/sqe-api-server.csproj"

WORKDIR /src/sqe-api-server

COPY ./sqe-api-server /src/sqe-api-server
COPY ./sqe-database-access /src/sqe-database-access
COPY ./sqe-dto /src/sqe-dto
COPY ./sqe-realtime-hub-builder /src/sqe-realtime-hub-builder

RUN dotnet build "sqe-api-server.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "sqe-api-server.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

# startup.sh picks up settings in environment variables and inserts them
# into appsettings.json before the program runs.
COPY ./startup.sh .
ENTRYPOINT ["/app/startup.sh", "dotnet", "sqe-api-server.dll"]