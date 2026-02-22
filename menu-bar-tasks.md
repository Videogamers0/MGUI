# Plan d'implémentation — Contrôle MenuBar (MGUI)

## Contexte

Ajout d'un contrôle `MGMenuBar` au framework MGUI (MonoGame). Ce contrôle représente la barre de menus horizontale affichée en haut d'une fenêtre (`MGWindow`), avec des menus déroulants hiérarchiques.

**Fonctionnalités prioritaires :**
- Menus hiérarchiques (niveaux illimités) + séparateurs
- Items désactivables (grisé), checkable (toggle), radio group
- Icône + texte

---

## Conventions

- **Langue du code** : anglais (noms de classes, propriétés, commentaires XML)
- **Langue des échanges** : français
- **Commit** : un commit atomique après chaque tâche terminée
- **Style** : factuel, concis, aucun storytelling

### Livrable après chaque tâche

```
## Résumé
- [quelques lignes max décrivant les modifications]

## Attendu
- [comportement attendu du code après cette tâche]

## À tester
- [ce que l'utilisateur doit vérifier manuellement]
```

---

## Fichiers de référence (patterns à suivre)

| Concept | Fichier de référence |
|---|---|
| Contrôle runtime | `MGUI.Core/UI/MGContextMenu.cs`, `MGContextMenuItem.cs` |
| Hiérarchie de classes | `MGElement` → `MGContentHost` → `MGSingleContentHost` / `MGMultiContentHost` |
| Enums | `MGUI.Core/UI/Enums.cs` (`MGElementType`) |
| Items context menu | `MGContextMenuItem.cs` (`ContextMenuItemType`, `MGContextMenuButton`, `MGContextMenuToggle`, `MGContextMenuSeparator`, `MGWrappedContextMenuItem`) |
| Radio group | `MGUI.Core/UI/MGRadioButton.cs` (`MGRadioButtonGroup`) |
| XAML classes | `MGUI.Core/UI/XAML/ContextMenu.cs`, `XAMLParser.cs` (dictionnaire `ElementNameAliases`) |
| Thème | `MGUI.Core/UI/MGTheme.cs` (backgrounds par `MGElementType`, `ThemeFontSettings`) |
| Samples | `MGUI.Samples/Controls/ContextMenu.xaml` + `.xaml.cs`, `Compendium.xaml.cs` (`SampleBase`) |
| Icônes | `MGUI.Core/UI/MGImage.cs` (propriétés `SourceName`, `Source`) |

---

## Tâches

### Tâche 1 — Enums et types de base

**Fichiers :** `MGUI.Core/UI/Enums.cs`

**Actions :**
1. Décommenter / ajouter `MenuBar` et `MenuItem` dans l'enum `MGElementType` (lignes ~54-55 où se trouvent les commentaires `//MenuBar?` et `//MenuItem?`)
2. Vérifier qu'aucun `switch` existant sur `MGElementType` ne casse (chercher `switch.*ElementType` dans le projet)

**Commit :** `feat(MenuBar): add MenuBar and MenuItem to MGElementType enum`

---

### Tâche 2 — Classe MGMenuItem (items de menu)

**Fichier à créer :** `MGUI.Core/UI/MGMenuItem.cs`

**Actions :**
1. Définir l'enum `MenuItemType` : `Normal`, `Toggle`, `Radio`, `Separator`
2. Créer la classe abstraite `MGMenuItem` héritant de `MGSingleContentHost` avec `MGElementType.MenuItem`
   - Propriété `MenuItemType MenuItemType { get; }`
   - Propriétés helper : `bool IsNormal`, `bool IsToggle`, `bool IsRadio`, `bool IsSeparator`
3. Créer `MGMenuItemBase` (abstraite, hérite `MGMenuItem`) — base pour les items affichables (Normal/Toggle/Radio)
   - Propriété `MGImage Icon` (icône optionnelle, affichée à gauche du texte)
   - Propriété `string CommandId` (identifiant de commande, comme dans `MGContextMenuButton`)
   - Propriété `bool IsEnabled` (hérité de `MGElement`, mais vérifier l'intégration visuelle — grisage)
   - Contenu wrappé dans un `MGButton` via un `ButtonWrapperTemplate` (pattern de `MGContextMenu`)
   - Zone header à gauche (icône / checkmark) + zone texte, même layout que `MGWrappedContextMenuItem`
   - Propriété `List<MGMenuItem> Children` — sous-items (niveaux illimités)
   - Si `Children.Count > 0`, afficher une flèche à droite (→) indiquant un sous-menu (pattern de `MGWrappedContextMenuItem.SubmenuArrowElement`)
4. Créer `MGMenuItemNormal` (hérite `MGMenuItemBase`)
   - Événement `Action<MGMenuItemNormal> OnSelected`
   - Au clic : fermer tous les menus ouverts + déclencher `OnSelected`
5. Créer `MGMenuItemToggle` (hérite `MGMenuItemBase`)
   - Propriété `bool IsChecked` (avec `NPC`)
   - Dessiner un checkmark dans la zone header quand `IsChecked == true` (pattern de `MGContextMenuToggle`)
   - Au clic : toggle `IsChecked` + événement `OnToggled`
6. Créer `MGMenuItemRadio` (hérite `MGMenuItemBase`)
   - Propriété `bool IsChecked` (avec `NPC`)
   - Propriété `string GroupName` — identifiant du radio group
   - Utiliser un mécanisme similaire à `MGRadioButtonGroup` pour l'exclusion mutuelle au sein du même `GroupName`
   - Dessiner un indicateur radio (bullet/dot) dans la zone header quand `IsChecked == true`
   - Au clic : sélectionner cet item + décocher les autres du même groupe
7. Créer `MGMenuItemSeparator` (hérite `MGMenuItem`)
   - Contient un `MGSeparator` horizontal
   - Aucune interaction (pas de hover, pas de clic)

**Design notes :**
- Les sous-menus (`Children`) sont affichés via un `MGContextMenu` positionné à droite de l'item (pour les items de sous-menus) ou en-dessous (pour les items de premier niveau). Réutiliser `MGContextMenu` comme conteneur popup est acceptable pour uniformiser le comportement.
- La gestion `IsEnabled = false` doit griser tout l'item visuellement (texte + icône). C'est déjà géré par le `VisualState` de `MGElement`.

**Commit :** `feat(MenuBar): add MGMenuItem hierarchy (Normal, Toggle, Radio, Separator)`

---

### Tâche 3 — Classe MGMenuBar (barre de menus)

**Fichier à créer :** `MGUI.Core/UI/MGMenuBar.cs`

**Actions :**
1. Créer `MGMenuBar` héritant de `MGSingleContentHost` avec `MGElementType.MenuBar`
2. Contenu interne : un `MGStackPanel` horizontal (`Orientation.Horizontal`) nommé `ItemsPanel`
3. Propriété `ObservableCollection<MGMenuItem> TopLevelItems`
   - Handler `CollectionChanged` pour synchroniser avec `ItemsPanel` (pattern identique à `MGContextMenu._Items`)
4. Méthodes helper pour ajout rapide :
   - `MGMenuItemNormal AddMenuItem(string text, Action<MGMenuItemNormal> onSelected = null)`
   - `MGMenuItemToggle AddToggleItem(string text, bool isChecked = false)`
   - `MGMenuItemRadio AddRadioItem(string text, string groupName, bool isChecked = false)`
   - `MGMenuItemSeparator AddSeparator()`
5. `MGComponent<MGBorder> BorderComponent` (bordure autour de la barre, pattern de `MGButton`)
6. Propriété `Func<MGWindow, MGButton> ButtonWrapperTemplate` — template pour wrapper chaque top-level item
7. **Gestion d'ouverture/fermeture des sous-menus :**
   - Propriété `bool IsMenuActive` — `true` quand un sous-menu est ouvert
   - Propriété `MGMenuItem ActiveTopLevelItem` — l'item dont le sous-menu est ouvert
   - Au clic sur un top-level item : ouvrir son sous-menu (positionné sous l'item)
   - Au survol d'un autre top-level item **pendant que `IsMenuActive == true`** : fermer l'ancien sous-menu, ouvrir le nouveau (comportement standard de barre de menus)
   - Au clic en dehors de la barre et des sous-menus : fermer tout (`IsMenuActive = false`)
   - À l'appui sur `Escape` : fermer tout
8. Événements :
   - `event EventHandler<MGMenuItemNormal> ItemSelected` — remonté depuis n'importe quel item Normal (y compris imbriqué)
   - `event EventHandler<MGMenuItemToggle> ItemToggled` — remonté depuis n'importe quel item Toggle
   - `event EventHandler<MGMenuItemRadio> RadioItemSelected` — remonté depuis n'importe quel item Radio
9. **Radio groups** : maintenir un `Dictionary<string, List<MGMenuItemRadio>> RadioGroups` interne, mis à jour quand des items radio sont ajoutés/retirés (récursivement dans les sous-menus)

**Commit :** `feat(MenuBar): add MGMenuBar control with submenu management`

---

### Tâche 4 — Intégration thème

**Fichier :** `MGUI.Core/UI/MGTheme.cs`

**Actions :**
1. Ajouter des backgrounds par défaut pour `MGElementType.MenuBar` et `MGElementType.MenuItem` dans le dictionnaire `_Backgrounds`
   - `MenuBar` : fond similaire à un `ToolBar` ou un `Window` header (couleur neutre semi-transparente)
   - `MenuItem` : pas de fond par défaut (transparent), hover avec surbrillance
2. Ajouter `MenuBarFontSize` dans `ThemeFontSettings` (ou réutiliser `ContextMenuFontSize`)
3. Vérifier les couleurs pour chaque `BuiltInTheme` (Dark, Light, etc.)
4. S'assurer que les items désactivés (`IsEnabled = false`) utilisent bien l'opacité/grisage du `VisualState` existant

**Commit :** `feat(MenuBar): integrate MenuBar/MenuItem with theme system`

---

### Tâche 5 — Classes XAML (parsing)

**Fichiers :**
- Créer : `MGUI.Core/UI/XAML/MenuBar.cs`
- Modifier : `MGUI.Core/UI/XAML/XAMLParser.cs`

**Actions :**
1. Dans `MenuBar.cs`, créer les classes XAML suivantes (pattern identique à `MGUI.Core/UI/XAML/ContextMenu.cs`) :
   - `MenuBar` héritant de `SingleContentHost`
     - Attribut `[ContentProperty(nameof(Items))]`
     - `override MGElementType ElementType => MGElementType.MenuBar`
     - Propriété `List<MenuItem> Items`
     - Propriété `Button ButtonWrapperTemplate` (optionnelle)
     - `CreateElementInstance` → `new MGMenuBar(Window)`
     - `ApplyDerivedSettings` → ajouter chaque item, appliquer le template
     - `GetChildren()` → yield return tous les items + template
   - `MenuItem` (abstraite) héritant de `SingleContentHost`
   - `MenuItemNormal` héritant de `MenuItem`
     - Propriétés : `CommandId`, `Icon` (type `Image`), `List<MenuItem> Children`
     - `CreateElementInstance` → crée un `MGMenuItemNormal` et l'ajoute au parent
   - `MenuItemToggle` héritant de `MenuItem`
     - Propriétés : `IsChecked`, `CommandId`, `Icon`
   - `MenuItemRadio` héritant de `MenuItem`
     - Propriétés : `IsChecked`, `GroupName`, `CommandId`, `Icon`
   - `MenuItemSeparator` héritant de `MenuItem`
     - Propriété optionnelle `Separator` (configuration du séparateur)

2. Dans `XAMLParser.cs`, ajouter au dictionnaire `ElementNameAliases` :
   ```csharp
   // Noms complets
   { "MenuBar", nameof(MenuBar) },
   { "MenuItemNormal", nameof(MenuItemNormal) },
   { "MenuItemToggle", nameof(MenuItemToggle) },
   { "MenuItemRadio", nameof(MenuItemRadio) },
   { "MenuItemSeparator", nameof(MenuItemSeparator) },

   // Alias courts
   { "MB", nameof(MenuBar) },
   { "MI", nameof(MenuItemNormal) },
   { "MIT", nameof(MenuItemToggle) },
   { "MIR", nameof(MenuItemRadio) },
   { "MIS", nameof(MenuItemSeparator) },
   ```

**Commit :** `feat(MenuBar): add XAML parsing classes for MenuBar`

---

### Tâche 6 — Sample / démo

**Fichiers à créer :**
- `MGUI.Samples/Controls/MenuBar.xaml`
- `MGUI.Samples/Controls/MenuBar.xaml.cs`

**Fichiers à modifier :**
- `MGUI.Samples/Compendium.xaml.cs` (enregistrer le sample)
- `MGUI.Samples/Compendium.xaml` (ajouter un tab pour MenuBar)
- `MGUI.Samples/MGUI.Samples.csproj` (embedded resource si nécessaire)

**Actions :**
1. Créer `MenuBar.xaml` avec un exemple complet :
   ```xml
   <MenuBar>
     <MenuItemNormal Text="File">
       <MenuItemNormal Text="New" Icon="icon_new" />
       <MenuItemNormal Text="Open" Icon="icon_open" />
       <MenuItemNormal Text="Save" Icon="icon_save" />
       <MenuItemSeparator />
       <MenuItemNormal Text="Recent">
         <MenuItemNormal Text="Document1.txt" />
         <MenuItemNormal Text="Document2.txt" />
       </MenuItemNormal>
       <MenuItemSeparator />
       <MenuItemNormal Text="Exit" />
     </MenuItemNormal>
     <MenuItemNormal Text="Edit">
       <MenuItemNormal Text="Cut" Icon="icon_cut" />
       <MenuItemNormal Text="Copy" Icon="icon_copy" />
       <MenuItemNormal Text="Paste" Icon="icon_paste" />
     </MenuItemNormal>
     <MenuItemNormal Text="View">
       <MenuItemToggle Text="Show Grid" IsChecked="true" />
       <MenuItemToggle Text="Show Rulers" />
       <MenuItemSeparator />
       <MenuItemRadio Text="Small" GroupName="ViewSize" />
       <MenuItemRadio Text="Medium" GroupName="ViewSize" IsChecked="true" />
       <MenuItemRadio Text="Large" GroupName="ViewSize" />
     </MenuItemNormal>
   </MenuBar>
   ```
2. Créer `MenuBar.xaml.cs` — classe `MenuBarSamples` héritant de `SampleBase`
   - S'abonner aux événements `ItemSelected`, `ItemToggled`, `RadioItemSelected`
   - Afficher l'action dans un `MGTextBlock` de statut en bas de la fenêtre
3. Enregistrer dans `Compendium.xaml.cs` : `MenuBarSamples = new(Content, Desktop);`
4. Ajouter un onglet dans `Compendium.xaml`

**Commit :** `feat(MenuBar): add MenuBar sample with File/Edit/View menus`

---

### Tâche 7 — Navigation clavier

**Fichiers :** `MGUI.Core/UI/MGMenuBar.cs`, `MGUI.Core/UI/MGMenuItem.cs`

**Actions :**
1. Quand la barre est active (`IsMenuActive == true`) :
   - `←` / `→` : naviguer entre les top-level items (ouvrir le sous-menu du voisin)
   - `↑` / `↓` : naviguer dans le sous-menu ouvert
   - `Enter` : activer l'item surligné
   - `Escape` : fermer le sous-menu courant / désactiver la barre
2. Optionnel : `Alt` ou `F10` pour activer la barre de menus (convention Windows)
3. Utiliser `KeyboardHandler` (pattern existant dans `MGElement`)

**Commit :** `feat(MenuBar): add keyboard navigation (arrows, Enter, Escape)`

---

### Tâche 8 — Raccourcis clavier (accélérateurs)

**Fichiers :** `MGUI.Core/UI/MGMenuItem.cs`, `MGUI.Core/UI/MGMenuBar.cs`

**Actions :**
1. Ajouter une propriété `string ShortcutText` sur `MGMenuItemBase` (ex: `"Ctrl+S"`)
   - Affichage uniquement : texte aligné à droite dans l'item (comme dans les menus Windows)
   - Ne gère pas l'input directement (le binding de raccourci est de la responsabilité de l'application)
2. Ajouter un `MGTextBlock` à droite dans le layout de l'item pour afficher `ShortcutText`
3. Ajouter la propriété XAML correspondante dans `MenuItemNormal`, `MenuItemToggle`, `MenuItemRadio`

**Commit :** `feat(MenuBar): add ShortcutText display on menu items`

---

### Tâche 9 — Tests de régression et polish

**Fichiers :** tous les fichiers modifiés

**Actions :**
1. Vérifier que le projet compile sans erreur ni warning
2. Vérifier que les samples existants (ContextMenu, etc.) fonctionnent toujours
3. Tester les cas limites :
   - Menu vide (aucun item)
   - Item sans enfants (pas de flèche)
   - Item désactivé dans un sous-menu profond
   - Radio group avec un seul item
   - Séparateur en première/dernière position
4. Vérifier le grisage visuel des items `IsEnabled = false`
5. Vérifier que le thème s'applique correctement (tous les `BuiltInTheme`)
6. Vérifier le XAML : parser → runtime → affichage cohérent

**Commit :** `fix(MenuBar): regression tests and visual polish`

---

## Ordre d'exécution recommandé

```
Tâche 1 → Tâche 2 → Tâche 3 → Tâche 4 → Tâche 5 → Tâche 6 → Tâche 7 → Tâche 8 → Tâche 9
```

Les tâches 1-6 sont le socle fonctionnel minimal. Les tâches 7-8 sont des améliorations. La tâche 9 est la validation finale.
