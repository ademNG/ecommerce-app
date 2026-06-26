# Dockerfile du service Gateway (YARP, .NET 10) — multi-étapes, runtime chiselé non-root.
# ⚠️ Contexte de build = racine du repo, donc les COPY commencent par `src/`.
#   docker build -t ecommerce-gateway:latest -f src/ECommerce.Gateway/Dockerfile .

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ECommerce.Gateway/ECommerce.Gateway.csproj src/ECommerce.Gateway/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Gateway/ECommerce.Gateway.csproj

COPY src/ECommerce.Gateway/ src/ECommerce.Gateway/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Gateway/ECommerce.Gateway.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# /health n'est mappé qu'en Development (cf. ServiceDefaults.MapDefaultEndpoints).
ENV ASPNETCORE_ENVIRONMENT=Development

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Gateway.dll"]
