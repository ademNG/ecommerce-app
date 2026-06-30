# Dockerfile du frontend Web (Blazor Server, .NET 10) — multi-étapes, runtime chiselé non-root.
# ⚠️ Contexte de build = racine du repo, donc les COPY commencent par `src/`.
#   docker build -t ecommerce-web:latest -f src/ECommerce.Web/Dockerfile .

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ECommerce.Web/ECommerce.Web.csproj src/ECommerce.Web/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Web/ECommerce.Web.csproj

COPY src/ECommerce.Web/ src/ECommerce.Web/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Web/ECommerce.Web.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# Development pour rester cohérent avec les API (pas de redirection HTTPS forcée en démo).
ENV ASPNETCORE_ENVIRONMENT=Development

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Web.dll"]
