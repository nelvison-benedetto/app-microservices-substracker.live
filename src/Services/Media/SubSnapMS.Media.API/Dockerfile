# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/Services/Media/SubSnapMS.Media.API/Dockerfile -t subsnapms-media .
# Migrations: docker run --rm --network backend -e ConnectionStrings__MediaDb=... subsnapms-media --migrate

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SubSnapMS.Contracts/SubSnapMS.Contracts.csproj", "src/BuildingBlocks/SubSnapMS.Contracts/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/SubSnapMS.BuildingBlocks.Domain.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Application/SubSnapMS.BuildingBlocks.Application.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Application/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Persistence/SubSnapMS.BuildingBlocks.Persistence.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Persistence/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/SubSnapMS.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/SubSnapMS.BuildingBlocks.Web.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/"]
COPY ["src/Services/Media/SubSnapMS.Media.Core/SubSnapMS.Media.Core.csproj", "src/Services/Media/SubSnapMS.Media.Core/"]
COPY ["src/Services/Media/SubSnapMS.Media.API/SubSnapMS.Media.API.csproj", "src/Services/Media/SubSnapMS.Media.API/"]
RUN dotnet restore "src/Services/Media/SubSnapMS.Media.API/SubSnapMS.Media.API.csproj"

COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Media/", "src/Services/Media/"]
RUN dotnet publish "src/Services/Media/SubSnapMS.Media.API/SubSnapMS.Media.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.Media.API.dll"]