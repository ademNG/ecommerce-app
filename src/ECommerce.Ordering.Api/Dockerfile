# Dockerfile du service Ordering (.NET 10) — multi-étapes, runtime chiselé non-root.
# ⚠️ Contexte de build = racine du repo, donc les COPY commencent par `src/`.
#   docker build -t ecommerce-ordering:latest -f src/ECommerce.Ordering.Api/Dockerfile .

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj src/ECommerce.Ordering.Api/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj

COPY src/ECommerce.Ordering.Api/ src/ECommerce.Ordering.Api/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# /health n'est mappé qu'en Development (cf. ServiceDefaults.MapDefaultEndpoints).
ENV ASPNETCORE_ENVIRONMENT=Development

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Ordering.Api.dll"]
