# MGContextMenu — Améliorations & corrections

## Contexte

`MGContextMenu` (703 lignes, `MGUI.Core/UI/MGContextMenu.cs`) est un control MGUI complet
qui hérite de `MGWindow` et implémente `IContextMenuHost`.

Lors de l'implémentation du docking manager, un bug de **duplication d'items** a été
rencontré : lorsqu'on ajoutait dynamiquement des items via `ContextMenuOpening`, chaque
rappel de `RebuildTabHeaders` ajoutait un nouveau handler, provoquant l'accumulation
d'items à chaque clic droit.

Le contournement actuel (créer un `MGContextMenu` frais par clic) fonctionne mais met en
lumière plusieurs faiblesses de l'API.

---

## Fichiers concernés

| Fichier | Rôle |
|---|---|
| `MGUI.Core/UI/MGContextMenu.cs` | Control principal (items, open/close, submenus, auto-close) |
| `MGUI.Core/UI/MGContextMenuItem.cs` | Items (Button, Toggle, Separator) |
| `MGUI.Core/UI/MGElement.cs` | Propriété `ContextMenu` + `TryOpenContextMenuOnRightClick` (L655-692) |
| `MGUI.Core/UI/MGDesktop.cs` | `TryOpenContextMenu` côté Desktop (L160-210) |

---

## Tâches

### 1. Rendre `ContextMenuOpening` cancellable

**Fichier :** `MGContextMenu.cs` (L95, L109-111)

**Problème :** `ContextMenuOpening` utilise `EventArgs.Empty`. Impossible pour un subscriber
d'annuler l'ouverture côté `MGContextMenu` (c'est possible côté `MGDesktop` via
`ContextMenuOpeningClosingEventArgs.Cancel`).

**Action :**
1. Créer ou réutiliser une classe `CancelEventArgs` (ou `ContextMenuOpeningClosingEventArgs`).
2. Modifier la signature de l'event :
   ```csharp
   public event EventHandler<CancelEventArgs> ContextMenuOpening;
   ```
3. Dans `InvokeContextMenuOpening()`, vérifier `Cancel` et retourner un `bool` :
   ```csharp
   internal bool InvokeContextMenuOpening()
   {
       var args = new CancelEventArgs();
       ContextMenuOpening?.Invoke(this, args);
       return !args.Cancel;
   }
   ```
4. Adapter les deux appelants (`MGDesktop.TryOpenContextMenu` L186 et
   `MGContextMenu.TryOpenContextMenu` L319) pour interrompre l'ouverture si `Cancel == true`.
5. Vérifier que le subscriber existant dans le constructeur (L571, bug workaround
   `SV.InvalidateLayout()`) et celui dans `MGContextMenuItem` (L115) sont compatibles.

**Risque :** Breaking change si des consommateurs externes castent les args. Fournir un
overload ou un typedef backward-compatible si nécessaire.

---

### 2. Ajouter un callback `ItemsFactory` pour la construction différée d'items

**Fichier :** `MGContextMenu.cs`

**Problème :** Le pattern courant "souscrire à `ContextMenuOpening` → `Clear()` → `AddButton()`"
est fragile : si le handler est souscrit N fois, les items sont ajoutés N fois. Le framework
ne propose pas d'alternative propre pour les menus dynamiques.

**Action :**
1. Ajouter une propriété `Func<MGContextMenu, IEnumerable<MGContextMenuItem>>? ItemsFactory` :
   ```csharp
   public Func<MGContextMenu, IEnumerable<MGContextMenuItem>>? ItemsFactory { get; set; }
   ```
2. Dans `InvokeContextMenuOpening()`, si `ItemsFactory != null` :
   - Appeler `Items.Clear()`.
   - Appeler le factory et ajouter les items retournés.
3. Documenter que `ItemsFactory` remplace le pattern `ContextMenuOpening += Clear+Add`.
4. Exemples dans la XML-doc.

**Limite :** Le factory ne doit PAS faire d'allocations coûteuses (rappeler dans la doc).
Optionnellement accepter un `Action<MGContextMenu>` en alternative (plus flexible, l'appelant ajoute directement via `AddButton`/`AddToggle`).

---

### 3. Corriger le bug workaround ScrollViewer (L564-575)

**Fichier :** `MGContextMenu.cs` (L564-575)

**Problème :** Un workaround marque `// Good luck to future me` : quand le ScrollViewer
vertical est visible et le menu est réouvert, le layout ignore la largeur du scrollbar.
Le fix actuel est `ContextMenuOpening += SV.InvalidateLayout()`.

**Action :**
1. Investiguer si le commit référencé (`dd4c1d6`) a résolu le problème sous-jacent.
2. Reproduire : créer un menu avec assez d'items pour afficher le scrollbar vertical,
   ouvrir → fermer → réouvrir, vérifier le layout.
3. Si le bug persiste, investiguer `MGScrollViewer` et `ScrollBarVisibility.Auto` :
   - La mesure initiale est-elle invalidée correctement quand la visibilité du scrollbar change ?
   - `TryGetRecentSelfMeasurement()` renvoie-t-il un stale measurement ?
4. Fixer la root cause (probablement dans `MGScrollViewer.MeasureOverride` ou cache de mesure).
5. Retirer le workaround une fois corrigé.

**Risque :** Touche au cœur du layout. Tester avec des menus courts (pas de scrollbar),
longs (scrollbar visible), et à la limite (scrollbar apparaît/disparaît).

---

### 4. Protéger contre la double-souscription event

**Fichier :** `MGContextMenu.cs`

**Problème :** Rien n'empêche de souscrire N fois à `ContextMenuOpening` et d'obtenir N
exécutions. C'est la root cause du bug de duplication dans le docking.

**Actions possibles (choisir une) :**

**Option A — Documentation + pattern recommandé :**
Ajouter dans la XML-doc de `ContextMenuOpening` un avertissement :
```xml
/// <remarks>
/// Warning: subscribing multiple times will result in multiple invocations.
/// For dynamic items, prefer using <see cref="ItemsFactory"/> (see task 2).
/// If you must use this event, ensure you unsubscribe before re-subscribing,
/// or use the fresh-menu-per-interaction pattern.
/// </remarks>
```

**Option B — `ContextMenuOpening` one-shot guard :**
Stocker un flag `_isInContextMenuOpening` pour empêcher la ré-entrée :
```csharp
private bool _isInContextMenuOpening;
internal bool InvokeContextMenuOpening()
{
    if (_isInContextMenuOpening) return true;
    _isInContextMenuOpening = true;
    try { /* fire event */ }
    finally { _isInContextMenuOpening = false; }
}
```

**Recommandation :** Option A (doc) + tâche 2 (`ItemsFactory`) résout le problème
structurellement sans ajouter de complexité.

---

### 5. `MGElement.ContextMenu` — supporter les menus dynamiques

**Fichier :** `MGElement.cs` (L655-692)

**Problème :** La propriété `MGElement.ContextMenu` stocke une instance unique réutilisée.
Le handler `TryOpenContextMenuOnRightClick` appelle `ContextMenu.TryOpenContextMenu(position)`
à chaque clic droit. Si le menu doit être dynamique (items changeants), le consommateur
est obligé de soit :
- Utiliser `ContextMenuOpening` (fragile, cf. tâches 2/4).
- Créer un nouveau menu manuellement (workaround actuel du docking).

**Action :**
1. Ajouter un event `ContextMenuRequested` sur `MGElement` :
   ```csharp
   public event EventHandler<ContextMenuRequestedEventArgs> ContextMenuRequested;
   ```
   Avec `ContextMenuRequestedEventArgs { Menu (get/set), Position, Handled }`.
2. Dans `TryOpenContextMenuOnRightClick`, fire `ContextMenuRequested` **avant** d'ouvrir
   le menu. Si le subscriber set `Menu` à une nouvelle instance, utiliser celle-ci.
3. Cela permet le pattern :
   ```csharp
   element.ContextMenuRequested += (s, e) =>
   {
       var menu = new MGContextMenu(window, "");
       menu.AddButton("Close", ...);
       e.Menu = menu;
   };
   ```
4. Rester backward-compatible : si `ContextMenuRequested` n'a pas de subscriber, utiliser
   `this.ContextMenu` comme aujourd'hui.

**Risque :** Faible. Additive only. Documenter la priorité
(`ContextMenuRequested` > `ContextMenu` property).

---

### 6. Tests unitaires — logique pure de positionnement

**Fichier :** Nouveau fichier de tests

**Problème :** `FitMenuToViewport` (static, ligne 46) est de la logique pure testable mais
n'a pas de tests.

**Action :**
1. Écrire des tests pour `FitMenuToViewport(Rectangle anchor, Size menuSize, Rectangle viewport)` :
   - Menu plus petit que le viewport → positionné sous l'anchor.
   - Menu déborde à droite → décalé à gauche.
   - Menu déborde en bas → affiché au-dessus de l'anchor.
   - Menu plus grand que le viewport → clampé.
2. Si d'autres méthodes de calcul pur existent (hit-test, auto-close distance), les tester aussi.

---

## Ordre d'exécution recommandé

```
Tâche 6 (tests FitMenuToViewport)     — pas de dépendance, base de confiance
  ↓
Tâche 1 (ContextMenuOpening cancellable) — pre-requis pour le factory
  ↓
Tâche 2 (ItemsFactory)                 — résout le pattern d'usage fragile
  ↓
Tâche 4 (doc/guard double-souscription) — complémente le factory
  ↓
Tâche 5 (ContextMenuRequested sur MGElement) — amélioration API consommateur
  ↓
Tâche 3 (fix ScrollViewer workaround)  — investigation indépendante, peut être parallélisée
```

## Critères de validation

- [ ] Build OK, pas de warnings nouveaux
- [ ] `FitMenuToViewport` couvert par tests unitaires
- [ ] Les menus existants (ComboBox dropdown, XAML samples) fonctionnent sans régression
- [ ] Le docking context menu fonctionne avec le nouveau `ItemsFactory` OU `ContextMenuRequested`
- [ ] Le workaround ScrollViewer est investigué (corrigé ou documenté)
- [ ] Pas d'allocations par frame ajoutées (les menus sont créés on-demand, pas par tick)
