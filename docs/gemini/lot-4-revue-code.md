# Lot 4 : revue du code, par Claude

Revue du 10/11/2026.

## Verdict

**Validé.** J'ai relancé `dotnet test` : 127 réussis, 0 échec, 2 ignorés. T1 à T9 sont appliqués, `MoneyHelper.ToCents` est conforme et utilisé par les 4 cas d'usage, aucun fichier XAML touché.

## Points notés, sans blocage

- **Les 2 tests ignorés ne sont pas des bugs de l'application :** `SiteService` lit `DeviceInfo` directement, ce qui n'est pas disponible en `net10.0`. À traiter plus tard, avec une petite abstraction du nom de l'appareil, si on veut couvrir l'activation.
- **`Easymakemoney1.sln` :** `dotnet sln add` a ajouté les configurations x64 et x86, au-delà du seul projet demandé en T2. Sans effet, accepté.
- **`Easymakemoney.Tests/.gitignore` ajouté par moi :** `bin/` et `obj/` n'étaient pas ignorés.
