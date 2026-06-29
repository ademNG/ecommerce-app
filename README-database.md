# Base de données PostgreSQL — Ecommerce App

Ce document explique comment installer, configurer et tester la base de données PostgreSQL pour les services `Catalog.Api` et `Ordering.Api`.

---

## Architecture

```
docker-compose
├── postgres          ← PostgreSQL 16 (port 5432)
├── pgadmin           ← Interface web pgAdmin (port 5050)
├── catalog           ← API Catalog → base catalog_db
├── ordering          ← API Ordering → base ordering_db
├── gateway           ← YARP reverse proxy
└── web               ← Frontend Blazor (port 8080)
```

### Schéma des bases

**`catalog_db`**
```
Products
├── Id              (int, PK, auto-increment)
├── Name            (varchar 200, required)
├── Description     (varchar 2000)
├── Price           (decimal 18,2)
└── AvailableStock  (int)
```

**`ordering_db`**
```
Orders
├── Id          (int, PK, auto-increment)
├── Customer    (varchar 200, required)
├── CreatedAt   (timestamptz)
└── Status      (int : 0=Pending, 1=Confirmed, 2=Shipped, 3=Cancelled)

OrderItem
├── Id           (int, PK, auto-increment)
├── OrderId      (int, FK → Orders.Id, CASCADE DELETE)
├── ProductId    (int)
├── ProductName  (varchar 200, required)
├── UnitPrice    (decimal 18,2)
└── Quantity     (int)
```

**Relation :** `Orders` → `OrderItem` = **one-to-many** avec cascade delete.

---

## Prérequis

- Docker Desktop installé et démarré
- Se placer dans le dossier `ecommerce-app/`

---

## 1. Lancer la stack complète

```bash
docker compose up --build -d
```

Cette commande :
1. Build les images .NET (Catalog, Ordering, Gateway, Web)
2. Démarre PostgreSQL
3. Applique automatiquement les migrations EF Core (crée les tables)
4. Seede les 4 produits dans `catalog_db`
5. Démarre pgAdmin

---

## 2. Lancer uniquement PostgreSQL et pgAdmin

```bash
docker compose up -d postgres pgadmin
```

---

## 3. Accès pgAdmin (interface web)

Ouvre dans le navigateur : **http://localhost:5050**

| Champ    | Valeur              |
|----------|---------------------|
| Email    | `admin@example.com` |
| Password | `admin`             |

### Enregistrer le serveur PostgreSQL dans pgAdmin

1. Clic droit **Servers** → **Register** → **Server**
2. Onglet **General** → Name : `ecommerce`
3. Onglet **Connection** :

| Champ                | Valeur      |
|----------------------|-------------|
| Host name/address    | `postgres`  |
| Port                 | `5432`      |
| Maintenance database | `postgres`  |
| Username             | `ecommerce` |
| Password             | `ecommerce` |

4. Coche **Save password** → **Save**

### Naviguer dans les tables

```
Servers → ecommerce → Databases
  ├── catalog_db → Schemas → public → Tables → Products
  └── ordering_db → Schemas → public → Tables → Orders / OrderItem
```

> Si les bases n'apparaissent pas : clic droit **Databases** → **Refresh**

---

## 4. Connexion directe via psql

```bash
# Lister toutes les bases
docker compose exec postgres psql -U ecommerce -d postgres -c "\l"

# Se connecter à catalog_db
docker compose exec postgres psql -U ecommerce -d catalog_db

# Se connecter à ordering_db
docker compose exec postgres psql -U ecommerce -d ordering_db
```

---

## 5. Tester les tables et les relations

Ouvre le **Query Tool** dans pgAdmin : sélectionne la base → menu **Tools** → **Query Tool**

### catalog_db — Voir les produits

```sql
SELECT * FROM "Products";
```

Résultat attendu : 4 produits seedés (Mechanical Keyboard, Wireless Mouse, 4K Monitor, USB-C Hub).

### ordering_db — Créer un ordre avec items

```sql
-- 1. Créer un ordre
INSERT INTO "Orders" ("Customer", "CreatedAt", "Status")
VALUES ('Jean Dupont', NOW(), 0)
RETURNING "Id";

-- 2. Ajouter des items (remplacer 1 par l'Id retourné)
INSERT INTO "OrderItem" ("OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity")
VALUES (1, 1, 'Mechanical Keyboard', 119.99, 2),
       (1, 2, 'Wireless Mouse', 49.50, 1);

-- 3. Voir l'ordre avec ses items (JOIN)
SELECT o."Id", o."Customer", o."Status",
       i."ProductName", i."Quantity", i."UnitPrice",
       (i."Quantity" * i."UnitPrice") AS "Subtotal"
FROM "Orders" o
JOIN "OrderItem" i ON i."OrderId" = o."Id";
```

### Tester la cascade delete

```sql
-- Supprimer l'ordre
DELETE FROM "Orders" WHERE "Id" = 1;

-- Vérifier que les items sont supprimés automatiquement
SELECT * FROM "OrderItem" WHERE "OrderId" = 1;
-- Résultat attendu : 0 lignes
```

---

## 6. Variables de connexion (docker-compose)

| Service   | Variable                                    | Valeur                                                              |
|-----------|---------------------------------------------|---------------------------------------------------------------------|
| catalog   | `ConnectionStrings__DefaultConnection`      | `Host=postgres;Database=catalog_db;Username=ecommerce;Password=ecommerce` |
| ordering  | `ConnectionStrings__DefaultConnection`      | `Host=postgres;Database=ordering_db;Username=ecommerce;Password=ecommerce` |

---

## 7. Migrations EF Core

Les migrations sont appliquées automatiquement au démarrage de chaque service (`Database.Migrate()`).

Pour créer une nouvelle migration après modification du modèle :

```bash
# Catalog
dotnet ef migrations add <NomMigration> \
  --project src/ECommerce.Catalog.Api \
  --startup-project src/ECommerce.Catalog.Api

# Ordering
dotnet ef migrations add <NomMigration> \
  --project src/ECommerce.Ordering.Api \
  --startup-project src/ECommerce.Ordering.Api
```

Pour lister les migrations appliquées :

```bash
dotnet ef migrations list --project src/ECommerce.Catalog.Api --startup-project src/ECommerce.Catalog.Api
```

---

## 8. Arrêter la stack

```bash
# Arrêter sans supprimer les données
docker compose down

# Arrêter ET supprimer les données PostgreSQL
docker compose down -v
```

---

## Accès rapide

| Service    | URL                       |
|------------|---------------------------|
| Web Blazor | http://localhost:8080      |
| pgAdmin    | http://localhost:5050      |
| PostgreSQL | localhost:5432             |
