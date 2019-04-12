FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["sqe-http-api/sqe-http-api.csproj", "sqe-http-api/"]
RUN dotnet restore "sqe-http-api/sqe-http-api.csproj"
COPY . .
WORKDIR "/src/sqe-http-api"
RUN dotnet build "sqe-http-api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "sqe-http-api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "sqe-http-api.dll"]