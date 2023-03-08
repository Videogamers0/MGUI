using MGUI.Core.UI;
using MGUI.Core.UI.XAML;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KnownColor = System.Drawing.KnownColor;
using DrawingColor = System.Drawing.Color;
using System.Reflection;
using MGUI.Core.UI.Brushes.Fill_Brushes;

namespace MGUI.Samples.Controls
{
    public readonly record struct NamedColor(string Name, Color Color)
    {
        public IFillBrush FillBrush { get; } = new MGSolidFillBrush(Color);
    }

    internal static class ColorUtils
    {
        private static readonly IEnumerable<PropertyInfo> _colorProperties =
            typeof(DrawingColor).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(DrawingColor));

        public static string GetApproximateColorName(DrawingColor color)
        {
            int minDistance = int.MaxValue;
            string minColor = DrawingColor.Black.Name;

            foreach (var colorProperty in _colorProperties)
            {
                var colorPropertyValue = (DrawingColor)colorProperty.GetValue(null, null);
                if (colorPropertyValue.R == color.R && colorPropertyValue.G == color.G && colorPropertyValue.B == color.B)
                {
                    return colorPropertyValue.Name;
                }

                int distance = Math.Abs(colorPropertyValue.R - color.R) + Math.Abs(colorPropertyValue.G - color.G) + Math.Abs(colorPropertyValue.B - color.B);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minColor = colorPropertyValue.Name;
                }
            }

            return minColor;
        }
    }

    public class ComboBoxSamples : SampleBase
    {
        private static DrawingColor ToDrawingColor(Color c) => DrawingColor.FromArgb(c.A, c.R, c.G, c.B);
        private static string GetColorName(Color c) => ColorUtils.GetApproximateColorName(ToDrawingColor(c));

        public ComboBoxSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "ComboBox.xaml")
        {
            MGTextBox TextBox1 = Window.GetElementByName<MGTextBox>("TextBox1");
            string XAMLString =
@"<ComboBox xmlns:System=""clr-namespace:System;assembly=mscorlib"" 
          ItemType=""{x:Type System:String}"">
    <System:String>A</System:String>
    <System:String>B</System:String>
    <System:String>C</System:String>
</ComboBox>";
            TextBox1.SetText(XAMLString);

            ILookup<int, DrawingColor> ColorLookup = Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>().Select(DrawingColor.FromKnownColor).ToLookup(c => c.ToArgb());

            List<Color> Colors = new List<Color>()
            {
                Color.Black,
                Color.White,
                Color.Gray,
                Color.Silver,
                Color.Maroon,
                Color.Red,
                Color.Purple,
                Color.Fuchsia,
                Color.Green,
                Color.Lime,
                Color.Olive,
                Color.Yellow,
                Color.Navy,
                Color.Blue,
                Color.Teal,
                Color.Aqua
            };

            ReadOnlyCollection<NamedColor> NamedColors = Colors.Select(x => new NamedColor(GetColorName(x), x)).ToList().AsReadOnly();

            foreach (string ComboBoxName in new List<string>() { "ComboBox1", "ComboBox2", "ComboBox3" })
            {
                MGComboBox<NamedColor> CB = Window.GetElementByName<MGComboBox<NamedColor>>(ComboBoxName);
                CB.SetItemsSource(NamedColors);
                CB.SelectedItem = NamedColors.First(x => x.Color == Color.Green);
            }
        }
    }
}
