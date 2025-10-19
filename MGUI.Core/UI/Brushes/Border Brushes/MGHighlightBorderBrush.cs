using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using MGUI.Shared.Rendering;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
	public enum HighlightAnimation : byte
	{
		/// <summary>The highlight will appear, fade out, wait a moment, then repeat.<para/>
		/// See also: <see cref="MGHighlightBorderBrush.PulseFadeDuration"/>, <see cref="MGHighlightBorderBrush.PulseDelay"/></summary>
		Pulse,
		/// <summary>The highlight will continually swap between and on and off state to create a flashing animation</summary>
		Flash,
		/// <summary>The highlight will cover a small slice of the border, and travel around the border similar to a circular progress bar's animation</summary>
		Progress,
        /// <summary>The highlight will move either vertically or horizontally across the bounds of the border, looping around when reaching one end</summary>
        Scan
    }

	public enum HighlightFlowDirection : byte
	{
		Clockwise,
		CounterClockwise
	}

    /// <summary>An <see cref="IBorderBrush"/> that draws a simple animation (using a <see cref="HighlightColor"/>) overtop of the border, typically to direct the user's attention
    /// to the bordered element. <see cref="MGHighlightBorderBrush"/>es are particularly useful for tutorials or directing the user's focus to a newly-unlocked piece of content on the UI.<para/>
    /// See also: <see cref="MGUniformBorderBrush"/>, <see cref="MGDockedBorderBrush"/>, <see cref="MGTexturedBorderBrush"/>, <see cref="MGBandedBorderBrush"/>, <see cref="MGCompositedBorderBrush"/></summary>
    public class MGHighlightBorderBrush : ViewModelBase, IBorderBrush
    {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private IBorderBrush _Underlay;
		/// <summary>The underlying border which the highlight will be rendered overtop of.</summary>
		public IBorderBrush Underlay
		{
			get => _Underlay;
			set
			{
				if (_Underlay != value)
				{
					_Underlay = value;
					NPC(nameof(Underlay));
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Color _HighlightColor;
		/// <summary>The color to use when drawing the highlight overtop of the <see cref="Underlay"/></summary>
		public Color HighlightColor
		{
			get => _HighlightColor;
			set
			{
				if (_HighlightColor != value)
				{
					_HighlightColor = value;
					NPC(nameof(HighlightColor));
					HighlightFillBrush = HighlightColor.AsFillBrush();
					HighlightBorderBrush = new MGUniformBorderBrush(HighlightFillBrush);
				}
			}
		}

		private MGUniformBorderBrush HighlightBorderBrush = MGUniformBorderBrush.Transparent;
		private MGSolidFillBrush HighlightFillBrush = MGSolidFillBrush.Transparent;

        #region Animation Settings
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private HighlightAnimation _AnimationType;
		/// <summary>The animation to use when drawing the highlight overtop of the <see cref="Underlay"/>.<para/>
		/// For further control over the animation:<br/>
		/// If set to <see cref="HighlightAnimation.Pulse"/>: Set <see cref="PulseFadeDuration"/> and <see cref="PulseDelay"/><br/>
		/// If set to <see cref="HighlightAnimation.Flash"/>: Set <see cref="FlashShowDuration"/> and <see cref="FlashHideDuration"/><br/>
		/// If set to <see cref="HighlightAnimation.Progress"/>: Set <see cref="ProgressFlowDirection"/>, <see cref="ProgressDuration"/>, <see cref="ProgressSize"/><br/>
		/// If set to <see cref="HighlightAnimation.Scan"/>: Set <see cref="ScanOrientation"/>, <see cref="ScanIsReversed"/>, <see cref="ScanDuration"/>, <see cref="ScanSize"/><para/>
		/// See also: <see cref="AnimationProgress"/></summary>
		public HighlightAnimation AnimationType
		{
			get => _AnimationType;
			set
			{
				if (_AnimationType != value)
				{
					_AnimationType = value;
					NPC(nameof(AnimationType));
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private double _AnimationProgress;
        /// <summary>Determines what percentage of the current animation cycle is complete. Min = 0.0 (0%), Max = 1.0 (100%)<para/>
        /// For example, if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Flash"/>, one cycle represents showing the highlight for a set amount of time, then hiding for a set amount of time.<para/>
		/// Note: For <see cref="HighlightAnimation.Progress"/>, this progress value represents where the CENTER of the highlight should appear on the perimeter. 0.0 = topleft, 0.50 = bottomright etc.</summary>
		/// <remarks>If this value is not in the range [0.0,1.0], only the decimal portion is used. EX: 2.35 is equivalent to 0.35.<br/>
		/// See also: <see cref="ActualAnimationProgress"/></remarks>
        public double AnimationProgress
		{
			get => _AnimationProgress;
			set
			{
				if (_AnimationProgress != value)
				{
					_AnimationProgress = value;
					NPC(nameof(AnimationProgress));
					NPC(nameof(ActualAnimationProgress));

					if (AnimationProgress < 0.0)
						throw new InvalidOperationException($"{nameof(MGBandedBorderBrush)}.{nameof(AnimationProgress)} cannot be negative. Value: {AnimationProgress}");
				}
			}
		}

		/// <summary>Same value as <see cref="AnimationProgress"/>, except this is converted to the range [0.0, 1.0]</summary>
		public double ActualAnimationProgress => 
			AnimationProgress == (int)AnimationProgress && AnimationProgress != 0.0 ? 1.0 : // Whole numbers above zero should be treated as 100% completion instead of 0%
			AnimationProgress - Math.Truncate(AnimationProgress);

		#region Pulse Settings
		/// <summary>Default value: 2.0s</summary>
		public static TimeSpan DefaultPulseFadeDuration { get; set; } = TimeSpan.FromSeconds(2.0);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _PulseFadeDuration;
		/// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Pulse"/>.<para/>
		/// The amount of time it will take for the <see cref="HighlightColor"/> to fade out from 100% to 0% opacity.<para/>
		/// Default value: <see cref="DefaultPulseFadeDuration"/><para/>
		/// See also:<br/><see cref="AnimationType"/>,<br/><see cref="PulseDelay"/></summary>
		public TimeSpan PulseFadeDuration
		{
			get => _PulseFadeDuration;
			set
			{
				if (_PulseFadeDuration != value)
				{
					_PulseFadeDuration = value;
					NPC(nameof(PulseFadeDuration));
				}
			}
		}

		/// <summary>Default value: 0.2s</summary>
		public static TimeSpan DefaultPulseDelay { get; set; } = TimeSpan.FromSeconds(0.2);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _PulseDelay;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Pulse"/>.<para/>
		/// The amount of time that the <see cref="HighlightColor"/> remains at 0% opacity before cycling back to 100% opacity.<para/>
		/// Default value: <see cref="DefaultPulseDelay"/><para/>
		/// See also:<br/><see cref="AnimationType"/>,<br/><see cref="PulseFadeDuration"/></summary>
        public TimeSpan PulseDelay
		{
			get => _PulseDelay;
			set
			{
				if (_PulseDelay != value)
				{
					_PulseDelay = value;
					NPC(nameof(PulseDelay));
				}
			}
		}

		private TimeSpan PulseCycleDuration => PulseFadeDuration + PulseDelay;
		#endregion Pulse Settings

		#region Flash Settings
		/// <summary>Default value: 0.4s</summary>
		public static TimeSpan DefaultFlashShowDuration { get; set; } = TimeSpan.FromSeconds(0.4);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _FlashShowDuration;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Flash"/>.<para/>
        /// The amount of time that the <see cref="HighlightColor"/> is visible during each flash cycle.<para/>
		/// Default value: <see cref="DefaultFlashShowDuration"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="FlashHideDuration"/></summary>
        public TimeSpan FlashShowDuration
		{
			get => _FlashShowDuration;
			set
			{
				if (_FlashShowDuration != value)
				{
					_FlashShowDuration = value;
					NPC(nameof(FlashShowDuration));
				}
			}
		}

        /// <summary>Default value: 0.4s</summary>
        public static TimeSpan DefaultFlashHideDuration { get; set; } = TimeSpan.FromSeconds(0.4);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _FlashHideDuration;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Flash"/>.<para/>
        /// The amount of time that the <see cref="HighlightColor"/> is hidden during each flash cycle.<para/>
        /// Default value: <see cref="DefaultFlashHideDuration"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="FlashShowDuration"/></summary>
        public TimeSpan FlashHideDuration
		{
			get => _FlashHideDuration;
			set
			{
				if (_FlashHideDuration != value)
				{
					_FlashHideDuration = value;
					NPC(nameof(FlashHideDuration));
				}
			}
		}

		private TimeSpan FlashCycleDuration => FlashShowDuration + FlashHideDuration;
		#endregion Flash Settings

		#region Progress Settings
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private HighlightFlowDirection _ProgressFlowDirection;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Progress"/>.<para/>
		/// The direction that the animation moves in.<para/>
		/// Default value: <see cref="HighlightFlowDirection.Clockwise"/><para/>
		/// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ProgressDuration"/></summary>
        public HighlightFlowDirection ProgressFlowDirection
		{
			get => _ProgressFlowDirection;
			set
			{
				if (_ProgressFlowDirection != value)
				{
					_ProgressFlowDirection = value;
					NPC(nameof(ProgressFlowDirection));
				}
			}
		}

		/// <summary>Default value: 3s</summary>
		public static TimeSpan DefaultProgressDuration { get; set; } = TimeSpan.FromSeconds(3.0);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _ProgressDuration;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Progress"/>.<para/>
		/// Determines how long it takes for the highlight to make one complete loop around the perimeter.<para/>
		/// Default value: <see cref="DefaultProgressDuration"/><para/>
		/// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ProgressFlowDirection"/>,<br/><see cref="ProgressSize"/></summary>
        public TimeSpan ProgressDuration
		{
			get => _ProgressDuration;
			set
			{
				if (_ProgressDuration != value)
				{
					_ProgressDuration = value;
					NPC(nameof(ProgressDuration));
				}
			}
		}

		/// <summary>Default value: 0.25</summary>
		public static double DefaultProgressSize { get; set; } = 0.25;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private double _ProgressSize;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Progress"/>.<para/>
        /// Determines what percentage of the perimeter is highlighted.<para/>
		/// Default value: <see cref="DefaultProgressSize"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ProgressFlowDirection"/>,<br/><see cref="ProgressDuration"/></summary>
        public double ProgressSize
		{
			get => _ProgressSize;
			set
			{
				if (_ProgressSize != value)
				{
					_ProgressSize = value;
					NPC(nameof(ProgressSize));
				}
			}
		}
		#endregion Progress Settings

		#region Scan Settings
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Orientation _ScanOrientation;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Scan"/>.<para/>
		/// The orientation of the highlight.<br/>
		/// If <see cref="Orientation.Vertical"/>, the highlight will be a vertical line that moves horizontally along the bounds of the border.<br/>
		/// If <see cref="Orientation.Horizontal"/>, the highlight will be a horizontal line that moves vertically along the bounds of the border.<para/>
		/// Default value: <see cref="Orientation.Horizontal"/><para/>
		/// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ScanIsReversed"/>,<br/><see cref="ScanDuration"/>,<br/><see cref="ScanSize"/></summary>
        public Orientation ScanOrientation
		{
			get => _ScanOrientation;
			set
			{
				if (_ScanOrientation != value)
				{
					_ScanOrientation = value;
					NPC(nameof(ScanOrientation));
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private bool _ScanIsReversed;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Scan"/>.<para/>
        /// If <see langword="false"/>, the highlight will move either top-to-bottom or left-to-right, depending on <see cref="ScanOrientation"/>.<br/>
		/// If <see langword="true"/>, the highlight will move either bottom-to-top or right-to-left, depending on <see cref="ScanOrientation"/>.<para/>
		/// Default value: <see langword="false"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ScanOrientation"/>,<br/><see cref="ScanDuration"/>,<br/><see cref="ScanSize"/></summary>
        public bool ScanIsReversed
		{
			get => _ScanIsReversed;
			set
			{
				if (_ScanIsReversed != value)
				{
					_ScanIsReversed = value;
					NPC(nameof(ScanIsReversed));
				}
			}
		}

		/// <summary>Default value: 1.5s</summary>
		public static TimeSpan DefaultScanDuration { get; set; } = TimeSpan.FromSeconds(1.5);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private TimeSpan _ScanDuration;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Scan"/>.<para/>
        /// Determines how long it takes for the highlight to fully move across the bounds of the border.<para/>
		/// Default value: <see cref="DefaultScanDuration"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ScanOrientation"/>,<br/><see cref="ScanIsReversed"/>,<br/><see cref="ScanSize"/></summary>
        public TimeSpan ScanDuration
		{
			get => _ScanDuration;
			set
			{
				if (_ScanDuration != value)
				{
					_ScanDuration = value;
					NPC(nameof(ScanDuration));
				}
			}
		}

		/// <summary>Default value: 0.2</summary>
		public static double DefaultScanSize { get; set; } = 0.2;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private double _ScanSize;
        /// <summary>Only relevant if <see cref="AnimationType"/> is set to <see cref="HighlightAnimation.Scan"/>.<para/>
        /// Determines the percentage of the border that is filled by the highlight. 0.0 = 0%, 1.0 = 100%<br/>
		/// If <see cref="ScanOrientation"/> is <see cref="Orientation.Vertical"/>, this determines the width of the highlight.<br/>
		/// If <see cref="ScanOrientation"/> is <see cref="Orientation.Horizontal"/>, this determines the height of the highlight.<para/>
		/// Default value: <see cref="DefaultScanSize"/><para/>
        /// See also:<br/><see cref="AnimationType"/>,<br/><see cref="ScanOrientation"/>,<br/><see cref="ScanIsReversed"/>,<br/><see cref="ScanDuration"/></summary>
        public double ScanSize
		{
			get => _ScanSize;
			set
			{
				if (_ScanSize != value)
				{
					_ScanSize = value;
					NPC(nameof(ScanSize));
				}
			}
		}
        #endregion Scan Settings
        #endregion Animation Settings

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private bool _IsEnabled;
		/// <summary>If <see langword="true"/>, the <see cref="HighlightColor"/> will be used to draw the highlight overtop of the <see cref="Underlay"/>.</summary>
		public bool IsEnabled
		{
			get => _IsEnabled;
			set
			{
				if (_IsEnabled != value)
				{
					_IsEnabled = value;
					NPC(nameof(IsEnabled));
				}
			}
		}

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private MGElement _Target;
		/// <summary>The <see cref="MGElement"/> that this border brush is being drawn on. This value is optional and only used if <see cref="StopOnMouseOver"/> or <see cref="StopOnClick"/> are set to <see langword="true"/></summary>
		public MGElement Target
		{
			get => _Target;
			set
			{
				if (_Target != value)
				{
					_Target = value;
					NPC(nameof(Target));
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private bool _StopOnMouseOver;
		/// <summary>If <see langword="true"/>, <see cref="IsEnabled"/> will automatically be set to <see langword="false"/> when the <see cref="Target"/> <see cref="MGElement"/> is hovered by the mouse.</summary>
		public bool StopOnMouseOver
		{
			get => _StopOnMouseOver;
			set
			{
				if (_StopOnMouseOver != value)
				{
					_StopOnMouseOver = value;
					NPC(nameof(StopOnMouseOver));
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private bool _StopOnClick;
        /// <summary>If <see langword="true"/>, <see cref="IsEnabled"/> will automatically be set to <see langword="false"/> when the <see cref="Target"/> <see cref="MGElement"/> 
		/// is interacted with by the mouse via a Left mouse button press. (Press only, does not need to be a full press including a mouse button released event)</summary>
        public bool StopOnClick
		{
			get => _StopOnClick;
			set
			{
				if (_StopOnClick != value)
				{
					_StopOnClick = value;
					NPC(nameof(StopOnClick));
				}
			}
		}

        /// <param name="Underlay">The underlying border which the highlight will be rendered overtop of.</param>
		/// <param name="HighlightColor">The color to use when drawing the highlight overtop of the <see cref="Underlay"/></param>
		/// <param name="AnimationType">The animation to use when drawing the highlight overtop of the <see cref="Underlay"/>.</param>
        /// <param name="Target">The <see cref="MGElement"/> that this border brush is being drawn on. This value is optional and only used if <see cref="StopOnMouseOver"/> or <see cref="StopOnClick"/> are set to <see langword="true"/></param>
        public MGHighlightBorderBrush(IBorderBrush Underlay, Color HighlightColor, HighlightAnimation AnimationType, MGElement Target = null)
		{
			this.Underlay = Underlay;
			this.HighlightColor = HighlightColor;
			this.AnimationType = AnimationType;
			AnimationProgress = 0.0;
			this.Target = Target;

			IsEnabled = true;

			PulseFadeDuration = DefaultPulseFadeDuration;
			PulseDelay = DefaultPulseDelay;

			FlashShowDuration = DefaultFlashShowDuration;
			FlashHideDuration = DefaultFlashHideDuration;

			ProgressFlowDirection = HighlightFlowDirection.Clockwise;
			ProgressDuration = DefaultProgressDuration;
			ProgressSize = DefaultProgressSize;

			ScanOrientation = Orientation.Horizontal;
			ScanIsReversed = false;
			ScanDuration = DefaultScanDuration;
			ScanSize = DefaultScanSize;

			StopOnMouseOver = false;
			StopOnClick = false;
		}

        void IBorderBrush.Update(UpdateBaseArgs UA)
		{
			Underlay?.Update(UA);

			if (Target != null && 
				((StopOnMouseOver && Target.VisualState.Secondary == SecondaryVisualState.Hovered) ||
				(StopOnClick && Target.VisualState.Secondary == SecondaryVisualState.Pressed)))
			{
				IsEnabled = false;
			}

			if (IsEnabled)
			{
				TimeSpan CycleDuration = AnimationType switch
				{
					HighlightAnimation.Pulse => PulseCycleDuration,
					HighlightAnimation.Flash => FlashCycleDuration,
					HighlightAnimation.Progress => ProgressDuration,
					HighlightAnimation.Scan => ScanDuration,
					_ => throw new NotImplementedException($"Unrecognized {nameof(HighlightAnimation)}: {AnimationType}")
				};

				double ElapsedPercent = UA.FrameElapsed / CycleDuration;
				AnimationProgress += ElapsedPercent;
			}
        }

		private static int PositiveModulo(int x, int m)
		{
			int r = x % m;
			return r < 0 ? r + m : r;
		}

        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT)
		{
			Underlay?.Draw(DA, Element, Bounds, BT);

			if (!IsEnabled || HighlightColor == Color.Transparent)
				return;

			double Progress = ActualAnimationProgress;
			switch (AnimationType)
			{
				case HighlightAnimation.Pulse:
					double FadePercent = PulseFadeDuration / PulseCycleDuration;
                    if (Progress < FadePercent)
					{
						float OpacityScalar = 1.0f - (float)(Progress / FadePercent);
                        HighlightBorderBrush.Draw(DA.SetOpacity(DA.Opacity * OpacityScalar), Element, Bounds, BT);
                    }
                    break;
				case HighlightAnimation.Flash:
					bool IsVisible = Progress <= FlashShowDuration / FlashCycleDuration;
					if (IsVisible)
						HighlightBorderBrush.Draw(DA, Element, Bounds, BT);
					break;
				case HighlightAnimation.Progress:
					{
                        List<Rectangle> Edges = new List<Rectangle>();
                        if (BT.Left > 0)
                            Edges.Add(new(Bounds.Left, Bounds.Top, BT.Left, Bounds.Height));
                        if (BT.Right > 0)
                            Edges.Add(new(Bounds.Right - BT.Right, Bounds.Top, BT.Right, Bounds.Height));
                        if (BT.Top > 0)
                            Edges.Add(new(Bounds.Left + BT.Left, Bounds.Top, Bounds.Width - BT.Width, BT.Top));
                        if (BT.Bottom > 0)
                            Edges.Add(new(Bounds.Left + BT.Left, Bounds.Bottom - BT.Bottom, Bounds.Width - BT.Width, BT.Bottom));

						int OuterPerimeterLength = Bounds.Width * 2 + (Bounds.Height - 1) * 2;
						int InnerPerimeterLength = OuterPerimeterLength - BT.Width * 2 - BT.Height * 2;
						int AvgPerimeterLength = (OuterPerimeterLength + InnerPerimeterLength) / 2;

						//  Returns a point along the outer perimeter of the Rectangle bounds, where the point represents travelling along the perimeter by the given percent
						//  0.0 = topleft corner, moves clockwise (or counterclockwise if IsReversed=true) along the perimeter
						Point GetPerimeterPosition(double Percent, bool IsReversed, out int EdgeIndex)
						{
                            int LinearPosition = PositiveModulo((int)(Percent * OuterPerimeterLength), OuterPerimeterLength);

							if (!IsReversed)
							{
								// Top edge
								if (LinearPosition <= Bounds.Width)
								{
									EdgeIndex = 0;
									return Bounds.TopLeft() + new Point(LinearPosition, 0);
								}
								// Right edge
								else if (LinearPosition <= Bounds.Width + Bounds.Height - 2)
								{
									EdgeIndex = 1;
									return Bounds.TopRight() + new Point(0, LinearPosition - Bounds.Width);
								}
								// Bottom edge
								else if (LinearPosition <= Bounds.Width + Bounds.Height - 2 + Bounds.Width)
								{
									EdgeIndex = 2;
									return Bounds.BottomRight() - new Point(LinearPosition - Bounds.Width - Bounds.Height + 1, 0);
								}
								// Left edge
								else
								{
									EdgeIndex = 3;
									return Bounds.BottomLeft() - new Point(0, LinearPosition - Bounds.Width - Bounds.Height + 2 - Bounds.Width);
								}
							}
							else
							{
                                // Top edge
                                if (LinearPosition == 0)
                                {
                                    EdgeIndex = 0;
                                    return Bounds.TopLeft();
                                }
								//  Left edge
								else if (LinearPosition <= Bounds.Height - 1)
								{
									EdgeIndex = 3;
									return Bounds.TopLeft() + new Point(0, LinearPosition);
								}
								//  Bottom edge
								else if (LinearPosition <= Bounds.Height - 1 + Bounds.Width)
								{
									EdgeIndex = 2;
									return Bounds.BottomLeft() + new Point(LinearPosition - Bounds.Height - 1, 0);
								}
								//  Right edge
								else if (LinearPosition <= Bounds.Height - 1 + Bounds.Width + Bounds.Height - 2)
								{
									EdgeIndex = 1;
									return Bounds.BottomRight() - new Point(0, LinearPosition - Bounds.Height + 1 - Bounds.Width);
								}
								//  Top edge
								else
								{
									EdgeIndex = 0;
									return Bounds.TopRight() - new Point(LinearPosition - Bounds.Height + 1 - Bounds.Width - Bounds.Height + 1, 0);
								}
                            }
                        }

						Point StartPosition = GetPerimeterPosition(Progress - ProgressSize / 2.0, ProgressFlowDirection == HighlightFlowDirection.CounterClockwise, out int StartEdge);
						Point EndPosition = GetPerimeterPosition(Progress + ProgressSize / 2.0, ProgressFlowDirection == HighlightFlowDirection.CounterClockwise, out int EndEdge);

						//  Create a list of vertices representing the outer bounds of the polygon we want to fill in
						List<Point> OuterVertices = new() { StartPosition };
						if (StartEdge != EndEdge) // If the start and end are on different edges of the bounds, append all the corners that they pass through
						{
							IReadOnlyDictionary<int, Point> OuterCornerLookup = new Dictionary<int, Point>()
							{
								{ 0, Bounds.TopLeft() },
								{ 1, Bounds.TopRight() },
								{ 2, Bounds.BottomRight() },
								{ 3, Bounds.BottomLeft() }
							};

							const int NumEdges = 4;
							int CurrentEdge = StartEdge;
							if (ProgressFlowDirection == HighlightFlowDirection.Clockwise)
							{
								while (CurrentEdge != EndEdge)
								{
									CurrentEdge = (CurrentEdge + 1) % NumEdges;
									OuterVertices.Add(OuterCornerLookup[CurrentEdge]);
								}
							}
							else if (ProgressFlowDirection == HighlightFlowDirection.CounterClockwise)
							{
                                while (CurrentEdge != EndEdge)
                                {
                                    OuterVertices.Add(OuterCornerLookup[CurrentEdge]);
									CurrentEdge = PositiveModulo(CurrentEdge - 1, NumEdges);
                                }
                            }
							else
								throw new NotImplementedException($"Unrecognized {nameof(HighlightFlowDirection)}: {ProgressFlowDirection}");
						}
						OuterVertices.Add(EndPosition);
						OuterVertices = OuterVertices.Distinct().ToList(); // There will be duplicate vertices if a start or end point was on a corner

						//  Key = Bounds of a corner, Value = the inner corner point
						Dictionary<Rectangle, Point> CornerBounds = new Dictionary<Rectangle, Point>()
						{
							{ new Rectangle(Bounds.Left, Bounds.Top, BT.Left, BT.Top), Bounds.TopLeft() + new Point(BT.Left, BT.Top) },
							{ new Rectangle(Bounds.Right - BT.Right, Bounds.Top, BT.Right, BT.Top), Bounds.TopRight() + new Point(-BT.Right, BT.Top) },
							{ new Rectangle(Bounds.Right - BT.Right, Bounds.Bottom - BT.Bottom, BT.Right, BT.Bottom), Bounds.BottomRight() + new Point(-BT.Right, -BT.Bottom) },
							{ new Rectangle(Bounds.Left, Bounds.Bottom - BT.Bottom, BT.Left, BT.Bottom), Bounds.BottomLeft() + new Point(BT.Left, -BT.Bottom) }
						};

                        List<Point> Polygon = new(OuterVertices);
						foreach (Point OuterVertex in OuterVertices.AsEnumerable().Reverse())
						{
							//  Compute the inner vertex that opposes this outer vertex
							Point InnerVertex = Point.Zero;

							//  First check if the vertex is within a corner region and if so use the inner corner vertex
							bool IsCorner = false;
							foreach (var KVP in CornerBounds)
							{
								if (KVP.Key.ContainsInclusive(OuterVertex))
								{
									InnerVertex = KVP.Value;
									IsCorner = true;
									break;
								}
							}

							//  Otherwise just move the vertex inwards by the corresponding edge's BorderThickness
							if (!IsCorner)
							{
								if (OuterVertex.Y == Bounds.Top) // Top Edge
									InnerVertex = OuterVertex + new Point(0, BT.Top);
								else if (OuterVertex.Y == Bounds.Bottom) // Bottom Edge
									InnerVertex = OuterVertex + new Point(0, -BT.Bottom);
								else if (OuterVertex.X == Bounds.Right) // Right Edge
									InnerVertex = OuterVertex + new Point(-BT.Right, 0);
								else if (OuterVertex.X == Bounds.Left) // Left Edge
									InnerVertex = OuterVertex + new Point(BT.Left, 0);
								else
									throw new InvalidOperationException($"Flawed logic in {nameof(MGHighlightBorderBrush)}.{nameof(Draw)}: Vertex was not along the outer edge of the bounds.");
							}

							Polygon.Add(InnerVertex);
                        }
						Polygon = Polygon.Distinct().ToList();

						DA.DT.FillPolygon(DA.Offset.ToVector2(), Polygon.Select(x => x.ToVector2()), HighlightColor);
                    }
                    break;
				case HighlightAnimation.Scan:
					{
						List<Rectangle> Edges = new List<Rectangle>();
						if (BT.Left > 0)
							Edges.Add(new(Bounds.Left, Bounds.Top, BT.Left, Bounds.Height));
						if (BT.Right > 0)
							Edges.Add(new(Bounds.Right - BT.Right, Bounds.Top, BT.Right, Bounds.Height));
						if (BT.Top > 0)
							Edges.Add(new(Bounds.Left + BT.Left, Bounds.Top, Bounds.Width - BT.Width, BT.Top));
						if (BT.Bottom > 0)
							Edges.Add(new(Bounds.Left + BT.Left, Bounds.Bottom - BT.Bottom, Bounds.Width - BT.Width, BT.Bottom));

						if (Edges.Any())
						{
							List<Rectangle> Scanlines = new List<Rectangle>();

							switch (ScanOrientation)
							{
								case Orientation.Horizontal:
									{
										int Left = Bounds.Left;
										int Right = Bounds.Right;

										int Width = Bounds.Width;
										int Height = Math.Max(1, (int)Math.Round(Math.Min(ScanSize, 1.0) * Bounds.Height));

										int Center = !ScanIsReversed ? Bounds.Top + (int)(Progress * Bounds.Height) : Bounds.Bottom - ((int)(Progress * Bounds.Height));
										int Top = Center - Height / 2;
										int Bottom = Center + Height / 2;

										//  Handle cases where the scanline needs to loop around to opposite side
										if (Top < Bounds.Top)
										{
											int Overflow = Bounds.Top - Top;
											Scanlines.Add(new Rectangle(Left, Bounds.Top, Width, Height - Overflow));
											Scanlines.Add(new Rectangle(Left, Bounds.Bottom - Overflow, Width, Overflow));
										}
										else if (Bottom > Bounds.Bottom)
										{
											int Overflow = Bottom - Bounds.Bottom;
											Scanlines.Add(new Rectangle(Left, Bounds.Bottom - Height + Overflow, Width, Height - Overflow));
											Scanlines.Add(new Rectangle(Left, Bounds.Top, Width, Overflow));
										}
										else
											Scanlines.Add(new Rectangle(Left, Top, Width, Height));
									}
									break;
								case Orientation.Vertical:
									{
										int Top = Bounds.Top;
										int Bottom = Bounds.Bottom;

										int Width = Math.Max(1, (int)Math.Round(Math.Min(ScanSize, 1.0) * Bounds.Width));
										int Height = Bounds.Height;

										int Center = !ScanIsReversed ? Bounds.Left + (int)(Progress * Bounds.Width) : Bounds.Right - ((int)(Progress * Bounds.Width));
										int Left = Center - Width / 2;
										int Right = Center + Width / 2;

										//  Handle cases where the scanline needs to loop around to opposite side
										if (Left < Bounds.Left)
										{
											int Overflow = Bounds.Left - Left;
											Scanlines.Add(new Rectangle(Bounds.Left, Top, Width - Overflow, Height));
											Scanlines.Add(new Rectangle(Bounds.Right - Overflow, Top, Overflow, Height));
										}
										else if (Right > Bounds.Right)
										{
											int Overflow = Right - Bounds.Right;
											Scanlines.Add(new Rectangle(Bounds.Right - Width + Overflow, Top, Width - Overflow, Height));
											Scanlines.Add(new Rectangle(Bounds.Left, Top, Overflow, Height));
										}
										else
											Scanlines.Add(new Rectangle(Left, Top, Width, Height));
									}
									break;
								default: throw new NotImplementedException($"Unrecognized {nameof(Orientation)}: {ScanOrientation}");
							}

							foreach (Rectangle Scanline in Scanlines)
							{
								foreach (Rectangle Edge in Edges)
								{
									Rectangle Intersection = Rectangle.Intersect(Scanline, Edge);
									if (!Intersection.IsEmpty)
										HighlightFillBrush.Draw(DA, Element, Intersection);
								}
							}
						}
					}
                    break;
				default: throw new NotImplementedException($"Unrecognized {nameof(HighlightAnimation)}: {AnimationType}");
			}
		}

        public IBorderBrush Copy()
		{
			MGHighlightBorderBrush Copy = new MGHighlightBorderBrush(Underlay, HighlightColor, AnimationType, Target)
			{
				AnimationProgress = AnimationProgress,
				IsEnabled = IsEnabled,
				PulseFadeDuration = PulseFadeDuration,
				PulseDelay = PulseDelay,
				FlashShowDuration = FlashShowDuration,
				FlashHideDuration = FlashHideDuration,
				ProgressFlowDirection = ProgressFlowDirection,
				ProgressDuration = ProgressDuration,
				ProgressSize = ProgressSize,
				ScanOrientation = ScanOrientation,
				ScanIsReversed = ScanIsReversed,
				ScanDuration = ScanDuration,
				ScanSize = ScanSize,
				StopOnMouseOver = StopOnMouseOver,
				StopOnClick = StopOnClick
            };
            return Copy;
		}
    }
}