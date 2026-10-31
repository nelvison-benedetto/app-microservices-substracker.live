# Build context: root della soluzione (SubSnapMS/)
#   docker build -f src/Services/Subscriptions/SubSnapMS.Subscriptions.API/Dockerfile -t subsnapms-subscriptions .
# Migrations (job una tantum, prima delle repliche):
#   docker run --rm --network backend -e ConnectionStrings__SubscriptionsDb=... subsnapms-subscriptions --migrate

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. File di root + soli .csproj: il restore resta in cache finché non cambiano le dipendenze
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/BuildingBlocks/SubSnapMS.Contracts/SubSnapMS.Contracts.csproj", "src/BuildingBlocks/SubSnapMS.Contracts/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/SubSnapMS.BuildingBlocks.Domain.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Domain/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Application/SubSnapMS.BuildingBlocks.Application.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Application/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Persistence/SubSnapMS.BuildingBlocks.Persistence.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Persistence/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/SubSnapMS.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/SubSnapMS.BuildingBlocks.Observability.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Observability/"]
COPY ["src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/SubSnapMS.BuildingBlocks.Web.csproj", "src/BuildingBlocks/SubSnapMS.BuildingBlocks.Web/"]
COPY ["src/Services/Subscriptions/SubSnapMS.Subscriptions.Core/SubSnapMS.Subscriptions.Core.csproj", "src/Services/Subscriptions/SubSnapMS.Subscriptions.Core/"]
COPY ["src/Services/Subscriptions/SubSnapMS.Subscriptions.Application/SubSnapMS.Subscriptions.Application.csproj", "src/Services/Subscriptions/SubSnapMS.Subscriptions.Application/"]
COPY ["src/Services/Subscriptions/SubSnapMS.Subscriptions.Infrastructure/SubSnapMS.Subscriptions.Infrastructure.csproj", "src/Services/Subscriptions/SubSnapMS.Subscriptions.Infrastructure/"]
COPY ["src/Services/Subscriptions/SubSnapMS.Subscriptions.API/SubSnapMS.Subscriptions.API.csproj", "src/Services/Subscriptions/SubSnapMS.Subscriptions.API/"]
RUN dotnet restore "src/Services/Subscriptions/SubSnapMS.Subscriptions.API/SubSnapMS.Subscriptions.API.csproj"

# 2. Sorgenti e publish
COPY ["src/BuildingBlocks/", "src/BuildingBlocks/"]
COPY ["src/Services/Subscriptions/", "src/Services/Subscriptions/"]
RUN dotnet publish "src/Services/Subscriptions/SubSnapMS.Subscriptions.API/SubSnapMS.Subscriptions.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SubSnapMS.Subscriptions.API.dll"]