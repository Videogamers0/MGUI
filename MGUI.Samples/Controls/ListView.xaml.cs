using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Samples.Controls
{
    public readonly record struct Person(int Id, string FirstName, string LastName, bool IsMale);

    public class ListViewSamples : SampleBase
    {
        public ListViewSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, nameof(Controls), "ListView.xaml")
        {
            //  Get the ListView
            MGListView<Person> ListView_Sample1 = Window.GetElementByName<MGListView<Person>>("ListView_Sample1");

            //  We already defined the CellTemplate of the first column in our XAML.
            //  But the other 2 columns will use slightly more complex logic that depends on the IsMale property, so we'll define those CellTemplates with c# code
            ListView_Sample1.Columns[1].CellTemplate = (person) => new MGTextBlock(Window, person.FirstName, person.IsMale ? Color.CornflowerBlue : Color.HotPink);
            ListView_Sample1.Columns[2].CellTemplate = (person) => new MGTextBlock(Window, person.LastName, person.IsMale ? Color.CornflowerBlue : Color.HotPink);

            //  Set the row data of the ListView
            List<Person> People = new()
            {
                new(1, "John", "Smith", true),
                new(2, "James", "Johnson", true),
                new(3, "Emily", "Doe", false),
                new(4, "Chris", "Brown", true),
                new(5, "Melissa", "Wilson", false),
                new(6, "Richard", "Anderson", true),
                new(7, "Taylor", "Moore", false),
                new(8, "Tom", "Lee", true),
                new(9, "Joe", "White", true),
                new(10, "Alice", "Wright", false)
            };
            ListView_Sample1.SetItemsSource(People);
        }
    }
}
