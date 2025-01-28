using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public class GridColorPickerSamples : SampleBase
    {
        public GridColorPickerSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "GridColorPicker.xaml")
        {
            Window.GetElementByName<MGTextBox>("SampleTB1").Text = @"<GridColorPicker Columns=""8"" CommaSeparatedColors=""Black,White,Red,Green,Blue,Yellow,Purple,Orange"" />";

            Window.GetElementByName<MGGridColorPicker>("GCP1").SelectedColorIndexes = new List<int>() { 1, 5, 24, 25, 26 };
            Window.GetElementByName<MGGridColorPicker>("GCP2").SelectedColorIndexes = new List<int>() { 3 };
            Window.GetElementByName<MGGridColorPicker>("GCP3").SelectedColorIndexes = new List<int>() { 3 };
            Window.GetElementByName<MGGridColorPicker>("GCP4").SelectedColorIndexes = new List<int>() { 3 };

            Window.WindowDataContext = this;
        }
    }
}
