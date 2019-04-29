FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY sqe-signalr-api.csproj sqe-signalr-api/
RUN dotnet restore "sqe-signalr-api/sqe-signalr-api.csproj"

COPY . sqe-signalr-api/

WORKDIR "/src/sqe-signalr-api"
RUN dotnet build "sqe-signalr-api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "sqe-signalr-api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "sqe-signalr-api.dll"]