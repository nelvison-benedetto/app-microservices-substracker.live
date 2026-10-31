# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/Services/Search/SubSnapMS.Search.API/Dockerfile -t subsnapms-search .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SubSnapMS.Contracts/SubSnapMS.Contracts.csproj", "src/BuildingBlocks/SubSnapMS.Contracts/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/SubSnapMS.BuildingBlocks.Domain.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/SubSnapMS.BuildingBlocks.Web.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/SubSnapMS.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/Services/Search/SubSnapMS.Search.API/SubSnapMS.Search.API.csproj", "src/Services/Search/SubSnapMS.Search.API/"]
RUN dotnet restore "src/Services/Search/SubSnapMS.Search.API/SubSnapMS.Search.API.csproj"

COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Search/", "src/Services/Search/"]
RUN dotnet publish "src/Services/Search/SubSnapMS.Search.API/SubSnapMS.Search.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.Search.API.dll"]