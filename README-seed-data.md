# Données de test — Ecommerce App

Guide pour l'équipe : comment peupler les bases `catalog_db` et `ordering_db`
avec des données de test réalistes.

---

## Contexte

L'application utilise deux bases PostgreSQL :

| Base | Tables | Description |
|------|--------|-------------|
| `catalog_db` | `Products` | Catalogue produits |
| `ordering_db` | `Orders`, `OrderItem` | Commandes clients |

**Relation clé :** chaque `OrderItem` référence un `ProductId` qui doit exister dans `catalog_db`.

---

## Prérequis

PostgreSQL et les services doivent tourner :

```bash
docker compose up -d
docker compose ps   # vérifier que postgres est "healthy"
```

---

## 1. Voir les données existantes

```bash
# Produits dans catalog
docker compose exec postgres psql -U ecommerce -d catalog_db \
  -c "SELECT \"Id\", \"Name\", \"Price\", \"AvailableStock\" FROM \"Products\" ORDER BY \"Id\";"

# Commandes dans ordering
docker compose exec postgres psql -U ecommerce -d ordering_db \
  -c "SELECT o.\"Id\", o.\"Customer\",
      CASE o.\"Status\" WHEN 0 THEN 'Pending' WHEN 1 THEN 'Confirmed'
                        WHEN 2 THEN 'Shipped' WHEN 3 THEN 'Cancelled' END AS \"Status\",
      COUNT(i.\"Id\") AS items,
      ROUND(SUM(i.\"UnitPrice\" * i.\"Quantity\")::numeric, 2) AS total
      FROM \"Orders\" o
      JOIN \"OrderItem\" i ON i.\"OrderId\" = o.\"Id\"
      GROUP BY o.\"Id\", o.\"Customer\", o.\"Status\"
      ORDER BY o.\"Id\";"
```

---

## 2. Ajouter des produits

### Via l'API REST (recommandé)

L'API valide les données et applique la logique métier :

```bash
curl -s -X POST http://localhost:8080/catalog/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Headset",
    "description": "7.1 surround sound",
    "price": 89.99,
    "availableStock": 45
  }'
```

### Via SQL direct

Plus rapide pour insérer plusieurs produits en une fois :

```bash
docker compose exec postgres psql -U ecommerce -d catalog_db -c "
INSERT INTO \"Products\" (\"Name\", \"Description\", \"Price\", \"AvailableStock\") VALUES
  ('Monitor Stand',   'Adjustable dual arm',    79.99,  25),
  ('Laptop Backpack', '17\" waterproof bag',    49.99,  90),
  ('Desk Fan',        'Silent USB 3-speed',     24.99,  70),
  ('LED Strip 5m',    'RGB WiFi controlled',    34.99, 110),
  ('Numeric Keypad',  'Slim wireless TKL',      39.99,  65);
SELECT \"Id\", \"Name\", \"Price\" FROM \"Products\" ORDER BY \"Id\" DESC LIMIT 5;"
```

---

## 3. Ajouter des commandes

### Via l'API REST (recommandé)

L'API vérifie automatiquement que les produits existent dans catalog :

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

> Les `productId` doivent exister dans `catalog_db.Products`.

### Via SQL direct

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

**Valeurs de Status :**

| Valeur | Signification |
|--------|---------------|
| `0` | Pending |
| `1` | Confirmed |
| `2` | Shipped |
| `3` | Cancelled |

---

## 4. Jeu de données complet (batch)

Insère 6 produits + 5 commandes en une seule commande :

```bash
# Produits
docker compose exec postgres psql -U ecommerce -d catalog_db -c "
INSERT INTO \"Products\" (\"Name\", \"Description\", \"Price\", \"AvailableStock\") VALUES
  ('Gaming Headset',   '7.1 surround sound',    89.99,  45),
  ('Webcam 4K',        '60fps autofocus',       129.99,  30),
  ('SSD 1TB NVMe',     'PCIe 4.0 M.2',          99.99,  80),
  ('Mouse Pad XL',     '90x40cm anti-slip',      19.99, 150),
  ('USB Microphone',   'Cardioid condenser',     59.99,  60),
  ('Numeric Keypad',   'Slim wireless TKL',      39.99,  65);"

# Commandes (adapter les ProductId selon les Ids réels dans ta base)
docker compose exec postgres psql -U ecommerce -d ordering_db -c "
WITH o1 AS (INSERT INTO \"Orders\" (\"Customer\",\"CreatedAt\",\"Status\") VALUES ('Sophie Bernard', NOW(), 2) RETURNING \"Id\")
INSERT INTO \"OrderItem\" (\"OrderId\",\"ProductId\",\"ProductName\",\"UnitPrice\",\"Quantity\")
SELECT \"Id\", 3, '4K Monitor', 329.00, 1 FROM o1 UNION ALL
SELECT \"Id\", 2, 'Wireless Mouse', 49.50, 1 FROM o1;

WITH o2 AS (INSERT INTO \"Orders\" (\"Customer\",\"CreatedAt\",\"Status\") VALUES ('Marc Leroy', NOW(), 1) RETURNING \"Id\")
INSERT INTO \"OrderItem\" (\"OrderId\",\"ProductId\",\"ProductName\",\"UnitPrice\",\"Quantity\")
SELECT \"Id\", 1, 'Mechanical Keyboard', 119.99, 2 FROM o2 UNION ALL
SELECT \"Id\", 4, 'USB-C Hub', 39.99, 3 FROM o2;"
```

---

## 5. Vérification finale

```bash
# Résumé catalog
docker compose exec postgres psql -U ecommerce -d catalog_db \
  -c "SELECT COUNT(*) AS total_produits FROM \"Products\";"

# Résumé ordering avec totaux
docker compose exec postgres psql -U ecommerce -d ordering_db \
  -c "SELECT o.\"Id\", o.\"Customer\",
      CASE o.\"Status\" WHEN 0 THEN 'Pending' WHEN 1 THEN 'Confirmed'
                        WHEN 2 THEN 'Shipped' WHEN 3 THEN 'Cancelled' END AS \"Status\",
      COUNT(i.\"Id\") AS items,
      ROUND(SUM(i.\"UnitPrice\" * i.\"Quantity\")::numeric, 2) AS total
      FROM \"Orders\" o
      JOIN \"OrderItem\" i ON i.\"OrderId\" = o.\"Id\"
      GROUP BY o.\"Id\", o.\"Customer\", o.\"Status\"
      ORDER BY o.\"Id\";"
```

---

## 6. Remettre à zéro

```bash
# Vider les commandes (supprime aussi les OrderItems par cascade)
docker compose exec postgres psql -U ecommerce -d ordering_db \
  -c "TRUNCATE \"Orders\" RESTART IDENTITY CASCADE;"

# Vider les produits (garder les 4 seedés par défaut)
docker compose exec postgres psql -U ecommerce -d catalog_db \
  -c "DELETE FROM \"Products\" WHERE \"Id\" > 4;"

# Ou tout supprimer et laisser les migrations re-seeder au redémarrage
docker compose down -v
docker compose up --build -d
```

---

## État actuel de la base (au 29/06/2026)

### catalog_db — 15 produits

| Id | Produit | Prix |
|----|---------|------|
| 1 | Mechanical Keyboard | 119.99 € |
| 2 | Wireless Mouse | 49.50 € |
| 3 | 4K Monitor | 329.00 € |
| 4 | USB-C Hub | 39.99 € |
| 5 | Gaming Headset | 89.99 € |
| 6 | Webcam 4K | 129.99 € |
| 7 | SSD 1TB NVMe | 99.99 € |
| 8 | Mouse Pad XL | 19.99 € |
| 9 | USB Microphone | 59.99 € |
| 10–15 | Monitor Stand, Laptop Backpack, Cable Manager, Desk Fan, LED Strip 5m, Numeric Keypad | … |

### ordering_db — 7 commandes

| Client | Statut | Total |
|--------|--------|-------|
| Alice Martin | Confirmed | 309.97 € |
| Bob Dupont | Pending | 459.96 € |
| Sophie Bernard | Shipped | 458.49 € |
| Marc Leroy | Confirmed | 249.97 € |
| Lucie Petit | Pending | 279.97 € |
| Pierre Moreau | Confirmed | 319.95 € |
| Emma Rousseau | Cancelled | 154.95 € |

---

## Accès rapide

| Service | URL / Commande |
|---------|----------------|
| Frontend web | http://localhost:8080 |
| pgAdmin | http://localhost:5050 (admin@example.com / admin) |
| API Catalog | http://localhost:8080/catalog/api/products |
| API Ordering | http://localhost:8080/ordering/api/orders |
