using MGUI.Core.UI.Data_Binding;
using MGUI.Core.UI.Data_Binding.Converters;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XNAColor = Microsoft.Xna.Framework.Color;

namespace MGUI.Core.UI.XAML
{
    [TypeConverter(typeof(ElementStringConverter))]
    public abstract class Element
    {
        public abstract MGElementType ElementType { get; }

        public string Name { get; set; }

        [Category("Layout")]
        public Thickness? Margin { get; set; }
        [Category("Layout")]
        public Thickness? Padding { get; set; }

        [Category("Layout")]public HorizontalAlignment? HorizontalAlignment { get; set; }
        [Category("Layout")]public VerticalAlignment? VerticalAlignment { get; set; }
        [Category("Layout")]public HorizontalAlignment? HorizontalContentAlignment { get; set; }
        [Category("Layout")]public VerticalAlignment? VerticalContentAlignment { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public HorizontalAlignment? HA { get => HorizontalAlignment; set => HorizontalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public VerticalAlignment? VA { get => VerticalAlignment; set => VerticalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public HorizontalAlignment? HCA { get => HorizontalContentAlignment; set => HorizontalContentAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public VerticalAlignment? VCA { get => VerticalContentAlignment; set => VerticalContentAlignment = value; }

        [Category("Layout")]
        public int? MinWidth { get; set; }
        [Category("Layout")]
        public int? MinHeight { get; set; }
        [Category("Layout")]
        public int? MaxWidth { get; set; }
        [Category("Layout")]
        public int? MaxHeight { get; set; }

        [Browsable(false)]
        public int? PreferredWidth { get; set; }
        [Browsable(false)]
        public int? PreferredHeight { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Layout")]
        public int? Width { get => PreferredWidth; set => PreferredWidth = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Layout")]
        public int? Height { get => PreferredHeight; set => PreferredHeight = value; }

        public ToolTip ToolTip { get; set; }
        public ContextMenu ContextMenu { get; set; }

        [Category("Behavior")]
        public bool? CanHandleInputsWhileHidden { get; set; }
        [Category("Behavior")]
        public bool? IsHitTestVisible { get; set; }

        [Category("Behavior")]
        public bool? IsSelected { get; set; }
        [Category("Behavior")]
        public bool? IsEnabled { get; set; }

        [Category("Appearance")]
        public Thickness? BackgroundRenderPadding { get; set; }
        [Category("Appearance")]
        public FillBrush Background { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public FillBrush BG { get => Background; set => Background = value; }
        [Category("Appearance")]
        public FillBrush DisabledBackground { get; set; }
        [Category("Appearance")]
        public FillBrush SelectedBackground { get; set; }
        [Category("Appearance")]
        public XAMLColor? BackgroundFocusedColor { get; set; }

        [Category("Appearance")]
        public XAMLColor? TextForeground { get; set; }
        [Category("Appearance")]
        public XAMLColor? DisabledTextForeground { get; set; }
        [Category("Appearance")]
        public XAMLColor? SelectedTextForeground { get; set; }

        [Category("Appearance")]
        public Visibility? Visibility { get; set; }

        [Category("Layout")]
        public bool? ClipToBounds { get; set; }

        [Category("Appearance")]
        public float? Opacity { get; set; }

        [Category("Appearance")]
        public float? RenderScale { get; set; }

        /// <summary>Used by <see cref="DockPanel"/>'s children</summary>
        [Category("Attached")]
        public Dock Dock { get; set; } = Dock.Top;

        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        [Browsable(false)]
        public int GridRow { get; set; } = 0;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        [Browsable(false)]
        public int GridColumn { get; set; } = 0;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        [Browsable(false)]
        public int GridRowSpan { get; set; } = 1;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        [Browsable(false)]
        public int GridColumnSpan { get; set; } = 1;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        [Category("Attached")]
        public bool GridAffectsMeasure { get; set; } = true;

        /// <summary>Used by <see cref="OverlayPanel"/>'s children</summary>
        [Category("Attached")]
        public Thickness Offset { get; set; } = new();
        /// <summary>Used by <see cref="OverlayPanel"/>'s children and by <see cref="Overlay"/>s.</summary>
        [Category("Attached")]
        public double? ZIndex { get; set; } = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Attached")]
        public int Row { get => GridRow; set => GridRow = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Attached")]
        public int Column { get => GridColumn; set => GridColumn = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Attached")]
        public int RowSpan { get => GridRowSpan; set => GridRowSpan = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Category("Attached")]
        public int ColumnSpan { get => GridColumnSpan; set => GridColumnSpan = value; }

        /// <summary>If true, this object can have <see cref="Setter"/>s applied to its properties.<para/>
        /// Default value: true</summary>
        [Category("Appearance")]
        public bool IsStyleable { get; set; } = true;
        [Category("Appearance")]
        public List<Style> Styles { get; set; } = new();
        /// <summary>The names of the named <see cref="Style"/>s that should be applied to this <see cref="Element"/>.<br/>
        /// Use a comma to delimit multiple names, such as: "Style1,Style2<br/>
        /// to apply <see cref="Style"/> with <see cref="Style.Name"/>="Style1" and <see cref="Style"/> with <see cref="Style.Name"/>="Style2" to this <see cref="Element"/><para/>
        /// See also: <see cref="Style.Name"/></summary>
        [Category("Appearance")]
        public string StyleNames { get; set; }

        [Category("Attached")]
        public Dictionary<string, object> AttachedProperties { get; set; } = new();

        [Category("Attached")]
        public object Tag { get; set; }

        protected internal List<BindingConfig> Bindings { get; } = new();

        /// <param name="ApplyBaseSettings">If not null, this action will be invoked before <see cref="ApplySettings(MGElement, MGElement, bool)"/> executes.</param>
        public T ToElement<T>(MGWindow Window, MGElement Parent, Action<T> ApplyBaseSettings = null) 
            where T : MGElement
        {
            T Element = CreateElementInstance(Window, Parent) as T;
            ApplyBaseSettings?.Invoke(Element);
            ApplySettings(Parent, Element, true);
            return Element;
        }

        /// <param name="IncludeContent">Recommended value: true. If true, the child XAML content, if any, will also be processed.</param>
        protected internal void ApplySettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            using (Element.BeginInitializing())
            {
                ApplyBaseSettings(Parent, Element, true);
                ApplyDerivedSettings(Parent, Element, IncludeContent);
            }
        }

        internal void ApplyBaseSettings(MGElement Parent, MGElement Element, bool IncludeBindings)
        {
            using (Element.BeginInitializing())
            {
                if (Name != null)
                    Element.Name = Name;

                if (Margin.HasValue)
                    Element.Margin = Margin.Value.ToThickness();
                if (Padding.HasValue)
                    Element.Padding = Padding.Value.ToThickness();

                if (HorizontalAlignment.HasValue)
                    Element.HorizontalAlignment = HorizontalAlignment.Value;
                if (VerticalAlignment.HasValue)
                    Element.VerticalAlignment = VerticalAlignment.Value;
                if (HorizontalContentAlignment.HasValue)
                    Element.HorizontalContentAlignment = HorizontalContentAlignment.Value;
                if (VerticalContentAlignment.HasValue)
                    Element.VerticalContentAlignment = VerticalContentAlignment.Value;

                if (MinWidth.HasValue)
                    Element.MinWidth = MinWidth.Value;
                if (MinHeight.HasValue)
                    Element.MinHeight = MinHeight.Value;
                if (MaxWidth.HasValue)
                    Element.MaxWidth = MaxWidth.Value;
                if (MaxHeight.HasValue)
                    Element.MaxHeight = MaxHeight.Value;

                if (PreferredWidth.HasValue)
                    Element.PreferredWidth = PreferredWidth.Value;
                if (PreferredHeight.HasValue)
                    Element.PreferredHeight = PreferredHeight.Value;

                if (ToolTip != null)
                    Element.ToolTip = ToolTip.ToElement<MGToolTip>(Element.SelfOrParentWindow, Element);
                if (ContextMenu != null)
                    Element.ContextMenu = ContextMenu.ToElement<MGContextMenu>(Element.SelfOrParentWindow, Element);

                if (CanHandleInputsWhileHidden.HasValue)
                    Element.CanHandleInputsWhileHidden = CanHandleInputsWhileHidden.Value;
                if (IsHitTestVisible.HasValue)
                    Element.IsHitTestVisible = IsHitTestVisible.Value;
                if (IsSelected.HasValue)
                    Element.IsSelected = IsSelected.Value;
                if (IsEnabled.HasValue)
                    Element.IsEnabled = IsEnabled.Value;

                if (BackgroundRenderPadding.HasValue)
                    Element.BackgroundRenderPadding = BackgroundRenderPadding.Value.ToThickness();
                ApplyBackground(Element);

                if (TextForeground.HasValue)
                    Element.DefaultTextForeground.NormalValue = TextForeground.Value.ToXNAColor();
                if (DisabledTextForeground.HasValue)
                    Element.DefaultTextForeground.DisabledValue = DisabledTextForeground.Value.ToXNAColor();
                if (SelectedTextForeground.HasValue)
                    Element.DefaultTextForeground.SelectedValue = SelectedTextForeground.Value.ToXNAColor();

                if (Visibility.HasValue)
                    Element.Visibility = Visibility.Value;

                if (ClipToBounds.HasValue)
                    Element.ClipToBounds = ClipToBounds.Value;

                if (Opacity.HasValue)
                    Element.Opacity = Opacity.Value;

                if (RenderScale.HasValue)
                    Element.RenderScale = new(RenderScale.Value, RenderScale.Value);

                Element.Tag = this.Tag;

                if (IncludeBindings)
                {
                    //  These bindings are initialized in Window.ToElement(Desktop, Theme)
                    //  Because ElementName references cannot be resolved until named elements have been processed and added to the MGWindow instance
                    if (Bindings.Any())
                        Element.Metadata[BindingsMetadataKey] = this.Bindings;
                }
            }
        }

        private const string BindingsMetadataKey = "TmpBindings";

        //  DataBindings are defined in XAML (so they are applied to the properties of the XAML types)
        //  but are bound to the properties of the actual type (such as MGUI.Core.UI.MGButton instead of MGUI.Core.UI.XAML.Button).
        //  This dictionary is intended to handle cases where a property on the XAML type isn't the same name/path as the actual property of the binding
        private static readonly Dictionary<string, string> BindingPathMappings = new()
        {
            { nameof(Background), $"{nameof(MGElement.BackgroundBrush)}.{nameof(VisualStateFillBrush.NormalValue)}" },
            { nameof(SelectedBackground), $"{nameof(MGElement.BackgroundBrush)}.{nameof(VisualStateFillBrush.SelectedValue)}" },
            { nameof(DisabledBackground), $"{nameof(MGElement.BackgroundBrush)}.{nameof(VisualStateFillBrush.DisabledValue)}" },
            { nameof(TextForeground), $"{nameof(MGElement.DefaultTextForeground)}.{nameof(VisualStateSetting<XNAColor>.NormalValue)}" },
            { nameof(SelectedTextForeground), $"{nameof(MGElement.DefaultTextForeground)}.{nameof(VisualStateSetting<XNAColor>.SelectedValue)}" },
            { nameof(DisabledTextForeground), $"{nameof(MGElement.DefaultTextForeground)}.{nameof(VisualStateSetting<XNAColor>.DisabledValue)}" },
            { nameof(Width), $"{nameof(MGElement.PreferredWidth)}" },
            { nameof(Height), $"{nameof(MGElement.PreferredHeight)}" }
        };

        /// <summary>Resolves any pending <see cref="BindingConfig"/>s by converting them into <see cref="DataBinding"/>s</summary>
        /// <param name="DataContextOverride">If not null, this value will be applied to the <see cref="MGElement.DataContextOverride"/> value of every element that is processed.<para/>
        /// If <paramref name="RecurseChildren"/> is false, this is only applied to <paramref name="Element"/>. Else it's applied to <paramref name="Element"/> and all its nested children.</param>
        internal static void ProcessBindings(MGElement Element, bool RecurseChildren, object DataContextOverride)
        {
            if (RecurseChildren)
            {
                foreach (MGElement Child in Element.TraverseVisualTree(true, true, true, true, MGElement.TreeTraversalMode.Preorder))
                {
                    ProcessBindings(Child, false, DataContextOverride);
                }
            }
            else
            {
                if (DataContextOverride != null)
                    Element.DataContextOverride = DataContextOverride;

                if (Element.Metadata.TryGetValue(BindingsMetadataKey, out object Value) && Value is List<BindingConfig> Bindings)
                {
                    foreach (BindingConfig Binding in Bindings)
                    {
                        object TargetObject = Element;
                        BindingConfig PostProcessedBinding = Binding;

                        //  Handle some special-cases where the name of the XAML property isn't the same as the corresponding property on the c# object
                        if (BindingPathMappings.TryGetValue(Binding.TargetPath, out string ActualPath))
                        {
                            PostProcessedBinding = Binding with { TargetPath = ActualPath };
                        }

                        if (PostProcessedBinding.Converter is StringToToolTipConverter StringToolTipConverter)
                            StringToolTipConverter.Host = Element;

                        DataBindingManager.AddBinding(PostProcessedBinding, TargetObject);
                    }
                    Element.Metadata.Remove(BindingsMetadataKey);
                }
            }
        }

        protected void ApplyBackground(MGElement Element)
        {
            MGDesktop Desktop = Element.GetDesktop();

            if (Background != null)
                Element.BackgroundBrush.NormalValue = Background.ToFillBrush(Desktop);
            if (DisabledBackground != null)
                Element.BackgroundBrush.DisabledValue = DisabledBackground.ToFillBrush(Desktop);
            if (SelectedBackground != null)
                Element.BackgroundBrush.SelectedValue = SelectedBackground.ToFillBrush(Desktop);
            if (BackgroundFocusedColor != null)
                Element.BackgroundBrush.FocusedColor = BackgroundFocusedColor.Value.ToXNAColor();
        }

        protected abstract MGElement CreateElementInstance(MGWindow Window, MGElement Parent);
        /// <param name="IncludeContent">Recommended value: true. If true, child XAML content, if any, will also be processed.</param>
        protected internal abstract void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent);

        protected internal abstract IEnumerable<Element> GetChildren();

        protected internal void ProcessStyles(MGResources Resources)
        {
            Dictionary<string, Style> StylesByName = Resources.Styles.ToDictionary(x => x.Key, x => x.Value);
            ProcessStyles(StylesByName, new Dictionary<MGElementType, Dictionary<string, List<object>>>());
        }
        private void ProcessStyles(Dictionary<string, Style> StylesByName, Dictionary<MGElementType, Dictionary<string, List<object>>> StylesByType)
        {
            Dictionary<string, List<object>> ValuesByProperty;

            //  Append current style setters to indexed data
            foreach (Style Style in this.Styles.Where(x => x.Setters.Any()))
            {
                if (Style.Name != null)
                {
                    StylesByName.Add(Style.Name, Style);
                }
                else
                {
                    MGElementType Type = Style.TargetType;
                    if (!StylesByType.TryGetValue(Type, out ValuesByProperty))
                    {
                        ValuesByProperty = new();
                        StylesByType.Add(Type, ValuesByProperty);
                    }

                    foreach (Setter Setter in Style.Setters)
                    {
                        string Property = Setter.Property;
                        if (!ValuesByProperty.TryGetValue(Property, out List<object> Values))
                        {
                            Values = new();
                            ValuesByProperty.Add(Property, Values);
                        }

                        Values.Add(Setter.Value);
                    }
                }
            }

            //  Apply the appropriate style setters to this instance
            if (this.IsStyleable)
            {
                Type ThisType = GetType();

                HashSet<string> ModifiedPropertyNames = new();

                //  Apply implicit styles (styles that aren't referenced by a Name)
                if (StylesByType.TryGetValue(this.ElementType, out ValuesByProperty))
                {
                    foreach (KeyValuePair<string, List<object>> KVP in ValuesByProperty)
                    {
#if DEBUG
                        //  Sanity check
                        if (KVP.Value.Count == 0)
                            throw new InvalidOperationException($"{nameof(Element)}.{nameof(ProcessStyles)}.{nameof(ValuesByProperty)} should never be empty. The indexed data might not be properly updated.");
#endif

                        string PropertyName = KVP.Key;
                        PropertyInfo PropertyInfo = ThisType.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance); // | BindingFlags.IgnoreCase?
                        if (PropertyInfo != null)
                        {
                            if (ModifiedPropertyNames.Contains(PropertyName) 
                                || PropertyInfo.GetValue(this) == default) // Don't allow a style to override a value that was already explicitly set (needs more robust logic since some controls initialize properties to non-null values)
                            {
                                TypeConverter Converter = TypeDescriptor.GetConverter(PropertyInfo.PropertyType);
                                foreach (object Value in KVP.Value)
                                {
                                    if (Value is string StringValue)
                                        PropertyInfo.SetValue(this, Converter.ConvertFromString(StringValue));
                                    else
                                        PropertyInfo.SetValue(this, Value);
                                }

                                ModifiedPropertyNames.Add(PropertyName);
                            }
                        }
                    }
                }

                //  Apply explicit styles (styles that were explicitly referenced by their Name)
                if (StyleNames != null)
                {
                    string[] Names = StyleNames.Split(',');
                    List<Style> ExplicitStyles = Names.Select(x => StylesByName[x]).Where(x => x.TargetType == this.ElementType).ToList();

                    //  Get all the properties that the explicit styles will modify
                    HashSet<string> PropertyNames = ExplicitStyles.SelectMany(x => x.Setters).Select(x => x.Property).ToHashSet();
                    Dictionary<string, PropertyInfo> PropertiesByName = new();
                    foreach (string PropertyName in PropertyNames)
                    {
                        PropertyInfo PropertyInfo = ThisType.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance); // | BindingFlags.IgnoreCase?
                        if (PropertyInfo != null)
                        {
                            if (ModifiedPropertyNames.Contains(PropertyName) 
                                || PropertyInfo.GetValue(this) == default) // Don't allow a style to override a value that was already explicitly set
                            {
                                PropertiesByName.Add(PropertyName, PropertyInfo);
                            }
                        }
                    }

                    //  Apply the values of each setter
                    foreach (Style Style in ExplicitStyles)
                    {
                        foreach (Setter Setter in Style.Setters)
                        {
                            string PropertyName = Setter.Property;
                            if (PropertiesByName.TryGetValue(PropertyName, out PropertyInfo PropertyInfo))
                            {
                                TypeConverter Converter = TypeDescriptor.GetConverter(PropertyInfo.PropertyType);
                                if (Setter.Value is string StringValue)
                                    PropertyInfo.SetValue(this, Converter.ConvertFromString(StringValue));
                                else
                                    PropertyInfo.SetValue(this, Setter.Value);

                                ModifiedPropertyNames.Add(PropertyName);
                            }
                        }
                    }
                }
            }

            //  Recursively process all children
            foreach (Element Child in GetChildren())
            {
                Child.ProcessStyles(StylesByName, StylesByType);
            }

            //  Remove current style setters from indexed data
            foreach (Style Style in this.Styles.Where(x => x.Setters.Any()))
            {
                if (Style.Name != null)
                {
                    StylesByName.Remove(Style.Name);
                }
                else
                {
                    MGElementType Type = Style.TargetType;
                    if (StylesByType.TryGetValue(Type, out ValuesByProperty))
                    {
                        foreach (Setter Setter in Style.Setters)
                        {
                            string Property = Setter.Property;
                            if (ValuesByProperty.TryGetValue(Property, out List<object> Values))
                            {
                                if (Values.Remove(Setter.Value) && Values.Count == 0)
                                {
                                    ValuesByProperty.Remove(Property);
                                    if (ValuesByProperty.Count == 0)
                                        StylesByType.Remove(Type);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class ElementStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                TextBlock TextBlock = new() { Text = stringValue };
                return TextBlock;
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}