using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;
using MGUI.Shared.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Size = MonoGame.Extended.Size;

namespace MGUI.Core.UI
{
    /// <summary>Stores various resources in Dictionary Key-Value pairs so that they can be accessed by their string keys.<para/>
    /// For example: the name of the command that an <see cref="MGButton"/> executes when clicked, the name of a <see cref="Texture2D"/> that an <see cref="MGImage"/> draws,
    /// or the name of an object in <see cref="StaticResources"/> for databinding purposes.<para/>
    /// This instance is usually accessed via <see cref="MGDesktop.Resources"/> (See also: <see cref="MGElement.GetResources"/>)</summary>
    public class MGResources
    {
        public MGResources(FontManager FontManager)
            : this(new MGTheme(FontManager.DefaultFontFamily)) { }

        public MGResources(MGTheme DefaultTheme)
        {
            this.DefaultTheme = DefaultTheme ?? throw new ArgumentNullException(nameof(DefaultTheme));
        }

        #region Textures
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, MGTextureData> _Textures = new();
        public IReadOnlyDictionary<string, MGTextureData> Textures => _Textures;

        public void AddTexture(string Name, MGTextureData Data)
        {
            _Textures.Add(Name, Data);
            OnTextureAdded?.Invoke(this, (Name, Data));
        }

        public bool RemoveTexture(string Name)
        {
            if (_Textures.TryGetValue(Name, out MGTextureData Data))
            {
                _Textures.Remove(Name);
                OnTextureRemoved?.Invoke(this, (Name, Data));
                return true;
            }
            else
                return false;
        }

        public bool TryGetTexture(string Name, out MGTextureData Data)
        {
            if (Name != null && _Textures.TryGetValue(Name, out Data))
                return true;
            else
            {
                Data = default;
                return false;
            }
        }

        internal (int? Width, int? Height) GetTextureDimensions(string Name)
        {
            if (TryGetTexture(Name, out MGTextureData Texture))
            {
                Size Size = Texture.RenderSize;
                return (Size.Width, Size.Height);
            }
            else
                return (null, null);
        }

        public bool TryDrawTexture(DrawTransaction DT, string Name, Rectangle TargetBounds, float Opacity = 1.0f, Color? Color = null)
            => TryDrawTexture(DT, Name, TargetBounds.TopLeft(), TargetBounds.Width, TargetBounds.Height, Opacity, Color);
        public bool TryDrawTexture(DrawTransaction DT, MGTextureData? TextureData, Rectangle TargetBounds, float Opacity = 1.0f, Color? Color = null)
            => TryDrawTexture(DT, TextureData, TargetBounds.TopLeft(), TargetBounds.Width, TargetBounds.Height, Opacity, Color);
        public bool TryDrawTexture(DrawTransaction DT, string Name, Point Position, int? Width, int? Height, float Opacity = 1.0f, Color? Color = null)
        {
            if (TryGetTexture(Name, out MGTextureData TextureData))
                return TryDrawTexture(DT, TextureData, Position, Width, Height, Opacity, Color);
            else
                return false;
        }
        public bool TryDrawTexture(DrawTransaction DT, MGTextureData? TextureData, Point Position, int? Width, int? Height, float Opacity = 1.0f, Color? Color = null)
        {
            if (TextureData != null)
            {
                int ActualWidth = Width ?? TextureData.Value.RenderSize.Width;
                int ActualHeight = Height ?? TextureData.Value.RenderSize.Height;
                Rectangle Destination = new(Position.X, Position.Y, ActualWidth, ActualHeight);

                DT.DrawTextureTo(TextureData.Value.Texture, TextureData.Value.SourceRect, Destination, (Color ?? Microsoft.Xna.Framework.Color.White) * Opacity * TextureData.Value.Opacity);

                return true;
            }
            else
                return false;
        }

        public event EventHandler<(string Name, MGTextureData Data)> OnTextureAdded;
        public event EventHandler<(string Name, MGTextureData Data)> OnTextureRemoved;
        #endregion Textures

        #region Commands
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, Action<MGElement>> _Commands = new();
        /// <summary>This dictionary is commonly used by <see cref="MGButton.CommandName"/> or by <see cref="MGTextBlock"/> to reference delegates by a string key value.<para/>
        /// See also:<br/><see cref="AddCommand(string, Action{MGElement})"/><br/><see cref="RemoveComand(string)"/><para/>
        /// EX: If you create an <see cref="MGTextBlock"/> and set its text to:
        /// <code>[Command=ABC]This text invokes a delegate when clicked[/Command] but this text doesn't</code>
        /// then the <see cref="Action{MGElement}"/> with the name "ABC" will be invoked when clicking the substring "This text invokes a delegate when clicked"</summary>
        public IReadOnlyDictionary<string, Action<MGElement>> Commands => _Commands;

        /// <param name="Name">Must be unique. If the command is intended to be window-specific, 
        /// you may wish to prefix the command name with the <see cref="MGWindow"/>'s <see cref="MGElement.UniqueId"/> to ensure uniqueness.</param>
        public void AddCommand(string Name, Action<MGElement> Command)
        {
            _Commands.Add(Name, Command);
            OnCommandAdded?.Invoke(this, (Name, Command));
        }

        public bool RemoveComand(string Name)
        {
            if (_Commands.TryGetValue(Name, out Action<MGElement> Command))
            {
                _Commands.Remove(Name);
                OnCommandRemoved?.Invoke(this, (Name, Command));
                return true;
            }
            else
                return false;
        }

        public bool TryGetCommand(string Name, out Action<MGElement> Command)
        {
            if (Name != null && _Commands.TryGetValue(Name, out Command))
                return true;
            else
            {
                Command = null;
                return false;
            }
        }

        /// <summary>Invoked when a new value is added to <see cref="Commands"/> via <see cref="AddCommand(string, Action{MGElement})"/><para/>
        /// See also: <see cref="OnCommandRemoved"/></summary>
        public event EventHandler<(string Name, Action<MGElement> Command)> OnCommandAdded;
        /// <summary>Invoked when a value is removed from <see cref="Commands"/> via <see cref="RemoveComand(string)"/><para/>
        /// See also: <see cref="OnCommandAdded"/></summary>
        public event EventHandler<(string Name, Action<MGElement> Command)> OnCommandRemoved;

        //TODO refactor MGWindow.NamedToolTips to be a Dictionary<Window, Dictionary<string, ToolTip>> in MGResources?
        #endregion Commands

        #region Themes
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, MGTheme> _Themes = new();
        public IReadOnlyDictionary<string, MGTheme> Themes => _Themes;

        public void AddTheme(string Name, MGTheme Theme)
        {
            _Themes.Add(Name, Theme);
            OnThemeAdded?.Invoke(this, (Name, Theme));
        }

        public bool RemoveTheme(string Name)
        {
            if (_Themes.TryGetValue(Name, out MGTheme Theme))
            {
                _Themes.Remove(Name);
                OnThemeRemoved?.Invoke(this, (Name, Theme));
                return true;
            }
            else
                return false;
        }

        /// <param name="DefaultValue">The default theme to return if there is no theme with the given <paramref name="Name"/>. Uses <see cref="DefaultTheme"/> if null.</param>
        /// <param name="WarnIfNotFound">If a <paramref name="Name"/> is specified but no corresponding theme is found, a warning will be written via <see cref="Debug.WriteLine(string?)"/></param>
        public MGTheme GetThemeOrDefault(string Name, MGTheme DefaultValue = null, bool WarnIfNotFound = true)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (_Themes.TryGetValue(Name, out MGTheme Result))
                    return Result;
                else if (WarnIfNotFound)
                    Debug.WriteLine($"Warning - No {nameof(MGTheme)} was found with the name '{Name}' in {nameof(MGResources)}.{nameof(Themes)}");
            }

            return DefaultValue ?? DefaultTheme;
        }

        public event EventHandler<(string Name, MGTheme Theme)> OnThemeAdded;
        public event EventHandler<(string Name, MGTheme Theme)> OnThemeRemoved;

        private MGTheme _DefaultTheme;
        /// <summary>The <see cref="MGTheme"/> to assign to an <see cref="MGWindow"/> after parsing a XAML string (unless the window explicitly specifies a different theme).<para/>
        /// Note: Changing this value will not dynamically update the theme of any windows that have already been parsed. This value is only applied once on each window, when the XAML is parsed.<br/>
        /// So if you do change this value, you may want to re-parse your XAML content to initialize a new window.<para/>
        /// See also: <see cref="XAMLParser.LoadRootWindow(MGDesktop, string, bool, bool)"/><para/>
        /// This value cannot be null.</summary>
        public MGTheme DefaultTheme
        {
            get => _DefaultTheme;
            set => _DefaultTheme = value ?? throw new ArgumentNullException(nameof(DefaultTheme));
        }
        #endregion Themes

        #region Styles
        //TODO maybe this should be split up into ImplicitStyles (which are indexed by their MGElementType) and ExplicitStyles (which are indexed by their Name)
        //  If so, need to modify Element.ProcessStyles to initialize the 'StylesByType' dictionary before proceeding with the styling logic
        //Dictionary<MGElementType, Style> ImplicitStyles
        //      AddImplicitStyle(Style Style)
        //          If style has no setters, return
        //          If the Style has a name, throw exception
        //          look in ImplicitStyles for a value at Style.TargetType
        //          Create if not found. Then merge all the Style Setters together, overwriting any existing setters for properties
        //      RemoveImplicitStyle(MGElementType TargetType)
        //Dictionary<string, Style> ExplicitStyles
        //      AddExplicitStyle(Style Style)
        //          If style has no setters, return
        //          If the Style doesn't have a name, throw exception
        //          Else add it to ExplicitStyles
        //Maybe Window should have bool InheritsImplicitStyles
        //      If true, Window's Element.Styles is pulled from Resources.ImplicitStyles
        //      Or the ImplicitStyles would have to be stored as some kind of named Set?
        //      Dictionary<string, StyleSet> where StyleSet contains List<Style>
        //      Then you could say <Window StyleSetNames="...">...</Window>
        //      to automatically initialize Window.Styles to whatever is found in Resources.StyleSets[StyleSetName]
        //      could treat it as a comma separated list like StyleNames, and apply it to any Element
        //      then in Element.ProcessStyles, foreach (style in this.styles AND foreach style in each styleset that we can find in the resources... append to dictionaries etc

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, Style> _Styles = new();
        public IReadOnlyDictionary<string, Style> Styles => _Styles;

        public void AddStyle(string Name, Style Style)
        {
            _Styles.Add(Name, Style);
            OnStyleAdded?.Invoke(this, (Name, Style));
        }

        public bool RemoveStyle(string Name)
        {
            if (_Styles.TryGetValue(Name, out Style Style))
            {
                _Styles.Remove(Name);
                OnStyleRemoved?.Invoke(this, (Name, Style));
                return true;
            }
            else
                return false;
        }

        public event EventHandler<(string Name, Style Style)> OnStyleAdded;
        public event EventHandler<(string Name, Style Style)> OnStyleRemoved;
        #endregion Styles

        #region StaticResources
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, object> _StaticResources = new();
        public IReadOnlyDictionary<string, object> StaticResources => _StaticResources;

        public void AddStaticResource(string Name, object Value)
        {
            _StaticResources.Add(Name, Value);
            OnStaticResourceAdded?.Invoke(this, (Name, Value));
        }

        public bool RemoveStaticResource(string Name)
        {
            if (_StaticResources.TryGetValue(Name, out object Value))
            {
                _StaticResources.Remove(Name);
                OnStaticResourceRemoved?.Invoke(this, (Name, Value));
                return true;
            }
            else
                return false;
        }

        public bool TryGetStaticResource(string Name, out object Value)
        {
            if (Name != null && _StaticResources.TryGetValue(Name, out Value))
                return true;
            else
            {
                Value = null;
                return false;
            }
        }

        public event EventHandler<(string Name, object Value)> OnStaticResourceAdded;
        public event EventHandler<(string Name, object Value)> OnStaticResourceRemoved;
        #endregion StaticResources

        #region Element Templates
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, MGElementTemplate> _ElementTemplates = new();
        public IReadOnlyDictionary<string, MGElementTemplate> ElementTemplates => _ElementTemplates;

        public void AddElementTemplate(MGElementTemplate Template)
        {
            _ElementTemplates.Add(Template.Name, Template);
            OnElementTemplateAdded?.Invoke(this, (Template.Name, Template));
        }

        public bool RemoveElementTemplate(string Name)
        {
            if (_ElementTemplates.TryGetValue(Name, out MGElementTemplate Template))
            {
                _ElementTemplates.Remove(Name);
                OnElementTemplateRemoved?.Invoke(this, (Name, Template));
                return true;
            }
            else
                return false;
        }

        public bool TryGetElementTemplate(string Name, out MGElementTemplate Template)
        {
            if (Name != null && _ElementTemplates.TryGetValue(Name, out Template))
                return true;
            else
            {
                Template = null;
                return false;
            }
        }

        public event EventHandler<(string Name, MGElementTemplate Template)> OnElementTemplateAdded;
        public event EventHandler<(string Name, MGElementTemplate Template)> OnElementTemplateRemoved;
        #endregion Element Templates
    }
}
