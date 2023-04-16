using MGUI.Core.UI.XAML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI
{
    /// <summary>Represents a template that can be used to generate <see cref="MGElement"/> instances.<para/>
    /// For example, <see cref="ContentTemplate"/> may specify an <see cref="MGElementTemplate"/> via <see cref="ContentTemplate.ContentTemplateName"/> instead of using <see cref="ContentTemplate.Content"/></summary>
    public class MGElementTemplate
    {
        public readonly string Name;
        /// <summary>If true, this template will only generate a single element instance and return that same instance every time <see cref="GetInstance(MGWindow)"/> is invoked.<para/>
        /// If unsure, set this to false.</summary>
        public readonly bool IsShared;
        /// <summary>The function that is invoked to generate the element instance.<para/>
        /// Recommended to use <see cref="GetInstance(MGWindow)"/> instead of invoking this function to account for <see cref="IsShared"/>.</summary>
        public readonly Func<MGWindow, MGElement> Template;

        private readonly Dictionary<MGWindow, MGElement> Instances = new();
        public MGElement GetInstance(MGWindow Window)
        {
            if (Window == null)
                throw new ArgumentNullException(nameof(Window));

            if (!IsShared)
                return Template(Window);
            else if (Instances.TryGetValue(Window, out MGElement SharedInstance))
                return SharedInstance;
            else
            {
                MGElement Instance = Template(Window);
                Instances.Add(Window, Instance);
                return Instance;
            }
        }

        /// <param name="IsShared">If true, this template will only generate a single element instance and return that same instance every 
        /// time <see cref="GetInstance(MGWindow)"/> is invoked.<para/>If unsure, set this to false.</param>
        /// <param name="Template">The function that will be invoked to generate the element instance.</param>
        public MGElementTemplate(string Name, bool IsShared, Func<MGWindow, MGElement> Template)
        {
            this.Name = Name ?? throw new ArgumentNullException(nameof(Name));
            this.IsShared = IsShared;
            this.Template = Template ?? throw new ArgumentNullException(nameof(Template));
        }
    }
}
