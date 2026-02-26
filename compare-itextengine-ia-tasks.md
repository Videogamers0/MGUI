# Agent IA — Parité de rendu SpriteFontTextEngine ↔ FontStashSharpTextEngine

## Contexte

Le projet MGUI utilise une interface `ITextEngine` (`MGUI.Shared/Text/Engines/ITextEngine.cs`) avec deux implémentations :
- **SpriteFontTextEngine** (`MGUI.Shared/Text/Engines/SpriteFontTextEngine.cs`) — moteur historique basé sur les atlas MonoGame `.xnb`
- **FontStashSharpTextEngine** (`MGUI.FontStashSharp/FontStashSharpTextEngine.cs`) — moteur alternatif basé sur FontStashSharp / StbTrueType

**Objectif** : Charger la même police (Arial) à la même taille (ex. 20 pt) doit produire un rendu identique au pixel près entre les deux moteurs.

**Branche** : `ITextEngine`

## Fichiers clés

| Fichier | Rôle |
|---|---|
| `MGUI.Shared/Text/Engines/ITextEngine.cs` | Interface commune : `ResolveFont`, `MeasureText`, `MeasureGlyph`, `DrawText`, `GetLineHeight`, `GetSpaceWidth` |
| `MGUI.Shared/Text/Engines/SpriteFontTextEngine.cs` | Implémentation SpriteFont (213 lignes). Classe interne `SpriteFontHandle` (SF, Scale, ExactScale, Size, FontHeight, Origin, Glyphs) |
| `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` | Implémentation FSS (426 lignes). Classe interne `FSSFontHandle` (Font, LineHeight, SpaceWidth, cache _glyphWidths) |
| `MGUI.Shared/Text/ResolvedFont.cs` | DTO retourné par `ResolveFont` : Spec, ActualSize, ExactScale, SuggestedScale, LineHeight, SpaceWidth, DrawOrigin, IsFallback, NativeFont |
| `MGUI.Shared/Text/GlyphMetrics.cs` | Record struct : LeftSideBearing, GlyphWidth, RightSideBearing, Height. Propriétés TotalWidth et TotalWidthFirstGlyph |
| `MGUI.Shared/Text/FontSet.cs` | Collection de SpriteFonts par taille. Calcule `Heights[size]` (cropping serré), `Origins[size]` (Vector2(0, MinCroppingY)), `ExactScale`, `SuggestedScale` |
| `MGUI.Shared/Text/FontManager.cs` | Gestion des FontSet par famille. Délègue à `FontSet.TryGetFont` |
| `MGUI.Core/UI/MGTextBlock.cs` | Dessin du texte : choisit `drawScale = UseExactScale ? ExactScale : SuggestedScale`, passe `resolved.DrawOrigin` |
| `MGUI.Core/UI/Text/MGTextLine.cs` | Parsing de lignes : `LineTextHeight = max(MeasureText().Y)`, `LineTotalHeight = max(MinLineHeight, LineTextHeight, LineImageHeight)` |
| `MGUI.Samples/Game1.cs` | Initialisation FSS : charge `arial.ttf`, appelle `AddFontSystem` + `MatchSpriteFontSizing` |

## Différences identifiées entre les deux moteurs

### Résumé rapide

| # | Aspect | SpriteFontTextEngine | FontStashSharpTextEngine | Impact |
|---|---|---|---|---|
| 1 | **LineHeight** | `FontSet.Heights[bakedSize] × ExactScale` — cropping serré (MinGlyphTop → MaxGlyphBottom+1) | `SpriteFontBase.LineHeight` — métrique hhea ascender+descender | Hauteur de ligne différente → wrapping/layout divergent |
| 2 | **DrawOrigin** | `Vector2(0, MinCroppingY)` — remonte le texte pour supprimer l'espace vide | `Vector2.Zero` | Texte FSS décalé vers le bas |
| 3 | **MeasureGlyph** | Vrais `LSB / Width / RSB` depuis `SpriteFont.Glyph` × ExactScale | `LSB=0, GlyphWidth=MeasureString(c).X, RSB=0` | Caret TextBox mal positionné ; TotalWidthFirstGlyph diffère |
| 4 | **MeasureText** | `SF.MeasureString(text).X × ExactScale` + SpriteFont.Spacing | `Font.MeasureString(text).X` | Largeurs légèrement différentes |
| 5 | **Scale model** | ExactScale ≠ SuggestedScale possibles (ex: taille 11 → 0.306 vs 0.333) | Toujours 1.0 / 1.0 | Écart mesure↔rendu côté SF |
| 6 | **Spacing/Kerning** | SpriteFont.Spacing (entier, défaut 0) entre chaque glyphe ; pas de kerning OpenType | Kerning OpenType natif ; pas de Spacing | Avances cumulées divergentes |

---

## Tâches

**Règles pour l'agent** :
- Commiter après chaque tâche complète avec un message descriptif en anglais
- Ne pas faire de workaround — trouver une solution propre et intelligente
- Chaque tâche a une section "Vérification" : l'agent doit s'assurer que les critères sont remplis avant de commiter
- Les tâches sont ordonnées par dépendance — les suivre dans l'ordre

---

### Tâche 1 — Diagnostic : écrire un utilitaire de comparaison des deux moteurs

**But** : Créer un outil runtime qui affiche côte à côte les métriques des deux moteurs pour identifier et quantifier les écarts.

**Fichier à créer** : `MGUI.Samples/TextEngineDiagnostic.cs`

**Instructions** :
1. Créer une classe statique `TextEngineDiagnostic` dans le namespace `MGUI.Samples`
2. Méthode `public static void Run(MGDesktop desktop, SpriteFontTextEngine sfEngine, FontStashSharpTextEngine fssEngine)`
3. Pour chaque taille dans `{8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 24, 36, 48}` :
   - Résoudre la font Arial Normal via `sfEngine.ResolveFont(new FontSpec("Arial", size, CustomFontStyles.Normal))` et idem pour `fssEngine`
   - Logger sous forme de tableau :
     ```
     Size | SF.LineHeight | FSS.LineHeight | ΔHeight | SF.SpaceWidth | FSS.SpaceWidth | ΔSpace | SF.DrawOrigin.Y | FSS.DrawOrigin.Y
     ```
4. Pour chaque chaîne dans `{"A", "Hello World", "Stardew Valley", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "The quick brown fox jumps over the lazy dog"}` et chaque taille dans `{10, 12, 14, 20, 24}` :
   - Mesurer via `MeasureText(resolved, string)` sur les deux moteurs
   - Logger :
     ```
     Size | String | SF.Width | FSS.Width | ΔWidth | SF.Height | FSS.Height | ΔHeight
     ```
5. Pour "Hello World" à taille 20, logger les `MeasureGlyph` caractère par caractère :
   ```
   Char | SF.LSB | SF.Width | SF.RSB | SF.Total | FSS.LSB | FSS.Width | FSS.RSB | FSS.Total | ΔTotal
   ```
6. Toutes les sorties via `System.Diagnostics.Debug.WriteLine` (visible dans la fenêtre Output de Visual Studio)

**Appeler le diagnostic** : Dans `MGUI.Samples/Game1.cs`, appeler `TextEngineDiagnostic.Run(...)` une seule fois après l'initialisation des deux moteurs (dans `LoadContent` ou au premier `Update`). Il faut stocker les références aux deux moteurs pour pouvoir les passer.

**Vérification** :
- Le projet compile sans erreur
- Le diagnostic produit une sortie structurée lisible dans la console Debug

---

### Tâche 2 — Aligner le LineHeight de FSS sur celui de SpriteFont

**But** : Faire en sorte que `ResolvedFont.LineHeight` retourné par FSS soit identique (±1 px) à celui de SF pour la même taille.

**Fichier à modifier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

**Contexte technique** :
- SpriteFontTextEngine calcule le LineHeight via `FontSet.Heights[bakedSize] × ExactScale`
- `Heights[bakedSize]` est calculé dans `FontSet.cs` (lignes ~130-140) :
  ```csharp
  int MinY = Glyphs.Select(x => x.Cropping.Top).DefaultIfEmpty(0).Min();
  int MaxY = MinY + Glyphs.Select(x => x.BoundsInTexture.Height).DefaultIfEmpty(0).Max();
  _Heights[FontSize] = MaxY - MinY + 1;
  ```
- FSS utilise `SpriteFontBase.LineHeight` qui est la métrique hhea (ascender − descender) — plus grande que le cropping serré

**Instructions** :
1. Enrichir `MatchSpriteFontSizing` pour aussi capturer le LineHeight de SF :
   - Après avoir résolu chaque `ptSize` via `fontManager.TryGetFont(...)`, calculer `sfLineHeight = Heights[bakedSize] * exactScale`
   - Stocker dans un nouveau dictionnaire `_calibratedLineHeight` : `Dictionary<int, float>` mappant `ptSize → sfLineHeight`
   - Il faut accéder à `FontSet.Heights` via le `FontSet` retourné par `TryGetFont`. Le paramètre `out FontSet fs` est déjà retourné.
2. Dans `ResolveFont`, utiliser `_calibratedLineHeight[spec.Size]` comme `lineHeight` au lieu de `handle.LineHeight` quand la calibration est disponible
3. Mettre à jour la valeur `SpaceWidth` de la même manière si nécessaire : stocker `sfSpaceWidth` dans un dictionnaire `_calibratedSpaceWidth` et l'utiliser dans `ResolveFont`

**Attention** :
- `FontManager.TryGetFont` retourne déjà `out FontSet fs` — il faut vérifier la signature dans `FontManager.cs`. Si elle ne retourne pas `fs`, adapter : on peut appeler `fontManager.GetFontFamilyOrDefault(family)` pour obtenir le `FontSet` puis appeler `fs.Heights[bakedSize]`.
- Le dictionnaire `_calibratedLineHeight` doit être `null`-checked comme `_calibratedEffectivePt`

**Vérification** :
- Re-exécuter le diagnostic (Tâche 1) et vérifier que `|ΔHeight| ≤ 1 px` pour toutes les tailles
- `MeasureText().Y` retourne la même valeur (±1 px) entre les deux moteurs

---

### Tâche 3 — Aligner le DrawOrigin de FSS sur celui de SpriteFont

**But** : Faire en sorte que le texte FSS soit dessiné à la même position Y que le texte SF.

**Fichier à modifier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

**Contexte technique** :
- SpriteFontTextEngine utilise `DrawOrigin = FontSet.Origins[bakedSize]` = `Vector2(0, MinCroppingY)`
- `MinCroppingY` est le minimum de `Glyph.Cropping.Top` sur tous les glyphes du CharacterSet pour cette taille
- Ce DrawOrigin est passé à `SpriteBatch.DrawString(..., origin: DrawOrigin, ...)` ce qui décale le rendu vers le haut
- FSS utilise `DrawOrigin = Vector2.Zero` donc le texte est rendu plus bas (il inclut l'espace vide au-dessus des glyphes)

**Instructions** :
1. Dans `MatchSpriteFontSizing`, capturer aussi `FontSet.Origins[bakedSize]` pour chaque ptSize
   - Stocker dans `_calibratedDrawOrigin` : `Dictionary<int, Vector2>` mappant `ptSize → sfDrawOrigin`
   - Attention : l'Origin SF est dans l'espace natif du SpriteFont (coordonnées de l'atlas à `bakedSize`). Il est passé tel quel à `DrawString` et le scale est appliqué séparément. Pour FSS où scale=1, il faut pré-scaler : `calibratedOrigin = sfOrigin * exactScale` (car le glyphe FSS est déjà à la bonne taille)
2. Dans `ResolveFont`, passer `_calibratedDrawOrigin[spec.Size]` comme `drawOrigin` au lieu de `Vector2.Zero` quand la calibration est disponible
3. Vérifier dans `DrawText` de FSS que le `origin` est bien passé à `Font.DrawText` (c'est déjà le cas)

**Point délicat** :
- L'origin SF est en coordonnées du SpriteFont natif (ex: pour bakedSize=20, MinCroppingY=2 signifie 2 px dans l'atlas 20pt). Quand SF dessine avec `scale=suggestedScale`, MonoGame multiplie l'origin par le scale internement ? Non — l'origin est en coordonnées source (avant scale). Donc pour une comparaison pixel-à-pixel, l'origin FSS doit être calculé pour produire le même décalage final en pixels.
- Le décalage final SF en pixels est `MinCroppingY * drawScale` (car DrawString multiplie l'origin par le scale). Côté FSS le drawScale est 1.0, donc l'origin FSS doit directement être `MinCroppingY * sfDrawScale`. Il faut stocker `MinCroppingY * suggestedScale` ou `MinCroppingY * exactScale` selon ce qui est utilisé côté SF (par défaut suggestedScale car `UseExactScale=false`).
- **Solution recommandée** : stocker l'origin brut (non scalé) dans `_calibratedDrawOrigin`. Côté FSS le scale est toujours 1.0 et l'origin sera passé à `DrawText` avec scale=1.0, donc FSS `Font.DrawText` appliquera `origin * scale = origin * 1 = origin`. Vérifier que FSS `Font.DrawText` applique bien l'origin de la même manière que `SpriteBatch.DrawString`.

**Vérification** :
- Visuellement, le texte doit apparaître à la même position Y avec les deux moteurs
- Le diagnostic doit montrer que `SF.DrawOrigin.Y` et `FSS.DrawOrigin.Y` sont proches

---

### Tâche 4 — Aligner MeasureText (largeur) entre les deux moteurs

**But** : `MeasureText(font, "Hello World").X` doit retourner la même valeur (±1 px) avec les deux moteurs.

**Fichier à modifier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

**Contexte technique** :
- SF : `SF.MeasureString(text).X × ExactScale`
- FSS : `Font.MeasureString(text).X` — la taille est déjà calibrée via `MatchSpriteFontSizing` mais des écarts subsistent car :
  - SpriteFont.Spacing (espacement horizontal additionnel, entier, défaut 0)
  - Méthode de calcul d'avance différente (bitmap vs outline)
  - Arrondis différents

**Instructions** :
1. D'abord, exécuter le diagnostic pour quantifier les écarts après les tâches 2 et 3
2. Vérifier si `SpriteFont.Spacing` est non-zéro :
   - Dans `MatchSpriteFontSizing`, après `fontManager.TryGetFont(...)`, accéder à la SpriteFont retournée et lire sa propriété `Spacing`
   - Si `Spacing > 0`, appliquer un `CharacterSpacing` équivalent côté FSS : `FontSystem.CharacterSpacing = sfSpacing` ou compenser dans `MeasureText`
3. Si après la calibration existante (`_calibratedEffectivePt`) + Spacing, un delta systématique > 1 px persiste :
   - Dans `MatchSpriteFontSizing`, calculer un facteur de correction par taille :
     ```
     sfWidth = sfEngine.MeasureText(sfResolved, referenceString).X
     fssWidth = fssEngine.MeasureText(fssResolved, referenceString).X
     correctionFactor[ptSize] = sfWidth / fssWidth
     ```
   - Stocker dans `_widthCorrectionFactor` : `Dictionary<int, float>`
   - Appliquer dans `MeasureText` : `return new Vector2(size.X * correction, lineHeight)`
   - **Attention** : cette approche est un dernier recours. Essayer d'abord d'affiner `_calibratedEffectivePt` en utilisant `bakedSize * exactScale` au lieu de `bakedSize * suggestedScale` pour voir si ça réduit le delta

**Vérification** :
- Le diagnostic montre `|ΔWidth| ≤ 1 px` pour toutes les chaînes de test à toutes les tailles
- Le wrapping du texte est identique visuellement entre les deux moteurs

---

### Tâche 5 — Aligner MeasureGlyph (métriques par glyphe)

**But** : Le caret dans les TextBox doit se positionner au même endroit avec les deux moteurs.

**Fichier à modifier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

**Contexte technique** :
- SF décompose : `LSB = Glyph.LeftSideBearing × scale`, `Width = Glyph.Width × scale`, `RSB = Glyph.RightSideBearing × scale`
- FSS retourne : `LSB = 0`, `GlyphWidth = Font.MeasureString(c.ToString()).X`, `RSB = 0`
- `GlyphMetrics.TotalWidthFirstGlyph` clampe les LSB négatifs : `max(LSB, 0) + Width + RSB`
  - Avec SF, un LSB négatif (ex: "j") change le positionnement du premier glyphe
  - Avec FSS, le LSB est toujours 0 donc ce clamp n'a jamais d'effet

**Instructions** :
1. Investiguer si FontStashSharp expose les métriques par glyphe :
   - Chercher dans l'API FontStashSharp : `DynamicSpriteFont.GetGlyphs()`, `GetGlyphInfo()`, ou méthodes similaires
   - Vérifier les sources de FontStashSharp sur GitHub si nécessaire
2. **Option A — API FSS disponible** : si FSS expose `advanceWidth`, `leftSideBearing` et `glyphWidth` par caractère, les utiliser dans `MeasureGlyph` :
   ```csharp
   return new GlyphMetrics(lsb, glyphWidth, rsb, lineHeight);
   // où rsb = advanceWidth - lsb - glyphWidth
   ```
3. **Option B — Parser le TTF** : si FSS n'expose pas les bearings, pré-parser les tables TTF `hmtx` / `cmap` :
   - Au moment de `AddFontSystem(family, style, fontSystem, ttfData)`, parser les données TTF
   - Extraire pour chaque glyphe : `advanceWidth` et `leftSideBearing` depuis la table `hmtx`
   - Mapper les caractères Unicode vers les glyph IDs via la table `cmap`
   - Stocker un `Dictionary<char, (int advanceWidth, int leftSideBearing)>` en unités font
   - Dans `MeasureGlyph`, convertir en pixels : `lsb = rawLsb * scale`, où `scale = fssPixelSize / unitsPerEm`
   - `glyphWidth` = la largeur du bitmap du glyphe (peut être approximée comme `advanceWidth - lsb - rsb`, mais le RSB est `advanceWidth - lsb - (xMax - xMin)` ce qui nécessite la table `glyf`)
   - **Simplification acceptable** : utiliser `GlyphWidth = advanceWidth * scale - lsb`, `RSB = 0`, car seul le LSB impacte réellement le positionnement
4. **Option C — Calibration depuis SF** : lors de `MatchSpriteFontSizing`, capturer les `MeasureGlyph` de SF pour le CharacterSet et les stocker dans un dictionnaire `_calibratedGlyphMetrics[ptSize][char] = GlyphMetrics`. Les retourner tels quels dans `MeasureGlyph` FSS.
   - **Avantage** : parity parfaite
   - **Inconvénient** : consomme de la mémoire, ne fonctionne que quand la calibration est active

**Recommandation** : Option C est la plus simple et garantit la parité. Option B est la plus clean si on veut que FSS fonctionne de manière autonome.

**Vérification** :
- Le diagnostic (Tâche 1) montre que `ΔTotal ≤ 0.5 px` pour chaque caractère
- Le caret dans un TextBox est au même endroit avec les deux moteurs

---

### Tâche 6 — Vérifier SpriteFont.Spacing et compenser côté FSS

**But** : S'assurer que l'espacement inter-caractère est identique.

**Fichiers à lire** : `MGUI.Shared/Text/FontSet.cs`, SpriteFonts chargés

**Instructions** :
1. Dans le diagnostic (Tâche 1), ajouter une ligne qui affiche `SpriteFont.Spacing` pour chaque taille :
   - Accéder à la SpriteFont via `((SpriteFontHandle)sfResolved.NativeFont).SF.Spacing`
   - (Si le cast échoue, passer par le SpriteFontTextEngine qui expose le SpriteFont en interne)
2. Si `Spacing == 0` pour toutes les polices Arial chargées → cette tâche est terminée, rien à corriger
3. Si `Spacing > 0` :
   - Soit appliquer `FontSystem.CharacterSpacing` du côté FSS (attention : s'applique globalement au FontSystem, pas par style)
   - Soit compenser dans `FontStashSharpTextEngine.MeasureText` : ajouter `(text.Length - 1) * spacing * scale` à la largeur
   - Soit compenser dans `FontStashSharpTextEngine.MeasureGlyph` : ajouter `spacing` au `RightSideBearing`

**Vérification** :
- Les avances cumulées glyph-by-glyph pour "Hello World" à taille 20 sont identiques (±0.5 px par glyphe, ±1 px en total)

---

### Tâche 7 — Mettre à jour MeasureText pour utiliser le LineHeight calibré

**But** : `MeasureText(font, text).Y` doit retourner le LineHeight calibré, pas le LineHeight FSS natif.

**Fichier à modifier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`

**Contexte** :
- Actuellement, `MeasureText` retourne `new Vector2(size.X, h.LineHeight)` où `h.LineHeight = SpriteFontBase.LineHeight`
- Après la tâche 2, `ResolvedFont.LineHeight` est calibré mais `h.LineHeight` dans `FSSFontHandle` ne l'est pas
- `MeasureText` devrait utiliser `font.LineHeight` (le calibré) et non `h.LineHeight` (le natif FSS)

**Instructions** :
1. Dans `MeasureText`, remplacer `h.LineHeight` par `font.LineHeight` :
   ```csharp
   return new Vector2(size.X, font.LineHeight);
   ```
2. Dans `MeasureGlyph`, remplacer `h.LineHeight` par `font.LineHeight` :
   ```csharp
   return new GlyphMetrics(0f, h.GetGlyphWidth(c), 0f, font.LineHeight);
   ```
3. Dans `GetLineHeight`, c'est déjà `font.LineHeight` — vérifier que c'est le cas

**Vérification** :
- `MeasureText(font, "Hello World").Y` == `font.LineHeight` pour chaque taille (cohérence interne)
- Le diagnostic montre les bonnes valeurs

---

### Tâche 8 — Test de non-régression

**But** : S'assurer que les modifications ne cassent rien et que la parité est maintenue.

**Instructions** :
1. Exécuter le diagnostic final (Tâche 1) et vérifier que tous les critères sont remplis :
   - `|ΔLineHeight| ≤ 1 px` pour toutes les tailles 8–48
   - `|ΔWidth| ≤ 1 px` pour toutes les chaînes de test
   - `|ΔDrawOrigin.Y| ≤ 1 px`
   - `|ΔGlyphTotal| ≤ 0.5 px` par caractère
2. Copier la sortie du diagnostic dans le commit message ou dans un commentaire de PR
3. Vérifier visuellement dans `MGUI.Samples` :
   - Lancer l'application, afficher du texte à taille 20
   - Appuyer sur F1 pour basculer entre les moteurs
   - Le texte ne doit pas bouger ni changer de taille visuellement
4. Vérifier que le build compile sans warning ni erreur :
   ```
   dotnet build MGUI.sln --configuration Debug
   ```

---

## Ordre d'exécution

```
Tâche 1 (Diagnostic)
    ↓
Tâche 2 (LineHeight) → Tâche 7 (MeasureText.Y utilise le calibré)
    ↓
Tâche 3 (DrawOrigin)
    ↓
Tâche 4 (MeasureText largeur)
    ↓
Tâche 5 (MeasureGlyph LSB/RSB)
    ↓
Tâche 6 (SpriteFont.Spacing)
    ↓
Tâche 8 (Validation finale)
```

Commiter après chaque tâche. Message de commit en anglais, format : `fix(FSS): <description courte>`
