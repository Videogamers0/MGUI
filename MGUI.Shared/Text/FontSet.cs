using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MGUI.Shared.Text
{
    [Flags]
    public enum CustomFontStyles
    {
        [Description("None")]
        None = 0b_0000_0000,
        [Description("Normal")]
        Normal = 0b_0000_0001,
        [Description("Bold")]
        Bold = 0b_0000_0010,
        //[Description("SC")]
        //SmallCaps = 0b_0000_0100,
        //[Description("Mono")]
        //Monospaced = 0b_0000_1000,
        [Description("Italic")]
        Italic = 0b_0001_0000
    }

    public readonly record struct FontMetadata(int Size, bool IsBold, bool IsItalic);

    internal readonly record struct FontParseResult(int Size, CustomFontStyles Styles, string RelativeFilePath);

    /// <summary>Represents a collection of fonts belonging to the same family. This is usually several different sizes and FontStyles (Bold, Italics etc) of the same font.</summary>
    public class FontSet
    {
        private static readonly ReadOnlyCollection<char> Digits = "0123456789".ToCharArray().ToList().AsReadOnly();
        private static readonly ReadOnlyCollection<char> Alphabet = "abcdefghijklmnopqrstuvwxyz".ToCharArray().ToList().AsReadOnly();
        private static readonly ReadOnlyCollection<char> SpecialCharacters = @"`~!@#$%^&*()-_=+[{]}\|;:'"",<.>/?".ToCharArray().ToList().AsReadOnly();
        private static readonly HashSet<char> CharacterSet = Digits.Union(Alphabet.Select(x => char.ToLower(x))).Union(Alphabet.Select(x => char.ToUpper(x))).Union(SpecialCharacters).ToHashSet();

        internal static readonly ReadOnlyCollection<CustomFontStyles> AllStyles = Enum.GetValues(typeof(CustomFontStyles)).Cast<CustomFontStyles>().ToList().AsReadOnly();
        public static readonly string FontsBasePath = "Fonts";

        /// <summary>EX: "Arial". Doesn't include suffixes such as "Arial Bold"</summary>
        public string Name { get; }

        private ReadOnlyCollection<CustomFontStyles> SupportedStyles { get; }
        public bool SupportsStyle(CustomFontStyles Styles) => SupportedStyles.Contains(Styles);

        private Dictionary<int, Dictionary<CustomFontStyles, SpriteFont>> Fonts { get; }
        public ReadOnlyCollection<int> SupportedSizes { get; }
        private int MaxSize { get; }

        public bool IsValid { get; }

        private Dictionary<int, Vector2> _Origins { get; }
        public IReadOnlyDictionary<int, Vector2> Origins => _Origins;

        private Dictionary<int, int> _Heights { get; }
        public IReadOnlyDictionary<int, int> Heights => _Heights;

        /// <summary>Generates a <see cref="FontSet"/> from all .xnb files found in:<br/>
        /// <code>{ExeFolder}\{Content.RootDirectory}\Fonts\{FontName}\</code><para/>
        /// where the .xnb's filename is in the following format:<br/>
        /// <code>{FontSize}_{Underscore-Delimited-FontStyles}.xnb</code><para/>
        /// Example format:<br/>
        /// {ExeFolder}\Content\Fonts\Arial\10_Bold.xnb - Represents a SpriteFont where Size=10pts for the 'Arial Bold' Font Family.<br/>
        /// {ExeFolder}\Content\Fonts\Calibri\12_Bold_Italic.xnb - Represents a SpriteFont where Size=12pts for the 'Calibri Bold Italic' Font Family.<br/>
        /// {ExeFolder}\Content\Fonts\Helvetica\14_Normal.xnb - Represents a SpriteFont where Size=14pts for the 'Helvetica' Font Family.<para/>
        /// Supported FontStyles: <see cref="CustomFontStyles.Normal"/>, <see cref="CustomFontStyles.Bold"/>, <see cref="CustomFontStyles.Italic"/><para/>
        /// To programmatically generate .spritefont files in the desired file name format, consider using:<br/>
        /// <see cref="SpritefontGenerator.GenerateDefault(ContentManager, string)"/><br/>
        /// and then adding the generated .spritefont files to your MonoGame Content</summary>
        public FontSet(ContentManager Content, string FontName)
        {
            this.Name = FontName;

            string FontDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Content.RootDirectory, FontsBasePath, Name);
            List<string> FontFilenames = !Directory.Exists(FontDirectory) ?
                new List<string>() :
                Directory.GetFiles(FontDirectory, "*.xnb", SearchOption.TopDirectoryOnly).Select(x => Path.GetFileName(x)).ToList();

            if (!FontFilenames.Any())
            {
                SupportedStyles = new List<CustomFontStyles>().AsReadOnly();
                Fonts = new();
                SupportedSizes = new List<int>().AsReadOnly();
                QuickSizeLookup = new();
                MaxSize = 0;
                _Origins = new();
                _Heights = new();
                IsValid = false;
            }
            else
            {
                string RelativeFolderPath = Path.Combine(FontsBasePath, Name);

                Fonts = new();
                foreach (string FontFilename in FontFilenames)
                {
                    if (TryParseFontFilename(RelativeFolderPath, FontFilename, out FontParseResult? Data))
                    {
                        if (!Fonts.TryGetValue(Data.Value.Size, out Dictionary<CustomFontStyles, SpriteFont> Variations))
                        {
                            Variations = new();
                            Fonts.Add(Data.Value.Size, Variations);
                        }

                        string RelativeFilePath = Path.Combine(RelativeFolderPath, Path.GetFileNameWithoutExtension(FontFilename));
                        SpriteFont SF = Content.Load<SpriteFont>(RelativeFilePath);
                        Variations.Add(Data.Value.Styles, SF);
                    }
                }

                //  I'm not a fan of all the excess space above and below the text that Monogame leaves by default when using SpriteFont.DrawString.
                //  So this logic determines the top and bottom Y values of the characters that I care about drawing
                //  and is then used to precisely position and measure my text
                _Origins = new();
                _Heights = new();
                foreach (var KVP in Fonts)
                {
                    int FontSize = KVP.Key;
                    IEnumerable<SpriteFont> SpriteFonts = KVP.Value.Values;
                    IEnumerable<SpriteFont.Glyph> Glyphs = SpriteFonts.SelectMany(x => x.GetGlyphs().Values).Where(x => CharacterSet.Contains(x.Character));

                    int MinY = Glyphs.Select(x => x.Cropping.Top).DefaultIfEmpty(0).Min();
                    int MaxY = MinY + Glyphs.Select(x => x.BoundsInTexture.Height).DefaultIfEmpty(0).Max();

                    _Origins[FontSize] = new(0, MinY);
                    _Heights[FontSize] = MaxY - MinY + 1;
                }

                SupportedStyles = Fonts.Values.SelectMany(x => x.Keys).Distinct()
                    .Where(x => Fonts.Values.All(y => y.ContainsKey(x))).ToList().AsReadOnly();
                SupportedSizes = Fonts.Keys.OrderBy(x => x).ToList().AsReadOnly();
                MaxSize = SupportedSizes.DefaultIfEmpty(0).Max();
                IsValid = true;

                QuickSizeLookup = new();
                foreach (int Size in SupportedSizes)
                {
                    QuickSizeLookup[(Size / 4f).ToString("0.0")] = Size;
                    QuickSizeLookup[(Size / 3f).ToString("0.0")] = Size;
                    QuickSizeLookup[(Size / 2f).ToString("0.0")] = Size;
                }
                foreach (int Size in SupportedSizes)
                {
                    QuickSizeLookup[Size.ToString("0.0")] = Size;
                }
            }
        }

        public FontSet(string FontName, Dictionary<SpriteFont, FontMetadata> SpriteFonts)
        {
            this.Name = FontName;

            if (!SpriteFonts.Any())
            {
                SupportedStyles = new List<CustomFontStyles>().AsReadOnly();
                Fonts = new();
                SupportedSizes = new List<int>().AsReadOnly();
                QuickSizeLookup = new();
                MaxSize = 0;
                _Origins = new();
                _Heights = new();
                IsValid = false;
            }
            else
            {
                Fonts = new();
                foreach (KeyValuePair<SpriteFont, FontMetadata> KVP in SpriteFonts)
                {
                    SpriteFont SF = KVP.Key;
                    int Size = KVP.Value.Size;
                    bool IsBold = KVP.Value.IsBold;
                    bool IsItalic = KVP.Value.IsItalic;

                    if (!Fonts.TryGetValue(Size, out Dictionary<CustomFontStyles, SpriteFont> Variations))
                    {
                        Variations = new();
                        Fonts.Add(Size, Variations);
                    }

                    CustomFontStyles Styles = CustomFontStyles.None;
                    if (IsBold)
                        Styles |= CustomFontStyles.Bold;
                    if (IsItalic)
                        Styles |= CustomFontStyles.Italic;
                    Variations.Add(Styles, SF);
                }

                //  I'm not a fan of all the excess space above and below the text that Monogame leaves by default when using SpriteFont.DrawString.
                //  So this logic determines the top and bottom Y values of the characters that I care about drawing
                //  and is then used to precisely position and measure my text
                _Origins = new();
                _Heights = new();
                foreach (var KVP in Fonts)
                {
                    int FontSize = KVP.Key;
                    IEnumerable<SpriteFont.Glyph> Glyphs = KVP.Value.Values.SelectMany(x => x.GetGlyphs().Values).Where(x => CharacterSet.Contains(x.Character));

                    int MinY = Glyphs.Select(x => x.Cropping.Top).DefaultIfEmpty(0).Min();
                    int MaxY = MinY + Glyphs.Select(x => x.BoundsInTexture.Height).DefaultIfEmpty(0).Max();

                    _Origins[FontSize] = new(0, MinY);
                    _Heights[FontSize] = MaxY - MinY + 1;
                }

                SupportedStyles = Fonts.Values.SelectMany(x => x.Keys).Distinct()
                    .Where(x => Fonts.Values.All(y => y.ContainsKey(x))).ToList().AsReadOnly();
                SupportedSizes = Fonts.Keys.OrderBy(x => x).ToList().AsReadOnly();
                MaxSize = SupportedSizes.DefaultIfEmpty(0).Max();
                IsValid = true;

                QuickSizeLookup = new();
                foreach (int Size in SupportedSizes)
                {
                    QuickSizeLookup[(Size / 4f).ToString("0.0")] = Size;
                    QuickSizeLookup[(Size / 3f).ToString("0.0")] = Size;
                    QuickSizeLookup[(Size / 2f).ToString("0.0")] = Size;
                }
                foreach (int Size in SupportedSizes)
                {
                    QuickSizeLookup[Size.ToString("0.0")] = Size;
                }
            }
        }

        private static readonly string FontSizePattern = @"(?<Size>\d{1,3})";
        private static readonly string FontStylesPattern = $"(?<Styles>(({string.Join("|", AllStyles.Where(x => x != CustomFontStyles.None).Select(x => x.GetDescription()))})_?)*)";
        private static readonly string FontFileExtPattern = @"(\.(?i)(spritefont|xnb)(?-i))?";
        private static readonly string FontFilenamePattern = $@"^{FontSizePattern}_{FontStylesPattern}{FontFileExtPattern}$";
        private static readonly Regex FontFilenameParser = new Regex(FontFilenamePattern);

        private static bool TryParseFontFilename(string FolderPath, string Filename, out FontParseResult? Result)
        {
            //EX: "8_Bold_Italic.spritefont"
            //  Size = 8
            //  Styles = CustomFontStyles.Bold | CustomFontStyles.Italic

            if (!FontFilenameParser.IsMatch(Filename))
            {
                Result = null;
                return false;
            }
            else
            {
                Match Match = FontFilenameParser.Match(Filename);

                string SizeString = Match.Groups["Size"].Value;
                int Size = int.Parse(SizeString);

                CustomFontStyles Styles = CustomFontStyles.None;
                string StylesString = Match.Groups["Styles"].Value;
                foreach (string StyleName in StylesString.Split("_"))
                {
                    Styles |= AllStyles.First(x => x.GetDescription().Equals(StyleName, StringComparison.CurrentCultureIgnoreCase));
                }

                Result = new(Size, Styles, Path.Combine(FolderPath, Filename));
                return true;
            }
        }

        /// <param name="DesiredFontSize">The desired height, in points, of the returned <see cref="SpriteFont"/> <paramref name="Result"/></param>
        /// <param name="PreferDownsampled">If true, will attempt to retrieve a font size that is approximately twice as large as the <paramref name="DesiredFontSize"/>, and scale it down by approximatly 0.5</param>
        /// <param name="ExactScale">The scale factor that must be applied to the returned <see cref="SpriteFont"/> <paramref name="Result"/> to make it have the desired <paramref name="DesiredFontSize"/></param>
        /// <param name="SuggestedScale">Recommended scale factor that might not result in the exact value for <paramref name="DesiredFontSize"/>, but will result in sharper, less blurred text.</param>
        public bool TryGetFont(CustomFontStyles Style, int DesiredFontSize, bool PreferDownsampled, out SpriteFont Result, out int Size, out float ExactScale, out float SuggestedScale)
        {
            if (IsValid && SupportsStyle(Style))
            {
                Size = GetTextSize(DesiredFontSize, PreferDownsampled);
                Result = Fonts[Size][Style];
                ExactScale = DesiredFontSize * 1.0f / Size;
                SuggestedScale = 1 / (float)Math.Round(Size * 1.0 / DesiredFontSize, MidpointRounding.ToEven);
                return true;
            }
            else
            {
                Result = null;
                Size = default;
                ExactScale = 0.0f;
                SuggestedScale = 0.0f;
                return false;
            }
        }

        private Dictionary<string, int> QuickSizeLookup { get; }

        private int GetTextSize(float DesiredFontSize, bool PreferDownsampled)
        {
            string Key = DesiredFontSize.ToString("0.0");
            if (QuickSizeLookup.TryGetValue(Key, out int QuickSize))
                return QuickSize;
            else
                return SupportedSizes.SkipWhile(x => PreferDownsampled ? x < DesiredFontSize * 3.0f : x < DesiredFontSize).DefaultIfEmpty(MaxSize).First();
        }
    }
}
