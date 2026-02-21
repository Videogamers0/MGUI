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

### 3. Corriger le bug workaround ScrollViewer (L564-575) — ✅ état actuel + plan profond

**Fichier :** `MGContextMenu.cs`

**Problème originel :** Quand le ScrollViewer vertical (Auto) est visible et le menu est
réouvert, les items ignorent la largeur du scrollbar.

**État actuel (commit `1fe8b8e`):** Le `SV.InvalidateLayout()` a été déplacé dans
`InvokeContextMenuOpening()` avec explication du cache stale. Le workaround fonctionne
mais ne résout pas la root cause dans le moteur de layout.

---

#### Analyse de la root cause (résultat d'investigation)

**Chaîne de mesure lors de l'ouverture :**
```
TryOpenContextMenu
  → InvokeContextMenuOpening()          ← workaround ici
  → ComputeContentSize(100,40,1000,800)
      → UpdateMeasurement(MaxSize)       ← sur le MGContextMenu
          → TryGetRecentSelfMeasurement  ← CanCacheSelfMeasurement=true → peut retourner du cache
          → UpdateContentMeasurement     ← délègue au ScrollViewer
              → SV.UpdateMeasurement     ← CanCacheSelfMeasurement=false → re-mesure toujours
                  → MeasureSelfOverride  ← calcule scrollbar width
                      → Content.UpdateMeasurement ← ItemsPanel (StackPanel)
                          → TryGetRecentSelfMeasurement ← CanCacheSelfMeasurement=true → CACHE
  → ApplySizeToContent(SizeToContent.WidthAndHeight, ...)
      → LayoutChanged(this, true)        ← InvalidateLayout() sur ancêtres
      → UpdateLayout(bounds)
          → TryGetCachedMeasurement      ← check IsLayoutValid
```

**L'élément clé :** `TryGetCachedMeasurement` dans `UpdateLayout` (L1828) vérifie
`IsLayoutValid`. Après la première ouverture, `IsLayoutValid = true`
(L1936 : `UpdateLayout` le set à `true` en fin de méthode). Sur le cycle
`Close → Re-open` :

1. **Fermeture :** Rien n'invalide le layout. `IsLayoutValid` reste `true` sur le menu
   ET ses enfants (ScrollViewer, ItemsPanel, items).
2. **Réouverture :** `ComputeContentSize` → la mesure est recalculée via `UpdateMeasurement`
   (pas `UpdateLayout`), donc `TryGetCachedMeasurement` n'est PAS impliqué ici. Mais
   `ApplySizeToContent` appelle `UpdateLayout` enSUITE :
   - `LayoutChanged(this, true)` invalide le ContextMenu (pas les enfants en profondeur)
   - Le SV a `IsLayoutValid=true` → `TryGetCachedMeasurement` peut retourner un cache
     de l'arrangement précédent, dont les bounds tenaient compte d'un scrollbar qui n'est
     peut-être plus pertinent

**La vraie root cause est double :**
1. **Pas d'invalidation à la fermeture :** `InvokeContextMenuClosed` ne touche pas au layout.
   Un menu fermé devrait être considéré « layout-invalide » pour le prochain open.
2. **Pas de propagation descendante :** `InvalidateLayout()` ne recurse PAS dans l'arbre.
   `LayoutChanged()` propage vers le **haut** (parents), pas vers le **bas** (enfants).
   Quand le ContextMenu invalide son layout, le ScrollViewer et l'ItemsPanel gardent
   `IsLayoutValid = true` avec des caches de mesure potentiellement stale.

---

#### Sous-tâches pour le fix correct

##### 3a. Ajouter `InvalidateLayoutTree()` sur `MGElement`

**Fichier :** `MGElement.cs`

**Action :** Ajouter une méthode récursive descendante :
```csharp
/// <summary>Recursively invalidates the layout of this element and all its
/// descendants. Use when the entire subtree's cached measurements may be stale,
/// e.g. when a popup window closes and will be re-opened later.</summary>
internal protected void InvalidateLayoutTree()
{
    InvalidateLayout();
    foreach (MGElement child in GetVisualTreeChildren())
        child.InvalidateLayoutTree();
}
```

**Pré-requis :** Vérifier que `GetVisualTreeChildren()` (ou équivalent) est accessible.
Sinon utiliser le pattern de traversal existant dans le framework.

**Risque :** Performance si l'arbre est profond. Pour un ContextMenu avec ~5-20 items,
c'est négligeable. Ne PAS appeler par frame.

##### 3b. Invalider le layout tree à la fermeture du menu

**Fichier :** `MGContextMenu.cs`

**Action :** Dans `InvokeContextMenuClosed()` :
```csharp
internal void InvokeContextMenuClosed()
{
    // Invalidate the entire layout tree so the next open starts with clean
    // caches. Without this, TryGetCachedMeasurement in UpdateLayout can
    // return stale values from the previous open cycle, causing items to
    // overlap the scrollbar.
    InvalidateLayoutTree();

    NPC(nameof(IsContextMenuOpen));
    ContextMenuClosed?.Invoke(this, EventArgs.Empty);
}
```

**Effet :** Après fermeture, TOUS les caches de mesure (self et full) sont vidés sur tout
le sous-arbre. Le prochain open commence un layout pass entièrement frais.

##### 3c. Retirer le workaround de `InvokeContextMenuOpening`

**Fichier :** `MGContextMenu.cs`

**Action :** Retirer l'appel à `ScrollViewerElement.InvalidateLayout()` dans
`InvokeContextMenuOpening()`, puisque la tâche 3b a déjà invalidé tout le sous-arbre
à la fermeture. Conserver un commentaire explicatif.

**Validation :** Créer un test reproductible :
1. Ouvrir un ContextMenu avec suffisamment d'items pour triggerr le scrollbar vertical
2. Fermer le menu
3. Réouvrir le menu
4. Vérifier que les items ne chevauchent pas le scrollbar

**Si le bug re-apparaît** malgré l'invalidation à la fermeture, le problème est plus profond
(peut-être dans `UpdateContentLayout` du ScrollViewer qui re-mesure le content avec des
caches stale). Dans ce cas, garder le workaround dans `InvokeContextMenuOpening` ET
l'invalidation à la fermeture (ceinture et bretelles).

##### 3d. (Optionnel) Fix structurel dans `MGScrollViewer.UpdateContentLayout`

**Fichier :** `MGScrollViewer.cs` (L580-600)

**Problème potentiel :** `UpdateContentLayout` appelle `Content.UpdateMeasurement` mais si
le content (ItemsPanel) utilise des mesures cachées, le résultat peut être stale. La
décision d'afficher le scrollbar dépend du contenu, mais le contenu est mesuré sans savoir
si le scrollbar sera affiché → **problème circulaire**.

**Action :** Dans `UpdateContentLayout`, avant l'appel à `Content.UpdateMeasurement`,
forcer l'invalidation du content si la visibilité du scrollbar a changé depuis la dernière
layout pass :
```csharp
protected override void UpdateContentLayout(Rectangle Bounds)
{
    if (Content != null)
    {
        // If scrollbar visibility might have changed, force fresh content measurement
        if (VSBVisibility == ScrollBarVisibility.Auto || HSBVisibility == ScrollBarVisibility.Auto)
            Content.InvalidateLayout();
        // ... existing code
    }
}
```

**Risque :** Peut légèrement impacter les performances car le content est re-mesuré à chaque
arrange quand le scrollbar est Auto. Ne faire QUE si 3a-3c ne suffisent pas.
Si appliqué, le workaround et l'invalidation à la fermeture deviennent redondants pour ce
cas spécifique (mais restent utiles pour d'autres cas de cache stale).

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
PHASE 1 — Terminée (tâches 1-6)
  Tâche 6  ✅  tests FitMenuToViewport
  Tâche 1  ✅  ContextMenuOpening cancellable
  Tâche 2  ✅  ItemsFactory
  Tâche 4  ✅  doc double-souscription
  Tâche 5  ✅  ContextMenuRequested sur MGElement
  Tâche 3  ✅  workaround déplacé dans InvokeContextMenuOpening (partiel)

PHASE 2 — Fix profond du cache de layout
  Tâche 3a (InvalidateLayoutTree sur MGElement)
    ↓
  Tâche 3b (Invalider le layout tree à la fermeture du menu)
    ↓
  Tâche 3c (Retirer le workaround de InvokeContextMenuOpening + valider)
    ↓
  Tâche 3d (Optionnel: fix structurel dans MGScrollViewer.UpdateContentLayout)
```

## Critères de validation

- [x] Build OK, pas de warnings nouveaux
- [x] `FitMenuToViewport` couvert par tests unitaires (16 tests)
- [ ] Les menus existants (ComboBox dropdown, XAML samples) fonctionnent sans régression
- [x] Le docking context menu fonctionne avec le nouveau `ItemsFactory` OU `ContextMenuRequested`
- [ ] Le workaround ScrollViewer est remplacé par un fix propre (3a→3c) ou conservé avec justification
- [x] Pas d'allocations par frame ajoutées (les menus sont créés on-demand, pas par tick)
- [ ] Test reproductible : ouvrir menu avec scrollbar → fermer → réouvrir → layout correct
- [ ] `InvalidateLayoutTree()` disponible comme utilitaire général sur `MGElement`
