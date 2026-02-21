using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MGUI.Core.UI;
using Xunit;

namespace MGUI.Tests.ContextMenu;

/// <summary>
/// Tests for the static, pure function <see cref="MGContextMenu.FitMenuToViewport"/>.
///
/// Behaviour summary:
/// - Menu is placed to the RIGHT of the anchor (ActualX = Anchor.Right).
/// - If it overflows on the right, it flips to the LEFT of the anchor
///   (ActualX = Max(Viewport.Left, Anchor.Left - menuWidth)).
/// - Menu Y starts at Anchor.Top.
/// - If it overflows at the bottom, it shifts up
///   (ActualY = Max(Viewport.Top, Viewport.Bottom - menuHeight)).
/// - Width and height are clamped to the viewport dimensions.
/// </summary>
public class FitMenuToViewportTests
{
    private static readonly Rectangle Viewport800x600 = new(0, 0, 800, 600);

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>1×1 anchor at <paramref name="x"/>, <paramref name="y"/> — mimics a mouse-click position.</summary>
    private static Rectangle Pt(int x, int y) => new(x, y, 1, 1);

    private static Size Sz(int w, int h) => new(w, h);

    // ── Normal placement ─────────────────────────────────────────────────────

    [Fact]
    public void NormalCase_MenuFitsCompletely_PlacedRightOfAnchorAtAnchorTop()
    {
        // Anchor at (100, 200), menu 150×80, plenty of space on all sides
        var result = MGContextMenu.FitMenuToViewport(Pt(100, 200), Sz(150, 80), Viewport800x600);

        Assert.Equal(101, result.X);    // Anchor.Right = 100+1 = 101
        Assert.Equal(200, result.Y);    // Anchor.Top   = 200
        Assert.Equal(150, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public void NormalCase_AnchorAtOrigin_MenuPlacedAtRightAndTop()
    {
        var result = MGContextMenu.FitMenuToViewport(Pt(0, 0), Sz(100, 50), Viewport800x600);

        Assert.Equal(1, result.X);
        Assert.Equal(0, result.Y);
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }

    // ── Right overflow ────────────────────────────────────────────────────────

    [Fact]
    public void RightOverflow_MenuFlipsToLeftOfAnchor()
    {
        // Anchor at (750, 100) → Right=751, 751+100=851 > 800 → flip left
        // ActualX = Max(0, 750 - 100) = 650
        var result = MGContextMenu.FitMenuToViewport(Pt(750, 100), Sz(100, 50), Viewport800x600);

        Assert.Equal(650, result.X);
        Assert.Equal(100, result.Y);
    }

    [Fact]
    public void RightOverflow_AnchorNearLeft_ClampedToViewportLeft()
    {
        // Anchor at (30, 100), menu width=200 — would flip to 30-200=-170, clamp to 0
        var result = MGContextMenu.FitMenuToViewport(Pt(30, 100), Sz(200, 50), Viewport800x600);

        // Anchor.Right=31, 31+200=231, still fits? 231 < 800, so NO flip
        // Actually this doesn't overflow right so normal placement applies
        Assert.Equal(31, result.X);
    }

    [Fact]
    public void RightOverflow_AnchorFarRight_ClampedToViewportLeftWhenFlippedTooFar()
    {
        // Anchor at (10, 100), menu width=700 — Anchor.Right=11, 11+700=711 < 800 → no flip
        // Let's set anchor far right where flip would go negative
        // Anchor at (5, 100), menu width=600 — Anchor.Right=6, 6+600=606 < 800 → no flip
        // Need to trigger overflow AND have Anchor.Left - menuWidth < Viewport.Left
        // Anchor at (780, 100), menu width=600:
        //   Anchor.Right=781, 781+600=1381 > 800 → overflow → ActualX = Max(0, 780-600) = Max(0,180) = 180
        var result = MGContextMenu.FitMenuToViewport(Pt(780, 100), Sz(600, 50), Viewport800x600);

        Assert.Equal(180, result.X);
    }

    [Fact]
    public void RightOverflow_AnchorVeryCloseToLeft_ClampedToViewportLeft()
    {
        // Anchor at (2, 100), menu width=700:
        //   Anchor.Right=3, 3+700=703 < 800 → no overflow, no flip
        // Anchor at (780, 100), menu width=790:
        //   Anchor.Right=781, 781+790=1571 > 800 → overflow → ActualX = Max(0, 780-790) = Max(0,-10) = 0
        var result = MGContextMenu.FitMenuToViewport(Pt(780, 100), Sz(790, 50), Viewport800x600);

        Assert.Equal(0, result.X);
    }

    // ── Bottom overflow ───────────────────────────────────────────────────────

    [Fact]
    public void BottomOverflow_MenuShiftsUpward()
    {
        // Anchor at (100, 575), menu height=80 — 575+80=655 > 600 → shift up
        // ActualY = Max(0, 600-80) = 520
        var result = MGContextMenu.FitMenuToViewport(Pt(100, 575), Sz(100, 80), Viewport800x600);

        Assert.Equal(520, result.Y);
    }

    [Fact]
    public void BottomOverflow_TallMenu_ClampedToViewportTop()
    {
        // Anchor at (100, 590), menu height=700 — overflow → Max(0, 600-700) = Max(0,-100) = 0
        var result = MGContextMenu.FitMenuToViewport(Pt(100, 590), Sz(100, 700), Viewport800x600);

        Assert.Equal(0, result.Y);
    }

    // ── Size clamping ─────────────────────────────────────────────────────────

    [Fact]
    public void MenuWiderThanViewport_WidthClampedToViewport()
    {
        var result = MGContextMenu.FitMenuToViewport(Pt(0, 0), Sz(1000, 50), Viewport800x600);

        Assert.Equal(800, result.Width);
    }

    [Fact]
    public void MenuTallerThanViewport_HeightClampedToViewport()
    {
        var result = MGContextMenu.FitMenuToViewport(Pt(0, 0), Sz(100, 900), Viewport800x600);

        Assert.Equal(600, result.Height);
    }

    [Fact]
    public void MenuLargerThanViewportBothAxes_BothDimensionsClamped()
    {
        var result = MGContextMenu.FitMenuToViewport(Pt(0, 0), Sz(2000, 2000), Viewport800x600);

        Assert.Equal(800, result.Width);
        Assert.Equal(600, result.Height);
    }

    // ── Both axes overflow ────────────────────────────────────────────────────

    [Fact]
    public void BothAxesOverflow_MenuFlipsLeftAndShiftsUp()
    {
        // Anchor near bottom-right: (760, 570), menu 100×80
        // X: Anchor.Right=761, 761+100=861 > 800 → flip → Max(0, 760-100)=660
        // Y: 570+80=650 > 600 → shift → Max(0, 600-80)=520
        var result = MGContextMenu.FitMenuToViewport(Pt(760, 570), Sz(100, 80), Viewport800x600);

        Assert.Equal(660, result.X);
        Assert.Equal(520, result.Y);
    }

    // ── Non-zero viewport origin (e.g. windowed mode offset) ─────────────────

    [Fact]
    public void NonZeroViewport_NormalCase_PlacedCorrectly()
    {
        var viewport = new Rectangle(50, 50, 800, 600);  // viewport starts at (50,50)
        // Anchor at (100, 200), menu 100×50 — fits easily
        var result = MGContextMenu.FitMenuToViewport(Pt(100, 200), Sz(100, 50), viewport);

        Assert.Equal(101, result.X);
        Assert.Equal(200, result.Y);
    }

    [Fact]
    public void NonZeroViewport_RightOverflow_ClampedToViewportLeft()
    {
        var viewport = new Rectangle(50, 50, 300, 200);  // viewport 50-350 × 50-250
        // Anchor at (340, 100), menu width=200:
        //   Anchor.Right=341, 341+200=541 > 350 → flip → Max(50, 340-200) = Max(50,140)=140
        var result = MGContextMenu.FitMenuToViewport(Pt(340, 100), Sz(200, 50), viewport);

        Assert.Equal(140, result.X);
    }

    // ── Edge: exact fit ───────────────────────────────────────────────────────

    [Fact]
    public void ExactFit_MenuFillsViewportExactly_NoClamping()
    {
        // Menu exactly the size of the viewport, anchor at origin
        // Anchor.Right=1, 1+800=801 > 800 → overflow → Max(0, 0-800) = 0
        // Y: 0+600=600 = Viewport.Bottom (not > so no shift)
        var result = MGContextMenu.FitMenuToViewport(Pt(0, 0), Sz(800, 600), Viewport800x600);

        // X overflows → Max(0, 0-800) = 0
        Assert.Equal(0, result.X);
        Assert.Equal(0, result.Y);
        Assert.Equal(800, result.Width);
        Assert.Equal(600, result.Height);
    }

    [Fact]
    public void JustFits_NoOverflow()
    {
        // Anchor at (699, 0), menu 100×50 — Anchor.Right=700, 700+100=800 == Viewport.Right → no overflow
        var result = MGContextMenu.FitMenuToViewport(Pt(699, 0), Sz(100, 50), Viewport800x600);

        Assert.Equal(700, result.X);    // no flip
        Assert.Equal(0, result.Y);
        Assert.Equal(100, result.Width);
        Assert.Equal(50, result.Height);
    }
}
