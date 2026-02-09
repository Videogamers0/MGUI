using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using System.Diagnostics;

namespace MGUI.Samples.Dialogs
{
    public class Registration : SampleBase
    {
        public Registration(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Dialogs)}", $"{nameof(Registration)}.xaml")
        {
            //  Create an action that is executed when clicking the "Terms of Service" link
            //  (XAML: '<TextBlock Text="I agree to the [Action=OpenTOS][color=#3483eb][i][u]Terms of service[/u][/i][/color][/Action]" />')
            Window.GetResources().AddCommand("OpenTOS", x =>
            {
                string SampleTOSWebpage = @"https://www.google.com";
                Process.Start(new ProcessStartInfo()
                {
                    FileName = SampleTOSWebpage,
                    UseShellExecute = true
                });
            });

            //  Retrieve named elements from the window
            MGTextBox TextBox_Email = Window.GetElementByName<MGTextBox>("TextBox_Email");
            MGTextBox TextBox_Username = Window.GetElementByName<MGTextBox>("TextBox_Username");
            MGPasswordBox TextBox_Password = Window.GetElementByName<MGPasswordBox>("TextBox_Password");
            MGCheckBox CheckBox_TOS = Window.GetElementByName<MGCheckBox>("CheckBox_TOS");
            MGButton Button_Register = Window.GetElementByName<MGButton>("Button_Register");

            //  React to the terms of service checkbox
            CheckBox_TOS.OnCheckStateChanged += (sender, e) =>
            {
                if (e.NewValue.Value)
                {
                    Button_Register.IsEnabled = true;
                    Button_Register.Opacity = 1f;
                }
                else
                {
                    Button_Register.IsEnabled = false;
                    Button_Register.Opacity = 0.5f;
                }
            };

            //  React to the register button
            Button_Register.AddCommandHandler((btn, e) =>
            {
                if (CheckBox_TOS.IsChecked.Value)
                {
                    string Email = TextBox_Email.Text;
                    string Username = TextBox_Username.Text;
                    string Password = TextBox_Password.Password;

                    //TODO
                    //Do something with email/username/password inputs
                    //
                }
            });
        }
    }
}
