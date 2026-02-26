# PR #35 — Review Comments — Task List

> Generated from: https://github.com/Videogamers0/MGUI/pull/35  
> Copilot review: commit `6302802` — Developer (Videogamers0) review: commit `9001d7a`  
> Analysis date: 2025-02-25

## Summary

### Commentaires Copilot AI

| # | Comment | File | Status |
|---|---------|------|--------|
| C1 | SpriteEffects ignored in FSS DrawText | `FontStashSharpTextEngine.cs` | ✅ Already fixed |
| C2 | Missing null check in `MGTextBlock.MeasureText` | `MGTextBlock.cs` | ✅ Already fixed |
| C3 | Layout invalidation after engine switch | `Game1.cs` | ✅ Already fixed |
| C4 | `DrawTextViaEngine` parameter naming inconsistency | `DrawTransaction.cs` | ✅ Already fixed |
| C5 | Unnecessary x64/x86 platform configs in SLN | `MGUI.sln` | ❌ **À corriger** |
| C6 | `FontSizeScale` not thread-safe | `FontStashSharpTextEngine.cs` | ⚠️ **Partiel — doc manquante** |
| C7 | Inconsistent null check (`DrawShadowedText` vs `MeasureText`) | `DrawTransaction.cs` | ✅ Already fixed |

### Commentaires développeur Videogamers0 (Owner)

| # | Comment | Fichier | Status |
|---|---------|---------|--------|
| D1 | Text wrapping erroné — texte plus large → mots coupés à la ligne | `TextRenderInfo.cs` / layout | ❌ **À corriger** |
| D2 | `InvalidateAllLayouts()` non récursif — ne vide pas `RecentSelfMeasurements` | `MGDesktop.cs` / `MGTextBlock.cs` | ❌ **À corriger** |

---

## Tâches développeur (Videogamers0) — À corriger en priorité

### Task D1 — Text wrapping erroné / texte trop large horizontalement

**Commentaire original (Videogamers0) :**
> On first glance I think the text-wrapping is incorrect or functionally different by a slight amount.
> - The "Stardew Valley" button only displays "Stardew". I assume it's wrapping the "Valley" text to a 2nd line which the button isn't tall enough to display.
> - In the FF7 sample window, the "Restores health by" label is cutting off the last word since it's wrapping to a new line.

**Symptôme :** Avec les nouvelles modifications, le rendu `SpriteFontTextEngine` prend légèrement plus d'espace horizontal pour certains textes, ce qui provoque un wrapping là où il n'y en avait pas sur master. Résultat : texte tronqué dans les boutons du Compendium et les TextBlocks du sample FF7.

**Cause probable :** Modification du calcul de la largeur de texte dans `TextRenderInfo.UpdateLines` — la nouvelle méthode de mesure (entière chaîne via `engine.MeasureText`) produit des valeurs légèrement différentes des caractères individuels sommés comme avant.

**Investigation :**
- Comparer `TextRenderInfo.UpdateLines` : ancienne somme de largeurs de glyphe vs nouvelle `engine.MeasureText(wholeRun)`
- Vérifier si `MGTextBlock.MeasureText` / `RecentSelfMeasurements` est cohérent avec `TextRenderInfo`
- Reproduire avec "Stardew Valley" dans le Compendium (SpriteFontTextEngine, pas FSS)

**Fichiers concernés :**
- `MGUI.Core/UI/Text/TextRenderInfo.cs` — `UpdateLines()`
- `MGUI.Core/UI/MGTextBlock.cs` — `MeasureText()`, `MinWidth` calcul
- `MGUI.Samples/Controls/Button.xaml` et la grille du Compendium

---

### Task D2 — `InvalidateAllLayouts()` non récursif / ne vide pas `RecentSelfMeasurements`

**Commentaire original (Videogamers0) :**
> This function doesn't recursively invalidate layouts on child elements. May want to use `MGElement.TraverseVisualTree` to invoke `InvalidateLayout` on every element instead. Or if the intended purpose is just to re-measure everything when the text engine changes, then iterate all elements with `TraverseVisualTree`, and call `LayoutChanged` on all textblock elements.
>
> ```csharp
> public void RecalculateTextLayout()
> {
>     foreach (MGTextBlock textBlock in Windows.SelectMany(x => x.TraverseVisualTree<MGTextBlock>()))
>         textBlock.LayoutChanged(textBlock, true);
> }
> ```
>
> Actually, it'd probably need to call `MGTextBlock.InvokeLayoutChanged` which also clears `MGTextBlock.RecentSelfMeasurements` cache. And might need to update the runs and the lines like `MGTextBlock.SetText` does.

**Problème :** L'actuel `InvalidateAllLayouts()` appelle `window.InvalidateLayout()` sur chaque `MGWindow`, mais ne descend pas récursivement dans l'arbre visuel. Les `MGTextBlock` enfants gardent leur cache `RecentSelfMeasurements` obsolète après un changement de moteur de texte.

**Action :**
1. Remplacer `InvalidateAllLayouts()` (ou créer `RecalculateTextLayout()`) qui :
   - Parcourt tous les Windows via `Windows`
   - Pour chaque window, utilise `TraverseVisualTree<MGTextBlock>()` 
   - Appelle `InvokeLayoutChanged()` sur chaque `MGTextBlock` (vide le cache + re-mesure runs + lignes)
2. Appeler cette méthode dans `Game1.cs` après le toggle `TextEngine`
3. Vérifier l'API `MGTextBlock.InvokeLayoutChanged` (existe-t-elle ? sinon simuler via `SetText`)

**Fichiers concernés :**
- `MGUI.Core/UI/MGDesktop.cs` — remplacer/améliorer `InvalidateAllLayouts()`
- `MGUI.Core/UI/MGTextBlock.cs` — vérifier `InvokeLayoutChanged()` et `RecentSelfMeasurements`
- `MGUI.Samples/Game1.cs` — appel après toggle engine

---

## Tâches Copilot restantes

### Task C5 — Supprimer les platform configs x64/x86 inutiles dans MGUI.sln

**Problème :** Le fichier `MGUI.sln` contient des configurations `Debug|x64`, `Debug|x86`, `Release|x64`, `Release|x86` dans `SolutionConfigurationPlatforms` et dans `ProjectConfigurationPlatforms`, mais **tous les projets mappent ces plateformes vers `Any CPU`**. Ces entrées n'apportent rien et encombrent le fichier solution.

**Action :** Supprimer les lignes x64/x86 de :
- `GlobalSection(SolutionConfigurationPlatforms)` (lignes ~24-28)
- `GlobalSection(ProjectConfigurationPlatforms)` — toutes les entrées `*.x64.*` et `*.x86.*` pour chaque projet

**Impact :** Cosmétique / propreté. Aucun impact fonctionnel.

---

### Task C6 — Documenter la contrainte single-thread de `FontSizeScale`

**Problème :** Le setter de `FontSizeScale` invalide correctement le cache (`InvalidateCache()`), mais il n'y a aucune documentation avertissant que la propriété **n'est pas thread-safe**. Le reviewer demande soit un mécanisme de synchronisation, soit une note XML doc.

**État actuel :** Le setter fonctionne correctement (cache invalidation OK), mais la doc XML ne mentionne pas la contrainte de thread.

**Action :** Ajouter une remarque `<remarks>` dans la doc XML de `FontSizeScale` indiquant :
> This property is not thread-safe. It must only be set from the main game
> thread (the same thread that calls `Update`/`Draw`), which is the standard
> threading model for MonoGame applications.

**Impact :** Documentation uniquement. Le code est déjà correct pour l'usage single-thread de MonoGame.

---

## Tâches Copilot déjà corrigées (pour référence)

### ✅ Task C1 — SpriteEffects dans FSS DrawText
Corrigé : simulation du flip via négation de scale (`FontStashSharpTextEngine.cs` L405-408).

### ✅ Task C2 — Null check dans `MGTextBlock.MeasureText`
Corrigé : `if (resolved?.NativeFont == null) return Vector2.Zero;` (`MGTextBlock.cs` L575).

### ✅ Task C3 — `InvalidateAllLayouts()` après toggle engine
Corrigé : `Desktop.InvalidateAllLayouts()` appelé après le switch (`Game1.cs` L114).

### ✅ Task C4 — Doc `DrawTextViaEngine`
Corrigé : commentaire XML expliquant pourquoi `SpriteBatch` est omis (`DrawTransaction.cs` L179-185).

### ✅ Task C7 — Null check cohérent
Corrigé : les deux méthodes utilisent `resolved.NativeFont == null` (`DrawTransaction.cs` L224, L241).
