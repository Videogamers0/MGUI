using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MGUI.Core.UI.Data_Binding
{
    //Bindings to MGElement properties are created through this process:
    //1. MGBinding.ProvideValue stores the binding data in BindableBase.Bindings while the XAML is being parsed
    //2. After the underlying MGElement instance has been created from the MGUI.Core.UI.XAML.Element instance,
    //      Element.ApplyBaseSettings copies the Bindings to MGElement.Metadata[BindingsMetadataKey], temporarily storing the data on the MGElement instance
    //3. After the entire XAML content is done being parsed, Element.ProcessBindings is called (at the end of Window.ToElement())
    //4. Element.ProcessBindings calls DataBindingsManager.AddBinding to finalize the databinding
    //Step #4 is intentionally deferred until after the entire window is done parsing, because otherwise bindings to ElementName might not work properly
    //      (EX: A StackPanel contains 2 buttons, A and B. Button A is parsed 1st since it's the 1st child of the StackPanel, and Button A has an ElementName binding to button B.
    //      So Button A's bindings cannot be resolved until Button B is parsed)

    /// <summary>Base class for MGUI objects that support <see cref="MGBinding"/> <see cref="DataBinding"/>s in XAML.</summary>
    public abstract class XAMLBindableBase : ViewModelBase, IObservableDataContext
    {
        //Note: To make an object support DataBindings in XAML, make the object extend XAMLBindableBase
        //and change fields to properties, where each property setter should invoke ViewModelBase.NotifyPropertyChanged.
        //The XAML middleman type, if any, should also extend XAMLBindableBase.

        /// <summary>This property is used to temporarily store <see cref="MGBinding"/> data on objects while they are being parsed from XAML.<para/>
        /// Most MGUI elements have middleman objects that XAML is first parsed to (such as MGUI.Core.UI.XAML.Button), and then those middleman objects are later converted to the underlying type
        /// (such as MGUI.Core.UI.MGButton) after the entire XAML string is done parsing. <see cref="DataBinding"/>s cannot be instantiated until after
        /// the entire window's XAML content is parsed/post-processed (otherwise the source object of the data-binding wouldn't exist yet and ElementName references might not be resolvable).<para/>
        /// So this property allows temporarily storing the necessary binding data during parsing, then copying it to the underlying type after it's created, then creating the binding after all other window content is done processing.</summary>
        protected internal List<BindingConfig> Bindings { get; } = new();

        /// <summary>Enumerates all nested <see cref="XAMLBindableBase"/> objects that are properties of this <see cref="XAMLBindableBase"/>.</summary>
        protected internal virtual IEnumerable<(XAMLBindableBase Item, string Path)> GetNestedBindableObjects() => Enumerable.Empty<(XAMLBindableBase, string)>();

        /// <summary>The source data that <see cref="DataBinding"/>s retrieve bound data from.</summary>
        public virtual object DataContext { get; set; }
        /// <summary>Invoked after <see cref="DataContext"/> is changed.</summary>
        public event EventHandler<object> DataContextChanged;
        protected internal void InvokeDataContextChanged() => DataContextChanged?.Invoke(this, DataContext);
    }
}