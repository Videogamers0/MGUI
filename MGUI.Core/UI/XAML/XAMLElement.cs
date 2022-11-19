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

namespace MGUI.Core.UI.XAML
{
    [TypeConverter(typeof(XAMLElementStringConverter))]
    public abstract class Element
    {
        public abstract MGElementType ElementType { get; }

        public string Name { get; set; }

        public Thickness? Margin { get; set; }
        public Thickness? Padding { get; set; }

        public HorizontalAlignment? HorizontalAlignment { get; set; }
        public VerticalAlignment? VerticalAlignment { get; set; }
        public HorizontalAlignment? HorizontalContentAlignment { get; set; }
        public VerticalAlignment? VerticalContentAlignment { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public HorizontalAlignment? HA { get => HorizontalAlignment; set => HorizontalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public VerticalAlignment? VA { get => VerticalAlignment; set => VerticalAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public HorizontalAlignment? HCA { get => HorizontalContentAlignment; set => HorizontalContentAlignment = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public VerticalAlignment? VCA { get => VerticalContentAlignment; set => VerticalContentAlignment = value; }

        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public int? PreferredWidth { get; set; }
        public int? PreferredHeight { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int? Width { get => PreferredWidth; set => PreferredWidth = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int? Height { get => PreferredHeight; set => PreferredHeight = value; }

        public ToolTip ToolTip { get; set; }
        public ContextMenu ContextMenu { get; set; }

        public bool? CanHandleInputsWhileHidden { get; set; }
        public bool? IsHitTestVisible { get; set; }

        public bool? IsSelected { get; set; }
        public bool? IsEnabled { get; set; }

        //public VisualStateBrush Background
        public FillBrush Background { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public FillBrush BG { get => Background; set => Background = value; }
        public XAMLColor? TextForeground { get; set; }

        public Visibility? Visibility { get; set; }

        public bool? ClipToBounds { get; set; }

        public float? Opacity { get; set; }

        /// <summary>Used by <see cref="DockPanel"/>'s children</summary>
        public Dock Dock { get; set; } = Dock.Top;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        public int GridRow { get; set; } = 0;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        public int GridColumn { get; set; } = 0;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        public int GridRowSpan { get; set; } = 1;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        public int GridColumnSpan { get; set; } = 1;
        /// <summary>Used by <see cref="Grid"/>'s children</summary>
        public bool GridAffectsMeasure { get; set; } = true;
        /// <summary>Used by <see cref="OverlayPanel"/>'s children</summary>
        public Thickness Offset { get; set; } = new();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int Row { get => GridRow; set => GridRow = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int Column { get => GridColumn; set => GridColumn = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int RowSpan { get => GridRowSpan; set => GridRowSpan = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]public int ColumnSpan { get => GridColumnSpan; set => GridColumnSpan = value; }

        /// <summary>If true, this object can have <see cref="Setter"/>s applied to its properties.<para/>
        /// Default value: true</summary>
        public bool IsStyleable { get; set; } = true;
        public List<Style> Styles { get; set; } = new();
        /// <summary>The names of the named <see cref="Style"/>s that should be applied to this <see cref="Element"/>.<br/>
        /// Use a comma to delimit multiple names, such as: "Style1,Style2<br/>
        /// to apply <see cref="Style"/> with <see cref="Style.Name"/>="Style1" and <see cref="Style"/> with <see cref="Style.Name"/>="Style2" to this <see cref="Element"/><para/>
        /// See also: <see cref="Style.Name"/></summary>
        public string StyleNames { get; set; }

        public Dictionary<string, object> AttachedProperties { get; set; } = new();

        public T ToElement<T>(MGWindow Window, MGElement Parent) 
            where T : MGElement
        {
            MGElement Element = CreateElementInstance(Window, Parent);
            ApplySettings(Parent, Element);
            return Element as T;
        }

        protected internal void ApplySettings(MGElement Parent, MGElement Element)
        {
            using (Element.BeginInitializing())
            {
                ApplyBaseSettings(Parent, Element);
                ApplyDerivedSettings(Parent, Element);
            }
        }

        private void ApplyBaseSettings(MGElement Parent, MGElement Element)
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

                if (Background != null)
                    Element.BackgroundBrush.NormalValue = Background.ToFillBrush();
                if (TextForeground.HasValue)
                    Element.DefaultTextForeground.NormalValue = TextForeground.Value.ToXNAColor();

                if (Visibility.HasValue)
                    Element.Visibility = Visibility.Value;

                if (ClipToBounds.HasValue)
                    Element.ClipToBounds = ClipToBounds.Value;

                if (Opacity.HasValue)
                    Element.Opacity = Opacity.Value;
            }
        }

        protected abstract MGElement CreateElementInstance(MGWindow Window, MGElement Parent);
        protected internal abstract void ApplyDerivedSettings(MGElement Parent, MGElement Element);

        protected internal abstract IEnumerable<Element> GetChildren();

        protected internal void ProcessStyles() => ProcessStyles(new Dictionary<string, Style>(), new Dictionary<MGElementType, Dictionary<string, List<object>>>());
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
                            if (ModifiedPropertyNames.Contains(PropertyName) || PropertyInfo.GetValue(this) == default) // Don't allow a style to override a value that was already explicitly set
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
                    List<Style> ExplicitStyles = Names.Select(x => StylesByName[x]).ToList();

                    //  Get all the properties that the explicit styles will modify
                    HashSet<string> PropertyNames = ExplicitStyles.SelectMany(x => x.Setters).Select(x => x.Property).ToHashSet();
                    Dictionary<string, PropertyInfo> PropertiesByName = new();
                    foreach (string PropertyName in PropertyNames)
                    {
                        PropertyInfo PropertyInfo = ThisType.GetProperty(PropertyName, BindingFlags.Public | BindingFlags.Instance); // | BindingFlags.IgnoreCase?
                        if (PropertyInfo != null)
                        {
                            if (ModifiedPropertyNames.Contains(PropertyName) || PropertyInfo.GetValue(this) == default) // Don't allow a style to override a value that was already explicitly set
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

    public class XAMLElementStringConverter : TypeConverter
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
