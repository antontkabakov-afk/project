FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY server.csproj ./
RUN dotnet restore ./server.csproj

COPY . .
RUN dotnet publish ./server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
  CMD curl --fail http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "server.dll"]
