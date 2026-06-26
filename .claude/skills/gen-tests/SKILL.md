---
name: gen-tests
description: Génère des tests pour l'ecommerce-app (.NET) — tests unitaires xUnit et tests d'intégration WebApplicationFactory/Testcontainers. Vise les cas limites, n'invente pas de comportement, et laisse l'humain valider la couverture.
---

# Skill : gen-tests

Objectif : produire des **tests utiles et lisibles**, pas juste « du test pour faire du chiffre ».

## Étapes à suivre

1. **Repérer quoi tester**
   - Identifier la classe/endpoint cible (ex. `Catalog.Api`) et son comportement attendu.
   - Lister les cas : nominal, limites (vide, null, quantité 0/négative), erreurs (404, 400).

2. **Créer le projet de test si besoin**
   - `dotnet new xunit -o tests/ECommerce.Catalog.Tests` puis référencer le projet cible.
   - Ajouter les paquets : `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers` (intégration).

3. **Tests unitaires (xUnit)**
   - Une méthode = un cas, nommée `Methode_Condition_ResultatAttendu`.
   - Structure **Arrange / Act / Assert**.
   - `[Fact]` pour un cas unique, `[Theory] + [InlineData]` pour plusieurs jeux de données.
   - Mocker les dépendances avec **Moq** (ne pas toucher la vraie base).

4. **Tests d'intégration**
   - `WebApplicationFactory<Program>` pour tester l'API de bout en bout (vrais endpoints HTTP).
   - **Testcontainers** pour une vraie base jetable (PostgreSQL) quand c'est pertinent.

5. **Lancer et mesurer**
   - `dotnet test` ; couverture avec `--collect:"XPlat Code Coverage"` (Coverlet).
   - Proposer les **cas manquants** plutôt que de gonfler artificiellement la couverture.

## Garde-fous
- Ne **jamais** affaiblir un test pour le faire passer (pas d'assertion bidon).
- Les tests ne doivent dépendre d'**aucun secret** ni d'un environnement réel de prod.
- La décision « la couverture est suffisante » reste **humaine**.
