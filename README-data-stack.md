# Data Stack — Ecommerce App

Guide complet : de l'installation de PostgreSQL jusqu'au dashboard Grafana.

---

## Vue d'ensemble

```
docker-compose
├── postgres      (port 5432)  ← moteur PostgreSQL 16
├── pgadmin       (port 5050)  ← interface web d'administration
├── grafana       (port 3000)  ← dashboards de reporting
├── catalog       (port interne 8080)  ← API .NET → catalog_db
├── ordering      (port interne 8080)  ← API .NET → ordering_db
├── gateway       (port interne 8080)  ← reverse proxy YARP
└── web           (port 8080)  ← frontend Blazor

Bases de données
├── catalog_db    ← produits du catalogue
├── ordering_db   ← commandes clients
└── reporting_db  ← data warehouse (star schema)
```

---

## Étape 1 — Lancer la stack

```bash
cd ecommerce-app
docker compose up --build -d
```

Cette commande :
- Build les images .NET (Catalog, Ordering, Gateway, Web)
- Démarre PostgreSQL, pgAdmin, Grafana
- Applique les migrations EF Core (crée les tables automatiquement)
- Seede les 4 produits par défaut dans `catalog_db`

Vérifier que tout tourne :

```bash
docker compose ps
```

Résultat attendu — tous les services `Up` :

```
postgres    Up (healthy)   0.0.0.0:5432->5432/tcp
pgadmin     Up             0.0.0.0:5050->80/tcp
grafana     Up             0.0.0.0:3000->3000/tcp
catalog     Up             8080/tcp
ordering    Up             8080/tcp
gateway     Up             8080/tcp
web         Up             0.0.0.0:8080->8080/tcp
```

---

## Étape 2 — Bases de données opérationnelles

Les migrations EF Core créent automatiquement les bases et les tables au démarrage.

### catalog_db

```
Products
├── Id              (int, PK, auto-increment)
├── Name            (varchar 200, requis)
├── Description     (varchar 2000)
├── Price           (decimal 18,2)
└── AvailableStock  (int)
```

### ordering_db

```
Orders
├── Id          (int, PK, auto-increment)
├── Customer    (varchar 200, requis)
├── CreatedAt   (timestamptz)
└── Status      (int : 0=Pending, 1=Confirmed, 2=Shipped, 3=Cancelled)

OrderItem
├── Id           (int, PK, auto-increment)
├── OrderId      (int, FK → Orders.Id, CASCADE DELETE)  ← relation
├── ProductId    (int)
├── ProductName  (varchar 200, requis)
├── UnitPrice    (decimal 18,2)
└── Quantity     (int)
```

**Relation :** `Orders` → `OrderItem` = one-to-many avec cascade delete.

Vérifier les bases :

```bash
docker compose exec postgres psql -U ecommerce -d postgres -c "\l"
```

---

## Étape 3 — pgAdmin (interface d'administration)

### Accès

Ouvre **http://localhost:5050**

| Champ    | Valeur              |
|----------|---------------------|
| Email    | `admin@example.com` |
| Password | `admin`             |

### Enregistrer le serveur PostgreSQL

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

### Navigation dans les tables

```
Servers → ecommerce → Databases
  ├── catalog_db  → Schemas → public → Tables → Products
  ├── ordering_db → Schemas → public → Tables → Orders / OrderItem
  └── reporting_db → Schemas → public → Materialized Views
```

> Si les bases n'apparaissent pas : clic droit **Databases** → **Refresh**

---

## Étape 4 — Données de test

### Voir l'état actuel

```bash
# Produits
docker compose exec postgres psql -U ecommerce -d catalog_db \
  -c "SELECT \"Id\", \"Name\", \"Price\" FROM \"Products\" ORDER BY \"Id\";"

# Commandes
docker compose exec postgres psql -U ecommerce -d ordering_db \
  -c "SELECT o.\"Id\", o.\"Customer\",
      CASE o.\"Status\" WHEN 0 THEN 'Pending' WHEN 1 THEN 'Confirmed'
                        WHEN 2 THEN 'Shipped'  WHEN 3 THEN 'Cancelled' END AS \"Status\",
      COUNT(i.\"Id\") AS items,
      ROUND(SUM(i.\"UnitPrice\" * i.\"Quantity\")::numeric, 2) AS total
      FROM \"Orders\" o JOIN \"OrderItem\" i ON i.\"OrderId\" = o.\"Id\"
      GROUP BY o.\"Id\", o.\"Customer\", o.\"Status\" ORDER BY o.\"Id\";"
```

### Ajouter des produits via API

```bash
curl -s -X POST http://localhost:8080/catalog/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Gaming Headset","description":"7.1 surround sound","price":89.99,"availableStock":45}'
```

### Ajouter des produits via SQL

```bash
docker compose exec postgres psql -U ecommerce -d catalog_db -c "
INSERT INTO \"Products\" (\"Name\", \"Description\", \"Price\", \"AvailableStock\") VALUES
  ('Gaming Headset',   '7.1 surround sound',    89.99,  45),
  ('Webcam 4K',        '60fps autofocus',       129.99,  30),
  ('SSD 1TB NVMe',     'PCIe 4.0 M.2',          99.99,  80),
  ('Mouse Pad XL',     '90x40cm anti-slip',      19.99, 150),
  ('USB Microphone',   'Cardioid condenser',     59.99,  60);"
```

### Ajouter des commandes via API

```bash
curl -s -X POST http://localhost:8080/ordering/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customer": "Sophie Bernard",
    "items": [
      {"productId": 1, "quantity": 2},
      {"productId": 3, "quantity": 1}
    ]
  }'
```

### Ajouter des commandes via SQL

```bash
docker compose exec postgres psql -U ecommerce -d ordering_db -c "
WITH new_order AS (
  INSERT INTO \"Orders\" (\"Customer\", \"CreatedAt\", \"Status\")
  VALUES ('Marc Leroy', NOW(), 1) RETURNING \"Id\"
)
INSERT INTO \"OrderItem\" (\"OrderId\", \"ProductId\", \"ProductName\", \"UnitPrice\", \"Quantity\")
SELECT \"Id\", 1, 'Mechanical Keyboard', 119.99, 2 FROM new_order
UNION ALL
SELECT \"Id\", 2, 'Wireless Mouse',       49.50, 1 FROM new_order;"
```

---

## Étape 5 — Data Warehouse (reporting_db)

### Architecture star schema

```
reporting_db
│
├── ft_products    (foreign table → catalog_db.Products)
├── ft_orders      (foreign table → ordering_db.Orders)
├── ft_order_items (foreign table → ordering_db.OrderItem)
│
├── dim_products   (dimension produits)
├── dim_customers  (dimension clients)
├── dim_date       (dimension dates)
└── fact_orders    (table de faits — jointure orders + items + produits)
```

La base `reporting_db` interroge `catalog_db` et `ordering_db` via **postgres_fdw**
(foreign data wrapper) — aucune duplication de données.

### Créer le data warehouse (si pas encore fait)

```bash
# 1. Créer la base
docker compose exec postgres psql -U ecommerce -d postgres \
  -c "CREATE DATABASE reporting_db OWNER ecommerce;"

# 2. Configurer postgres_fdw et les vues matérialisées
docker compose exec postgres psql -U ecommerce -d reporting_db << 'EOF'
CREATE EXTENSION IF NOT EXISTS postgres_fdw;

CREATE SERVER local_pg FOREIGN DATA WRAPPER postgres_fdw
  OPTIONS (host 'localhost', port '5432', dbname 'catalog_db');
CREATE SERVER local_pg_ordering FOREIGN DATA WRAPPER postgres_fdw
  OPTIONS (host 'localhost', port '5432', dbname 'ordering_db');

CREATE USER MAPPING FOR ecommerce SERVER local_pg
  OPTIONS (user 'ecommerce', password 'ecommerce');
CREATE USER MAPPING FOR ecommerce SERVER local_pg_ordering
  OPTIONS (user 'ecommerce', password 'ecommerce');

CREATE FOREIGN TABLE ft_products (
  "Id" integer, "Name" varchar(200), "Description" varchar(2000),
  "Price" numeric(18,2), "AvailableStock" integer
) SERVER local_pg OPTIONS (schema_name 'public', table_name 'Products');

CREATE FOREIGN TABLE ft_orders (
  "Id" integer, "Customer" varchar(200),
  "CreatedAt" timestamptz, "Status" integer
) SERVER local_pg_ordering OPTIONS (schema_name 'public', table_name 'Orders');

CREATE FOREIGN TABLE ft_order_items (
  "Id" integer, "OrderId" integer, "ProductId" integer,
  "ProductName" varchar(200), "UnitPrice" numeric(18,2), "Quantity" integer
) SERVER local_pg_ordering OPTIONS (schema_name 'public', table_name 'OrderItem');

CREATE MATERIALIZED VIEW dim_products AS
SELECT "Id" AS product_id, "Name" AS product_name,
       "Price" AS unit_price, "AvailableStock" AS available_stock
FROM ft_products;

CREATE MATERIALIZED VIEW dim_customers AS
SELECT DISTINCT ROW_NUMBER() OVER (ORDER BY "Customer") AS customer_id,
       "Customer" AS customer_name FROM ft_orders;

CREATE MATERIALIZED VIEW fact_orders AS
SELECT oi."Id" AS order_item_id, o."Id" AS order_id,
       o."Customer" AS customer_name, DATE(o."CreatedAt") AS order_date,
       o."CreatedAt" AS order_datetime,
       CASE o."Status" WHEN 0 THEN 'Pending' WHEN 1 THEN 'Confirmed'
                       WHEN 2 THEN 'Shipped'  WHEN 3 THEN 'Cancelled' END AS order_status,
       oi."ProductId" AS product_id, oi."ProductName" AS product_name,
       oi."UnitPrice" AS unit_price, oi."Quantity" AS quantity,
       (oi."UnitPrice" * oi."Quantity") AS line_total,
       p."AvailableStock" AS available_stock
FROM ft_orders o
JOIN ft_order_items oi ON oi."OrderId" = o."Id"
LEFT JOIN ft_products p ON p."Id" = oi."ProductId";

CREATE INDEX ON fact_orders (order_date);
CREATE INDEX ON fact_orders (order_status);
CREATE INDEX ON fact_orders (product_id);
EOF
```

### Rafraîchir les vues après ajout de données

Les vues matérialisées ne se mettent pas à jour automatiquement.
À lancer après chaque batch de données :

```bash
docker compose exec postgres psql -U ecommerce -d reporting_db -c "
REFRESH MATERIALIZED VIEW dim_products;
REFRESH MATERIALIZED VIEW dim_customers;
REFRESH MATERIALIZED VIEW fact_orders;"
```

### Requêtes de reporting utiles

```sql
-- Chiffre d'affaires total (hors annulés)
SELECT ROUND(SUM(line_total)::numeric, 2) AS ca_total
FROM fact_orders WHERE order_status != 'Cancelled';

-- Top 5 produits les plus vendus
SELECT product_name, SUM(quantity) AS qte_vendue,
       ROUND(SUM(line_total)::numeric, 2) AS ca
FROM fact_orders WHERE order_status != 'Cancelled'
GROUP BY product_name ORDER BY qte_vendue DESC LIMIT 5;

-- CA par client
SELECT customer_name, COUNT(DISTINCT order_id) AS nb_commandes,
       ROUND(SUM(line_total)::numeric, 2) AS ca_total
FROM fact_orders WHERE order_status != 'Cancelled'
GROUP BY customer_name ORDER BY ca_total DESC;

-- Répartition des statuts
SELECT order_status, COUNT(DISTINCT order_id) AS nb_commandes
FROM fact_orders GROUP BY order_status;
```

---

## Étape 6 — Grafana (dashboards)

### Accès

Ouvre **http://localhost:3000**

| Champ    | Valeur  |
|----------|---------|
| Username | `admin` |
| Password | `admin` |

### Trouver le dashboard

**Dashboards → Ecommerce → Ecommerce Dashboard**

### Panels disponibles

| Panel | Type | Description |
|-------|------|-------------|
| Chiffre d'affaires total | Stat | CA cumulé hors annulés |
| Nombre de commandes | Stat | Total des commandes |
| Produits en catalogue | Stat | Nombre de produits |
| Panier moyen | Stat | Moyenne par commande |
| Répartition des statuts | Camembert | Pending / Confirmed / Shipped / Cancelled |
| Top 5 produits | Barchart | Les plus vendus en quantité |
| CA par client | Tableau | Classement clients par CA |
| Commandes récentes | Tableau | 20 dernières commandes |

### La datasource est préconfigurée automatiquement

La connexion à `reporting_db` est provisionnée via :
`grafana/provisioning/datasources/postgres.yml`

Le dashboard est chargé automatiquement depuis :
`grafana/dashboards/ecommerce.json`

---

## Résumé des accès

| Service    | URL                   | Login                        |
|------------|-----------------------|------------------------------|
| App Web    | http://localhost:8080 | —                            |
| pgAdmin    | http://localhost:5050 | admin@example.com / admin    |
| Grafana    | http://localhost:3000 | admin / admin                |
| PostgreSQL | localhost:5432        | ecommerce / ecommerce        |

---

## Commandes utiles

```bash
# Démarrer toute la stack
docker compose up --build -d

# Arrêter sans perdre les données
docker compose down

# Arrêter ET supprimer toutes les données
docker compose down -v

# Voir les logs d'un service
docker compose logs -f catalog
docker compose logs -f grafana

# Ouvrir un shell SQL
docker compose exec postgres psql -U ecommerce -d catalog_db
docker compose exec postgres psql -U ecommerce -d ordering_db
docker compose exec postgres psql -U ecommerce -d reporting_db
```
