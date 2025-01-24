﻿using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGUI.Shared.Rendering;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>See also:<br/><see cref="MGUniformBorderBrush"/><br/><see cref="MGDockedBorderBrush"/><br/><see cref="MGBandedBorderBrush"/><br/><see cref="MGTexturedBorderBrush"/><br/><see cref="MGHighlightBorderBrush"/><br/><see cref="MGCompositedFillBrush"/></summary>
    public interface IBorderBrush : ICloneable
    {
        public void Update(UpdateBaseArgs UA) { }
        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT);

        public IBorderBrush Copy();
        object ICloneable.Clone() => Copy();
    }
}
