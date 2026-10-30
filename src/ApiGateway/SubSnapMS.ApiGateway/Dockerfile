# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/ApiGateway/SubSnapMS.ApiGateway/Dockerfile -t subsnapms-api-gateway .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. File di root + soli .csproj: il restore resta in cache finché non cambiano le dipendenze
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/ApiGateway/SubSnapMS.ApiGateway/SubSnapMS.ApiGateway.csproj", "src/ApiGateway/SubSnapMS.ApiGateway/"]
RUN dotnet restore "src/ApiGateway/SubSnapMS.ApiGateway/SubSnapMS.ApiGateway.csproj"

# 2. Sorgenti e publish
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/ApiGateway/SubSnapMS.ApiGateway/", "src/ApiGateway/SubSnapMS.ApiGateway/"]
RUN dotnet publish "src/ApiGateway/SubSnapMS.ApiGateway/SubSnapMS.ApiGateway.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Utente non-root già presente nelle immagini .NET 8+
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.ApiGateway.dll"]