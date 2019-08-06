# This Dockerfile builds the HTTP container.
# Note that you need to supply .. as the build context when building an image
FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY ./sqe-api/sqe-api.csproj ./sqe-api/
COPY ./data-access/data-access.csproj ./data-access/

RUN dotnet restore "sqe-api/sqe-api.csproj"

WORKDIR /src/sqe-api

COPY ./sqe-api /src/sqe-api
COPY ./data-access /src/data-access

RUN dotnet build "sqe-api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "sqe-api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

# startup.sh picks up settings in environment variables and inserts them
# into appsettings.json before the program runs.
COPY ./startup.sh .
ENTRYPOINT ["/app/startup.sh", "dotnet", "sqe-api.dll"]