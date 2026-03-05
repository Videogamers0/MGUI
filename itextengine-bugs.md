# ITextEngine — FontStashSharp Measure/Draw Width Mismatch

## Symptôme

Lorsque `FontStashSharpTextEngine` est activé (F1), le texte est affiché
avec une **largeur plus grande** que ce que le moteur de layout alloue,
ce qui provoque un **clipping** visible (texte coupé à droite).
Constaté sur le sample FF7 Inventory (noms d'objets, descriptions).

## Cause racine

`FontStashSharpTextEngine.MeasureText` en mode **calibré** (`MatchSpriteFontSizing`)
calcule la largeur en sommant les métriques de glyphes du SpriteFont atlas
(`glyph.Width`, `glyph.LeftSideBearing`, `glyph.RightSideBearing` — valeurs entières
× `ExactScale`), tandis que `DrawText` rend le texte via le font FSS natif à
`effectivePt × FontSizeScale` pixels avec des avances de glyphes en virgule flottante
(StbTrueType).

Les largeurs entières du SpriteFont atlas sont **systématiquement plus étroites**
que les valeurs float de FSS. Sur une ligne de 30+ caractères, la différence cumulée
atteint plusieurs pixels, causant le dépassement et le clipping.

**Résumé** : `MeasureText → SpriteFont widths` ≠ `DrawText → FSS native widths`.

---

## Tâches

### ✅ Tâche 1 — Créer le fichier de diagnostic et la liste de tâches

- **Fichier** : `itextengine-bugs.md` (ce fichier)
- **Action** : analyser les screenshots et le code, documenter la cause racine
- **Commit** : `docs: create itextengine-bugs.md with FSS measure/draw mismatch analysis`

---

### ✅ Tâche 2 — Fix `MeasureText` : utiliser la mesure FSS native pour la largeur

- **Fichier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Problème** : le chemin calibré somme les métriques SpriteFont (entiers × scale)
  au lieu d'utiliser `SpriteFontBase.MeasureString()` qui correspond au rendu réel.
- **Fix** : dans `MeasureText`, remplacer le calcul calibré par
  `h.Font.MeasureString(text).X` pour la composante largeur.
  Garder `font.LineHeight` (calibré) pour la composante hauteur.
- **Justification** : aligner mesure et rendu dans le même moteur FSS.
  Le layout allouera un espace correspondant à ce que DrawText affiche réellement.
- **Commit** : `fix(fss): MeasureText uses native FSS width instead of calibrated SpriteFont sum`

---

### ✅ Tâche 3 — Fix `MeasureGlyph` : utiliser la mesure FSS native par glyphe

- **Fichier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Problème** : `MeasureGlyph` retourne les métriques calibrées du SpriteFont atlas,
  mais `TextRenderInfo.UpdateLines` utilise ces valeurs pour positionner les carets
  dans le TextBox. Si les positions de carets ne correspondent pas au rendu FSS,
  l'édition de texte est décalée.
- **Fix** : dans `MeasureGlyph`, utiliser `h.GetGlyphWidth(c)` (FSS natif) au lieu
  des métriques calibrées. Garder `font.LineHeight` calibré pour la hauteur.
- **Commit** : `fix(fss): MeasureGlyph uses native FSS glyph width for caret consistency`

---

### ✅ Tâche 4 — Fix `SpaceWidth` dans `ResolveFont` : utiliser la valeur FSS native

- **Fichier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Problème** : `SpaceWidth` calibré provient de `SpriteFont.MeasureString(" ")`,
  mais le rendu FSS dessine l'espace à sa propre largeur. Si SpaceWidth calibré est
  plus étroit, les espaces visuels sont plus larges que l'espace alloué.
- **Fix** : dans `ResolveFont`, toujours utiliser `handle.SpaceWidth` (FSS natif)
  au lieu de la valeur calibrée pour SpaceWidth.
- **Commit** : `fix(fss): ResolveFont uses native FSS SpaceWidth for consistent spacing`

---

### ✅ Tâche 5 — Documenter le root cause dans le code

- **Fichier** : `MGUI.FontStashSharp/FontStashSharpTextEngine.cs`
- **Action** : ajouter des commentaires XML `<remarks>` sur `MeasureText`,
  `MeasureGlyph`, et `MatchSpriteFontSizing` expliquant pourquoi les métriques
  calibrées de glyphes ne sont PAS utilisées pour la largeur (mesure-rendu
  inconsistance avec FSS DrawText).
- **Commit** : `docs(fss): explain calibrated glyph metrics vs FSS-native width decision`

---

### ✅ Tâche 6 — Tests unitaires de consistance mesure/rendu

- **Fichier** : `MGUI.Tests/Text/FSSMeasureDrawConsistencyTests.cs`
- **Action** : créer des tests vérifiant que :
  1. `MeasureText` retourne une largeur ≥ à la somme des `MeasureGlyph` (after reconciliation)
  2. `MeasureText` et `MeasureGlyph` utilisent le chemin FSS natif (pas le calibrage SpriteFont)
  3. `SpaceWidth` du `ResolvedFont` correspond à `MeasureText(" ")` width
- **Notes** : les tests peuvent utiliser un mock de `SpriteFontBase` ou un
  `FontStashSharpTextEngine` sans calibration (pas de `MatchSpriteFontSizing`).
- **Commit** : `test(fss): add measure-draw consistency tests for FSS text engine`

---

### ✅ Tâche 7 — Vérification build + tests + régression

- **Action** :
  1. `dotnet build MGUI.sln` — 0 erreur
  2. `dotnet test MGUI.Tests/` — tous les tests passent
  3. Vérifier aucun avertissement nouveau introduit
- **Commit** : (pas de commit séparé, vérification seulement)

---

### ✅ Tâche 8 — Nettoyage final

- **Action** :
  1. Supprimer les TODO/FIXME temporaires
  2. Mettre à jour les icônes de statut dans ce fichier (✅ pour chaque tâche terminée)
  3. `git status` propre
- **Commit** : `chore: final cleanup and status update in itextengine-bugs.md`

---

### ✅ Tâche 9 — Fix régression line-wrap (FSS wraps sur 3 lignes au lieu de 2)

- **Symptôme** : après les fixes Tâches 2-4, le texte « Developer note: The transparent
  ones aren't implemented yet... » s'affichait sur **3 lignes** avec FSS contre
  **2 lignes** avec SpriteFontTextEngine.
- **Cause racine** : `FSS.MeasureString(text)` sur le font brut à
  `effectivePt × FontSizeScale` retourne des largeurs **plus grandes** que
  `SF.MeasureString(text) × exactScale` (avances float StbTrueType > avances entières atlas SF).
  `ParseLines` voyait donc des lignes plus larges et renvoyait à la ligne plus tôt.
- **Fix** : dans `ResolveFont`, après avoir calculé `rawPixelSize = effectivePt × FontSizeScale`,
  appliquer un facteur de correction linéaire :
  ```
  ratio = calibrated_SF_spaceWidth / FSS_native_spaceWidth(rawPixelSize)
  correctedPixelSize = rawPixelSize × ratio
  ```
  FSS étant linéaire en taille de pixel, TOUS les caractères sont réduits
  proportionnellement, ce qui aligne FSS sur SpriteFontTextEngine pour mesure ET rendu.
  Sans calibration (`_calibratedSpaceWidth` absent) le chemin de fallback est utilisé.
- **Fichiers** :
  - `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` — `ResolveFont`
  - `MGUI.Tests/Text/FSSMeasureDrawConsistencyTests.cs` — commentaires mis à jour,
    2 nouveaux tests (`MeasureGlyph_SpaceChar_MatchesSpaceWidth`,
    `FSSMeasurement_ScalesProportionally_WithPixelSize`)
- **Commit** : `fix(fss): calibrate FSS pixel size to match SpriteFontTextEngine widths and eliminate wrap regression`

---

## Contexte technique

### Flux de mesure avec calibration (avant fix)

```
MeasureText(font, "Hello")
  │
  ├─ calibrated path: Σ SpriteFont glyph metrics (integers × ExactScale)
  │  → returns width W₁ (narrow, integer-based)
  │
  └─ FSS native: h.Font.MeasureString("Hello").X
     → real width W₂ (wider, float-precision)

DrawText(..., scale=1.0)
  │
  └─ FSS renders text at effectivePt × FontSizeScale
     → visual width ≈ W₂ > W₁
     → TEXT CLIPPED because layout allocated W₁
```

### Après Tâches 2-4 (clipping corrigé, mais régression line-wrap)

```
MeasureText(font, "Hello")
  │
  └─ FSS native: h.Font.MeasureString("Hello").X  ← same font as DrawText
     → returns width W₂ (wider than SF W₁)

ParseLines wraps when committed width > available width
  │
  └─ W₂ > W₁ → ParseLines wraps earlier → 3 lines instead of 2
```

### Flux corrigé final (après Tâche 9 — calibration pixel-size)

```
ResolveFont:
  rawPixelSize = effectivePt × FontSizeScale
  ratio = calibrated_SF_spaceWidth / FSS_native_spaceWidth(rawPixelSize)
  correctedPixelSize = rawPixelSize × ratio       ← scales ALL advances
  font = FSS.GetFont(correctedPixelSize)

MeasureText(font, "Hello")
  │
  └─ FSS native on corrected font → W₃ ≈ W₁ (matches SF)
     → ParseLines wraps at same point as SF → 2 lines ✓

DrawText(..., scale=1.0) on corrected font
  │
  └─ visual width ≈ W₃ ≈ W₁ → no clipping ✓
```

### Fichiers impactés

| Fichier | Changement |
|---------|-----------|
| `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` | MeasureText, MeasureGlyph, ResolveFont (pixel-size calibration) |
| `MGUI.Tests/Text/FSSMeasureDrawConsistencyTests.cs` | Nouveaux tests + mises à jour commentaires |
| `itextengine-bugs.md` | Ce fichier (statut des tâches) |
