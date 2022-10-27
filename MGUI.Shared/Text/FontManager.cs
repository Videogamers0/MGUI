using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Text
{
    public class FontManager
    {
        private Dictionary<string, FontSet> _FontsByFamily { get; }
        public IReadOnlyDictionary<string, FontSet> FontsByFamily => _FontsByFamily;

        public void AddFontSet(FontSet Set) => _FontsByFamily.Add(Set.Name, Set);

        /// <summary>The name of the font family that should be used by default when no font has been explicitly specified for text content.</summary>
        public string DefaultFontFamily { get; set; }
        public FontSet GetFontFamilyOrDefault(string FamilyName)
        {
            if (_FontsByFamily.TryGetValue(FamilyName, out FontSet FS))
                return FS;
            else if (DefaultFontFamily != null && _FontsByFamily.TryGetValue(DefaultFontFamily, out FS))
                return FS;
            else
                return _FontsByFamily.Values.First();
        }

        public FontManager(ContentManager Content, string DefaultFontFamily)
        {
            this.DefaultFontFamily = DefaultFontFamily;
            List<string> BuiltInFontFamilyNames = new() { "Arial" };
            _FontsByFamily = BuiltInFontFamilyNames.Select(x => new FontSet(Content, x)).ToDictionary(x => x.Name);
        }

        /// <param name="DesiredFontSize">The desired height, in points, of the returned <see cref="SpriteFont"/> <paramref name="SF"/></param>
        /// <param name="PreferDownsampled">If true, will attempt to retrieve a font size that is approximately twice as large as the <paramref name="DesiredFontSize"/>, and scale it down by approximately 0.5</param>
        /// <param name="ExactScale">The scale factor that must be applied to the returned <see cref="SpriteFont"/> <paramref name="SF"/> to make it have the desired <paramref name="DesiredFontSize"/></param>
        /// <param name="SuggestedScale">Recommended scale factor that might not result in the exact value for <paramref name="DesiredFontSize"/>, but will result in sharper, less blurred text.</param>
        public bool TryGetFont(string Family, CustomFontStyles Style, int DesiredFontSize, bool PreferDownsampled, out FontSet FS, out SpriteFont SF, out int Size, out float ExactScale, out float SuggestedScale)
        {
            if (_FontsByFamily.TryGetValue(Family, out FS))
            {
                return FS.TryGetFont(Style, DesiredFontSize, PreferDownsampled, out SF, out Size, out ExactScale, out SuggestedScale);
            }
            else
            {
                SF = null;
                Size = default;
                ExactScale = 0.0f;
                SuggestedScale = 0.0f;
                return false;
            }
        }

        public SpriteFont GetFont(string Family, int DesiredFontSize, bool PreferDownsampled, out int Size, out float ExactScale, out float SuggestedScale, out FontSet FS)
            => GetFont(Family, CustomFontStyles.Normal, DesiredFontSize, PreferDownsampled, out Size, out ExactScale, out SuggestedScale, out FS);

        public SpriteFont GetFont(string Family, CustomFontStyles Style, int DesiredFontSize, bool PreferDownsampled, out int Size, out float ExactScale, out float SuggestedScale, out FontSet FS)
        {
            if (!TryGetFont(Family, Style, DesiredFontSize, PreferDownsampled, out FS, out SpriteFont Font, out Size, out ExactScale, out SuggestedScale))
            {
                throw new ArgumentException($"No font found for {nameof(Family)}={Family} with {nameof(Style)}={Style}");
            }
            else
            {
                return Font;
            }
        }

        public bool HasStyle(string Family, CustomFontStyles Style) => _FontsByFamily.TryGetValue(Family, out FontSet FS) && FS.SupportsStyle(Style);
    }
}
