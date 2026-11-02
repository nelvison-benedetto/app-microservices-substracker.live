# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/Services/Notification/SubSnapMS.Notification.API/Dockerfile -t subsnapms-notification .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/BuildingBlocks/SubSnapMS.Contracts/SubSnapMS.Contracts.csproj", "src/BuildingBlocks/SubSnapMS.Contracts/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/SubSnapMS.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/Services/Notification/SubSnapMS.Notification.API/SubSnapMS.Notification.API.csproj", "src/Services/Notification/SubSnapMS.Notification.API/"]
RUN dotnet restore "src/Services/Notification/SubSnapMS.Notification.API/SubSnapMS.Notification.API.csproj"

COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Notification/", "src/Services/Notification/"]
RUN dotnet publish "src/Services/Notification/SubSnapMS.Notification.API/SubSnapMS.Notification.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.Notification.API.dll"]