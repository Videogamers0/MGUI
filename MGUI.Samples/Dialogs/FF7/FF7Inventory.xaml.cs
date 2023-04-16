using MGUI.Core.UI.XAML;
using MGUI.Core.UI;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.Collections.ObjectModel;

namespace MGUI.Samples.Dialogs.FF7
{
    public class InventoryItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string szPropertyName) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(szPropertyName)); }
        /// <summary>Notify Property Changed for the given <paramref name="szPropertyName"/></summary>
        public void NPC(string szPropertyName) { NotifyPropertyChanged(szPropertyName); }

        public FF7Inventory Inventory { get; }
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

        public InventoryItem(FF7Inventory Inventory, Item Item, int Quantity)
        {
            this.Inventory = Inventory;
            this.Item = Item;
            this.Quantity = Quantity;
        }
    }

    public class FF7Inventory : SampleBase
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
                    NPC(nameof(SelectedItem));

                    if (SelectedItem == null)
                        ItemsList.ClearSelection();
                    else if (ItemsList.SelectedItems.Count != 1 || ItemsList.SelectedItems.First().Data != SelectedItem)
                        ItemsList.SelectItem(SelectedItem, true);
                }
            }
        }

        private MGListBox<InventoryItem> ItemsList { get; }
        //private MGTextBlock ItemDescriptionLabel { get; }

        public FF7Inventory(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(FF7)}", $"{nameof(FF7Inventory)}.xaml")
        {
            Rectangle WindowScreenBounds = MGElement.ApplyAlignment(Desktop.ValidScreenBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new(Window.WindowWidth, Window.WindowHeight));
            Window.TopLeft = WindowScreenBounds.TopLeft();

            ItemIcon = Content.Load<Texture2D>(Path.Combine("Icons", "Item"));
            Desktop.Resources.AddTexture("FF7ItemIcon", new MGTextureData(ItemIcon));

            //  Create a sample Party
            this.Party = new();
            PartyMember Barret = Party.AddMember("Barret", new MGTextureData(Content.Load<Texture2D>(Path.Combine("Portraits", "Barret"))), 65077, 1601, 199);
            Barret.CurrentHP = (int)(Barret.CurrentHP * 0.92);
            Barret.CurrentMP = (int)(Barret.CurrentMP * 0.90);
            PartyMember Cloud = Party.AddMember("Cloud", new MGTextureData(Content.Load<Texture2D>(Path.Combine("Portraits", "Cloud"))), 70140, 1455, 232);
            Cloud.CurrentHP = (int)(Cloud.CurrentHP * 0.77);
            Cloud.CurrentMP = (int)(Cloud.CurrentMP * 0.48);
            PartyMember RedXIII = Party.AddMember("Red XIII", new MGTextureData(Content.Load<Texture2D>(Path.Combine("Portraits", "Red XIII"))), 55126, 1477, 204);
            RedXIII.CurrentHP = (int)(RedXIII.CurrentHP * 0.65);
            RedXIII.CurrentMP = (int)(RedXIII.CurrentMP * 0.80);

            MGListBox<PartyMember> PartyList = Window.GetElementByName<MGListBox<PartyMember>>("PartyList");
            PartyList.SetItemsSource(Party.Members.ToList());

            PartyList.ItemContainerStyle = (Border) =>
            {
                PartyList.ApplyDefaultItemContainerStyle(Border);
                Border.BorderThickness = new(0);
                Border.BackgroundBrush.SelectedValue = null;
            };

            //  Add some items to the inventory
            AddItem(Item.Potion, 43);
            AddItem(Item.HiPotion, 16);
            AddItem(Item.XPotion, 1);
            AddItem(Item.MiniEther, 7);
            AddItem(Item.Ether, 2);
            AddItem(Item.TurboEther, 7);
            AddItem(Item.Elixir, 1);
            AddItem(Item.PhoenixDown, 6);
            AddItem(Item.PoisonousBrew, 25);
            AddItem(Item.Antidote, 22);
            AddItem(Item.EyeDrop, 12);
            AddItem(Item.Soft, 15);
            AddItem(Item.MaidensKiss, 4);
            AddItem(Item.EchoScreen, 10);
            AddItem(Item.Remedy, 5);
            AddItem(Item.Megalixir, 2);

            ItemsList = Window.GetElementByName<MGListBox<InventoryItem>>("ItemsList");
            ItemsList.SetItemsSource(_Items);

            ItemsList.ItemContainerStyle = (Border) =>
            {
                ItemsList.ApplyDefaultItemContainerStyle(Border);
                Border.BorderThickness = new(0);
                Border.Padding = new(6, 3);
            };

            ItemsList.SelectionChanged += (sender, e) => { SelectedItem = e.FirstOrDefault()?.Data; };

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
            CloseButton.AddCommandHandler((btn, e) => { Hide(); });

            Window.WindowDataContext = this;
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
    }
}
