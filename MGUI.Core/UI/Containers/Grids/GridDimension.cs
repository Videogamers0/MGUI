using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Containers.Grids
{
    /// <summary>A <see cref="GridLength"/> that is also associated with a minimum and maximum size.</summary>
    /// <param name="MinSize">Represents either <see cref="ColumnDefinition.MinWidth"/> or <see cref="RowDefinition.MinHeight"/></param>
    /// <param name="MaxSize">Represents either <see cref="ColumnDefinition.MaxWidth"/> or <see cref="RowDefinition.MaxHeight"/></param>
    public readonly record struct ConstrainedGridLength(GridLength Length, int? MinSize, int? MaxSize)
    {
        private static readonly string SizeConstraintsPattern = @"(\[(?<MinSize>\d*),(?<MaxSize>\d*)\])?";
        private static readonly Regex UnanchoredParser = new($@"((?<Length>{GridLength.UnanchoredParser}){SizeConstraintsPattern})");
        private static readonly Regex AnchoredParser = new($@"^(?<Length>{GridLength.UnanchoredParser}){SizeConstraintsPattern}$");

        public static ConstrainedGridLength Parse(string Value)
        {
            Match Match = AnchoredParser.Match(Value);
            GridLength Length = GridLength.Parse(Match.Groups["Length"].Value);
            string MinSizeString = Match.Groups["MinSize"].Value;
            int? MinSize = MinSizeString == "" ? null : int.Parse(MinSizeString);
            string MaxSizeString = Match.Groups["MaxSize"].Value;
            int? MaxSize = MaxSizeString == "" ? null : int.Parse(MaxSizeString);
            return new(Length, MinSize, MaxSize);
        }

        /// <param name="CommaSeparatedValues">A comma-separated list of <see cref="GridLength"/>s with optional size constraints at the end of each one.<para/>
        /// Example:
        /// <code>1.5*[,100],Auto[50,200],250</code><para/>
        /// Parses to 3 results:<br/>
        /// 1.5*[,100] = Weighted <see cref="GridLength"/>: Weight=1.5. MinSize=null, MaxSize=100<br/>
        /// Auto[50,200] = <see cref="GridLength.Auto"/>: MinSize=50, MaxSize=200<br/>
        /// 250 = Pixel <see cref="GridLength"/>: Pixels=250, MinSize=null, MaxSize=null</param>
        public static IEnumerable<ConstrainedGridLength> ParseMultiple(string CommaSeparatedValues)
        {
            if (!string.IsNullOrEmpty(CommaSeparatedValues))
            {
                MatchCollection Matches = UnanchoredParser.Matches(CommaSeparatedValues);
                foreach (Match Match in Matches.Cast<Match>())
                    yield return Parse(Match.Value);
            }
        }
    }

    /// <summary>Base class for <see cref="RowDefinition"/> and <see cref="ColumnDefinition"/></summary>
    public abstract class GridDimensionDefinition : ViewModelBase
    {
        public MGGrid Grid { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private GridLength _Length;
        public GridLength Length
        {
            get => _Length;
            set
            {
                if (_Length != value)
                {
                    GridLength Previous = Length;
                    _Length = value;
                    NPC(nameof(Length));
                    LengthChanged?.Invoke(this, new(Previous, Length));
                    InvokeDimensionsChanged();
                }
            }
        }

        public event EventHandler<EventArgs<GridLength>> LengthChanged;

        protected void InvokeDimensionsChanged() => DimensionsChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>Invoked when any property changes that could affect the dimensions measurement/layout.</summary>
        public event EventHandler<EventArgs> DimensionsChanged;

        public abstract int Index { get; }

        protected GridDimensionDefinition(MGGrid Grid, GridLength Length)
        {
            this.Grid = Grid;
            this.Length = Length;
        }
    }

    public class ColumnDefinition : GridDimensionDefinition
    {
        public override int Index => Grid.GetColumnIndex(this);

        public int? MinWidth { get; private set; }
        public int? MaxWidth { get; private set; }

        public void SetSizeConstraints(int? MinWidth, int? MaxWidth)
        {
            if (MinWidth.HasValue && MaxWidth.HasValue && MinWidth.Value > MaxWidth.Value)
                throw new ArgumentException($"{nameof(MinWidth)} cannot be greater than {nameof(MaxWidth)}");

            this.MinWidth = MinWidth;
            this.MaxWidth = MaxWidth;

            InvokeDimensionsChanged();

            NPC(nameof(MinWidth));
            NPC(nameof(MaxWidth));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Left;
        public int Left
        {
            get => _Left;
            protected internal set
            {
                if (_Left != value)
                {
                    _Left = value;
                    NPC(nameof(Left));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Width;
        public int Width
        {
            get => _Width;
            protected internal set
            {
                if (_Width != value)
                {
                    _Width = value;
                    NPC(nameof(Width));
                }
            }
        }

        public ColumnDefinition(MGGrid Grid, GridLength Length, int? MinWidth = null, int? MaxWidth = null)
            : base(Grid, Length)
        {
            SetSizeConstraints(MinWidth, MaxWidth);
        }


        public ColumnDefinition GetCopy() => new(Grid, Length, MinWidth, MaxWidth) { Left = this.Left, Width = this.Width };

        public override string ToString() => $"{nameof(ColumnDefinition)}-{Index}({Length})";
    }

    public class RowDefinition : GridDimensionDefinition
    {
        public override int Index => Grid.GetRowIndex(this);

        public int? MinHeight { get; private set; }
        public int? MaxHeight { get; private set; }

        public void SetSizeConstraints(int? MinHeight, int? MaxHeight)
        {
            if (MinHeight.HasValue && MaxHeight.HasValue && MinHeight.Value > MaxHeight.Value)
                throw new ArgumentException($"{nameof(MinHeight)} cannot be greater than {nameof(MaxHeight)}");

            this.MinHeight = MinHeight;
            this.MaxHeight = MaxHeight;

            InvokeDimensionsChanged();

            NPC(nameof(MinHeight));
            NPC(nameof(MaxHeight));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Top;
        public int Top
        {
            get => _Top;
            protected internal set
            {
                if (_Top != value)
                {
                    _Top = value;
                    NPC(nameof(Top));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _Height;
        public int Height
        {
            get => _Height;
            protected internal set
            {
                if (_Height != value)
                {
                    _Height = value;
                    NPC(nameof(Height));
                }
            }
        }

        public RowDefinition(MGGrid Grid, GridLength Length, int? MinHeight = null, int? MaxHeight = null)
            : base(Grid, Length)
        {
            SetSizeConstraints(MinHeight, MaxHeight);
        }

        public RowDefinition GetCopy() => new(Grid, Length, MinHeight, MaxHeight) { Top = this.Top, Height = this.Height };

        public override string ToString() => $"{nameof(RowDefinition)}-{Index}({Length})";
    }
}
