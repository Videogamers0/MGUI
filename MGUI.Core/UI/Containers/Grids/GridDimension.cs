using MGUI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Core.UI.Containers.Grids
{
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
        }

        public int Left { get; protected internal set; }
        public int Width { get; protected internal set; }

        public ColumnDefinition(MGGrid Grid, GridLength Length, int? MinWidth = null, int? MaxWidth = null)
            : base(Grid, Length)
        {
            SetSizeConstraints(MinWidth, MaxWidth);
        }

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
        }

        public int Top { get; protected internal set; }
        public int Height { get; protected internal set; }

        public RowDefinition(MGGrid Grid, GridLength Length, int? MinHeight = null, int? MaxHeight = null)
            : base(Grid, Length)
        {
            SetSizeConstraints(MinHeight, MaxHeight);
        }

        public override string ToString() => $"{nameof(RowDefinition)}-{Index}({Length})";
    }
}
