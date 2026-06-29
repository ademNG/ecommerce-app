---
name: db-setup
description: >
  Installe et configure une base de données relationnelle PostgreSQL pour
  les services .NET de l'ecommerce-app (EF Core + Npgsql, migrations,
  relations entre entités, docker-compose, Helm). À utiliser dès que
  l'utilisateur mentionne : base de données, PostgreSQL, EF Core,
  migrations, relations (one-to-many, many-to-many), schéma, InMemory →
  PostgreSQL, DbContext, connexion base de données, data layer, entités
  liées — même s'il ne dit pas explicitement "db-setup".
---

# Skill : db-setup

Objectif : remplacer EF Core InMemory par **PostgreSQL** dans les services
.NET, créer les migrations, configurer les relations entre entités, et
mettre à jour docker-compose et le chart Helm — sans casser les tests
existants.

## Étapes à suivre

### 1. Identifier les services cibles
- Lister les projets qui ont un `DbContext` avec `UseInMemoryDatabase`.
- Confirmer avec l'utilisateur lesquels migrer (ex. Catalog.Api, Ordering.Api).

### 2. Ajouter le package Npgsql
Pour chaque service ciblé :
```bash
dotnet add src/<Service>/ECommerce.<Service>.csproj \
  package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 3. Configurer le DbContext
Dans `Program.cs`, remplacer :
```csharp
// Avant
options.UseInMemoryDatabase("catalog");

// Après
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
```

Garder `UseInMemoryDatabase` dans les projets de tests (`WebApplicationFactory`).

### 4. Définir les relations dans OnModelCreating
Configurer explicitement les relations dans `<Context>.cs` :

**One-to-many** (un Order contient plusieurs OrderItems) :
```csharp
modelBuilder.Entity<Order>()
    .HasMany(o => o.Items)
    .WithOne(i => i.Order)
    .HasForeignKey(i => i.OrderId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Many-to-many** (ex. produits ↔ catégories) :
```csharp
modelBuilder.Entity<Product>()
    .HasMany(p => p.Categories)
    .WithMany(c => c.Products)
    .UsingEntity("ProductCategory");
```

Supprimer `HasData` (seed) du `OnModelCreating` avant de créer les migrations
— les migrations et le seed ne font pas bon ménage avec `EnsureCreated`.

### 5. Créer les migrations EF Core
```bash
# Installer l'outil EF (une seule fois)
dotnet tool install --global dotnet-ef

# Créer la migration initiale
dotnet ef migrations add InitialCreate \
  --project src/ECommerce.<Service> \
  --startup-project src/ECommerce.<Service>

# Appliquer en dev (optionnel — docker-compose peut le faire au démarrage)
dotnet ef database update \
  --project src/ECommerce.<Service> \
  --startup-project src/ECommerce.<Service>
```

Appliquer les migrations au démarrage de l'app (recommandé en dev/preprod) :
```csharp
// Program.cs, après app.Build()
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
```

### 6. Mettre à jour docker-compose.yml
Ajouter le container PostgreSQL et les variables de connexion :
```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: ecommerce
      POSTGRES_PASSWORD: ecommerce
      POSTGRES_DB: ecommerce_db
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks: [ecommerce]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ecommerce"]
      interval: 5s
      retries: 5

  catalog:
    environment:
      ConnectionStrings__DefaultConnection: >
        Host=postgres;Database=catalog_db;Username=ecommerce;Password=ecommerce
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

### 7. Mettre à jour le chart Helm
Ajouter un Secret pour les credentials (jamais en clair dans les values) :

`helm/ecommerce/templates/secret-db.yaml` :
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: postgres-credentials
  namespace: {{ .Release.Namespace }}
type: Opaque
stringData:
  connection-string: {{ .Values.database.connectionString | required "database.connectionString is required" | quote }}
```

Dans `deployment.yaml`, injecter via `secretKeyRef` :
```yaml
env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: postgres-credentials
        key: connection-string
```

Dans `values.yaml` :
```yaml
database:
  connectionString: ""  # injecter via --set ou secret manager en CI/CD
```

### 8. Conserver InMemory dans les tests
Dans le projet de tests, surcharger `AddDbContext` :
```csharp
// WebApplicationFactory override
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        var descriptor = services.Single(
            d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
        services.Remove(descriptor);
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseInMemoryDatabase("TestCatalog"));
    });
}
```

## Garde-fous
- **Jamais de mot de passe en clair** dans un fichier commité — utiliser des variables d'environnement ou un Secret Kubernetes.
- **Confirmation humaine** avant `dotnet ef database update` sur un environnement partagé (risque de perte de données).
- **Migrations à commiter** dans le repo — elles font partie du code source.
- Ne jamais appeler `EnsureCreated()` si des migrations EF Core sont utilisées — les deux mécanismes sont incompatibles.
- Toujours vérifier `dotnet ef migrations list` avant d'appliquer pour détecter les migrations en attente.
