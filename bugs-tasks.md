# MGUI — Liste de tâches : Bugs & TODOs

> **Instructions pour l'agent IA** : Traiter chaque tâche dans l'ordre. **Commiter après chaque tâche terminée** avec un message descriptif. Ne pas regrouper plusieurs tâches dans un seul commit.
>
> **Chaque correction DOIT inclure une preuve vérifiable** (test unitaire, log, démo, assertion, ou screenshot). La preuve doit être incluse dans le même commit que le fix.
>
> **Légende** : ✅ Terminée &nbsp;|&nbsp; ⬜ À faire

---

## Priorité Critique

### ✅ Tâche 1 — Memory leak dans MGListView (HandleTemplatedContentRemoved)
- **Fichier** : `MGUI.Core/UI/MGListView.cs` (ligne ~53)
- **Bug** : `HandleTemplatedContentRemoved` n'est jamais appelé lorsque des contenus créés automatiquement via `CellTemplate` sont supprimés. Les anciens `DataBindings` restent abonnés à `PropertyChanged`, causant une fuite mémoire.
- **Action** : Implémenter l'appel à `HandleTemplatedContentRemoved` aux endroits appropriés (suppression de lignes, changement d'`ItemsSource`, destruction du `ListView`). Ajouter un mécanisme de nettoyage des bindings dans le cycle de vie des cellules.
- **Preuve** : Créer un test unitaire qui : (1) crée un `MGListView` avec des items bindés, (2) supprime des items ou change l'`ItemsSource`, (3) vérifie via `WeakReference` que les anciens éléments sont bien collectés par le GC après `GC.Collect()`. Ajouter un log `Debug.WriteLine("[MGListView] Cleaned up {count} DataBindings")` dans `HandleTemplatedContentRemoved`.
- **Commit** : `fix: clean up DataBindings in MGListView to prevent memory leak`

### ⬜ Tâche 2 — Dictionnaires statiques non thread-safe dans DataBinding
- **Fichier** : `MGUI.Core/UI/Data Binding/DataBinding.cs` (lignes ~571-577)
- **Bug** : `CachedCanConvertFrom` et `CachedCanConvertTo` sont des `static Dictionary` accédés sans synchronisation. Accès concurrents possibles si des opérations de binding se font depuis plusieurs threads.
- **Action** : Remplacer par `ConcurrentDictionary<(Type, Type), bool>`. Le TODO mentionne aussi que le cache ne tient pas compte du `ITypeDescriptorContext` — documenter cette limitation ou corriger le cache key.
- **Preuve** : Créer un test unitaire multi-threadé qui accède aux caches `CachedCanConvertFrom`/`CachedCanConvertTo` depuis 4+ threads en parallèle via `Task.WhenAll`. Le test doit passer sans `ConcurrentModificationException` ni résultats incohérents.
- **Commit** : `fix: make DataBinding TypeConverter caches thread-safe`

### ⬜ Tâche 3 — Initialisation de polices silencieusement ignorée
- **Fichier** : `MGUI.Samples/Game1.cs` (ligne ~113)
- **Bug** : Le bloc `catch (Exception ex) { Debug.WriteLine(ex); }` avale toute erreur d'initialisation de polices. L'UI se retrouve avec des polices manquantes sans notification à l'utilisateur.
- **Action** : Ajouter un logging explicite ou relancer l'exception avec un message informatif. Au minimum, afficher un avertissement visible dans la console/sortie Debug.
- **Preuve** : Simuler une erreur de chargement de police (chemin invalide) et vérifier que : (1) un message d'erreur clair apparaît dans la sortie Debug, (2) l'application ne crash pas silencieusement. Ajouter un log avec le niveau `[ERROR]` : `Debug.WriteLine($"[ERROR] Font initialization failed: {ex.Message}")`. Capturer un screenshot de la sortie console montrant le message.
- **Commit** : `fix: properly handle font initialization failures in Game1`

---

## Priorité Haute

### ⬜ Tâche 4 — Bug de mesure des composants dans MGElement
- **Fichier** : `MGUI.Core/UI/MGElement.cs` (lignes ~107-120)
- **Bug** : Deux bugs documentés :
  1. Les composants partagent leur taille avec d'autres composants au lieu de ne la partager qu'avec le contenu.
  2. Les composants ne peuvent pas partager leur taille avec le Padding.
  Affecte le layout de `MGListBox` et `MGTabControl`.
- **Action** : Revoir la logique de `MeasureOverride` pour séparer correctement le calcul de taille des composants vs contenu. Ajouter des tests unitaires pour les cas edge.
- **Preuve** : Créer un sample de démo `ComponentMeasureTest` dans `MGUI.Samples` qui affiche un `MGListBox` et un `MGTabControl` avec des bordures colorées montrant les bounds calculés. Ajouter des assertions `Debug.Assert` dans `MeasureOverride` vérifiant que la taille d'un composant ne dépasse jamais la taille disponible. Vérifier visuellement que le layout est correct.
- **Commit** : `fix: correct component measurement logic in MGElement`

### ⬜ Tâche 5 — Bug d'ActualLayoutBounds avec le Padding parent
- **Fichier** : `MGUI.Core/UI/MGElement.cs` (lignes ~1482-1500)
- **Bug** : `Rectangle.Intersect(Parent.ActualLayoutBounds, this.LayoutBounds)` ne prend pas correctement en compte le Padding du parent. Un `#if true` workaround est en place.
- **Action** : Implémenter une solution propre qui compresse l'`ActualLayoutBounds` en tenant compte du Padding du parent, tout en gérant les cas spéciaux (ex: `MGTabControl` dont le HeadersPanel n'a pas le Padding appliqué). Supprimer le `#if true` block.
- **Preuve** : Ajouter un `Debug.Assert` dans `Update()` vérifiant que `ActualLayoutBounds` est toujours contenu dans le parent's `ActualLayoutBounds` (moins le Padding quand applicable). Créer un test visuel avec un élément enfant dans un parent avec un Padding de 20px et vérifier que les bounds sont corrects via un overlay de debug.
- **Commit** : `fix: properly account for parent Padding in ActualLayoutBounds calculation`

### ⬜ Tâche 6 — Bug off-by-one dans MGTextBox (sélection de backslashes)
- **Fichier** : `MGUI.Core/UI/MGTextBox.cs` (lignes ~197-199)
- **Bug** : Bug connu de décalage d'index lors de la sélection de backslashes consécutifs, dû à la gestion des caractères échappés.
- **Action** : Corriger le calcul d'index dans la logique de sélection pour gérer correctement les séquences de backslashes consécutifs. Ajouter des cas de test.
- **Preuve** : Créer un test unitaire avec les cas suivants : (1) texte `"a\\b"` — sélectionner chaque backslash individuellement, (2) texte `"\\\\"` (4 backslashes) — sélectionner du 2ème au 3ème, (3) vérifier que `SelectionStart` et `SelectionLength` correspondent aux indices attendus dans chaque cas. Le test doit passer sans erreur.
- **Commit** : `fix: correct off-by-one in MGTextBox escaped character selection`

### ⬜ Tâche 7 — Bug d'échappement dans FormattedTextTokenizer
- **Fichier** : `MGUI.Core/UI/Text/FormattedTextTokenizer.cs` (lignes ~426-435)
- **Bug** : Les caractères doublement échappés suivis immédiatement d'un crochet ouvrant ne sont pas gérés correctement. La validation du nombre impair d'échappements est manquante.
- **Action** : Ajouter une logique de comptage des caractères d'échappement consécutifs avant un crochet ouvrant. Seul un nombre impair devrait échapper le crochet.
- **Preuve** : Créer un test unitaire qui tokenize les chaînes suivantes et vérifie les résultats : (1) `"\\[Bold]"` → le `[` est échappé (littéral), (2) `"\\\\[Bold]"` → le `[` n'est PAS échappé (double escape = littéral backslash + tag Bold), (3) `"\\\\\\[Bold]"` → le `[` est échappé. Vérifier le nombre de tokens produits et leur contenu.
- **Commit** : `fix: handle double-escaped characters before brackets in FormattedTextTokenizer`

### ⬜ Tâche 8 — Overlay ne bloque pas les entrées clavier
- **Fichier** : `MGUI.Core/UI/MGOverlay.cs` (ligne ~204)
- **Bug** : Le TODO indique que les entrées clavier ne sont pas bloquées par l'overlay, contrairement aux entrées souris.
- **Action** : Ajouter le blocage des entrées clavier dans la logique de l'overlay, similaire à ce qui est fait pour la souris.
- **Preuve** : Créer un sample de démo montrant : (1) un `MGTextBox` focusable sous un `MGOverlay`, (2) vérifier que les touches tapées ne sont PAS reçues par le `MGTextBox` quand l'overlay est actif, (3) vérifier qu'elles SONT reçues quand l'overlay est inactif. Ajouter un log `Debug.WriteLine("[MGOverlay] Keyboard input blocked")` quand l'input est intercepté.
- **Commit** : `fix: block keyboard inputs through MGOverlay`

### ⬜ Tâche 9 — Ordre de mise à jour ModalWindow vs NestedWindow
- **Fichier** : `MGUI.Core/UI/MGWindow.cs` (ligne ~1014)
- **Bug** : L'ordre de mise à jour entre `ModalWindow` et `NestedWindow` est potentiellement incorrect (TODO documenté).
- **Action** : Analyser l'ordre de mise à jour et s'assurer que le `ModalWindow` est toujours mis à jour après (au-dessus de) le `NestedWindow` pour une gestion correcte du focus et de l'input.
- **Preuve** : Créer un scénario de test avec une fenêtre parente ayant à la fois un `NestedWindow` et un `ModalWindow`. Ajouter des logs `Debug.WriteLine($"[Update] {WindowName} updated at tick {tick}")` dans `OnBeginUpdateContents`. Vérifier dans la sortie que le `ModalWindow` est toujours mis à jour après le `NestedWindow`. Vérifier que le focus clavier est correctement capturé par le `ModalWindow`.
- **Commit** : `fix: correct update order for ModalWindow vs NestedWindow`

---

## Priorité Moyenne

### ⬜ Tâche 10 — Catch blocks vides / exceptions avalées dans DataBinding
- **Fichier** : `MGUI.Core/UI/Data Binding/DataBinding.cs` (lignes ~403, ~434, ~450, ~459, ~489)
- **Bug** : Plusieurs blocs `catch` avalent ou ignorent silencieusement des exceptions. Les setters de propriétés peuvent échouer sans notification, laissant la cible dans un état incohérent.
- **Action** : Ajouter un logging approprié via `Debug.WriteLine` pour tous les catch blocks. Pour les setters de propriétés (lignes ~403, ~434), ajouter un mécanisme de rollback ou au minimum un état d'erreur consultable.
- **Preuve** : Créer un test unitaire qui provoque chaque type d'erreur (conversion impossible, type incompatible, setter qui throw) et vérifie que : (1) un message est bien loggé (intercepter `Debug.Listeners`), (2) la propriété `HasError` (nouveau) est `true` après l'échec, (3) `LastError` contient le message d'exception.
- **Commit** : `fix: improve error handling in DataBinding catch blocks`

### ⬜ Tâche 11 — Erreurs XAML avalées en mode Release
- **Fichier** : `MGUI.Samples/Compendium.xaml.cs` (lignes ~72-77)
- **Bug** : Les erreurs de parsing XAML sont relancées en Debug (`#if DEBUG throw;`) mais avalées en Release. Les fenêtres échouent silencieusement.
- **Action** : Ajouter un logging en mode Release et/ou afficher un indicateur visuel d'erreur au lieu de simplement ignorer.
- **Preuve** : Introduire intentionnellement une erreur XAML dans un fichier sample, compiler en Release, lancer l'application et vérifier que : (1) le message d'erreur apparaît dans `Debug.WriteLine` avec `[XAML ERROR]` prefix, (2) la fenêtre affiche un contenu d'erreur visuel (ex: fond rouge avec le message) au lieu d'être vide.
- **Commit** : `fix: log XAML parse errors in Release mode`

### ⬜ Tâche 12 — Event handlers non désabonnés (memory leaks pattern)
- **Fichiers** :
  - `MGUI.Shared/Rendering/MainRenderer.cs` (lignes ~65-69)
  - `MGUI.Core/UI/MGWindow.cs` (ligne ~1032)
  - `MGUI.Samples/Dialogs/SampleHUD.xaml.cs` (ligne ~206)
- **Bug** : Des lambdas anonymes sont abonnées à des événements (`Game.PreviewUpdate`, `ClientSizeChanged`, `WindowDataContextChanged`) et ne peuvent jamais être désabonnées.
- **Action** : Convertir les lambdas anonymes en méthodes nommées qui peuvent être désabonnées. Implémenter `IDisposable` là où c'est nécessaire pour nettoyer les abonnements.
- **Preuve** : Créer un test unitaire qui : (1) crée et dispose un `MainRenderer` / `MGWindow`, (2) vérifie via `WeakReference` que l'objet disposé est bien collecté par le GC. Ajouter un log `Debug.WriteLine($"[Dispose] {GetType().Name} unsubscribed {count} event handlers")` dans chaque `Dispose()`. Vérifier que les compteurs d'abonnés des événements source diminuent après dispose.
- **Commit** : `fix: convert anonymous event handlers to named methods for proper unsubscription`

### ⬜ Tâche 13 — Click-through sur WindowStyle.None
- **Fichier** : `MGUI.Core/UI/MGWindow.cs` (ligne ~1314)
- **Bug** : Le TODO indique que `AllowsClickThrough=false` devrait probablement être utilisé pour les fenêtres `WindowStyle.None`, mais les événements de clic passent à travers incorrectement.
- **Action** : Ajouter `AllowsClickThrough = false` par défaut pour `WindowStyle.None` et vérifier que les événements sont correctement bloqués.
- **Preuve** : Créer un sample de démo avec deux fenêtres superposées, celle du dessus en `WindowStyle.None`. Ajouter un compteur de clics sur la fenêtre du dessous. Vérifier que cliquer sur la zone de la fenêtre du dessus n'incrémente PAS le compteur. Ajouter un `Debug.Assert(AllowsClickThrough == false)` dans le constructeur quand `WindowStyle == None`.
- **Commit** : `fix: prevent click-through on WindowStyle.None windows`

### ⬜ Tâche 14 — ToolTip occlusion manquante pour fenêtres imbriquées
- **Fichier** : `MGUI.Core/UI/MGDesktop.cs` (ligne ~467)
- **Bug** : La logique d'occlusion des ToolTips ne gère pas les fenêtres imbriquées (`MGWindow.OnBeginUpdateContents`).
- **Action** : Ajouter une logique similaire de gestion des ToolTips dans le contexte des fenêtres imbriquées.
- **Preuve** : Créer un sample de démo avec une fenêtre parente contenant une fenêtre imbriquée, chaque fenêtre ayant des éléments avec des ToolTips. Vérifier que : (1) le ToolTip de la fenêtre imbriquée masque celui de la fenêtre parente quand les deux se chevauchent, (2) seul le ToolTip de l'élément le plus profond (topmost) est visible. Ajouter un log `Debug.WriteLine($"[ToolTip] Occluded by nested window: {windowName}")` quand l'occlusion se produit.
- **Commit** : `fix: handle ToolTip occlusion for nested windows`

### ⬜ Tâche 15 — Deadlock potentiel dans MGXAMLDesigner
- **Fichier** : `MGUI.Core/UI/MGXAMLDesigner.cs` (ligne ~213)
- **Bug** : `StartSTATask(Browse).Result` appelle `.Result` sur un `Task<T>` depuis un thread potentiellement non-STA, ce qui peut provoquer un deadlock.
- **Action** : Utiliser `async/await` au lieu de `.Result`. Ajouter aussi l'implémentation manquante de `TryBrowseFilePath` pour non-Windows (ligne ~216).
- **Preuve** : Vérifier que le build compile sans warning async. Ajouter un test qui appelle `TryBrowseFilePath` sur un thread non-STA et vérifie qu'il ne bloque pas (timeout de 5s). Vérifier que la version non-Windows retourne `false` avec un log `Debug.WriteLine("[WARN] File browse not supported on this platform")`.
- **Commit** : `fix: avoid potential deadlock in MGXAMLDesigner file browse`

---

## Priorité Basse — Fonctionnalités Manquantes / Améliorations

### ⬜ Tâche 16 — Implémenter les primitives de dessin d'ellipse
- **Fichier** : `MGUI.Shared/Rendering/DrawTransaction.cs` (ligne ~410)
- **TODO** : `StrokeAndFillEllipse`, `StrokeEllipse`, `FillEllipse` ne sont pas implémentés.
- **Action** : Implémenter les méthodes de dessin d'ellipse dans `DrawTransaction`.
- **Preuve** : Créer un sample de démo `EllipseDemo` dans `MGUI.Samples` qui dessine des ellipses de différentes tailles, couleurs et épaisseurs de trait. Vérifier visuellement le rendu. Ajouter un test unitaire vérifiant que les méthodes ne lèvent pas d'exception avec des paramètres valides et edge cases (rayon 0, taille 1x1).
- **Commit** : `feat: implement ellipse drawing primitives in DrawTransaction`

### ⬜ Tâche 17 — Support SetEffect dans DrawTransaction
- **Fichier** : `MGUI.Shared/Rendering/DrawTransaction.cs` (ligne ~18)
- **TODO** : `SetEffect`/`SetEffectTemporary` pour le support des shaders.
- **Action** : Implémenter le support des effets/shaders dans `DrawTransaction`.
- **Preuve** : Créer un sample de démo appliquant un `Effect` simple (ex: grayscale ou tint) à un élément UI via `SetEffectTemporary`. Vérifier visuellement que l'effet est appliqué et retiré correctement. Ajouter un test unitaire vérifiant que `SetEffect`/`SetEffectTemporary` restaure l'état précédent après le scope.
- **Commit** : `feat: add shader effect support in DrawTransaction`

### ⬜ Tâche 18 — Scrolling intégré pour MGTextBox
- **Fichier** : `MGUI.Core/UI/MGTextBox.cs` (ligne ~1202)
- **TODO** : Le scrolling intégré n'est pas implémenté.
- **Action** : Ajouter le support du scrolling horizontal/vertical dans `MGTextBox` pour les textes longs.
- **Preuve** : Créer un sample de démo avec un `MGTextBox` de largeur fixe (200px) contenant un texte long (500+ caractères). Vérifier que : (1) le curseur est toujours visible lors de la frappe, (2) le texte scrolle horizontalement quand le curseur atteint le bord, (3) Home/End scrolle correctement. Ajouter un log `Debug.WriteLine($"[MGTextBox] ScrollOffset={offset}")` pour vérifier le calcul.
- **Commit** : `feat: implement built-in scrolling for MGTextBox`

### ⬜ Tâche 19 — Fonctions Remove/Replace dans MGContextMenu
- **Fichier** : `MGUI.Core/UI/MGContextMenu.cs` (ligne ~503)
- **TODO** : Les fonctions pour supprimer/remplacer des éléments dans `_Items` ne sont pas implémentées.
- **Action** : Implémenter `RemoveItem`, `ReplaceItem`, `RemoveAt`, `Clear` dans `MGContextMenu`.
- **Preuve** : Créer un test unitaire qui : (1) ajoute 5 items, (2) `RemoveAt(2)` → vérifie Count == 4 et l'item à l'index 2 a changé, (3) `ReplaceItem(0, newItem)` → vérifie que l'item 0 est le nouveau, (4) `Clear()` → vérifie Count == 0. Ajouter un sample de démo avec un bouton "Remove Last" et "Clear" sur un ContextMenu.
- **Commit** : `feat: implement Remove/Replace operations for MGContextMenu items`

### ⬜ Tâche 20 — Click-drag scrolling pour MGScrollViewer
- **Fichier** : `MGUI.Core/UI/MGScrollViewer.cs` (ligne ~25)
- **TODO** : `AllowClickDragScrolling` n'est pas implémenté.
- **Action** : Ajouter une option pour permettre le scroll par click-drag dans le `ScrollViewer`.
- **Preuve** : Créer un sample de démo avec un `MGScrollViewer` contenant du contenu long et `AllowClickDragScrolling = true`. Vérifier que : (1) click-drag vers le haut scrolle vers le bas (scroll naturel), (2) la vélocité de scroll correspond au déplacement de la souris, (3) le scroll s'arrête au relâchement. Ajouter un log de debug montrant les événements drag.
- **Commit** : `feat: add click-drag scrolling option to MGScrollViewer`

### ⬜ Tâche 21 — Support Orientation/FlowDirection dans MGRatingControl
- **Fichier** : `MGUI.Core/UI/MGRatingControl.cs` (ligne ~398)
- **TODO** : L'orientation et le `FlowDirection` ne sont pas supportés.
- **Action** : Implémenter le support vertical et RTL pour `MGRatingControl`.
- **Preuve** : Créer un sample de démo montrant 4 variantes du `MGRatingControl` : (1) Horizontal LTR, (2) Horizontal RTL, (3) Vertical TopToBottom, (4) Vertical BottomToTop. Vérifier visuellement que chaque orientation fonctionne et que le clic sélectionne la bonne étoile. Ajouter un `Debug.Assert` vérifiant la correspondance position→rating dans chaque mode.
- **Commit** : `feat: add Orientation and FlowDirection support to MGRatingControl`

### ⬜ Tâche 22 — Tiling pour MGTextureFillBrush
- **Fichier** : `MGUI.Core/UI/Brushes/Fill Brushes/MGTextureFillBrush.cs` (ligne ~20)
- **TODO** : Le tiling (tessellation) n'est pas implémenté pour les modes de stretch autres que `Fill`.
- **Action** : Implémenter le mode de tiling pour les textures.
- **Preuve** : Créer un sample de démo montrant une texture 32x32 en mode tiling sur une surface de 200x200. Vérifier visuellement que la texture se répète correctement sans distorsion ni gaps. Ajouter un test unitaire vérifiant que le nombre de draw calls correspond au nombre attendu de tuiles.
- **Commit** : `feat: add tiling support for MGTextureFillBrush`

### ⬜ Tâche 23 — Refactoring des styles (ImplicitStyles vs ExplicitStyles)
- **Fichier** : `MGUI.Core/UI/MGResources.cs` (ligne ~215)
- **TODO** : Le système de styles devrait être séparé en styles implicites et explicites.
- **Action** : Refactorer `MGResources` pour séparer clairement la gestion des styles implicites (appliqués par type) et explicites (appliqués par clé).
- **Preuve** : Créer un test unitaire qui : (1) enregistre un style implicite pour `MGButton`, (2) enregistre un style explicite avec la clé `"PrimaryButton"`, (3) vérifie qu'un `MGButton` sans clé reçoit le style implicite, (4) vérifie qu'un `MGButton` avec `Style="PrimaryButton"` reçoit le style explicite. Vérifier que les samples existants continuent de fonctionner.
- **Commit** : `refactor: split styles into ImplicitStyles and ExplicitStyles in MGResources`

### ⬜ Tâche 24 — Refactoring des NamedToolTips
- **Fichier** : `MGUI.Core/UI/MGResources.cs` (ligne ~157)
- **TODO** : `MGWindow.NamedToolTips` devrait être refactoré en `Dictionary<Window, Dictionary<string, ToolTip>>` dans `MGResources`.
- **Action** : Déplacer la gestion des ToolTips nommés de `MGWindow` vers `MGResources` avec la structure de données appropriée.
- **Preuve** : Vérifier que les samples existants utilisant des NamedToolTips (rechercher `NamedToolTips` dans le code) fonctionnent toujours. Ajouter un test unitaire qui enregistre un ToolTip nommé dans `MGResources`, le récupère par nom et par fenêtre, et vérifie que l'ancien API `MGWindow.NamedToolTips` est marqué `[Obsolete]` et délègue au nouveau.
- **Commit** : `refactor: move NamedToolTips management to MGResources`

### ⬜ Tâche 25 — Bindings sur objets XAMLBindableBase imbriqués
- **Fichier** : `MGUI.Core/UI/XAML/Element.cs` (ligne ~309)
- **TODO** : Les bindings sur des `XAMLBindableBase` imbriqués (ex: `MGBorderedFillBrush.FillBrush`) sont silencieusement ignorés.
- **Action** : Implémenter le support de la traversée de propriétés imbriquées pour les bindings, ou au minimum ajouter un avertissement diagnostique.
- **Preuve** : Créer un test unitaire avec un binding sur `MGBorderedFillBrush.FillBrush.Color`. Si le support complet est implémenté : vérifier que le binding met à jour la couleur. Si seulement l'avertissement : vérifier que `Debug.WriteLine("[WARN] Nested binding on {path} is not supported")` est émis et interceptable via `TraceListener`.
- **Commit** : `fix: support or warn about bindings on nested XAMLBindableBase objects`

### ⬜ Tâche 26 — Gestion robuste du premier élément XAML
- **Fichier** : `MGUI.Core/UI/XAML/XAMLParser.cs` (ligne ~140)
- **TODO** : L'insertion du namespace XAML peut échouer si le document commence par un commentaire au lieu d'un `XElement`.
- **Action** : Ajouter une vérification du premier élément et gérer le cas des commentaires en tête de document.
- **Preuve** : Créer un test unitaire qui parse un document XAML commençant par `<!-- Comment -->\n<Window ...>` et vérifie que le namespace est correctement inséré et que le parsing produit un `MGWindow` valide. Tester aussi avec plusieurs commentaires et des espaces blancs en tête.
- **Commit** : `fix: handle comment-first XAML documents in XAMLParser`

### ⬜ Tâche 27 — AffectsComponents dans Style
- **Fichier** : `MGUI.Core/UI/XAML/Style.cs` (ligne ~27)
- **TODO** : Les styles ne distinguent pas entre les éléments explicites de l'arbre et les éléments composants.
- **Action** : Ajouter un booléen `AffectsComponents` pour contrôler si un style s'applique aux composants internes.
- **Preuve** : Créer un test unitaire qui : (1) définit un style avec `AffectsComponents = false` ciblant `MGBorder`, (2) l'applique à un `MGButton` (qui a un `MGBorder` interne comme composant), (3) vérifie que le style n'est PAS appliqué au composant `MGBorder` interne mais seulement aux `MGBorder` explicites dans l'arbre.
- **Commit** : `feat: add AffectsComponents option to XAML Style`

### ⬜ Tâche 28 — Effets visuels du Timer
- **Fichier** : `MGUI.Core/UI/MGTimer.cs` (ligne ~179)
- **TODO** : Effets visuels (clignotement, surbrillance, secousse à certains seuils) non implémentés.
- **Action** : Implémenter les effets visuels optionnels pour le timer.
- **Preuve** : Créer un sample de démo avec un `MGTimer` de 30 secondes montrant : (1) clignotement rouge à 10s restantes, (2) surbrillance à 5s, (3) secousse à 3s. Ajouter un log `Debug.WriteLine($"[MGTimer] Effect triggered: {effectName} at {remainingTime}s")`. Vérifier visuellement les transitions.
- **Commit** : `feat: implement visual effects for MGTimer thresholds`

### ⬜ Tâche 29 — Nettoyer les références statiques obsolètes dans MGSolidFillBrush
- **Fichier** : `MGUI.Core/UI/Brushes/Fill Brushes/MGSolidFillBrush.cs` (ligne ~22)
- **TODO** : Mettre à jour les références aux anciens `static MGSolidFillBrush` vers les nouveaux.
- **Action** : Identifier et remplacer toutes les références aux anciennes constantes statiques.
- **Preuve** : Faire un `grep` complet du projet pour les anciens noms de constantes statiques. Le résultat doit être vide après le refactoring. Vérifier que le projet compile sans warning. Lister dans le commit message les constantes remplacées (ex: `OldName → NewName`).
- **Commit** : `refactor: update MGSolidFillBrush static references to newer versions`

### ⬜ Tâche 30 — Supprimer les TODO template dans Game1.cs
- **Fichier** : `MGUI.Samples/Game1.cs` (lignes ~132, ~141)
- **Action** : Remplacer les commentaires `// TODO: Add your update/drawing logic here` par du code ou les supprimer.
- **Preuve** : Faire un `grep -n "TODO:.*Add your" MGUI.Samples/Game1.cs` — le résultat doit être vide. Vérifier que le fichier compile sans erreur.
- **Commit** : `chore: remove template TODO comments from Game1.cs`

### ⬜ Tâche 31 — Implémenter le handler Registration vide
- **Fichier** : `MGUI.Samples/Dialogs/Registration.xaml.cs` (ligne ~63)
- **TODO** : Le handler de registration est vide.
- **Action** : Soit implémenter une logique de démo, soit ajouter un commentaire explicatif, soit lever `NotImplementedException`.
- **Preuve** : Si implémenté : créer un test qui invoque le handler et vérifie un résultat attendu. Si stubé : vérifier que `NotImplementedException` est levée avec le message `"Registration handler not yet implemented"`. Ajouter un log `Debug.WriteLine("[Registration] Handler invoked")` dans tous les cas.
- **Commit** : `fix: implement or stub Registration dialog handler`

### ⬜ Tâche 32 — Tester la logique de viewport pour ScrollViewers imbriqués
- **Fichiers** :
  - `MGUI.Core/UI/Containers/Grids/MGGrid.cs` (ligne ~634)
  - `MGUI.Core/UI/Containers/Grids/MGUniformGrid.cs` (ligne ~422)
- **TODO** : La logique de viewport pour les `ScrollViewers` imbriqués à l'intérieur d'autres `ScrollViewers` n'est pas testée.
- **Action** : Créer des scénarios de test avec des ScrollViewers imbriqués et corriger les bugs découverts.
- **Preuve** : Créer un sample de démo `NestedScrollViewerTest` avec : (1) un `MGGrid` dans un `MGScrollViewer`, le tout dans un autre `MGScrollViewer`, (2) vérifier que scroller le parent et l'enfant indépendamment montre le bon contenu, (3) ajouter des assertions sur les viewport bounds des deux niveaux. Documenter les cas testés et leurs résultats dans un commentaire en tête du fichier sample.
- **Commit** : `test: validate nested ScrollViewer viewport logic in grids`

### ⬜ Tâche 33 — Appliquer ApplyBaseSettings dans Templates
- **Fichier** : `MGUI.Core/UI/XAML/Templates.cs` (ligne ~55)
- **TODO** : L'instanciation de template n'intercale pas correctement les `ApplyBaseSettings`.
- **Action** : Revoir la logique d'instanciation pour appliquer les settings de base dans le bon ordre.
- **Preuve** : Créer un test unitaire qui instancie un template avec des `ApplyBaseSettings` et vérifie que : (1) les settings de base sont appliqués AVANT les settings spécifiques du template, (2) les overrides du template prennent bien le dessus sur les base settings. Logger l'ordre d'application via `Debug.WriteLine($"[Template] Applied {settingName} = {value}")` et vérifier la séquence.
- **Commit** : `fix: properly interleave ApplyBaseSettings in template instantiation`

### ⬜ Tâche 34 — Option d'affichage de valeur pour MGSlider
- **Fichier** : `MGUI.Core/UI/MGSlider.cs` (ligne ~580)
- **TODO** : Ajouter une option pour afficher la valeur dans un `TextBlock` bordé.
- **Action** : Implémenter une propriété `ShowValueLabel` avec un `MGTextBlock` positionnable.
- **Preuve** : Créer un sample de démo avec 3 `MGSlider` : (1) `ShowValueLabel = false` (défaut, pas de label), (2) `ShowValueLabel = true` avec format `"F0"`, (3) `ShowValueLabel = true` avec format `"P0"`. Vérifier que le label se met à jour en temps réel lors du drag. Ajouter un `Debug.Assert` que la valeur affichée correspond à `Slider.Value.ToString(format)`.
- **Commit** : `feat: add value display option to MGSlider`

### ⬜ Tâche 35 — Option de resize unidirectionnel pour MGResizeGrip
- **Fichier** : `MGUI.Core/UI/MGResizeGrip.cs` (ligne ~134)
- **TODO** : Options pour restreindre le resize à un seul axe.
- **Action** : Ajouter des propriétés `AllowHorizontalResize` et `AllowVerticalResize`.
- **Preuve** : Créer un sample de démo avec 3 fenêtres resizables : (1) `AllowHorizontalResize = true, AllowVerticalResize = true` (défaut), (2) `AllowHorizontalResize = true, AllowVerticalResize = false`, (3) `AllowHorizontalResize = false, AllowVerticalResize = true`. Vérifier que le drag dans l'axe interdit ne modifie pas la taille. Ajouter un `Debug.Assert` dans le handler de resize que la taille contrainte n'a pas changé.
- **Commit** : `feat: add axis-restricted resize options to MGResizeGrip`
