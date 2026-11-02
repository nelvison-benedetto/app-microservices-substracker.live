# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/Services/Graph/SubSnapMS.Graph.API/Dockerfile -t subsnapms-graph .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/BuildingBlocks/SubSnapMS.Contracts/SubSnapMS.Contracts.csproj", "src/BuildingBlocks/SubSnapMS.Contracts/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/SubSnapMS.BuildingBlocks.Domain.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/SubSnapMS.BuildingBlocks.Web.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/SubSnapMS.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/Services/Graph/SubSnapMS.Graph.API/SubSnapMS.Graph.API.csproj", "src/Services/Graph/SubSnapMS.Graph.API/"]
RUN dotnet restore "src/Services/Graph/SubSnapMS.Graph.API/SubSnapMS.Graph.API.csproj"

COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Graph/", "src/Services/Graph/"]
RUN dotnet publish "src/Services/Graph/SubSnapMS.Graph.API/SubSnapMS.Graph.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.Graph.API.dll"]