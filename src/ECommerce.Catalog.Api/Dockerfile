# Dockerfile du service Catalog — prérequis de l'UC5 (Terraform) et de la CI.
# Build multi-étapes : SDK pour compiler, runtime CHISELÉ non-root pour l'exécution.
# ⚠️ Contexte de build = racine du repo, donc les COPY commencent par `src/`.
#   docker build -t ecommerce-catalog:latest -f src/ECommerce.Catalog.Api/Dockerfile .

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# .csproj d'abord (Catalog + sa dépendance ServiceDefaults) → cache Docker : le restore
# n'est rejoué que si un .csproj change.
COPY src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj src/ECommerce.Catalog.Api/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj

# Puis le code source, et publication en Release.
COPY src/ECommerce.Catalog.Api/ src/ECommerce.Catalog.Api/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
# L'image "chiseled" ne contient ni shell ni gestionnaire de paquets → plus petite, plus sûre.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# /health et /alive ne sont mappés qu'en Development (cf. ServiceDefaults.MapDefaultEndpoints).
# Sans ça, la vérification `curl .../health` de l'UC5 renverrait 404.
ENV ASPNETCORE_ENVIRONMENT=Development

# $APP_UID est défini par l'image .NET : on exécute en utilisateur NON-root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Catalog.Api.dll"]
