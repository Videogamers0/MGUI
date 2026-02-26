# ITextEngine — Parity SpriteFontTextEngine ↔ FontStashSharpTextEngine

**Objectif** : Obtenir un rendu identique (au pixel près) lorsqu'on charge la même
police (Arial) à la même taille (ex. 20 pt) avec les deux moteurs ITextEngine.

---

## 0. Sommaire des différences architecturales

| Aspect | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| **Source de la police** | Atlas bitmap `.xnb` pré-rasterisé via Content Pipeline (96 DPI, em-square sizing) | TTF rasterisé à la volée par StbTrueType (ascender−descender sizing) |
| **Tailles disponibles** | Discrètes : 8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 24, 36, 48, 72 | Continues (n'importe quelle taille fractionnaire) |
| **Scale model** | Demande taille X → choisit atlas le plus grand, applique `ExactScale = X / bakedSize` ou `SuggestedScale = 1/round(bakedSize/X)` | Pas de scaling externe ; FSS rasterise directement à `effectivePt × FontSizeScale` px |
| **ExactScale / SuggestedScale** | Deux valeurs potentiellement différentes ; mesure utilise ExactScale, dessin utilise l'un ou l'autre selon `UseExactScale` | Toujours 1.0 ; pas de scale interne |
| **LineHeight** | `Heights[bakedSize] × ExactScale` — `Heights` = cropping custom (min glyph top → max glyph bottom + 1) | `SpriteFontBase.LineHeight` = ascender + descender natif FSS (métrique hhea) |
| **SpaceWidth** | `SF.MeasureString(" ") × ExactScale` | `Font.MeasureString(" ")` — à la taille FSS rasterisée |
| **MeasureText(string)** | `SF.MeasureString(text).X × ExactScale` — kerning & Spacing SpriteFont inclus | `Font.MeasureString(text).X` — kerning StbTrueType inclus |
| **MeasureGlyph(char)** | `Glyph.LeftSideBearing × ExactScale`, `Glyph.Width × ExactScale`, `Glyph.RightSideBearing × ExactScale` | `LSB = 0`, `GlyphWidth = Font.MeasureString(c.ToString()).X`, `RSB = 0` — pas de décomposition LSB/RSB |
| **DrawOrigin** | `FontSet.Origins[bakedSize]` = `(0, MinGlyphCroppingY)` — décale le rendu vers le haut pour supprimer l'espace vide au-dessus des glyphes | `Vector2.Zero` — FSS dessine depuis le top-left natif (inclut internal leading) |
| **drawScale passé à DrawText** | `ResolvedFont.SuggestedScale` (ou `ExactScale` si `UseExactScale=true`) | `ResolvedFont.SuggestedScale` = 1.0 (pas d'effet) |
| **SpriteFont.Spacing** | Espacement horizontal additionnel entre glyphes (par défaut 0, mais peut être > 0 dans le .spritefont XNB) | Non applicable — StbTrueType gère via advanceWidth natif |
| **FontSizeScale** | N/A | `(96/72) × (hhea.ascender − hhea.descender) / head.unitsPerEm` — transforme les pt logiques en px FSS |
| **MatchSpriteFontSizing** | N/A | Calibration: `table[ptSize] = bakedSize × suggestedScale` pour forcer FSS à utiliser la même taille effective |

---

## 1. Analyse des sources de divergence

### 1.1 — LineHeight (hauteur de ligne)

**Impact** : décale le texte verticalement, change la hauteur mesurée des
TextBlocks, affecte le wrapping multi-ligne et la taille des containers.

| | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| **Formule** | `Heights[bakedSize] × ExactScale` | `SpriteFontBase.LineHeight` (= FSS ascender + descender + lineGap en px) |
| **Ce que Heights représente** | Cropping custom : `Max(glyphBitmapHeight) - Min(croppingTop) + 1` — exclut le leading au-dessus et en-dessous | Métriques hhea×scale : inclut le lineGap (espace inter-lignes natif de la police) |

**Problème** : Même à taille identique en pixels, les deux LineHeight diffèrent
parce qu'ils mesurent des choses différentes :
- SpriteFont mesure la **boîte englobante serrée** des glyphes effectivement rendus.
- FSS mesure la **hauteur de ligne typographique** (ascender − descender [+ lineGap]).

Pour Arial 20 pt par exemple, SpriteFont donne un Height de ~22 px (cropping serré)
tandis que FSS donne ~23 px ou +.

**Actions** :
- [ ] **T1.1** — Écrire un diagnostic runtime qui, pour chaque taille de 8 à 48, log :
  `[SF] size={spec.Size} → bakedSize={actual}, exactScale={es}, Heights[baked]={h}, lineHeight={h*es}`
  `[FSS] size={spec.Size} → fssPixelSize={effectivePt * FontSizeScale}, LineHeight={fssLH}`
  Comparer les deltas.

- [ ] **T1.2** — Déterminer si FSS `SpriteFontBase.LineHeight` inclut ou non le
  `lineGap` de la table hhea. Si oui, reproduire côté FSS le même calcul de
  cropping serré que FontSet fait (min CroppingY → max bitmap height). Cela
  nécessite d'accéder aux métriques par glyphe FSS ou de pré-calculer depuis le TTF.

- [ ] **T1.3** — Alternative : stocker dans `ResolvedFont` deux valeurs de
  hauteur : `LayoutLineHeight` (pour le calcul de layout/wrapping, identique
  entre les deux moteurs) et `NativeLineHeight` (brute du moteur, pour usage
  interne). L'interface `ITextEngine.GetLineHeight` retournerait la valeur layout.

---

### 1.2 — MeasureText (largeur d'une chaîne)

**Impact** : si la largeur mesurée diffère ne serait-ce que d'un pixel, le wrapping
et le positionnement du texte divergent visiblement.

| | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| **API** | `SF.MeasureString(text).X × ExactScale` | `Font.MeasureString(text).X` |
| **Kerning** | `SpriteFont.Spacing` ajouté entre chaque caractère ; pas de vrai kerning OpenType | StbTrueType interpole les tables de kerning OpenType si présentes |
| **Arrondi** | `SF.MeasureString` opère en entiers natifs puis * float scale | FSS opère en float natif tout le long |

**Problème** : Même avec `MatchSpriteFontSizing` (qui calibre la taille effective),
les avances par glyphe peuvent différer légèrement car :
1. StbTrueType utilise un rasterizer et un calcul d'avance différent du Content Pipeline.
2. Le `SpriteFont.Spacing` (entier, par défaut 0 mais potentiellement > 0 dans le .spritefont) n'a pas d'équivalent FSS.
3. Les arrondis (ceil/floor/round) sont appliqués à des moments différents.

**Actions** :
- [ ] **T2.1** — Écrire un diagnostic qui, pour un set de chaînes test
  ("A", "Hello World", "Stardew Valley", "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
  log la largeur retournée par les deux moteurs à taille 10, 11, 12, 14, 20.
  Calculer le delta absolu et relatif.

- [ ] **T2.2** — Vérifier si `SpriteFont.Spacing` est non-zéro dans les XNB
  chargés. Si oui, l'injecter dans FSS (via `FontSystem.CharacterSpacing`
  ou le compenser manuellement dans `FontStashSharpTextEngine.MeasureText`).

- [ ] **T2.3** — Après calibration, si un delta > 1 px subsiste, envisager un
  facteur de correction post-mesure dans `FontStashSharpTextEngine.MeasureText` :
  `width *= correctionFactor[spec.Size]` calculé une fois au moment de
  `MatchSpriteFontSizing`.

---

### 1.3 — MeasureGlyph (métrique par glyphe)

**Impact** : utilisé par `TextRenderInfo.UpdateLines()` pour positionner le caret
dans le TextBox. Si les advances par glyphe ne correspondent pas, le caret apparaît
décalé par rapport au texte rendu.

| | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| **LSB** | `Glyph.LeftSideBearing × scale` (peut être négatif) | Toujours 0 |
| **GlyphWidth** | `Glyph.Width × scale` = largeur du bitmap | `Font.MeasureString(c.ToString()).X` = advance complet |
| **RSB** | `Glyph.RightSideBearing × scale` | Toujours 0 |
| **TotalWidth** | `LSB + Width + RSB` | `advance` |

**Problème** : Pour SpriteFont, `LSB + Width + RSB = advance`, ce qui correspond
conceptuellement à l'advance FSS. Mais la **décomposition** diffère : SpriteFont
donne une vraie ventilation, FSS met tout dans `GlyphWidth`.

Cela affecte `TextRenderInfo.UpdateLines()` qui utilise `TotalWidthFirstGlyph`
(où le LSB négatif est clampé à 0) pour le premier glyphe d'une ligne. Avec FSS
ce clamp n'a aucun effet (LSB=0), donc le premier glyphe sera positionné
différemment si le SpriteFont avait un LSB négatif.

De plus, `TextRenderInfo` fait un « residual correction » :
`wholeStringWidth - sumOfPerGlyphAdvances → distribué proportionnellement`.
Ce résidu est typiquement petit pour SpriteFont (quelques fractions de pixel)
mais pourrait être plus grand pour FSS si `MeasureString(c)` ≠ advance réel
(car FSS peut inclure du kerning pair-dépendant dans la mesure multi-char mais
pas dans la mesure single-char).

**Actions** :
- [ ] **T3.1** — Implémenter `MeasureGlyph` proprement côté FSS en utilisant
  l'API StbTrueType / FontStashSharp interne pour obtenir les vrais `LSB`,
  `advanceWidth` et calculer `RSB = advance - LSB - glyphWidth`. Vérifier si
  `FontStashSharp.DynamicSpriteFont` expose ces données ou s'il faut parser
  le TTF (tables `hmtx` + `glyf`).

- [ ] **T3.2** — Si l'API FSS n'expose pas les bearings natifs, pré-parser les
  tables TTF `hmtx` (horizontal metrics) au moment de `AddFontSystem` et stocker
  un mapping `char → (LSB, advanceWidth)` en unités font, converti au vol avec
  le scale FSS.

- [ ] **T3.3** — Comparer character par character pour "Hello World" à taille 20
  entre les deux moteurs : `LSB`, `Width`, `RSB`, `TotalWidth`. Vérifier que la
  somme des `TotalWidth` glyphes ≈ `MeasureText("Hello World")` pour les deux.

---

### 1.4 — DrawOrigin (origine de dessin / positionnement vertical)

**Impact** : le texte est dessiné à une position Y différente, ce qui le fait
paraître décalé verticalement même si les largeurs sont identiques.

| | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| **DrawOrigin** | `(0, MinCroppingY)` — MinCroppingY est souvent ~2-4 px pour un atlas 20 pt | `Vector2.Zero` |
| **Effet** | `DrawString(sf, text, pos, color, 0, origin, scale, ...)` → décale le rendu vers le haut de `MinCroppingY × scale` pixels | Aucun décalage ; le texte inclut le leading natif au-dessus |

**Problème** : Quand on passe de SF à FSS, le texte « descend » visuellement parce
que FSS n'applique pas le même offset vertical. Le TextBlock positionne le texte à
`TextYPosition` (calculé via `ApplyAlignment`) et passe `resolved.DrawOrigin` comme
origin à `DrawText`. Pour SF, cela remonte le texte ; pour FSS, rien ne se passe.

**Actions** :
- [ ] **T4.1** — Pour chaque taille, log la valeur `DrawOrigin.Y` de SF et
  comparer avec l'internal leading FSS (écart entre le haut du em-square et le
  premier pixel du glyphe le plus haut). Calculer le `DrawOrigin` équivalent côté FSS.

- [ ] **T4.2** — Calculer côté `FontStashSharpTextEngine.ResolveFont` un
  `DrawOrigin` non-zéro qui reproduit le même décalage que FontSet fait :
  déterminer le `MinCroppingY` équivalent FSS. Options :
  - Parser la table TTF `glyf` pour les glyphes du CharacterSet et calculer le MinY .
  - Ou mesurer empiriquement : rasteriser "|" et détecter le premier pixel non-transparent.
  - Ou utiliser `hhea.ascender` / `OS/2.sTypoAscender` pour approximer.

- [ ] **T4.3** — Si un `DrawOrigin` exact n'est pas calculable, ajouter un offset Y
  constant dans `FontStashSharpTextEngine.DrawText` qui aligne le baseline FSS
  sur le baseline SpriteFont pour chaque taille.

---

### 1.5 — drawScale (échelle de dessin)

**Impact** : le texte est rendu à une taille légèrement différente visuellement.

| | SpriteFontTextEngine | FontStashSharpTextEngine |
|---|---|---|
| `resolved.SuggestedScale` | `1/round(bakedSize/desiredSize)` — ex: taille 20 → bakedSize=20, scale=1.0 ; taille 11 → bakedSize=36, scale=0.333 | Toujours 1.0 |
| `resolved.ExactScale` | `desiredSize/bakedSize` — ex: taille 11 → 11/36=0.306 | Toujours 1.0 |

**Problème** : MGTextBlock choisit le drawScale selon `UseExactScale`.
Pour SpriteFontTextEngine, `DrawText(sb, font, text, pos, color, origin, scale=0.333)`
fait que MonoGame scale le bitmap. Pour FSS, on passe scale=1.0 et FSS a déjà
rasterisé le glyphe à la bonne taille — **pas de problème fonctionnel** mais le
`MeasureText` (qui utilise ExactScale=1.0) est déjà cohérent avec le drawScale=1.0.

Cependant, le fait que SF mesure avec `ExactScale` mais dessine avec `SuggestedScale`
crée un **écart mesure↔rendu** côté SpriteFontTextEngine quand `UseExactScale=false`.
Ce même écart n'existe pas côté FSS (les deux sont à 1.0).

**Actions** :
- [ ] **T5.1** — Documenter quelles tailles de police ont un écart
  `SuggestedScale ≠ ExactScale` pour les baked sizes disponibles. Pour la taille
  20 (bakedSize=20), `ExactScale=1.0`, `SuggestedScale=1.0` → pas d'écart.
  Mais pour taille 11 (bakedSize=36), `ExactScale=0.306`, `SuggestedScale=0.333` →
  9% d'écart mesure↔rendu.

- [ ] **T5.2** — Évaluer si ce problème mesure↔rendu dans SpriteFontTextEngine
  (non lié à FSS) doit être corrigé dans le cadre de cette tâche ou traité
  séparément.

---

### 1.6 — SpriteFont.Spacing vs FSS kerning

**Impact** : décalage horizontal cumulatif sur les longues chaînes.

**Problème** : `SpriteFont.MeasureString` ajoute `SpriteFont.Spacing` (un entier,
par défaut 0) entre chaque paire de caractères. Le Content Pipeline ne génère pas
de kerning OpenType. FSS en revanche utilise les tables de kerning TTF natives.

Même si Spacing=0, les deux moteurs placent les glyphes différemment car la méthode
de calcul d'avance (bitmap-based vs outline-based) diffère fondamentalement.

**Actions** :
- [ ] **T6.1** — Vérifier `SpriteFont.Spacing` pour les polices chargées. Si > 0,
  appliquer `FontSystem.CharacterSpacing` côté FSS pour compenser.

- [ ] **T6.2** — Comparer les avances cumulées glyph-by-glyph pour "Hello World"
  entre SF et FSS. Si delta > 0.5 px par glyph, envisager une table de correction
  par caractère dans FSS.

---

## 2. Plan d'action détaillé

### Phase A — Diagnostic (pas de changement de code)

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| A1 | Écrire un utilitaire `TextEngineDiagnostic` qui, pour Arial à tailles 8–48, collecte de chaque moteur : `lineHeight`, `spaceWidth`, `MeasureText("Hello World")`, `DrawOrigin`, et les métriques glyph-by-glyph pour "AaBbCc 123" | Nouveau fichier dans `MGUI.Samples/` ou script standalone | Haute |
| A2 | Dumper les résultats dans un CSV/table Markdown pour comparaison visuelle | Console output | Haute |
| A3 | Capturer des screenshots avec chaque moteur à taille 20 et les superposer pour mesurer les deltas en pixels | Manuel (runtime) | Moyenne |

### Phase B — Aligner LineHeight

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| B1 | Calculer côté FSS le même `Heights` que FontSet : itérer les glyphes du CharacterSet, trouver minCroppingY et maxBitmapHeight, stocker `Heights[size]` | `FontStashSharpTextEngine.cs` | Haute |
| B2 | Si l'API FSS n'expose pas le cropping des glyphes, pré-parser les tables TTF `glyf` / `loca` pour extraire les bounding boxes par glyphe, ou utiliser une approche par rasterisation : `MeasureString("|")` et ajuster | `FontStashSharpTextEngine.cs` ou helper TTF | Haute |
| B3 | Passer le LineHeight FSS corrigé dans `ResolvedFont.LineHeight` | `FontStashSharpTextEngine.ResolveFont` | Haute |

### Phase C — Aligner DrawOrigin

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| C1 | Calculer côté FSS le `DrawOrigin = (0, minCroppingY)` équivalent | `FontStashSharpTextEngine.ResolveFont` | Haute |
| C2 | S'assurer que `DrawText` FSS applique correctement cet origin | `FontStashSharpTextEngine.DrawText` | Haute |
| C3 | Valider que le texte est rendu à la même position Y que SF | Runtime comparison | Haute |

### Phase D — Aligner MeasureText (largeur)

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| D1 | Après B+C, re-mesurer les largeurs. Si delta > 1 px, investiguer `SpriteFont.Spacing` | Diagnostic | Moyenne |
| D2 | Si Spacing > 0, injecter dans FSS | `FontStashSharpTextEngine` | Moyenne |
| D3 | Si delta subsiste, ajouter un facteur de correction par taille dans `MatchSpriteFontSizing` (ratio `sfWidth / fssWidth` pour une chaîne de référence) | `FontStashSharpTextEngine.MatchSpriteFontSizing` | Moyenne |

### Phase E — Aligner MeasureGlyph (caret/TextBox)

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| E1 | Exposer les vrais LSB/RSB côté FSS (via parsing TTF `hmtx`) | `FontStashSharpTextEngine.MeasureGlyph` | Moyenne |
| E2 | Valider que le caret se positionne au même endroit dans un TextBox avec les deux moteurs | Manuel (runtime) | Moyenne |

### Phase F — Tests de non-régression

| # | Action | Fichiers | Priorité |
|---|---|---|---|
| F1 | Créer un test unitaire paramétré qui, pour chaque taille 8–48 et chaque chaîne d'un corpus, vérifie que `abs(sfWidth - fssWidth) ≤ 1 px` et `abs(sfLineHeight - fssLineHeight) ≤ 1 px` | Nouveau projet test ou dans `MGUI.Samples` | Haute |
| F2 | Ajouter un test visuel (screenshot comparison) dans le sample pour taille 20 avec les deux moteurs side-by-side | `MGUI.Samples` | Basse |

---

## 3. Résumé des fichiers à modifier

| Fichier | Changements attendus |
|---|---|
| `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` | `ResolveFont` : calculer LineHeight et DrawOrigin alignés sur SF ; `MeasureGlyph` : exposer vrais LSB/RSB ; `MatchSpriteFontSizing` : ajouter correction de largeur si nécessaire |
| `MGUI.Shared/Text/ResolvedFont.cs` | Éventuellement ajouter `LayoutLineHeight` si on décide de séparer les métriques |
| `MGUI.Shared/Text/Engines/ITextEngine.cs` | Possiblement ajuster la doc de `GetLineHeight` pour clarifier ce qu'il doit retourner |
| `MGUI.Samples/Game1.cs` | Ajouter le diagnostic (Phase A) ; éventuellement un rendu side-by-side |

---

## 4. Critères d'acceptation

Pour considérer la parité atteinte :

1. **LineHeight** : `abs(SF.lineHeight - FSS.lineHeight) ≤ 1 px` pour toute taille 8–72.
2. **MeasureText** : `abs(SF.width - FSS.width) ≤ 1 px` pour les chaînes de test à toute taille 8–72.
3. **DrawOrigin** : texte visuellement aligné au même Y à ±1 px entre les deux moteurs.
4. **MeasureGlyph** : position du caret dans TextBox identique à ±1 px.
5. **Rendu visuel** : screenshot overlay à taille 20 montre un décalage ≤ 1 px horizontal et vertical sur chaque glyphe.
