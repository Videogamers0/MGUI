using MGUI.Shared.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

#if UseWPF
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace MGUI.Core.UI.XAML
{
    [ContentProperty(nameof(Content))]
    public class ContentTemplate
    {
        /// <summary>The name of the <see cref="MGElementTemplate"/> to when generating the Content. <see cref="MGElementTemplate"/>s are retrieved via <see cref="MGResources.ElementTemplates"/><para/>
        /// You should only specify either <see cref="ContentTemplateName"/> or <see cref="Content"/>, not both.<para/>
        /// See also: <see cref="MGDesktop.Resources"/>, <see cref="MGResources.ElementTemplates"/></summary>
        [Category("Data")]
        public string ContentTemplateName { get; set; }

        [Category("Data")]
        public Element Content { get; set; }

        /// <param name="ApplyBaseSettings">If not null, this action will be invoked before <see cref="Element.ApplySettings(MGElement, MGElement, bool)"/>
        /// executes on the created <see cref="MGElement"/>.</param>
        public MGElement GetContent(MGWindow Window, MGElement Parent, object DataContext, Action<MGElement> ApplyBaseSettings = null)
        {
            if (Content != null)
            {
                Element ContentCopy = Content.Copy();
                MGElement Item = ContentCopy.ToElement(Window, Parent, ApplyBaseSettings);
                Element.ProcessBindings(Item, true, DataContext);
                return Item;
            }
            else if (ContentTemplateName != null)
            {
                MGResources Resources = Window.GetResources();
                if (!Resources.TryGetElementTemplate(ContentTemplateName, out MGElementTemplate Template))
                {
                    Debug.WriteLine($"Warning - No {nameof(MGElementTemplate)} was found with the name '{ContentTemplateName}' in {nameof(MGResources)}.{nameof(MGResources.ElementTemplates)}.");
                    return null;
                }

                MGElement Item = Template.GetInstance(Window);
                //TODO: Need a way to handle ApplyBaseSettings.
                //Maybe MGElementTemplate.Template should be split into 2 functions instead of 1 so that
                //we can interlace the ApplyBaseSettings delegate to just after the element is created, but before it's customized?
                //      MGElement Item = Template.CreateInstance(Window)
                //      ApplyBaseSettings?.Invoke(Item);
                //      Template.StyleInstance(Item);
                Debug.WriteLineIf(ApplyBaseSettings != null, $"Warning - {nameof(Template)}.{nameof(GetContent)} does not account for {nameof(ApplyBaseSettings)} parameter when using {nameof(ContentTemplateName)}. " +
                    $"This may result in incorrectly-styled Content, such as if this {nameof(Template)} was used to generate a {nameof(ComboBox)}'s {nameof(ComboBox.DropdownItemTemplate)}.");
                Element.ProcessBindings(Item, true, DataContext);
                return Item;
            }
            else
                return null;
        }
    }
}
