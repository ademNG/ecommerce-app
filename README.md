# ECommerce — application e-commerce .NET 10 (microservices + interface web Blazor)

Une application **e-commerce complète** : un **catalogue de produits**, un **panier**, le
**passage de commandes** et leur **suivi**, le tout avec une **interface web** et une **API
REST**. Elle est construite en **.NET 10 / ASP.NET Core** et orchestrée par **.NET Aspire**.

Tout démarre avec **une seule commande** et **sans installer de base de données** (les données
vivent en mémoire). Ce guide est pensé pour être suivi **même sans connaître .NET**.

---

## 1. Ce que fait l'application

### Côté interface web (Blazor)
- **Catalogue** : afficher la liste des produits (nom, description, prix, stock).
- **Panier** : ajouter des produits, ajuster les quantités.
- **Commande** : saisir un nom de client et **passer la commande**.
- **Suivi** : consulter l'historique des commandes.

### Côté technique (sous le capot)
- **Architecture microservices** : l'application est découpée en services indépendants.
- **Passerelle unique (gateway)** : un seul point d'entrée route le trafic vers les services.
- **Validation inter-services** : à la création d'une commande, chaque produit est **vérifié
  en temps réel** auprès du service Catalogue (un service appelle l'autre).
- **Tableau de bord d'observabilité** (dashboard Aspire) : voir en direct les **logs**, les
  **traces** des appels entre services, et les **métriques**.
- **Base de données en mémoire** : aucune installation, démarrage immédiat.

---

## 2. Architecture en un coup d'œil

```
        ┌───────────────────────────────┐
        │   Web (interface Blazor)       │   ← ce que voit l'utilisateur
        └───────────────┬───────────────┘
                        │  HTTP
                ┌───────▼────────┐
                │ Gateway (YARP) │             ← point d'entrée unique
                └───┬────────┬───┘
          /catalog  │        │  /ordering
           ┌────────▼──┐  ┌──▼───────────┐
           │ Catalog   │◄─┤ Ordering     │     ← Ordering appelle Catalog
           │ (produits)│  │ (commandes)  │       pour valider les produits
           └───────────┘  └──────────────┘
            base mémoire    base mémoire
```

| Projet | Rôle |
|---|---|
| **ECommerce.AppHost** | L'**orchestrateur** : c'est LE projet qu'on lance. Il démarre tous les services et le tableau de bord. |
| **ECommerce.Web** | L'**interface web** (Blazor). Pages Catalogue, Panier, Commandes. |
| **ECommerce.Gateway** | La **passerelle** (reverse proxy YARP). Route `/catalog/*` et `/ordering/*` vers les bons services. |
| **ECommerce.Catalog.Api** | Le service **Catalogue** : gère les produits (API REST + base en mémoire). |
| **ECommerce.Ordering.Api** | Le service **Commandes** : gère les commandes, et **appelle Catalog** pour valider chaque produit. |
| **ECommerce.ServiceDefaults** | Configuration **commune** à tous les services : observabilité, contrôles de santé, résilience réseau, découverte de services. |

> Les services ne connaissent jamais l'adresse exacte des autres : elle est **résolue
> automatiquement à l'exécution** par la *découverte de services* d'Aspire, à partir des noms
> définis dans `AppHost.cs` (ex. `https+http://catalog`).

---

## 3. Prérequis et installation

Il faut **une seule chose** : le **SDK .NET 10**.
👉 **Docker n'est PAS nécessaire** (l'application n'utilise aucun conteneur ; le tableau de
bord tourne directement dans le programme).

### 3.1 Vérifier / installer le SDK .NET 10

Dans un terminal :

```bash
dotnet --version
```

- Si le numéro commence par `10.` (ex. `10.0.301`) → c'est bon.
- Sinon, installez le **SDK** (pas seulement le « Runtime ») :
  - Téléchargement : <https://dotnet.microsoft.com/download/dotnet/10.0>
  - macOS (Homebrew) : `brew install dotnet-sdk`

> **Sur ce poste**, le SDK est dans `/usr/local/share/dotnet` mais n'est pas dans le PATH par
> défaut. À refaire dans **chaque nouveau terminal** :
> ```bash
> export PATH="/usr/local/share/dotnet:$PATH"
> dotnet --version   # doit afficher 10.x
> ```

### 3.2 Faire confiance au certificat HTTPS de développement (une seule fois)

**Étape requise.** Sans elle, le tableau de bord Aspire ne peut pas ouvrir sa connexion
sécurisée et le terminal se remplit d'erreurs `UntrustedRoot`.

```bash
dotnet dev-certs https --trust
```

(macOS peut demander le mot de passe du trousseau : approuvez.)

---

## 4. Lancer l'application

Placez-vous **à la racine du projet** (le dossier qui contient `ECommerce.slnx`) :

```bash
# (sur ce poste, d'abord :  export PATH="/usr/local/share/dotnet:$PATH" )

dotnet run --project src/ECommerce.AppHost
```

La première fois, .NET télécharge les dépendances et compile (quelques dizaines de secondes).
Ensuite, le terminal affiche plusieurs URL. **Ouvrez celle nommée `Login URL`** (elle contient
un jeton `?t=...` qui vous connecte directement au tableau de bord) :

```
Dashboard:  https://localhost:<port>
Login URL:  https://localhost:<port>/login?t=<jeton>   <-- celle-ci
OTLP/gRPC:  https://localhost:<autre-port>             <-- NE PAS ouvrir (telemetrie interne)
```

> ⚠️ Le **port change à chaque lancement** (ex. `17089`, `17042`…). Utilisez toujours l'URL
> affichée dans **votre** terminal, jamais un port fixe. La ligne `OTLP/gRPC` n'est pas une
> page web : ne l'ouvrez pas.

C'est le **tableau de bord Aspire**, qui liste tous les services (`catalog`, `ordering`,
`gateway`, `web`) avec leur état et leurs URL.

**Pour tout arrêter : `Ctrl+C`** dans le terminal qui exécute l'AppHost.

---

## 5. Utiliser l'application

Depuis le tableau de bord :

1. Cliquez sur la ressource **`web`** → l'**interface e-commerce** s'ouvre.
2. Allez sur **Catalog** : la liste des produits s'affiche. **Ajoutez** des produits au panier.
3. Saisissez un **nom de client** puis **Place order** (passer commande).
4. Ouvrez **Orders** : votre commande apparaît dans l'historique.
5. (Bonus) Onglets **Traces** / **Metrics** du tableau de bord : observez en direct l'appel
   `Ordering → Catalog` qui valide les produits au moment de la commande.

---

## 6. L'API REST (via la passerelle)

L'interface web n'est qu'un client parmi d'autres : tout passe par la **passerelle**. Vous
pouvez appeler l'API directement (l'adresse exacte de la ressource `gateway` est indiquée dans
le tableau de bord).

| Méthode | Route | Description |
|---|---|---|
| `GET` | `/catalog/api/products` | Liste des produits |
| `GET` | `/catalog/api/products/{id}` | Un produit (404 si absent) |
| `POST` | `/catalog/api/products` | Créer un produit |
| `GET` | `/ordering/api/orders` | Liste des commandes |
| `GET` | `/ordering/api/orders/{id}` | Une commande (404 si absente) |
| `POST` | `/ordering/api/orders` | Créer une commande (valide les produits via Catalog) |

Exemple — passer une commande :

```bash
curl -X POST http://<gateway>/ordering/api/orders \
  -H "Content-Type: application/json" \
  -d '{ "customer": "Ada Lovelace", "items": [ { "productId": 1, "quantity": 2 } ] }'
```

> En mode développement, chaque service expose aussi sa description OpenAPI sur
> `/openapi/v1.json`.

---

## 7. Structure du projet & rôle des fichiers

```
.
├── ECommerce.slnx                  ← la "solution" : liste les 6 projets
└── src/
    ├── ECommerce.AppHost/          ← l'orchestrateur (LE projet à lancer)
    │   └── AppHost.cs              ← déclare les services et leur ordre de démarrage
    ├── ECommerce.Web/              ← l'interface web Blazor
    │   ├── Components/Pages/       ← les pages (Home, Products, Orders)
    │   ├── Services/               ← clients HTTP vers la passerelle
    │   └── Program.cs              ← configuration de l'interface
    ├── ECommerce.Gateway/          ← la passerelle (reverse proxy YARP)
    │   ├── appsettings.json        ← les règles de routage /catalog et /ordering
    │   └── Program.cs
    ├── ECommerce.Catalog.Api/      ← service Catalogue (produits)
    │   ├── Models/Product.cs       ← la "forme" d'un produit
    │   ├── Data/                   ← la base + les produits de démo
    │   ├── Endpoints/              ← les routes /api/products
    │   └── Program.cs
    ├── ECommerce.Ordering.Api/     ← service Commandes
    │   ├── Models/Order.cs         ← la "forme" d'une commande
    │   ├── Services/               ← le client qui appelle Catalog pour valider
    │   ├── Endpoints/              ← les routes /api/orders
    │   └── Program.cs
    └── ECommerce.ServiceDefaults/  ← configuration commune à tous les services
        └── Extensions.cs           ← observabilité, santé, résilience, découverte
```

> Chaque projet contient un petit `README.md` qui détaille ses fichiers.
> Après le premier lancement, des dossiers `bin/` et `obj/` apparaissent : ce sont des
> **fichiers générés** par la compilation (on ne les modifie ni ne les versionne).

### Quelques mots de vocabulaire

| Terme | En clair |
|---|---|
| **.NET / SDK** | La plateforme de Microsoft et sa boîte à outils (commande `dotnet`). |
| **ASP.NET Core** | La partie de .NET pour faire des sites et des API web. |
| **Blazor** | La technologie pour construire l'interface web (les pages) en C#. |
| **API REST** | Un service qui répond à des requêtes HTTP (`GET`, `POST`…). |
| **EF Core** | L'outil qui relie le code C# à la base de données. |
| **Gateway / reverse proxy** | Un service qui reçoit toutes les requêtes et les redirige vers le bon service interne. |
| **.NET Aspire** | L'outil qui démarre tous les services ensemble et fournit le tableau de bord. |
| **Découverte de services** | Le mécanisme qui retrouve l'adresse d'un service par son **nom** plutôt que par une adresse codée en dur. |

---

## 8. Bon à savoir

- **Les données ne sont pas persistées.** Les bases vivent en mémoire : les produits/commandes
  créés disparaissent au prochain redémarrage, et les produits de démonstration reviennent.
  Pour persister, on remplacerait la base en mémoire par PostgreSQL ou SQLite.
- **Communication entre services en HTTP synchrone**, jamais via des adresses codées en dur :
  elles sont résolues par la découverte de services à partir des noms de `AppHost.cs`.

---

## 9. En cas de problème

| Symptôme | Solution |
|---|---|
| Le tableau de bord affiche `UntrustedRoot` / erreur SSL | Le certificat HTTPS n'est pas approuvé. Arrêtez (`Ctrl+C`), lancez `dotnet dev-certs https --trust`, relancez (étape 3.2). |
| `command not found: dotnet` | Le SDK n'est pas dans le PATH → étape 3.1 (`export PATH=...`). |
| Un port est déjà utilisé | Une exécution précédente tourne encore. Fermez-la avec `Ctrl+C`, puis relancez. |
| La page d'un service direct (sans passer par la passerelle) affiche une erreur sur `/` | Normal : les services n'ont pas de page d'accueil. Utilisez l'interface `web` ou les routes `/api/...`. |
| La commande (`POST /ordering/api/orders`) renvoie une erreur 400 | Le produit demandé n'existe pas, ou la commande est vide. Vérifiez les `productId` envoyés. |
