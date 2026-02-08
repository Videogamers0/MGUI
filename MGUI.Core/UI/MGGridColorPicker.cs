using Microsoft.Xna.Framework;
using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoGame.Extended;
using System.Diagnostics;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers.Grids;
using ColorStringConverter = MGUI.Core.UI.XAML.ColorStringConverter;
using MGUI.Core.UI.Containers;

namespace MGUI.Core.UI
{
    public enum ColorPalette
    {
        /// <summary>4 grayscale colors:<br/><see href="https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#2-bit_Grayscale"/></summary>
        _2Bit_Grayscale,
        /// <summary>16 grayscale colors:<br/><see href="https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#4-bit_Grayscale"/></summary>
        _4Bit_Grayscale,
        /// <summary>64 grayscale colors, starting from rgb(0,0,0), increasing by 4 with each color, and ending at rgb(252,252,252)</summary>
        _6Bit_Grayscale,
        /// <summary>8 colors:<br/><see href="https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#3-bit_RGB"/></summary>
        _3Bit_RGB,
        /// <summary>64 colors, palette used by a few consoles such as Sega Master System:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#6-bit_RGB"/><para/>
        /// This palette is often displayed with 16 columns, 4 rows.</summary>
        _6Bit_RGB,
        /// <summary>16 colors, similar to <see cref="_3Bit_RGB"/> but each color has both a 'bright' and 'dark' variant:<br/>
        /// <see href="https://en.wikipedia.org/w/index.php?title=List_of_monochrome_and_RGB_color_formats&amp;useskin=vector#4-bit_RGBI"/></summary>
        _4Bit_RGBI,
        /// <summary>27 colors:<br/>
        /// <see href="https://en.wikipedia.org/w/index.php?title=List_of_monochrome_and_RGB_color_formats&amp;useskin=vector#3-level_RGB"/><para/>
        /// Colors are ordered according to: <see href="https://upload.wikimedia.org/wikipedia/commons/thumb/b/b1/3-Level-RGB-Colors.svg/300px-3-Level-RGB-Colors.svg.png"/><para/>
        /// This palette is often displayed with 9 columns, 3 rows.</summary>
        _3Level_RGB,
        /// <summary>16 colors commonly used by Windows OS:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_software_palettes#Microsoft_Windows_and_IBM_OS/2_default_16-color_palette"/><para/>
        /// This palette is often displayed with 8 columns, 2 rows.</summary>
        Windows_16,
        /// <summary>20 colors commonly used by Windows OS:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_software_palettes#Microsoft_Windows_default_20-color_palette"/></summary>
        Windows_20,
        /// <summary>16 colors commonly used by Apple Macintosh computers:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_software_palettes#Apple_Macintosh_default_16-color_palette"/><para/>
        /// This palette is often displayed with 8 columns, 2 rows.</summary>
        Mac_16,
        /// <summary>64 colors:<br/><see href="https://en.wikipedia.org/wiki/Enhanced_Graphics_Adapter#Color_palette"/><para/>
        /// Colors are ordered according to: <see href="https://upload.wikimedia.org/wikipedia/commons/0/02/EGA_Table.svg"/> and <see href="https://en.wikipedia.org/wiki/File:EGA64_Full_Palette.png"/>.<para/>
        /// This palette is often displayed with 8 columns, 8 rows.</summary>
        EGA_64,
        /// <summary>128 colors used by the Atari 2600 NTSC format:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_video_game_console_palettes#Atari_2600"/><para/>
        /// This palette is often displayed with 16 columns, 8 rows.</summary>
        Atari2600_NTSC,
        /// <summary>52 colors used by the Nintendo Entertainment System:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_video_game_console_palettes#Nintendo_Entertainment_System"/><para/>
        /// This palette is often displayed with 13 columns, 4 rows<para/>
        /// Note: The duplicate blacks and whites have been omitted, and only 1 dark gray and 1 light gray are included in this palette, so it's not exactly the same as the wikipedia page shows.</summary>
        NES,
        /// <summary>16 colors:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_video_game_console_palettes#Intellivision"/><para/>
        /// Colors are ordered according to: <see href="https://en.wikipedia.org/wiki/File:Intellivision_Palette.png"/><para/>
        /// This palette is often displayed with 8 columns, 2 rows.</summary>
        Intellivision,
        /// <summary>16 colors:<br/>
        /// <see href="https://en.wikipedia.org/wiki/List_of_video_game_console_palettes#Super_Cassette_Vision"/><para/>
        /// This palette is often displayed with 4 columns, 4 rows.</summary>
        SuperCassetteVision
    }

    public static class ColorPalettes
    {
        public static readonly IReadOnlyDictionary<ColorPalette, int> SuggestedColumnCounts = new Dictionary<ColorPalette, int>()
        {
            { ColorPalette._2Bit_Grayscale, 4 },
            { ColorPalette._4Bit_Grayscale, 8 },
            { ColorPalette._6Bit_Grayscale, 8 },
            { ColorPalette._3Bit_RGB, 8 },
            { ColorPalette._6Bit_RGB, 16 },
            { ColorPalette._4Bit_RGBI, 8 },
            { ColorPalette._3Level_RGB, 9 },
            { ColorPalette.Windows_16, 8 },
            { ColorPalette.Windows_20, 10 },
            { ColorPalette.Mac_16, 8 },
            { ColorPalette.EGA_64, 8 },
            { ColorPalette.Atari2600_NTSC, 16 },
            { ColorPalette.NES, 13 },
            { ColorPalette.Intellivision, 8 },
            { ColorPalette.SuperCassetteVision, 4 }
        };

        private static readonly IReadOnlyDictionary<ColorPalette, IReadOnlyList<string>> Colors = new Dictionary<ColorPalette, IReadOnlyList<string>>()
        {
            {
                ColorPalette._2Bit_Grayscale,
                new List<string>() { "Black", "rgb(104,104,104)", "rgb(184,184,184)", "White" }
            },
            {
                ColorPalette._4Bit_Grayscale,
                new List<string>()
                { 
                    "Black", "rgb(17,17,17)", "rgb(34,34,34)", "rgb(51,51,51)", "rgb(68,68,68)", "rgb(85,85,85)", "rgb(102,102,102)", "rgb(119,119,119)",
                    "rgb(136,136,136)", "rgb(153,153,153)", "rgb(170,170,170)", "rgb(187,187,187)", "rgb(204,204,204)", "rgb(221,221,221)", "rgb(238,238,238)", "White",
                }
            },
            {
                ColorPalette._3Bit_RGB,
                new List<string>() { "Black", "rgb(0,39,251)", "rgb(0,249,44)", "rgb(0,252,254)", "rgb(255,48,22)", "rgb(255,63,252)", "rgb(255,253,51)", "White" }
            },
            {
                ColorPalette._6Bit_RGB,
                new List<string>()
                {
                    "#000000", "#000055", "#0000aa", "#0000ff", "#550000", "#550055", "#5500aa", "#5500ff", 
                    "#aa0000", "#aa0055", "#aa00aa", "#aa00ff", "#ff0000", "#ff0055", "#ff00aa", "#ff00ff", 
                    "#005500", "#005555", "#0055aa", "#0055ff", "#555500", "#555555", "#5555aa", "#5555ff",
                    "#aa5500", "#aa5555", "#aa55aa", "#aa55ff", "#ff5500", "#ff5555", "#ff55aa", "#ff55ff",
                    "#00aa00", "#00aa55", "#00aaaa", "#00aaff", "#55aa00", "#55aa55", "#55aaaa", "#55aaff",
                    "#aaaa00", "#aaaa55", "#aaaaaa", "#aaaaff", "#ffaa00", "#ffaa55", "#ffaaaa", "#ffaaff",
                    "#00ff00", "#00ff55", "#00ffaa", "#00ffff", "#55ff00", "#55ff55", "#55ffaa", "#55ffff",
                    "#aaff00", "#aaff55", "#aaffaa", "#aaffff", "#ffff00", "#ffff55", "#ffffaa", "#ffffff"
                }
            },
            {
                ColorPalette._4Bit_RGBI,
                new List<string>()
                {
                    "#000000", "#005500", "#00aa00", "#00ff00", "#0000ff", "#0055ff", "#00aaff", "#00ffff",
                    "#ff0000", "#ff5500", "#ffaa00", "#ffff00", "#ff00ff", "#ff55ff", "#ffaaff", "#ffffff"
                }
            },
            {
                ColorPalette._3Level_RGB,
                new List<string>()
                {
                    "#000000", "#000080", "#0000ff", "#800000", "#800080", "#8000FF", "#FF0000", "#FF0080", "#FF00FF",
                    "#008000", "#008080", "#0080FF", "#808000", "#808080", "#8080FF", "#FF8000", "#FF8080", "#FF80FF",
                    "#00FF00", "#00FF80", "#00FFFF", "#80FF00", "#80FF80", "#80FFFF", "#FFFF00", "#FFFF80", "#FFFFFF"
                }
            },
            {
                ColorPalette.Windows_16,
                new List<string>()
                {
                    "Black", "rgb(128,0,0)", "rgb(0,128,0)", "rgb(128,128,0)", "rgb(0,0,128)", "rgb(128,0,128)", "rgb(0,128,128)", "rgb(192,192,192)",
                    "rgb(128,128,128)", "rgb(255,0,0)", "rgb(0,255,0)", "rgb(255,255,0)", "rgb(0,0,255)", "rgb(255,0,255)", "rgb(0,255,255)", "White"
                }
            },
            {
                ColorPalette.Windows_20,
                new List<string>()
                {
                    "Black", "rgb(128,0,0)", "rgb(0,128,0)", "rgb(128,128,0)", "rgb(0,0,128)", "rgb(128,0,128)", "rgb(0,128,128)", "rgb(192,192,192)", "rgb(192,220,192)", "rgb(166,202,240)",
                    "rgb(255,251,240)", "rgb(160,160,164)", "rgb(128,128,128)", "rgb(255,0,0)", "rgb(0,255,0)", "rgb(255,255,0)", "rgb(0,0,255)", "rgb(255,0,255)", "rgb(0,255,255)", "White"
                }
            },
            {
                ColorPalette.Mac_16,
                new List<string>()
                {
                    "#ffffff", "#fcf305", "#ff6402", "#dd0806", "#f20884", "#4600a5", "#0000d4", "#02abea",
                    "#1fb714", "#006411", "#562c05", "#90713a", "#c0c0c0", "#808080", "#404040", "#000000"
                }
            },
            {
                ColorPalette.EGA_64,
                new List<string>()
                {
                    "#000000", "#0000AA", "#00AA00", "#00AAAA", "#AA0000", "#AA00AA", "#AAAA00", "#AAAAAA",
                    "#000055", "#0000FF", "#00AA55", "#00AAFF", "#AA0055", "#AA00FF", "#AAAA55", "#AAAAFF",
                    "#005500", "#0055AA", "#00FF00", "#00FFAA", "#AA5500", "#AA55AA", "#AAFF00", "#AAFFAA",
                    "#005555", "#0055FF", "#00FF55", "#00FFFF", "#AA5555", "#AA55FF", "#AAFF55", "#AAFFFF",
                    "#550000", "#5500AA", "#55AA00", "#55AAAA", "#FF0000", "#FF00AA", "#FFAA00", "#FFAAAA",
                    "#550055", "#5500FF", "#55AA55", "#55AAFF", "#FF0055", "#FF00FF", "#FFAA55", "#FFAAFF",
                    "#555500", "#5555AA", "#55FF00", "#55FFAA", "#FF5500", "#FF55AA", "#FFFF00", "#FFFFAA",
                    "#555555", "#5555FF", "#55FF55", "#55FFFF", "#FF5555", "#FF55FF", "#FFFF55", "#FFFFFF"
                }
            },
            {
                ColorPalette.Atari2600_NTSC,
                new List<string>()
                {
                    "#000000", "#444400", "#702800", "#841800", "#880000", "#78005c", "#480078", "#140084",
                    "#000088", "#00187c", "#002c5c", "#00402c", "#003c00", "#143800", "#2c3000", "#442800",
                    "#404040", "#646410", "#844414", "#983418", "#9c2020", "#8c2074", "#602090", "#302098",
                    "#1c209c", "#1c3890", "#1c4c78", "#1c5c48", "#205c20", "#345c1c", "#4c501c", "#644818",
                    "#6c6c6c", "#848424", "#985c28", "#ac5030", "#b03c3c", "#a03c88", "#783ca4", "#4c3cac",
                    "#3840b0", "#3854a8", "#386890", "#387c64", "#407c40", "#507c38", "#687034", "#846830",
                    "#909090", "#a0a034", "#ac783c", "#c06848", "#c05858", "#b0589c", "#8c58b8", "#6858c0",
                    "#505cc0", "#5070bc", "#5084ac", "#509c80", "#5c9c5c", "#6c9850", "#848c4c", "#a08444",
                    "#b0b0b0", "#b8b840", "#bc8c4c", "#d0805c", "#d07070", "#c070b0", "#a070cc", "#7c70d0",
                    "#6874d0", "#6888cc", "#689cc0", "#68b494", "#74b474", "#84b468", "#9ca864", "#b89c58",
                    "#c8c8c8", "#d0d050", "#cca05c", "#e09470", "#e08888", "#d084c0", "#b484dc", "#9488e0",
                    "#7c8ce0", "#7c9cdc", "#7cb4d4", "#7cd0ac", "#8cd08c", "#9ccc7c", "#b4c078", "#d0b46c",
                    "#dcdcdc", "#e8e85c", "#dcb468", "#eca880", "#eca0a0", "#dc9cd0", "#c49cec", "#a8a0ec",
                    "#90a4ec", "#90b4ec", "#90cce8", "#90e4c0", "#a4e4a4", "#b4e490", "#ccd488", "#e8cc7c",
                    "#ececec", "#fcfc68", "#fcbc94", "#fcb4b4", "#ecb0e0", "#d4b0fc", "#bcb4fc", "#a4b8fc",
                    "#a4c8fc", "#a4e0fc", "#a4fcd4", "#b8fcb8", "#c8fca4", "#e0ec9c", "#fce08c", "#ffffff"
                }
            },
            {
                ColorPalette.NES,
                new List<string>()
                {
                    "#000000", "#002a88", "#1412a8", "#3b00a4", "#5c007e", "#6e0040", "#6c0700", "#571d00", "#343500", "#0c4900", "#005200", "rgb(0,68,68)", "rgb(0,68,102)",
                    "#666666", "#155fda", "#4240fe", "#7627ff", "#a11bcd", "#b81e7c", "#b53220", "#994f00", "#6c6e00", "#388700", "#0d9400", "rgb(0,102,85)", "rgb(0,85,136)",
                    "#aeaeae", "#64b0fe", "#9390fe", "#c777fe", "#f36afe", "#fe6ecd", "#fe8270", "#eb9f23", "#bdbf00", "#89d900", "#5de530", "#45e182", "#48cedf",
                    "#ffffff", "#c1e0fe", "#d4d3fe", "#e9c8fe", "#fbc3fe", "#fec5eb", "#fecdc6", "#f7d9a6", "#e5e695", "#d0f097", "#bef5ab", "#b4f3cd", "#b5ecf3"
                }
            },
            {
                ColorPalette.Intellivision,
                new List<string>()
                {
                    "#0c0005", "#002dff", "#ff3e00", "#c9d464", "#00780f", "#00a720", "#faea27", "#fffcff",
                    "#a7a8a8", "#5acbff", "#ffa600", "#3c5800", "#ff3276", "#bd95ff", "#6ccd30", "#c81a7d"
                }
            },
            {
                ColorPalette.SuperCassetteVision,
                new List<string>()
                {
                    "#000000", "#ff0000", "#ffa100", "#ffa09f", "#ffff00", "#a3a000", "#00a100", "#00ff00",
                    "#a0ff9d", "#00009b", "#0000ff", "#a200ff", "#ff00ff", "#00ffff", "#a2a19f", "#ffffff"
                }
            }
        };

        public static IReadOnlyList<Color> GetColors(ColorPalette Palette)
        {
            if (Colors.ContainsKey(Palette))
                return Colors[Palette].Select(x => ColorStringConverter.ParseColor(x).ToXNAColor()).ToList();
            else
            {
                List<Color> Result = new();
                switch (Palette)
                {
                    case ColorPalette._6Bit_Grayscale:
                        for (int c = 0; c <= byte.MaxValue; c += 4)
                            Result.Add(new Color(c, c, c));
                        return Result;
                    default: throw new NotImplementedException($"Unrecognized {nameof(ColorPalette)}: {Palette}");
                };
            }
        }
    }

    /// <summary>Displays a grid of colors to choose from.</summary>
    public class MGGridColorPicker : MGElement
    {
        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Columns;
        /// <summary>Determines how many colors to display in each row before wrapping to the next row.<para/>
        /// See also: <see cref="Rows"/></summary>
        public int Columns
        {
            get => _Columns;
            set
            {
                if (_Columns != value)
                {
                    _Columns = value;
                    NPC(nameof(Columns));
                    NPC(nameof(Rows));
                    LayoutChanged(this, true);
                }
            }
        }

        /// <summary>Determines how many rows of color swabs to display in the grid.<para/>Derived from <see cref="Columns"/> and <see cref="Colors"/> count.</summary>
        public int Rows => Columns <= 0 || Colors == null || Colors.Count <= 0 ? 0 : (Colors.Count - 1) / Columns + 1;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<Color> _Colors;
        /// <summary>The colors to display in the grid. The first item is rendered in the topleft corner, then moves left-to-right, top-to-bottom with each item in this list.<para/>
        /// Note: Setting this value will clear the <see cref="SelectedColorIndexes"/>.</summary>
        public IReadOnlyList<Color> Colors
        {
            get => _Colors;
            set
            {
                if (_Colors != value)
                {
                    _Colors = value;
                    NPC(nameof(Colors));
                    NPC(nameof(Rows));
                    LayoutChanged(this, true);
                    HoveredColorIndex = null;
                    SelectedColorIndexes = new List<int>();
                }
            }
        }

        /// <summary>Sets the pickable <see cref="Colors"/> list to a pre-set color palette.</summary>
        /// <param name="TryUseRecommendedColumnCount">If <see langword="true"/>, will also attempt to set <see cref="Columns"/> to a value that is typically used to display the given <paramref name="Palette"/>.<para/>
        /// For example, <see cref="ColorPalette._3Level_RGB"/> consists of 27 colors that are usually displayed using 9 columns and 3 rows.</param>
        public void SetColors(ColorPalette Palette, bool TryUseRecommendedColumnCount)
        {
            Colors = ColorPalettes.GetColors(Palette);
            if (TryUseRecommendedColumnCount && ColorPalettes.SuggestedColumnCounts.TryGetValue(Palette, out int SuggestedColumnCount))
                Columns = SuggestedColumnCount;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Size _ColorSize;
        /// <summary>The total size of each color swab (including any BorderThicknesses)</summary>
        public Size ColorSize
        {
            get => _ColorSize;
            set
            {
                if (_ColorSize != value)
                {
                    _ColorSize = value;
                    NPC(nameof(ColorSize));
                    LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _RowSpacing;
        /// <summary>Empty space between each row of colors.<para/>
        /// See also: <see cref="ColumnSpacing"/></summary>
        public int RowSpacing
        {
            get => _RowSpacing;
            set
            {
                if (_RowSpacing != value)
                {
                    _RowSpacing = value;
                    NPC(nameof(RowSpacing));
                    LayoutChanged(this, true);
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _ColumnSpacing;
        /// <summary>Empty space between each column of colors.<para/>
        /// See also: <see cref="RowSpacing"/></summary>
        public int ColumnSpacing
        {
            get => _ColumnSpacing;
            set
            {
                if (_ColumnSpacing != value)
                {
                    _ColumnSpacing = value;
                    NPC(nameof(ColumnSpacing));
                    LayoutChanged(this, true);
                }
            }
        }

        #region Borders
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IBorderBrush _SelectedColorBorderBrush;
        /// <summary>A border to render around the selected color(s).<para/>
        /// See also:<br/><see cref="SelectedColorBorderBrush"/><br/><see cref="SelectedColorBorderThickness"/><br/><see cref="UnselectedColorBorderBrush"/><br/><see cref="UnselectedColorBorderThickness"/></summary>
        public IBorderBrush SelectedColorBorderBrush
        {
            get => _SelectedColorBorderBrush;
            set
            {
                if (_SelectedColorBorderBrush != value)
                {
                    _SelectedColorBorderBrush = value;
                    NPC(nameof(SelectedColorBorderBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _SelectedColorBorderThickness;
        /// <summary>The thickness to use when drawing the <see cref="SelectedColorBorderBrush"/> around the selected color(s).<para/>
        /// See also:<br/><see cref="SelectedColorBorderBrush"/><br/><see cref="SelectedColorBorderThickness"/><br/><see cref="UnselectedColorBorderBrush"/><br/><see cref="UnselectedColorBorderThickness"/></summary>
        public Thickness SelectedColorBorderThickness
        {
            get => _SelectedColorBorderThickness;
            set
            {
                if (!_SelectedColorBorderThickness.Equals(value))
                {
                    _SelectedColorBorderThickness = value;
                    NPC(nameof(SelectedColorBorderThickness));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IBorderBrush _UnselectedColorBorderBrush;
        /// <summary>A border to render around the unselected color(s).<para/>
        /// See also:<br/><see cref="SelectedColorBorderBrush"/><br/><see cref="SelectedColorBorderThickness"/><br/><see cref="UnselectedColorBorderBrush"/><br/><see cref="UnselectedColorBorderThickness"/></summary>
        public IBorderBrush UnselectedColorBorderBrush
        {
            get => _UnselectedColorBorderBrush;
            set
            {
                if (_UnselectedColorBorderBrush != value)
                {
                    _UnselectedColorBorderBrush = value;
                    NPC(nameof(UnselectedColorBorderBrush));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Thickness _UnselectedColorBorderThickness;
        /// <summary>The thickness to use when drawing the <see cref="UnselectedColorBorderBrush"/> around the unselected color(s).<para/>
        /// See also:<br/><see cref="SelectedColorBorderBrush"/><br/><see cref="SelectedColorBorderThickness"/><br/><see cref="UnselectedColorBorderBrush"/><br/><see cref="UnselectedColorBorderThickness"/></summary>
        public Thickness UnselectedColorBorderThickness
        {
            get => _UnselectedColorBorderThickness;
            set
            {
                if (!_UnselectedColorBorderThickness.Equals(value))
                {
                    _UnselectedColorBorderThickness = value;
                    NPC(nameof(UnselectedColorBorderThickness));
                }
            }
        }

        protected override IEnumerable<IBorderBrush> GetBorderBrushes()
        {
            foreach (IBorderBrush Brush in base.GetBorderBrushes())
                yield return Brush;

            yield return SelectedColorBorderBrush;
            yield return UnselectedColorBorderBrush;
        }
        #endregion Borders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int? _HoveredColorIndex;
        public int? HoveredColorIndex
        {
            get => _HoveredColorIndex;
            private set
            {
                if (_HoveredColorIndex != value)
                {
                    _HoveredColorIndex = value;
                    NPC(nameof(HoveredColorIndex));
                    NPC(nameof(HoveredColor));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _HoveredColorOverlay;
        /// <summary>An additional brush to draw overtop of the color that is hovered by the mouse position.<br/>
        /// This value should have transparency so that the color is still visible.<para/>
        /// Default value: Solid white with 0.2 opacity.</summary>
        public IFillBrush HoveredColorOverlay
        {
            get => _HoveredColorOverlay;
            set
            {
                if (_HoveredColorOverlay != value)
                {
                    _HoveredColorOverlay = value;
                    NPC(nameof(HoveredColorOverlay));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IFillBrush _SelectedColorOverlay;
        /// <summary>An additional brush to draw overtop of the selected color(s).<br/>
        /// This value should have transparency so that the color is still visible, such as a Yellow brush with 0.2 opacity.<para/>
        /// Default value: <see langword="null"/></summary>
        public IFillBrush SelectedColorOverlay
        {
            get => _SelectedColorOverlay;
            set
            {
                if (_SelectedColorOverlay != value)
                {
                    _SelectedColorOverlay = value;
                    NPC(nameof(SelectedColorOverlay));
                }
            }
        }

        public Color? HoveredColor => HoveredColorIndex.HasValue ? Colors[HoveredColorIndex.Value] : null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<int> _SelectedColorIndexes;
        /// <summary>The indexes of the selected colors.<para/>
        /// If <see cref="AllowMultiSelect"/> is <see langword="false"/>, this value can only be set to a list of at-most 1 element.<br/>
        /// Set to an empty list to clear the selection.</summary>
        public IReadOnlyList<int> SelectedColorIndexes
        {
            get => _SelectedColorIndexes;
            set
            {
                if (_SelectedColorIndexes != value)
                {
                    if (!AllowMultiSelect)
                        _SelectedColorIndexes = value?.Take(1).ToList() ?? new List<int>();
                    else
                        _SelectedColorIndexes = value ?? new List<int>();
                    NPC(nameof(SelectedColorIndexes));
                    NPC(nameof(SelectedColor));
                    NPC(nameof(SelectedColors));

                    RefreshSelectedColorLabelVisibility();
                    if (IsSelectedColorLabelVisible)
                        SelectedColorValue.Fill = SelectedColor.Value.AsFillBrush();
                }
            }
        }

        public IEnumerable<Color> SelectedColors => SelectedColorIndexes.Select(x => Colors[x]);

        /// <summary>Convenience property that just sets <see cref="SelectedColorIndexes"/> to a list of 1 item.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Color? SelectedColor
        {
            get => SelectedColorIndexes.Count >= 1 ? Colors[SelectedColorIndexes.First()] : null;
            set
            {
                if (!value.HasValue || !TryGetColorIndex(value.Value, out int Index))
                    SelectedColorIndexes = new List<int>();
                else
                    SelectedColorIndexes = new List<int>() { Index };
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _ShowSelectedColorLabel;
        /// <summary>If true, a text label and colored rectangle will be visible below the color grid, which shows the selected color.<para/>
        /// (The label is only visible if if <see cref="AllowMultiSelect"/> is <see langword="false"/> AND <see cref="SelectedColor"/> is not <see langword="null"/> AND <see cref="ShowSelectedColorLabel"/> is <see langword="true"/>)</summary>
        public bool ShowSelectedColorLabel
        {
            get => _ShowSelectedColorLabel;
            set
            {
                if (_ShowSelectedColorLabel != value)
                {
                    _ShowSelectedColorLabel = value;
                    NPC(nameof(ShowSelectedColorLabel));
                    RefreshSelectedColorLabelVisibility();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _AllowMultiSelect;
        /// <summary>If true, multiple colors can be selected by clicking them while holding CTRL</summary>
        public bool AllowMultiSelect
        {
            get => _AllowMultiSelect;
            set
            {
                if (_AllowMultiSelect != value)
                {
                    _AllowMultiSelect = value;
                    NPC(nameof(AllowMultiSelect));
                    if (!AllowMultiSelect && SelectedColorIndexes.Count > 1)
                        SelectedColorIndexes = SelectedColorIndexes.Take(1).ToList();
                    RefreshSelectedColorLabelVisibility();
                }
            }
        }

        public MGComponent<MGHeaderedContentPresenter> SelectedColorComponent { get; }
        /// <summary>The element that displays the Selected Color info. below the color grid.<para/>
        /// Only visible if <see cref="AllowMultiSelect"/> is <see langword="false"/> AND <see cref="SelectedColor"/> is not <see langword="null"/> AND <see cref="ShowSelectedColorLabel"/> is <see langword="true"/></summary>
        public MGHeaderedContentPresenter SelectedColorPresenter { get; }
        /// <summary>The textblock label next to the selected color value below the color grid. Displays the text: "Selected Color:"</summary>
        public MGTextBlock SelectedColorLabel { get; }
        /// <summary>The colored rectangle that displays the selected color below the color grid.</summary>
        public MGRectangle SelectedColorValue { get; }

        private bool IsSelectedColorLabelVisible => !AllowMultiSelect && SelectedColorIndexes.Count == 1 && ShowSelectedColorLabel;

        private void RefreshSelectedColorLabelVisibility() =>
            SelectedColorPresenter.Visibility = IsSelectedColorLabelVisible ? Visibility.Visible : Visibility.Collapsed;

        public MGGridColorPicker(MGWindow Window, int Columns, params Color[] Colors)
            : this(Window, Columns, new Size(16, 16), Colors) { }

        public MGGridColorPicker(MGWindow Window, int Columns, Size ColorSize, params Color[] Colors)
            : this(Window, Columns, ColorSize, Math.Max(4, ColorSize.Width / 5), Math.Max(4, ColorSize.Height / 5), Colors) { }

        public MGGridColorPicker(MGWindow Window, int Columns, Size ColorSize, int ColumnSpacing, int RowSpacing, params Color[] Colors)
            : base(Window, MGElementType.GridColorPicker)
        {
            using (BeginInitializing())
            {
                BorderElement = new(Window, 0, null as IFillBrush);
                BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);
                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                SelectedColorLabel = new(Window, "Selected Color:")
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new(5,2,0,2)
                };
                SelectedColorValue = new(Window, 16, 16, Color.Black, 1, Color.Transparent)
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new(0,2,5,2)
                };
                SelectedColorPresenter = new(Window, SelectedColorLabel, SelectedColorValue)
                {
                    HeaderPosition = Dock.Left,
                    Spacing = 5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                };
                SelectedColorPresenter.BackgroundBrush.SetAll(MGSolidFillBrush.Black * 0.75f);
                SelectedColorComponent = new MGComponent<MGHeaderedContentPresenter>(SelectedColorPresenter, true, false, false, false, false, true, true,
                    (AvailableBounds, ComponentSize) => ApplyAlignment(AvailableBounds.GetCompressed(Padding), HorizontalAlignment.Stretch, VerticalAlignment.Bottom, ComponentSize.Size));
                AddComponent(SelectedColorComponent);

                //  Sync the SelectedColor label's top margin to the entire color picker's bottom padding so the spacing above and below the label is equal to each other
                SelectedColorPresenter.Margin = SelectedColorPresenter.Margin.ChangeTop(Padding.Bottom);
                PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Padding))
                        SelectedColorPresenter.Margin = SelectedColorPresenter.Margin.ChangeTop(Padding.Bottom);
                };

                this.Columns = Columns;
                this.ColorSize = ColorSize;
                this.ColumnSpacing = ColumnSpacing;
                this.RowSpacing = RowSpacing;
                this.Colors = Colors.ToList();

                SelectedColorIndexes = new List<int>();
                AllowMultiSelect = false;
                ShowSelectedColorLabel = false;

                SelectedColorBorderBrush = new MGBandedBorderBrush(new List<Color>() { Color.Black, Color.Yellow }, new List<double>() { 1.0/3, 2.0/3 });
                SelectedColorBorderThickness = new(3);
                UnselectedColorBorderBrush = Color.Black.AsFillBrush().AsUniformBorderBrush();
                UnselectedColorBorderThickness = new(1);

                HoveredColorOverlay = MGSolidFillBrush.White * 0.2f;
                SelectedColorOverlay = null; //Color.Yellow.AsFillBrush() * 0.1f;

                MouseHandler.Exited += (sender, e) => HoveredColorIndex = null;
                MouseHandler.MovedInside += (sender, e) =>
                {
                    if (TryGetHoveredColorIndex(e.CurrentPosition, out GridCellIndex? CellIndex, out int? LinearIndex))
                    {
                        HoveredColorIndex = LinearIndex;
                    }
                    else
                        HoveredColorIndex = null;
                };

                //  Update the selection when a color is clicked
                MouseHandler.ClickedInside += (sender, e) =>
                {
                    if (HoveredColorIndex.HasValue)
                    {
                        if (AllowMultiSelect && e.Tracker.InputTracker.Keyboard.IsControlDown)
                        {
                            if (SelectedColorIndexes.Contains(HoveredColorIndex.Value)) // Deselect if already selected
                                SelectedColorIndexes = SelectedColorIndexes.Where(x => x != HoveredColorIndex.Value).ToList();
                            else
                                SelectedColorIndexes = SelectedColorIndexes.Union(new List<int>() { HoveredColorIndex.Value }).ToList();
                        }
                        else
                        {
                            SelectedColorIndexes = new List<int>() { HoveredColorIndex.Value };
                        }
                    }
                };
            }
        }

        /// <param name="MousePosition">The mouse position in screen space</param>
        /// <param name="CellIndex">The row/column index of the hovered color</param>
        /// <param name="LinearIndex">The index of the hovered color if counting left-to-right, top-to-bottom, starting from the top-left corner</param>
        public bool TryGetHoveredColorIndex(Point MousePosition, out GridCellIndex? CellIndex, out int? LinearIndex)
        {
            Point LayoutPosition = ConvertCoordinateSpace(CoordinateSpace.Screen, CoordinateSpace.Layout, MousePosition);
            Rectangle PaddedBounds = LayoutBounds.GetCompressed(BorderThickness).GetCompressed(Padding);
            if (!PaddedBounds.Contains(LayoutPosition))
            {
                CellIndex = null;
                LinearIndex = null;
                return false;
            }

            int RelativeX = LayoutPosition.X - PaddedBounds.Left;
            int SpacedWidth = ColorSize.Width + ColumnSpacing;
            int Column = RelativeX / SpacedWidth;
            if (RelativeX % SpacedWidth >= ColorSize.Width) // Check if the position is between 2 columns
            {
                CellIndex = null;
                LinearIndex = null;
                return false;
            }

            int RelativeY = LayoutPosition.Y - PaddedBounds.Top;
            int SpacedHeight = ColorSize.Height + RowSpacing;
            int Row = RelativeY / SpacedHeight;
            if (RelativeY % SpacedHeight >= ColorSize.Height) // Check if the position is between 2 rows
            {
                CellIndex = null;
                LinearIndex = null;
                return false;
            }

            CellIndex = new GridCellIndex(Row, Column);
            LinearIndex = Row * Columns + Column;

            if (LinearIndex >= (Colors?.Count ?? 0))
            {
                CellIndex = null;
                LinearIndex = null;
                return false;
            }

            return true;
        }

        public override Thickness MeasureSelfOverride(Size AvailableSize, out Thickness SharedSize)
        {
            SharedSize = new(0);
            int Width = ColorSize.Width * Columns + ColumnSpacing * (Columns - 1);
            int Height = ColorSize.Height * Rows + RowSpacing * (Rows - 1);
            return new(Width, Height, 0, 0);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            base.DrawSelf(DA, LayoutBounds);

            Rectangle PaddedBounds = LayoutBounds.GetCompressed(BorderThickness).GetCompressed(Padding);

            int StartX = PaddedBounds.Left;
            int StartY = PaddedBounds.Top;
            int Row = 0;
            int Column = 0;
            foreach (Color Color in Colors)
            {
                int X = StartX + Column * ColorSize.Width + Column * ColumnSpacing;
                int Y = StartY + Row * ColorSize.Height + Row * RowSpacing;
                Rectangle Bounds = new(X, Y, ColorSize.Width, ColorSize.Height);
                DA.DT.FillRectangle(DA.Offset.ToVector2(), Bounds, Color);

                int Index = Row * Columns + Column;
                if (IsIndexSelected(Index))
                {
                    SelectedColorOverlay?.Draw(DA, this, Bounds);
                    SelectedColorBorderBrush?.Draw(DA, this, Bounds, SelectedColorBorderThickness);
                }
                else
                    UnselectedColorBorderBrush?.Draw(DA, this, Bounds, UnselectedColorBorderThickness);

                if (HoveredColorIndex.HasValue && HoveredColorIndex.Value == Index)
                    HoveredColorOverlay?.Draw(DA, this, Bounds);

                Column++;
                if (Column >= Columns)
                {
                    Column = 0;
                    Row++;
                }
            }
        }

        public bool IsIndexSelected(int Index) => SelectedColorIndexes.Contains(Index);
        public bool IsColorSelected(Color c) => TryGetColorIndex(c, out int Index) && IsIndexSelected(Index);
        public bool TryGetColorIndex(Color c, out int Index) => TryGetIndexOf(Colors, c, out Index);

        private static bool TryGetIndexOf<T>(IReadOnlyList<T> self, T elementToFind, out int index)
        {
            int i = 0;
            foreach (T element in self)
            {
                if (Equals(element, elementToFind))
                {
                    index = i;
                    return true;
                }
                i++;
            }
            index = -1;
            return false;
        }
    }
}