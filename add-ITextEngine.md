# ITextEngine – Plan d'implémentation pour MGUI

> **Objectif** : abstraire la mesure et le rendu de texte derrière une interface `ITextEngine` pour permettre de choisir entre `SpriteFont` (MonoGame) et `FontStashSharp`, sans régression.

---

## 1. Design Decisions

| # | Décision | Justification |
|---|----------|---------------|
| **DD-1** | **Interface unique `ITextEngine`** regroupant mesure + rendu dans une seule façade, avec des méthodes distinctes `MeasureText`, `MeasureGlyph`, `DrawText`. | Garder une API simple pour les consommateurs (`DrawTransaction`, `MGTextBlock`) ; la séparation mesure/rendu est interne à chaque implémentation. |
| **DD-2** | **Struct `FontSpec`** (famille, taille, style `CustomFontStyles`) comme clé d'identification d'une police. | Évite de passer 3-4 params partout ; sert de clé de cache ; réutilise l'enum `CustomFontStyles` existant. |
| **DD-3** | **Struct `ResolvedFont`** retourné par `ITextEngine.ResolveFont(FontSpec)` — contient la taille effective, le scale, et un handle opaque (`object` ou générique interne). | Permet à chaque backend de stocker sa référence (SpriteFont, SpriteFontBase…) sans exposer le type concret ; le consommateur passe `ResolvedFont` aux méthodes de mesure/rendu. |
| **DD-4** | **Contrat de mesure glyph-par-glyph** : `ITextEngine.MeasureGlyph(ResolvedFont, char, bool isFirst)` retourne `GlyphMetrics` (leftBearing, width, rightBearing, height). | Requis par `MGTextBlock.MeasureText` pour mesure additive ; crucial pour le wrapping char-by-char dans `ParseLines` ; et par `TextRenderInfo` pour le positionnement du caret. |
| **DD-5** | **Placement dans `MGUI.Shared`** (nouveau namespace `MGUI.Shared.Text.Engines`). | `DrawTransaction` est dans `MGUI.Shared` et doit appeler `ITextEngine` ; `MGUI.Core` consomme via `DrawTransaction` ou directement. Le backend FontStashSharp sera un projet/package séparé. |
| **DD-6** | **`ITextEngine` porté par `MainRenderer`**, exposé via `MainRenderer.TextEngine` (property get/set). `DrawTransaction.TextEngine` délègue à `Renderer.TextEngine`. | Point d'injection unique, cohérent avec `MainRenderer.FontManager` actuel. Pas de changement de signature publique de `MGDesktop`. |
| **DD-7** | **Phase 1 = `SpriteFontTextEngine`** qui encapsule `FontManager` + `FontSet` existants ; comportement bit-pour-bit identique à l'actuel. | Aucune régression possible : on route le même code derrière l'interface. |
| **DD-8** | **`FontManager` reste public** mais marqué `[Obsolete("Use ITextEngine")]` en Phase 2. | Rétro-compatibilité : les utilisateurs qui appellent `FontManager.AddFontSet` continuent de fonctionner. `SpriteFontTextEngine` délègue à `FontManager` en interne. |
| **DD-9** | **Fallback configurable** : `ITextEngine.ResolveFont` ne lève pas d'exception — retourne un `ResolvedFont` valide (police par défaut) avec un flag `IsFallback`. | Évite les crash runtime ; le consommateur peut logger/ignorer. |
| **DD-10** | **Cache de `ResolvedFont`** dans `SpriteFontTextEngine` (dict `FontSpec → ResolvedFont`), invalidé explicitement par `InvalidateCache()`. | Pas de résolution répétée par frame ; cache O(1) — même pattern que l'actuel `QuickSizeLookup` dans `FontSet`. |

---

## 2. Phases & Tâches

### Phase 0 — Préparation (sans changement de comportement)

#### T0.1 – Créer `FontSpec` et `GlyphMetrics`
- **But** : Définir les value types qui serviront de contrat de données pour `ITextEngine`.
- **Fichiers** : `MGUI.Shared/Text/FontSpec.cs` (nouveau), `MGUI.Shared/Text/GlyphMetrics.cs` (nouveau)
- **Détails** :
  - `readonly record struct FontSpec(string Family, int Size, CustomFontStyles Style)`
  - `readonly record struct GlyphMetrics(float LeftSideBearing, float Width, float RightSideBearing, float Height)` avec propriété calculée `TotalWidth`
  - Ajouter `GetHashCode` / `Equals` performants (importants pour le cache)
  - Garder dans le namespace `MGUI.Shared.Text`
- **Done** :
  - Compile sans erreur
  - `FontSpec` est utilisable comme clé de dictionnaire avec hashcode stable
  - Aucun autre fichier modifié

#### T0.2 – Créer `ResolvedFont`
- **But** : Définir le handle opaque retourné par la résolution de police.
- **Fichiers** : `MGUI.Shared/Text/ResolvedFont.cs` (nouveau)
- **Détails** :
  - `class ResolvedFont` (ref type pour éviter les copies) : `FontSpec Spec`, `int ActualSize`, `float Scale`, `float LineHeight`, `float SpaceWidth`, `bool IsFallback`, `internal object NativeFont` (sera casté par l'implémentation)
  - Propriétés immutables (init-only ou readonly)
  - `NativeFont` est `internal` — pas visible hors de `MGUI.Shared`
- **Done** :
  - Compile
  - Le type est bien dans `MGUI.Shared.Text`

#### T0.3 – Créer l'interface `ITextEngine`
- **But** : Définir le contrat abstrait de mesure/rendu.
- **Fichiers** : `MGUI.Shared/Text/ITextEngine.cs` (nouveau)
- **Détails** :
  - ```csharp
    public interface ITextEngine
    {
        ResolvedFont ResolveFont(FontSpec spec);
        Vector2 MeasureText(ResolvedFont font, string text);
        GlyphMetrics MeasureGlyph(ResolvedFont font, char c);
        float GetLineHeight(ResolvedFont font);
        float GetSpaceWidth(ResolvedFont font);
        void DrawText(SpriteBatch spriteBatch, ResolvedFont font, string text,
                      Vector2 position, Color color, Vector2 origin,
                      float rotation, float depth, SpriteEffects effects);
        void InvalidateCache();
    }
    ```
  - `DrawText` prend un `SpriteBatch` car c'est le dénominateur commun (FSS l'utilise aussi via ses propres extensions)
  - Pas de méthode `LoadFont` — le chargement est spécifique au backend et fait au setup
- **Done** :
  - Compile
  - Pas de dépendance à `SpriteFont` dans l'interface (seulement `SpriteBatch`, `Vector2`, `Color`)
  - Bien documentée (XML doc)

#### T0.4 – Créer `SpriteFontTextEngine`
- **But** : Implémenter `ITextEngine` en déléguant à `FontManager` / `FontSet` existants — iso-fonctionnel.
- **Fichiers** : `MGUI.Shared/Text/Engines/SpriteFontTextEngine.cs` (nouveau)
- **Détails** :
  - Constructeur `SpriteFontTextEngine(FontManager fontManager)`
  - `ResolveFont` : appelle `FontManager.TryGetFont(...)`, construit un `ResolvedFont` avec `NativeFont = SpriteFont`, stocke aussi le `FontSet` et les `Origins`/`Heights` dans un wrapper interne `SpriteFontHandle` casté en `object`
  - `MeasureText` : extrait le `SpriteFont` depuis `NativeFont`, appelle `SF.MeasureString(text).X` pour la largeur, `Heights[size] * scale` pour la hauteur — **reproduit exactement** le comportement actuel de `DrawTransaction.MeasureText`
  - `MeasureGlyph` : extrait les glyphes via `SF.GetGlyphs()[c]`, retourne `GlyphMetrics(glyph.LeftSideBearing, glyph.Width, glyph.RightSideBearing, ...)` — reproduit le comportement de `MGTextBlock.MeasureText`
  - `DrawText` : appelle `spriteBatch.DrawString(SF, text, position, color, rotation, origin, scale, effects, depth)` — reproduit `DrawTransaction.DrawSpriteFontText`
  - Cache interne : `Dictionary<FontSpec, ResolvedFont>` + `Dictionary<(ResolvedFont, char), GlyphMetrics>`
  - Les glyphes sont cachés par `ResolvedFont` (pas par frame) — le dictionnaire de glyphes est déjà snapshot dans `SpriteFont.GetGlyphs()`
- **Done** :
  - Compile et passe des tests unitaires de mesure (T0.5)
  - `MeasureText("Hello", Arial/12/Normal)` retourne exactement la même `Vector2` que l'actuel `DrawTransaction.MeasureText`
  - `MeasureGlyph` retourne les mêmes valeurs que le code glyph-par-glyph de `MGTextBlock.MeasureText`

#### T0.5 – Tests unitaires pour `SpriteFontTextEngine`
- **But** : Vérifier la non-régression de la mesure.
- **Fichiers** : `MGUI.Tests/Text/SpriteFontTextEngineTests.cs` (nouveau projet de test)
- **Détails** :
  - Créer un projet `MGUI.Tests` (xUnit ou NUnit)
  - Tester `MeasureText` sur des strings connues avec un `SpriteFont` chargé depuis le Content pipeline de test
  - Tester la propriété additive : `Measure("AB").X ≈ MeasureGlyph('A').TotalWidth + MeasureGlyph('B').TotalWidth`
  - Tester `ResolveFont` avec famille inexistante → `IsFallback = true`
  - Tester `InvalidateCache` → re-résolution
- **Done** :
  - Au moins 8 tests passent (mesure simple, mesure vide, additivité, fallback, cache, styles)
  - Pas de crash ni d'exception non gérée

---

### Phase 1 — Intégration dans MGUI (câblage, sans changement de rendu)

#### T1.1 – Injecter `ITextEngine` dans `MainRenderer`
- **But** : Rendre le moteur de texte accessible à tout le pipeline de rendu.
- **Fichiers** : `MGUI.Shared/Rendering/MainRenderer.cs`
- **Détails** :
  - Ajouter `public ITextEngine TextEngine { get; set; }` 
  - Dans le constructeur, initialiser avec `new SpriteFontTextEngine(FontManager)` 
  - Garder `FontManager` public (pour rétro-compat)
  - Optionnel : ajouter un setter qui appelle `InvalidateCache()` quand on change d'engine
- **Done** :
  - `MainRenderer.TextEngine` est non-null après construction
  - `MGDesktop.Renderer.TextEngine` est accessible
  - Compile, aucun test existant cassé

#### T1.2 – Brancher `DrawTransaction` sur `ITextEngine`
- **But** : Faire passer les méthodes `DrawText`, `MeasureText`, `DrawShadowedText` par `ITextEngine` au lieu d'appeler directement `FontManager`/`SpriteFont`.
- **Fichiers** : `MGUI.Shared/Rendering/DrawTransaction.cs`
- **Détails** :
  - Ajouter `public ITextEngine TextEngine => Renderer.TextEngine;`
  - **`MeasureText(Family, Style, Text, Size, Exact)`** :
    - Appeler `TextEngine.ResolveFont(new FontSpec(Family, Size, Style))` puis `TextEngine.MeasureText(resolved, Text)`
    - Appliquer le flag `Exact` : si `Exact`, utiliser `resolved.Scale` ré-ajusté (= `ExactScale` vs `SuggestedScale` — cette logique doit être dans `ResolvedFont` ou un surcharge du resolve)
  - **`DrawText(Family, Style, Text, Position, Color, Size, Exact)`** :
    - Resolver, puis `BeginDraw(DrawContext.Sprites)`, puis `TextEngine.DrawText(SB, resolved, Text, Position, Color, origin, 0f, 0f, SpriteEffects.None)`
  - **`DrawSpriteFontText(SpriteFont, ...)`** : GARDER tel quel — c'est le low-level utilisé par `MGTextBlock.DrawSelf` qui passe déjà le `SpriteFont` résolu. Ce point sera migré en Phase 1.4.
  - **`DrawShadowedText`** : pas de changement (délègue à `DrawText`)
  - Gérer `Exact` : `ResolvedFont` doit contenir `ExactScale` ET `SuggestedScale` ; `MeasureText`/`DrawText` de `DrawTransaction` choisissent selon le flag
- **Done** :
  - `DrawTransaction.MeasureText` et `DrawTransaction.DrawText` passent par `ITextEngine`
  - Le rendu visuel est **identique** (vérification manuelle : lancer `MGUI.Samples` et comparer)
  - Pas de régression dans les samples

#### T1.3 – Migrer `MGTextBlock.MeasureText` (ITextMeasurer) vers `ITextEngine`
- **But** : Remplacer le code glyph-par-glyph hardcodé SpriteFont dans `MGTextBlock` par des appels à `ITextEngine.MeasureGlyph`.
- **Fichiers** : `MGUI.Core/UI/MGTextBlock.cs`, `MGUI.Core/UI/Text/MGTextLine.cs`
- **Détails** :
  - Dans `MGTextBlock.MeasureText(string, bool, bool, bool)` :
    - Remplacer l'itération sur `Glyphs[c].WidthIncludingBearings` par `TextEngine.MeasureGlyph(resolvedFont, c).TotalWidth`
    - `resolvedFont` sera stocké en cache sur `MGTextBlock` (un `ResolvedFont` par style : Regular, Bold, Italic, BoldItalic — 4 champs)
    - La hauteur utilise `resolvedFont.LineHeight` au lieu de `FontHeight * FontScale`
  - `MGTextBlock.TrySetFont` : résoudre les 4 `ResolvedFont` via `TextEngine.ResolveFont(...)` au lieu de `FontManager.TryGetFont(...)`
  - Garder `ITextMeasurer` interface inchangée — son contrat ne change pas, seul le corps de `MGTextBlock.MeasureText` change
  - `FontOrigin` : stocker dans `ResolvedFont` (ajouter un champ `Vector2 Origin` à `ResolvedFont` — alimenté par `FontSet.Origins[size]` dans `SpriteFontTextEngine`)
  - Supprimer les champs `SF_Regular`, `SF_Bold`, `SF_Italic`, `SF_BoldItalic` et les dictionnaires `Glyphs` de `MGTextBlock` — remplacés par `ResolvedFont_Regular` etc.
- **Done** :
  - `MGTextBlock` n'a plus de référence directe à `SpriteFont` ni à `SpriteFont.Glyph`
  - Le wrapping de texte (vérification : `MGUI.Samples`, TextBlock avec texte long et `WrapText=true`) produit le même résultat
  - `ParseLines` fonctionne identiquement (pas de changement dans `MGTextLine.cs`)

#### T1.4 – Migrer `MGTextBlock.DrawSelf` vers `ITextEngine`
- **But** : Remplacer les appels `DT.DrawSpriteFontText(SF, ...)` par `TextEngine.DrawText(...)`.
- **Fichiers** : `MGUI.Core/UI/MGTextBlock.cs`
- **Détails** :
  - Dans `DrawSelf`, pour chaque `MGTextRunText` :
    - Sélectionner le `ResolvedFont` approprié (Bold/Italic) au lieu de `GetFont(IsBold, IsItalic, out _)`
    - Appeler `TextEngine.DrawText(DT.SB, resolvedFont, text, position, color, resolvedFont.Origin, 0f, 0f, SpriteEffects.None)`
  - Gérer les ombres : 2 appels `DrawText` (shadow offset + foreground) — même logique qu'avant
  - Supprimer `DrawSpriteFontText` de `DrawTransaction` ? **Non** — le garder pour l'instant car d'autres consommateurs pourraient l'utiliser. Le marquer `[Obsolete]` en Phase 2.
- **Done** :
  - `MGTextBlock.DrawSelf` ne référence plus `SpriteFont` directement
  - Rendu identique visuellement dans les samples

#### T1.5 – Migrer `TextRenderInfo` vers `ITextEngine`
- **But** : Le code de positionnement du caret (`TextRenderInfo.UpdateLines`) utilise directement `SpriteFont.Glyph` — le migrer vers `ITextEngine.MeasureGlyph`.
- **Fichiers** : `MGUI.Core/UI/Text/TextRenderInfo.cs`
- **Détails** :
  - Remplacer `SpriteFont SF = TextBlockElement.GetFont(...)` + `Glyphs[c]` par `ResolvedFont` + `TextEngine.MeasureGlyph(resolvedFont, c)`
  - La largeur par caractère devient `GlyphMetrics.TotalWidth * resolvedFont.Scale`
  - Garder la logique `IsStartOfLine` → `Math.Max(leftBearing, 0)` pour le premier glyph (via paramètre `isFirst` de `MeasureGlyph`, ou appliqué côté callsite)
  - Accéder à `TextEngine` via `TextBlockElement.GetDesktop().Renderer.TextEngine` (ou le stocker en champ)
- **Done** :
  - `TextRenderInfo` ne contient plus de `SpriteFont` ni `SpriteFont.Glyph`
  - Le caret dans `MGTextBox` se positionne correctement (test manuel : taper du texte, vérifier le curseur)

#### T1.6 – Tests d'intégration Phase 1
- **But** : Valider la non-régression de bout en bout.
- **Fichiers** : `MGUI.Tests/Integration/TextRenderingIntegrationTests.cs` (nouveau)
- **Détails** :
  - Test "headless" : créer un `MGDesktop` + `MGWindow` + `MGTextBlock`, mesurer le layout et vérifier les dimensions
  - Test wrapping : texte > largeur disponible → vérifier le nombre de lignes et les largeurs
  - Test rich text : `"[b]Hello[/b] [i]World[/i]"` → vérifier 2 runs avec les bons styles
  - Test fallback : famille inexistante → pas de crash, texte rendu avec fallback
  - Comparaison pixel-perfect n'est pas requise (trop fragile) mais les dimensions doivent être numériquement identiques
- **Done** :
  - 6+ tests passent
  - Pas de régression dans `MGUI.Samples`

---

### Phase 2 — Backend FontStashSharp

#### T2.1 – Créer le projet `MGUI.FontStashSharp`
- **But** : Isoler la dépendance FontStashSharp dans un assembly séparé.
- **Fichiers** : `MGUI.FontStashSharp/MGUI.FontStashSharp.csproj` (nouveau), `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` (nouveau)
- **Détails** :
  - Référence `MGUI.Shared` + NuGet `FontStashSharp`
  - Class `FontStashSharpTextEngine : ITextEngine`
  - Constructeur : `FontStashSharpTextEngine(FontSystem fontSystem)` (ou multiple `FontSystem` par style)
  - Stocke un dict `FontSpec → SpriteFontBase`
  - API de setup : `AddFont(string family, CustomFontStyles style, byte[] fontData)` ou `AddFont(string family, CustomFontStyles style, string ttfPath)`
- **Done** :
  - Compile
  - Le projet est optionnel — ne pas le référencer depuis `MGUI.Core`

#### T2.2 – Implémenter `FontStashSharpTextEngine.ResolveFont`
- **But** : Charger / résoudre une police FSS.
- **Fichiers** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Détails** :
  - Utiliser `FontSystem.GetFont(size)` → `SpriteFontBase`
  - Mapper `CustomFontStyles.Bold` → un `FontSystem` dédié (Bold.ttf) ; si absent → fallback sur Regular
  - `ResolvedFont.NativeFont = SpriteFontBase`
  - `LineHeight` : utiliser `SpriteFontBase.LineHeight` (attention : peut différer en pixels du SpriteFont MonoGame pour la même taille — documenter)
  - `SpaceWidth` : `SpriteFontBase.MeasureString(" ").X`
  - Cache par `FontSpec`
- **Done** :
  - `ResolveFont` retourne un handle valide pour Arial 12 Regular
  - `IsFallback` est `true` si la famille n'est pas enregistrée

#### T2.3 – Implémenter mesure et rendu FSS
- **But** : Compléter `MeasureText`, `MeasureGlyph`, `DrawText` pour FSS.
- **Fichiers** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Détails** :
  - `MeasureText` : `SpriteFontBase.MeasureString(text)` — attention, FSS inclut le line spacing dans le Y ; ajuster si `ResolvedFont.LineHeight` doit être cohérent
  - `MeasureGlyph` : FSS n'expose pas les glyph bearings individuels facilement → **stratégie** : mesurer `MeasureString("x").X - MeasureString("").X` ≠ optimal. Mieux : utiliser `FontSystem.GetGlyphs(...)` ou l'API interne de FSS (`DynamicSpriteFont.GetGlyphInfo`). Si pas d'API publique → approximer via `MeasureString(c.ToString()).X` pour `TotalWidth` avec `LeftSideBearing=0, Width=totalWidth, RightSideBearing=0`
  - **Risque d'additivité** : vérifier que `Sum(MeasureGlyph(ci))` ≈ `MeasureText(string)`. Si non, implémenter un facteur de correction ou une mesure incrémentale (mesurer le préfixe croissant)
  - `DrawText` : `SpriteFontBase.DrawText(spriteBatch, text, position, color, rotation, origin, scale, effects, depth)` — FSS fournit cette extension
- **Done** :
  - Mesure avec FSS est fonctionnelle
  - Wrapping produit un résultat visuellement correct (pas forcément pixel-identique au SpriteFont)
  - Rendu FSS affiche du texte lisible dans `MGUI.Samples`

#### T2.4 – Sample de switchable engine
- **But** : Démontrer le switch SpriteFont ↔ FSS dans `MGUI.Samples`.
- **Fichiers** : `MGUI.Samples/Game1.cs`
- **Détails** :
  - Ajouter un toggle (par ex. touche F1) qui fait `Desktop.Renderer.TextEngine = new FontStashSharpTextEngine(...)` ou `new SpriteFontTextEngine(...)`
  - Montrer le re-rendu immédiat
  - Forcer un `InvalidateCache()` + relayout
  - Ajouter un TTF Arial dans `MGUI.Samples/Content/Fonts/` pour FSS
- **Done** :
  - On peut switcher à runtime entre les deux backends
  - Pas de crash

#### T2.5 – Tests unitaires FSS
- **But** : Valider les mesures FSS.
- **Fichiers** : `MGUI.Tests/Text/FontStashSharpTextEngineTests.cs`
- **Détails** :
  - Mêmes tests que T0.5 mais avec `FontStashSharpTextEngine`
  - Test spécifique : vérifier que `MeasureGlyph` est additif (ou documenter la marge d'erreur)
  - Test `LineHeight` cohérent avec `MeasureText("X").Y`
- **Done** :
  - 6+ tests passent
  - Additivité documentée (tolerance ≤ 1px par run)

---

### Phase 3 — Polish & nettoyage

#### T3.1 – Marquer l'ancien chemin `[Obsolete]`
- **But** : Guider les consommateurs vers `ITextEngine`.
- **Fichiers** : `MGUI.Shared/Rendering/DrawTransaction.cs`, `MGUI.Shared/Text/FontManager.cs`
- **Détails** :
  - `DrawTransaction.DrawSpriteFontText` → `[Obsolete("Use ITextEngine.DrawText")]`
  - `DrawTransaction.FontManager` → `[Obsolete]`
  - Garder fonctionnel — suppression dans une major version future
- **Done** :
  - Build warnings sur l'ancien code

#### T3.2 – Support per-Control `ITextEngine` (optionnel, prep)
- **But** : Préparer l'architecture pour un override par contrôle.
- **Fichiers** : `MGUI.Core/UI/MGElement.cs`, `MGUI.Core/UI/MGTextBlock.cs`
- **Détails** :
  - Ajouter `MGElement.TextEngineOverride` (nullable) — si null, remonte au Desktop
  - `GetTextEngine()` : `TextEngineOverride ?? GetDesktop().Renderer.TextEngine`
  - Pas d'implémentation active (juste le hook) — à valider par un cas d'usage
- **Done** :
  - Le hook existe, est documenté
  - Pas de changement de comportement si non utilisé

#### T3.3 – Documentation & migration guide
- **But** : Documenter l'API `ITextEngine` et fournir un guide de migration.
- **Fichiers** : `docs/ITextEngine.md` (nouveau), `README.md` (section ajoutée)
- **Détails** :
  - Exemples de setup avec SpriteFont et FSS
  - Comment ajouter un backend custom
  - Breaking changes (aucun si Phase 1 bien faite, mais lister les `[Obsolete]`)
- **Done** :
  - Doc relue et correcte

---

## 3. Risks & Mitigations

| Risque | Impact | Mitigation |
|--------|--------|------------|
| **Mesure substring non-additive (FSS)** | Le wrapping char-by-char dans `ParseLines` repose sur `Sum(MeasureGlyph) == MeasureText(fullString)`. FontStashSharp peut avoir du kerning contextuel qui casse cette propriété. | Implémenter `MeasureGlyph` via mesure incrémentale (`Measure(prefix + c) - Measure(prefix)`) si l'API glyph FSS est insuffisante. Documenter la tolérance. Ajouter un flag `SupportsAdditiveGlyphMeasure` à `ITextEngine`. |
| **Différence de baseline / LineHeight entre backends** | Un switch de backend peut modifier le layout (nombre de lignes wrappées, hauteur totale). | Accepter comme attendu. Documenter que les métriques ne sont pas identiques entre backends. Fournir un `BaselineOffset` dans `ResolvedFont` si nécessaire pour aligner visuellement. |
| **Performance du cache glyph** | `MeasureGlyph` est appelé par caractère dans `ParseLines` et `TextRenderInfo`. Si chaque appel résout un dict lookup + allocation, c'est coûteux. | Cache `Dictionary<(ResolvedFont, char), GlyphMetrics>` dans l'engine. Pour FSS : pré-cacher les glyphes Latin au `ResolveFont`. `GlyphMetrics` est un `readonly record struct` (pas d'allocation heap). |
| **Fallback avec font manquante** | Si la famille demandée n'existe pas et que le fallback produit des métriques très différentes, le layout peut casser. | Le fallback retourne la police par défaut (Arial) et log un warning. `ResolvedFont.IsFallback` permet au consommateur de réagir. |
| **Concurrence mono-thread** | MonoGame est single-thread pour le rendu, mais le layout peut être calculé en background (improbable mais possible). | `ITextEngine.MeasureText` et `MeasureGlyph` doivent être thread-safe pour la lecture (le cache est peuplé dans le thread principal). Documenter que `DrawText` doit être appelé depuis le thread de rendu. |
| **FontStashSharp texture atlas saturation** | FSS utilise un atlas dynamique. Beaucoup de tailles/styles différents peuvent le saturer. | Recommander un nombre limité de tailles dans le `FontSystem`. Exposer `FontSystem.Reset()` si nécessaire. Monitorer via `FontSystem.CurrentAtlasFull`. |
| **Caret positioning avec FSS** | `TextRenderInfo` utilise des mesures glyph-par-glyph. Si les métriques FSS divergent, le caret sera décalé. | Tester intensivement avec `MGTextBox`. Si nécessaire, `TextRenderInfo` peut utiliser la mesure incrémentale (prefix) au lieu de glyph-par-glyph. |

---

## 4. Plan de Tests

### 4.1 Tests unitaires (projet `MGUI.Tests`)

| # | Test | Input | Expected | Cible |
|---|------|-------|----------|-------|
| U1 | Mesure string vide | `MeasureText(resolved, "")` | `Vector2.Zero` | `SpriteFontTextEngine`, `FontStashSharpTextEngine` |
| U2 | Mesure string simple | `MeasureText(resolved, "Hello")` | Width > 0, Height == LineHeight | Les deux engines |
| U3 | Additivité glyph | `Sum(MeasureGlyph(ci) for ci in "Test")` vs `MeasureText("Test").X` | Égalité (±0.5px pour FSS) | Les deux engines |
| U4 | Additivité substring | `MeasureText("Hello").X + MeasureText("World").X` vs `MeasureText("HelloWorld").X` | Égalité (SpriteFont), ±1px (FSS) | Les deux engines |
| U5 | Fallback police | `ResolveFont(FontSpec("NonExistent", 12, Normal))` | `IsFallback == true`, pas d'exception | Les deux engines |
| U6 | Styles différents | `ResolveFont` pour Normal, Bold, Italic, BoldItalic | 4 `ResolvedFont` distincts, tous valides | SpriteFont engine |
| U7 | Cache invalidation | `ResolveFont` → `InvalidateCache()` → `ResolveFont` | Même résultat, pas de crash | Les deux engines |
| U8 | MeasureGlyph espace | `MeasureGlyph(resolved, ' ')` | Width > 0 | Les deux engines |
| U9 | Caractères spéciaux | `MeasureText(resolved, "é à ü ñ")` | Width > 0, pas d'exception | Les deux engines |
| U10 | Scale Exact vs Suggested | `ResolveFont` avec Exact flag | `ExactScale != SuggestedScale` (quand taille non native) | SpriteFont engine |

### 4.2 Tests d'intégration

| # | Test | Scénario | Validation |
|---|------|----------|------------|
| I1 | Wrapping simple | `MGTextBlock` width=100, texte "The quick brown fox jumps over the lazy dog", `WrapText=true` | Plusieurs `MGTextLine` produits, aucune ligne > 100px |
| I2 | Wrapping avec rich text | `"[b]Bold[/b] and [i]italic[/i] text wrapping test"`, width=150 | Runs correctement répartis sur les lignes |
| I3 | Ellipsis / troncature | Texte long dans un `TextBlock` de largeur fixe sans wrap | Texte tronqué sans crash |
| I4 | Inline images | `"Text [img=icon,16,16] more text"` | Image mesurée comme 16px de large dans le flow du texte |
| I5 | TextBox caret | Créer un `MGTextBox`, simuler du texte, vérifier `TextRenderInfo.Lines` | Positions de caractères cohérentes avec la mesure |
| I6 | Switch engine runtime | Changer `TextEngine` après le setup initial, re-layout | Pas de crash, layout mis à jour |
| I7 | Multi-size dans une page | Plusieurs `TextBlock` avec des `FontSize` différents (10, 14, 20) | Chacun mesuré/rendu correctement avec son propre `ResolvedFont` |
| I8 | Fallback font rendering | Famille inconnue sur un `TextBlock` | Texte rendu avec la police par défaut, pas de crash |

### 4.3 Tests visuels (manuels)

- Lancer `MGUI.Samples`, naviguer dans le Compendium
- Vérifier visuellement que tous les contrôles texte (TextBlock, TextBox, Button labels, ComboBox items, TabControl headers, etc.) sont rendus identiquement avant/après
- Vérifier le caret du TextBox : positionnement, sélection, scroll
- Vérifier le ContextMenu et ToolTip (texte correctement mesuré et positionné)
- (Phase 2) Switcher sur FSS et vérifier que le texte est lisible et le layout cohérent

---

## 5. Non-goals / Future Improvements

| Item | Raison du report |
|------|-----------------|
| **Remplacement complet de `FontManager`** | Trop risqué pour la v1. `FontManager` reste le backend de `SpriteFontTextEngine`. Suppression envisageable en v2 si FSS devient le défaut. |
| **Support de fontes variables (weight 100-900)** | `CustomFontStyles` utilise un flag enum discret. Ajouter un poids continu nécessiterait de changer le contrat. À évaluer si FSS le supporte bien. |
| **Rendu SDF (Signed Distance Field)** | FSS supporte le SDF mais ça change le pipeline de rendu (shaders). Architecture compatible (`ITextEngine.DrawText` encapsule le rendu) mais non implémenté. |
| **Layout multi-colonnes / flow avancé** | Hors scope de `ITextEngine`. Concerne `MGTextLine.ParseLines` et le layout engine. |
| **Per-character styling (gradient, outline, per-glyph color)** | L'actuel `MGTextRunConfig` gère le style par run, pas par glyph. Extension possible via `DrawText` overloads mais non prioritaire. |
| **Mesure asynchrone / background** | Le layout MonoGame est synchrone. Pas de besoin identifié. |
| **Hot-reload de polices** | `InvalidateCache()` permet un refresh, mais le rechargement de TTF à chaud nécessite du travail FSS-spécifique. |
| **Localisation / BiDi / shaping complexe (arabe, hindi)** | Ni SpriteFont ni FSS ne gèrent le shaping complexe. Nécessiterait HarfBuzz. Hors scope. |
| **Per-control `ITextEngine` override** | Préparé en T3.2 (hook dans `MGElement`) mais pas activé. Cas d'usage : mélanger SpriteFont et FSS dans la même UI. |
