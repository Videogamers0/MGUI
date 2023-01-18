using MGUI.Core.UI;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MGUI.Samples.FF7_Samples
{
    [Flags]
    public enum StatusAilments
    {
        None = 0,
        Poison = 1 << 0,
        Sleep = 1 << 1,
        Silence = 1 << 2,
        Confusion = 1 << 3,
        Slow = 1 << 4,
        Haste = 1 << 5,
        Stop = 1 << 6,
        Downed = 1 << 7,
        Darkness = 1 << 8,
        Petrify = 1 << 9,
        Frog = 1 << 10,
        All = None | Poison | Sleep | Silence | Confusion | Slow | Haste | Stop | Downed | Darkness | Petrify | Frog
    }

    public readonly record struct Item(string Name, string Description, int HPRecovery, double HPRecoveryPercent, int MPRecovery, double MPRecoveryPercent,
        StatusAilments HealedAilments = StatusAilments.None, StatusAilments AppliedAilments = StatusAilments.None);

    public class InventoryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string szPropertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(szPropertyName)); }
        /// <summary>Notify Property Changed for the given <paramref name="szPropertyName"/></summary>
        public void NPC(string szPropertyName) { NotifyPropertyChanged(szPropertyName); }

        public Inventory Inventory { get; }
        public Item Item { get; }
        public string Name => Item.Name;
        public string Description => Item.Description;

        private int _Quantity;
        public int Quantity
        {
            get => _Quantity;
            set
            {
                if (_Quantity != value)
                {
                    int Previous = Quantity;
                    _Quantity = value;
                    QuantityChanged?.Invoke(this, new(Previous, Quantity));
                    NPC(nameof(Quantity));
                }
            }
        }

        public event EventHandler<EventArgs<int>> QuantityChanged;

        public InventoryItem(Inventory Inventory, Item Item, int Quantity)
        {
            this.Inventory = Inventory;
            this.Item = Item;
            this.Quantity = Quantity;
        }
    }

    public class Inventory
    {
        public Texture2D ItemIcon { get; }

        private Dictionary<Item, InventoryItem> IndexedItems { get; } = new();
        private ObservableCollection<InventoryItem> _Items { get; } = new();
        public IReadOnlyList<InventoryItem> Items => _Items;

        public bool RemoveItem(Item Item)
        {
            if (IndexedItems.TryGetValue(Item, out InventoryItem InventoryItem))
            {
                IndexedItems.Remove(Item);
                _Items.Remove(InventoryItem);
                if (SelectedItem == InventoryItem)
                    SelectedItem = null;
                return true;
            }
            else
                return false;
        }

        public InventoryItem AddItem(Item Item, int Quantity)
        {
            if (!IndexedItems.TryGetValue(Item, out InventoryItem InventoryItem))
            {
                InventoryItem = new(this, Item, Quantity);
                _Items.Add(InventoryItem);
                IndexedItems.Add(Item, InventoryItem);
            }
            else
                InventoryItem.Quantity += Quantity;
            return InventoryItem;
        }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            private set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;

                    if (IsVisible)
                        Desktop.Windows.Add(Window);
                    else
                        Desktop.Windows.Remove(Window);
                }
            }
        }

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;

        public MGDesktop Desktop { get; }
        public MGWindow Window { get; }
        public Party Party { get; }

        private PartyMember _SelectedPartyMember;
        public PartyMember SelectedPartyMember
        {
            get => _SelectedPartyMember;
            set
            {
                if (_SelectedPartyMember != value)
                {
                    _SelectedPartyMember = value;
                }
            }
        }

        private InventoryItem _SelectedItem;
        public InventoryItem SelectedItem
        {
            get => _SelectedItem;
            set
            {
                if (_SelectedItem != value)
                {
                    _SelectedItem = value;

                    //  Update the item description label at the top of the window
                    ItemDescriptionLabel.SetText(SelectedItem?.Description ?? "Select an Item");

                    if (SelectedItem == null)
                        ItemsList.ClearSelection();
                    else if (ItemsList.SelectedItems.Count != 1 || ItemsList.SelectedItems.First().Data != SelectedItem)
                        ItemsList.SelectItem(SelectedItem);
                }
            }
        }

        private MGListBox<InventoryItem> ItemsList { get; }
        private MGTextBlock ItemDescriptionLabel { get; }

        public Inventory(ContentManager Content, MGDesktop Desktop, Party Party, Action<Inventory> InitializeItems)
        {
            this.ItemIcon = Content.Load<Texture2D>(Path.Combine("Icons", "Item"));
            Desktop.AddNamedTexture("FF7ItemIcon", ItemIcon);

            this.Desktop = Desktop;
            this.Party = Party;

            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples)}.{nameof(FF7_Samples)}.{nameof(Inventory)}UI.xaml";
            string XAML = ReadEmbeddedResourceAsString(Assembly.GetExecutingAssembly(), ResourceName);
            this.Window = XAMLParser.LoadRootWindow(Desktop, XAML, false, true, null);

            Rectangle WindowScreenBounds = MGElement.ApplyAlignment(Desktop.ValidScreenBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new(Window.WindowWidth, Window.WindowHeight));
            Window.TopLeft = WindowScreenBounds.TopLeft();

            MGListBox<PartyMember> PartyList = Window.GetElementByName<MGListBox<PartyMember>>("PartyList");
            PartyList.SetItemsSource(Party.Members.ToList());

            PartyList.ItemContainerStyle = (Border) =>
            {
                PartyList.ApplyDefaultItemContainerStyle(Border);
                Border.BorderThickness = new(0);
                Border.BackgroundBrush.SelectedValue = null;
            };

            InitializeItems?.Invoke(this);
            this.ItemsList = Window.GetElementByName<MGListBox<InventoryItem>>("ItemsList");
            ItemsList.SetItemsSource(_Items);

            ItemsList.ItemContainerStyle = (Border) =>
            {
                ItemsList.ApplyDefaultItemContainerStyle(Border);
                Border.BorderThickness = new(0);
                Border.Padding = new(6, 3);
            };

            ItemsList.SelectionChanged += (sender, e) => { this.SelectedItem = e.FirstOrDefault()?.Data; };

            this.ItemDescriptionLabel = Window.GetElementByName<MGTextBlock>("ItemDescriptionLabel");

            //  When a character is clicked, try to use the selected item on them
            foreach (MGListBoxItem<PartyMember> PartyMember in PartyList.ListBoxItems)
            {
                PartyMember.ContentPresenter.MouseHandler.LMBReleasedInside += (sender, e) =>
                {
                    if (TryUseItemOn(SelectedItem, PartyMember.Data) && SelectedItem.Quantity <= 0)
                    {
                        RemoveItem(SelectedItem.Item);
                    }
                };
            }

            //  Handle clicking on the close button
            MGButton CloseButton = Window.GetElementByName<MGButton>("CloseButton");
            CloseButton.AddCommandHandler((btn, e) =>
            {
                this.Hide();
            });
        }

        private bool TryUseItemOn(InventoryItem InventoryItem, PartyMember Character)
        {
            if (InventoryItem != null && InventoryItem.Quantity > 0 && Character != null)
            {
                Item Item = InventoryItem.Item;
                bool Used = false;

                //  Apply the item's HP recovery
                if (!Item.HealedAilments.HasFlag(StatusAilments.Downed) && (Item.HPRecovery != 0 || Item.HPRecoveryPercent != 0.0))
                {
                    int PreviousHP = Character.CurrentHP;
                    if (Item.HPRecovery != 0)
                        Character.CurrentHP = Math.Clamp(Character.CurrentHP + Item.HPRecovery, 0, Character.MaxHP);
                    if (Item.HPRecoveryPercent != 0.0)
                        Character.CurrentHP = Math.Clamp(Character.CurrentHP + (int)(Character.MaxHP * Item.HPRecoveryPercent), 0, Character.MaxHP);
                    Used = Character.CurrentHP != PreviousHP;
                }

                //  Apply the item's MP recovery
                if (Item.MPRecovery != 0 || Item.MPRecoveryPercent != 0.0)
                {
                    int PreviousMP = Character.CurrentMP;
                    if (Item.MPRecovery != 0)
                        Character.CurrentMP = Math.Clamp(Character.CurrentMP + Item.MPRecovery, 0, Character.MaxMP);
                    if (Item.MPRecoveryPercent != 0.0)
                        Character.CurrentMP = Math.Clamp(Character.CurrentMP + (int)(Character.MaxMP * Item.MPRecoveryPercent), 0, Character.MaxMP);
                    Used = Character.CurrentMP != PreviousMP;
                }

                //TODO: apply other effects like status ailment recovery or effects that apply to entire party etc

                if (Used)
                {
                    InventoryItem.Quantity--;
                    return true;
                }
            }

            return false;
        }

        private static string ReadEmbeddedResourceAsString(Assembly CurrentAssembly, string ResourceName)
        {
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream(ResourceName))
            using (StreamReader Reader = new StreamReader(ResourceStream))
                return Reader.ReadToEnd();
        }
    }
}
