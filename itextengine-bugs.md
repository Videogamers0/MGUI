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

- **Symptôme** : après les fixes Tâches 2-4, le texte s'affichait sur **3 lignes** avec FSS
  contre **2 lignes** avec SpriteFontTextEngine. La délégation à `SF.MeasureString`
  (commit `f787306`) pour le layout avait corrigé le wrapping mais avait **réintroduit
  le clipping** (DrawText FSS plus large que le layout alloué par SF).
- **Cause racine (finale)** :
  - Le ratio espace seul était toujours 1.0 (`calSW == rawSW`) → correction pixel-size
    précédente (commit `a30c4be`) inopérante.
  - `SF.MeasureString` délégué pour le layout → clipping car DrawText FSS rend plus large.
  - La divergence est dans les chaînes **multi-caractères** : `FSS.MeasureString(text)` >
    `SF.MeasureString(text) × exactScale`, ce qui n'est pas visible sur l'espace seul.
- **Fix (définitif)** : dans `ResolveFont`, mesurer une chaîne de calibration couvrant
  tout l'alphabet + chiffres + ponctuation avec SF ET FSS, puis corriger `pixelSize` :
  ```
  ratio = sfCalibWidth / fssCalibWidth
  pixelSize *= ratio
  ```
  La correction étant appliquée au `pixelSize` FSS, elle est proportionnelle à tous les
  caractères. `MeasureText` (FSS natif) et `DrawText` (FSS natif) utilisent le même
  font corrigé → pas de clipping. Les largeurs FSS corrigées ≈ SF → même wrapping.
- **Champ utilisé** : `_calibratedSpriteFont` (déjà populé), `CalibrationString` (constante).
- **Commits cumulatifs** :
  - `a30c4be` — tentative ratio espace (ratio = 1.0, non fonctionnel)
  - `aecead6` — diagnostic `Debug.WriteLine`
  - `f787306` — délégation SF.MeasureString (wrapping OK mais clipping revenu)
  - `4aa94a3` — **fix définitif** : correction pixel-size par chaîne multi-caractères

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

### Flux corrigé final (Tâche 9 commit `4aa94a3` — correction pixel-size multi-caractères)

```
ResolveFont:
  pixelSize = effectivePt * FontSizeScale
  sfW  = SF.MeasureString(CalibString) * exactScale
  fssW = FSS.MeasureString(CalibString) at pixelSize
  ratio = sfW / fssW  (< 1.0 : FSS était légèrement plus large)
  pixelSize *= ratio  ← corrige TOUS les caractères proportionnellement

MeasureText(font, "Hello")  [FSS natif sur font corrigé]
  │
  └─ FSS.MeasureString("Hello") ≈ SF * exactScale = W₁
     → ParseLines wraps at same point as SF → 2 lines ✓

DrawText(..., scale=1.0) [FSS natif, même font corrigé]
  │
  └─ visual width ≈ W₁ = MeasureText width → no clipping ✓
```

### Fichiers impactés

| Fichier | Changement |
|---------|-----------|
| `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` | MeasureText (SF delegation), MeasureGlyph (FSS native), ResolveFont (effectivePt) |
| `MGUI.Tests/Text/FSSMeasureDrawConsistencyTests.cs` | Nouveaux tests |
| `itextengine-bugs.md` | Ce fichier (statut des tâches) |
