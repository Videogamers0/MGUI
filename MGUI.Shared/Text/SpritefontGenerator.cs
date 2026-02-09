using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Xna.Framework.Content;
using MGUI.Shared.Helpers;

namespace MGUI.Shared.Text
{
    public enum SpritefontStyle
    {
        [Description("Regular")]
        Regular,
        [Description("Bold")]
        Bold,
        [Description("Italic")]
        Italic,
        [Description("Bold, Italic")]
        BoldItalic
    }

    public static class SpritefontGenerator
    {
        public const string DefaultCharacterRegionStart = "&#32;";
        public const string DefaultCharacterRegiondEnd = "&#126;";

        private const string DefaultSpritefontContent = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<XnaContent xmlns:Graphics=""Microsoft.Xna.Framework.Content.Pipeline.Graphics"">
    <Asset Type=""Graphics:FontDescription"">
    <FontName>{{FontName}}</FontName>
    <Size>{{FontSize}}</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>{{FontStyle}}</Style>
    <DefaultCharacter>*</DefaultCharacter>
    <CharacterRegions>
        <CharacterRegion>
        <Start>{{CharacterRegionStart}}</Start>
        <End>{{CharacterRegionEnd}}</End>
        </CharacterRegion>
    </CharacterRegions>
    </Asset>
</XnaContent>";

        /// <summary>Invokes <see cref="Generate(ContentManager, string, IEnumerable{SpritefontStyle}, IEnumerable{int}, string, string)"/> with default settings for font styles and sizes.</summary>
        public static void GenerateDefault(ContentManager Content, string FontName, string CharacterRegionStart = DefaultCharacterRegionStart, string CharacterRegionEnd = DefaultCharacterRegiondEnd)
        {
            List<SpritefontStyle> Styles = new() { SpritefontStyle.Regular, SpritefontStyle.Bold, SpritefontStyle.Italic };
            //https://community.monogame.net/t/font-runtime-rendering/10584/34 User recommends these sizes: 8,9,10,12,16,18,24,36,72
            //Note: When importing the .spritefont files into MGCB, don't forget to change the smaller sizes to 'Color' instead of 'Compressed' texture format
            List<int> FontSizes = new() { 8, 9, 10, 11, 12, 13, 14, 16, 18, 20, 24, 36, 48, 72 };
            Generate(Content, FontName, Styles, FontSizes, CharacterRegionStart, CharacterRegionEnd);
        }

        /// <summary>Creates several .spritefont files in {ExeFolder}\{Content.RootDirectory}\Fonts\{FontName}, one file for each permutation of <paramref name="FontStyles"/> and <paramref name="FontSizes"/><para/>
        /// The files will be named in the following format:<br/>
        /// <code>{FontSize}_{Underscore-Delimited-FontStyles}.xnb</code><para/>
        /// Example format:<br/>
        /// {ExeFolder}\Content\Fonts\Arial\10_Bold.xnb - Represents a SpriteFont where Size=10pts for the 'Arial Bold' Font Family.<br/>
        /// {ExeFolder}\Content\Fonts\Calibri\12_Bold_Italic.xnb - Represents a SpriteFont where Size=12pts for the 'Calibri Bold Italic' Font Family.<br/>
        /// {ExeFolder}\Content\Fonts\Helvetica\14_Normal.xnb - Represents a SpriteFont where Size=14pts for the 'Helvetica' Font Family.</summary>
        /// <param name="FontName">The base name of the font, such as 'Arial' or 'Times New Roman'. This value should not include any suffixes related to the font's style (like 'Arial Bold')</param>
        /// <param name="FontStyles">The font styles to generate a .spritefont file for.</param>
        /// <param name="FontSizes">The font sizes (in points) to generate a .spritefont file for.</param>
        /// <param name="CharacterRegionStart">The Start Character region to use. Default value: <see cref="DefaultCharacterRegionStart"/></param>
        /// <param name="CharacterRegionEnd">The End Character region to use. Default value: <see cref="DefaultCharacterRegiondEnd"/></param>
        public static void Generate(ContentManager Content, string FontName, IEnumerable<SpritefontStyle> FontStyles, IEnumerable<int> FontSizes, 
            string CharacterRegionStart = DefaultCharacterRegionStart, string CharacterRegionEnd = DefaultCharacterRegiondEnd)
        {
            string ContentAbsolutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "..", Content.RootDirectory);
            string SharedFontDirectory = Path.Combine(ContentAbsolutePath, FontSet.FontsBasePath);
            if (!Directory.Exists(SharedFontDirectory))
                throw new InvalidOperationException($"No font root direction was found at: {SharedFontDirectory}");

            string FontDirectory = Path.Combine(SharedFontDirectory, FontName);
            if (!Directory.Exists(FontDirectory))
                Directory.CreateDirectory(FontDirectory);

            foreach (SpritefontStyle SFStyle in FontStyles)
            {
                CustomFontStyles Style = IndexedStyles[SFStyle];
                string StyleString = string.Join("_", FontSet.AllStyles.Where(x => x != CustomFontStyles.None && Style.HasFlag(x)).Select(x => x.GetDescription()));

                //https://community.monogame.net/t/spritefont-style-bold-italic-etc-isnt-working/2263/12
                //  On Windows 10: Start -> "Font Settings" -> Pick a font -> Change the font face and see what value is listed under "Full name"
                string ActualFontName = GeneralUtils.IsPlatformUnix ?
                    FontName :
                    SFStyle switch
                    {
                        SpritefontStyle.Regular => FontName,
                        SpritefontStyle.Bold => $"{FontName} Bold",
                        SpritefontStyle.Italic => $"{FontName} Italic",
                        SpritefontStyle.BoldItalic => $"{FontName} Bold Italic",
                        _ => throw new NotImplementedException($"Unrecognized {nameof(SpritefontStyle)}: {SFStyle}")
                    };

                foreach (float SizePts in FontSizes)
                {
                    string Filename = $"{SizePts}_{StyleString}.spritefont";
                    string FilePath = Path.Combine(FontDirectory, Filename);

                    string FileContent = DefaultSpritefontContent
                        .Replace("{{FontName}}", ActualFontName)
                        .Replace("{{FontSize}}", SizePts.ToString("0.###"))
                        .Replace("{{FontStyle}}", SFStyle.GetDescription())
                        .Replace("{{CharacterRegionStart}}", CharacterRegionStart)
                        .Replace("{{CharacterRegionEnd}}", CharacterRegionEnd);

                    File.WriteAllText(FilePath, FileContent);
                }
            }
        }

        private static readonly Dictionary<SpritefontStyle, CustomFontStyles> IndexedStyles = new()
        {
            { SpritefontStyle.Regular, CustomFontStyles.Normal },
            { SpritefontStyle.Bold, CustomFontStyles.Bold },
            { SpritefontStyle.Italic, CustomFontStyles.Italic },
            { SpritefontStyle.BoldItalic, CustomFontStyles.Bold | CustomFontStyles.Italic }
        };
    }
}
