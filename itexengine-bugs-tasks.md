# ITextEngine — Bugs & Tasks (PR #35)

Tâches à effectuer séquentiellement par un agent IA.  
**Règle : committer après chaque tâche terminée** avec un message de commit descriptif.

> Contexte : La PR #35 introduit une abstraction `ITextEngine` pour découpler le rendu de texte
> de MonoGame `SpriteFont`. Le propriétaire du repo (Videogamers0) a identifié des régressions
> dans le word-wrapping et la mesure de texte. Le bug principal est un comportement
> non-idempotent de `ParseLines` : parser avec `MaxLineWidth=92` retourne 1 ligne de
> `LineWidth=87`, mais re-parser avec `MaxLineWidth=87` retourne 2 lignes (51 + 37).

---

## Tâche 1 — Reproduire le bug d'idempotence de `ParseLines` avec un test unitaire

**Fichiers concernés :**
- `MGUI.Core/UI/Text/MGTextLine.cs` (méthode `ParseLines`, ligne ~231)
- Nouveau fichier de test (créer si nécessaire, ex: `MGUI.Tests/Text/ParseLinesIdempotencyTests.cs`)

**Description :**  
Écrire un ou plusieurs tests unitaires qui reproduisent le scénario suivant :
1. Créer un mock `ITextMeasurer` qui retourne des mesures cohérentes (basées sur la largeur réelle des caractères, par exemple 7px/char pour simuler "Stardew Valley").
2. Appeler `MGTextLine.ParseLines(measurer, maxWidth: 92, wrapText: true, runs, false)` → vérifier qu'on obtient 1 ligne.
3. Récupérer le `LineWidth` de cette ligne (ex: 87).
4. Appeler `MGTextLine.ParseLines(measurer, maxWidth: 87, wrapText: true, runs, false)` → **le test doit échouer** car on obtient 2 lignes au lieu de 1.

Le test documente le bug et servira de test de non-régression après le fix.

**Commit :** `test(text): add ParseLines idempotency regression test (red)`

---

## Tâche 2 — Diagnostiquer la cause racine de la non-idempotence

**Fichiers concernés :**
- `MGUI.Core/UI/Text/MGTextLine.cs` — méthode `ParseLines` (lignes 231–503)

**Description :**  
Analyser le code de `ParseLines` pour identifier précisément pourquoi la largeur de la ligne
varie entre les deux appels. La cause probable est la divergence entre :

- **Accumulation mot-par-mot** (`CurrentX`) : pendant le wrapping, chaque mot est mesuré
  individuellement via `MeasureText(word)`. La somme des largeurs mot-par-mot est utilisée pour
  décider si le mot rentre sur la ligne courante (`CurrentX + TotalWidth <= MaxLineWidth`).
- **Re-mesure par run complète** (`FlushLine`) : au moment de flush, chaque run est re-mesuré
  comme un string entier via `MeasureText(run.Text)`. Grâce au kerning, cette mesure peut être
  **plus petite** que la somme des mots individuels.

Résultat : `ParseLines(92)` décide que "Stardew Valley" rentre sur une ligne (car la somme
mot-par-mot ≤ 92), puis `FlushLine` calcule `LineWidth = 87` (mesure entière de la chaîne avec kerning).
Mais `ParseLines(87)` utilise la somme mot-par-mot pour décider → "Stardew " + "Valley" > 87 → wrap sur 2 lignes.

**Livrable :** Ajouter des commentaires dans `ParseLines` documentant la cause racine identifiée
et la stratégie de fix prévue (voir Tâche 3).

**Commit :** `docs(text): document ParseLines idempotency root cause in comments`

---

## Tâche 3 — Corriger la non-idempotence de `ParseLines`

**Fichiers concernés :**
- `MGUI.Core/UI/Text/MGTextLine.cs` — méthode `ParseLines`
- `MGUI.Core/UI/Text/MGTextLine.cs` — méthode `FlushLine` (closure interne)

**Description :**  
Corriger le bug pour que `ParseLines` soit idempotent : si `ParseLines(W)` retourne des lignes
dont la plus large fait `W'`, alors `ParseLines(W')` doit retourner le même nombre de lignes.

**Approche recommandée :**  
Aligner la décision de wrapping avec la mesure finale. Deux stratégies possibles :

### Option A — Utiliser la mesure whole-string pour la décision de wrapping (recommandée)
Au lieu de comparer `CurrentX + TotalWidth` (somme mot-par-mot) avec `MaxLineWidth`,
re-mesurer le texte accumulé de la ligne courante **incluant** le mot candidat comme un seul
string. Si `MeasureText(lineText + wordText) <= MaxLineWidth`, le mot rentre.

**Inconvénient :** plus de calls à `MeasureText`, potentiel impact perf. Mitigeable avec du
caching.

### Option B — Utiliser la somme mot-par-mot dans `FlushLine` aussi
Au lieu de re-mesurer chaque run dans `FlushLine`, utiliser `CurrentX` comme `LineWidth`.

**Inconvénient :** perd la précision du kerning dans `LineWidth`, ce qui peut affecter
l'alignement (centrage, droite). Moins bon.

### Option C — Arrondir `LineWidth` vers le haut dans `FlushLine`
Utiliser `Math.Ceiling()` sur le `LineWidth` retourné par `FlushLine`, et comparer avec
`Math.Floor()` de `MaxLineWidth` dans la décision de wrapping, pour éliminer les micro-erreurs
de flottants.

**Ce qu'il faut éviter :** changer la signature de `ParseLines` ou casser la compatibilité.

**Commit :** `fix(text): make ParseLines idempotent by aligning wrap decision with FlushLine measurement`

---

## Tâche 4 — Vérifier que le test de la Tâche 1 passe (vert)

**Fichiers concernés :**
- Les tests créés en Tâche 1

**Description :**  
Exécuter les tests unitaires. Le test de non-régression de la Tâche 1 doit maintenant passer.
Ajuster le test si nécessaire (ex: corriger les valeurs attendues si la mesure whole-string
modifie légèrement le `LineWidth`).

**Commit :** `test(text): ParseLines idempotency test now green`

---

## Tâche 5 — Corriger `MeasureSelfOverride` pour éviter la re-mesure incohérente

**Fichiers concernés :**
- `MGUI.Core/UI/MGTextBlock.cs` — méthode `MeasureSelfOverride` (ligne ~641)

**Description :**  
Dans `MeasureSelfOverride`, la largeur finale retournée est `Math.Ceiling(Max(LineWidth))`.
Le framework WPF-like peut ensuite rappeler la mesure avec cette largeur comme contrainte.
Si `ParseLines` est maintenant idempotent (Tâche 3), ce scénario devrait fonctionner.

Vérifier que :
1. `MeasureSelfOverride(availableWidth=92)` → parse → `LineWidth=87` → retourne `Ceiling(87) = 87`
2. Si re-mesuré avec `availableWidth=87` → `ParseLines(87)` doit retourner 1 ligne (identique)

Ajouter un test pour ce scénario si possible.

**Commit :** `test(text): add MeasureSelfOverride re-measure consistency test`

---

## Tâche 6 — Vérifier les samples visuellement (Stardew Valley button + FF7 Inventory)

**Fichiers concernés :**
- `MGUI.Samples/Controls/Compendium.xaml` — bouton "Stardew Valley"
- `MGUI.Samples/Dialogs/FF7Inventory.xaml` — TextBlock description

**Description :**  
Dans le projet `MGUI.Samples` :
1. Vérifier que le bouton "Stardew Valley" dans la fenêtre Compendium affiche le texte
   complet sans troncature.
2. Vérifier que dans la fenêtre FF7 Inventory, en cliquant sur "Potion", le texte
   "Restores health by 100" s'affiche entièrement sans troncature.

Si les samples ne peuvent pas être exécutés (pas de GPU), documenter les vérifications
à faire manuellement.

**Commit :** `docs(samples): add manual verification notes for text-wrapping regression`

---

## Tâche 7 — Améliorer `RecalculateTextLayouts()` pour invalider correctement tout l'arbre

**Fichiers concernés :**
- `MGUI.Core/UI/MGDesktop.cs` — méthode `RecalculateTextLayouts` (ligne ~56)

**Description :**  
Le propriétaire du repo a noté que `RecalculateTextLayouts()` ne traverse que les `MGTextBlock`
et appelle `RefreshTextEngine()` sur chacun, mais ne propage pas l'invalidation aux parents non-TextBlock
(les conteneurs dont la taille dépend du contenu texte).

`RefreshTextEngine()` appelle `InvokeLayoutChanged()` → `LayoutChanged(this, true)` qui propage
**vers le haut** (parents). Cela devrait être suffisant en théorie pour invalider la chaîne.

**Vérifier** que :
1. `RefreshTextEngine()` est bien appelé sur **tous** les TextBlocks (y compris ceux dans les
   composants imbriqués : borders, tooltips, context menus).
2. La propagation vers le haut invalide correctement les conteneurs parents.
3. Les runs de texte sont bien recalculés (pas seulement le cache vidé).

Si la propagation upward ne suffit pas (cas des conteneurs qui ne re-mesurent pas leurs enfants
après invalidation), utiliser l'approche de Videogamers0 :

```csharp
public void RecalculateTextLayouts()
{
    foreach (MGWindow window in Windows)
    {
        foreach (MGTextBlock tb in window.TraverseVisualTree<MGTextBlock>(
            true, true, true, true, MGElement.TreeTraversalMode.Preorder))
        {
            tb.RefreshTextEngine();
        }
        // Force full tree invalidation after all text blocks are re-resolved
        window.InvalidateLayoutTree();
    }
}
```

**Commit :** `fix(desktop): RecalculateTextLayouts invalidates full layout tree after font re-resolution`

---

## Tâche 8 — Harmoniser les null-checks dans `DrawTransaction` (DrawShadowedText vs MeasureText)

**Fichiers concernés :**
- `MGUI.Shared/Rendering/DrawTransaction.cs`

**Description :**  
Copilot Review a identifié une incohérence :
- `DrawShadowedText` (ligne ~217) vérifie `resolved.NativeFont == null` → return early
- `MeasureText` (ligne ~238) vérifie `resolved.NativeFont == null && resolved.IsFallback` → return early

Harmoniser les deux vérifications. La condition correcte est probablement
`resolved.NativeFont == null` (simple) car si `NativeFont` est null, on ne peut ni dessiner
ni mesurer, que ce soit un fallback ou non.

**Commit :** `fix(rendering): harmonize null-check logic between DrawShadowedText and MeasureText`

---

## Tâche 9 — Documenter la thread-safety de `FontSizeScale`

**Fichiers concernés :**
- `MGUI.FontStashSharp/FontStashSharpTextEngine.cs` — propriété `FontSizeScale`

**Description :**  
Copilot Review a noté que `FontSizeScale` est mutable (`public set`) et lu pendant
`ResolveFont()`. Dans MonoGame tout est single-threaded, mais la documentation devrait
être explicite. Soit :

- Ajouter `/// <remarks>` documentant que la propriété ne doit pas être changée après
  la première résolution de font, OU
- La rendre `init`-only si c'est compatible avec le flow actuel (elle est déjà settée dans
  `MatchSpriteFontSizing`).

Vérifier si `FontSizeScale` est modifié après `MatchSpriteFontSizing`. Si oui, garder le setter
mais documenter. Si non, le rendre `init`-only.

**Commit :** `docs(fss): clarify FontSizeScale thread-safety and mutation contract`

---

## Tâche 10 — Nettoyage final et vérification globale

**Fichiers concernés :**
- Tous les fichiers modifiés dans les tâches précédentes

**Description :**
1. Exécuter tous les tests unitaires et vérifier qu'ils passent.
2. Vérifier qu'il n'y a pas d'erreurs de compilation dans la solution complète.
3. Supprimer tout code de diagnostic/debug temporaire ajouté pendant le développement.
4. Vérifier que les commentaires ajoutés sont clairs et utiles.

**Commit :** `chore(text): final cleanup and green test suite`
