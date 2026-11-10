#syntax=docker/dockerfile:1

# build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
RUN test -n "$PROJECT" || (echo "Build arg PROJECT mancante" && exit 1)

WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/

RUN dotnet restore "$PROJECT"
RUN dotnet publish "$PROJECT" -c Release --no-restore -o /app/publish /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG ASSEMBLY
RUN test -n "$ASSEMBLY" || (echo "Build arg ASSEMBLY mancante" && exit 1)

ENV APP_DLL=${ASSEMBLY}.dll \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

WORKDIR /app
COPY --from=build /app/publish .

# utente non-root già presente nelle immagini .NET (uid 1654)
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]