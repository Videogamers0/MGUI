using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers.Grids;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Dialogs.Stardew_Valley
{
    public class SDVInventory : SampleBase
    {
        public SDVInventory(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}.{nameof(Stardew_Valley)}", $"{nameof(SDVInventory)}.xaml")
        {
            Rectangle WindowScreenBounds = MGElement.ApplyAlignment(Desktop.ValidScreenBounds, HorizontalAlignment.Center, VerticalAlignment.Center, new(Window.WindowWidth, Window.WindowHeight));
            Window.TopLeft = WindowScreenBounds.TopLeft();

            MGButton Button_Close = Window.GetElementByName<MGButton>("Button_Close");
            Button_Close.AddCommandHandler((Button, e) =>
            {
                _ = Window.TryCloseWindow();
            });

            MGUniformGrid UniformGrid_Inventory = Window.GetElementByName<MGUniformGrid>("UniformGrid_Inventory");

            IFillBrush Border1 = new Color(214, 143, 84).AsFillBrush();
            IFillBrush Border2 = new Color(255, 228, 161).AsFillBrush();
            IBorderBrush CellBorderBrush = new MGDockedBorderBrush(Border2, Border1, Border1, Border2);
            UniformGrid_Inventory.CellBackground.NormalValue = new MGBorderedFillBrush(new(3), CellBorderBrush, new Color(255, 195, 118).AsFillBrush(), true);
        }
    }
}
