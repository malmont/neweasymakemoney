# Easymakemoney — application de gestion Izzipro

Application .NET MAUI (Android, iOS, Mac Catalyst, Windows) de gestion de la suite Izzipro : collections, commandes
fournisseurs, produits et variantes, notes de frais, frais de port, fournisseurs, tableaux de bord, stock et
statistiques. Elle s'adresse aux administrateurs (`ROLE_ADMIN`) d'un site et appelle l'API du backend Symfony
`izzipro_backend_symfony_v7` (autre dépôt).

> **État au 15/10/2026 : mise à niveau nécessaire.** Le backend Izzipro est multi-tenant (un site par client, choisi
> par l'en-tête `X-Tenant-Host`). Cette version, de mars 2025, vise en dur le domaine principal du backend et ne gère
> donc que le site `tenantdefaut`. Détail et plan : [docs/audit-multitenant.md](docs/audit-multitenant.md) ; modèle
> commun aux applications mobiles : [docs/strategie-multitenant-mobile.md](docs/strategie-multitenant-mobile.md).
> Démarrage de la mise à niveau (05/11/2026), changements du backend depuis l'audit et première tâche :
> [docs/topo-demarrage.md](docs/topo-demarrage.md).

## Organisation

| Dossier | Rôle |
|---|---|
| `Easymakemoney/Services/` | appels à l'API (`HttpService`, un service par écran ou liste), connexion, jeton |
| `Easymakemoney/UseCase/` | cas d'usage |
| `Easymakemoney/ViewModels/`, `Views/` | MVVM (écrans et leurs ViewModels) |
| `Easymakemoney/Models/` | modèles JSON |
| `Easymakemoney/const/Configurations.cs` | URL du backend (à sortir du code, voir l'audit) |

## Démarrer

```
dotnet build Easymakemoney1.sln
```

Essais : uniquement sur le site de test `demo` du backend. Le backend n'a pas de préproduction, chaque appel écrit
dans des bases réelles.
