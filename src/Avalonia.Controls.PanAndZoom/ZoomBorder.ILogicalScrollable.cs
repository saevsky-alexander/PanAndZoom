using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder : ILogicalScrollable
{
    private Size _extent;
    private Size _viewport;
    private Vector _offset;
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private EventHandler? _scrollInvalidated;

    /// <summary>
    /// Calculate scrollable properties.
    /// </summary>
    /// <param name="source">The source bounds.</param>
    /// <param name="matrix">The transform matrix.</param>
    /// <param name="extent">The extent of the scrollable content.</param>
    /// <param name="viewport">The size of the viewport.</param>
    /// <param name="offset">The current scroll offset.</param>
    public static void CalculateScrollable(Rect source, Matrix matrix, out Size extent, out Size viewport, out Vector offset)
    {
        var bounds = new Rect(0, 0, source.Width, source.Height);
            
        viewport = bounds.Size;

        var transformed = bounds.TransformToAABB(matrix);

        Log($"[CalculateScrollable] source: {source}, bounds: {bounds}, transformed: {transformed}");

        var width = transformed.Size.Width;
        var height = transformed.Size.Height;

        if (width < viewport.Width)
        {
            width = viewport.Width;

            if (transformed.Position.X < 0.0)
            {
                width += Abs(transformed.Position.X);
            }
            else
            {
                var widthTranslated = transformed.Size.Width + transformed.Position.X;
                if (widthTranslated > width)
                {
                    width += widthTranslated - width;
                }
            }
        }
        else if (!(width > viewport.Width))
        {
            width += Abs(transformed.Position.X);
        }
            
        if (height < viewport.Height)
        {
            height = viewport.Height;
                
            if (transformed.Position.Y < 0.0)
            {
                height += Abs(transformed.Position.Y);
            }
            else
            {
                var heightTranslated = transformed.Size.Height + transformed.Position.Y;
                if (heightTranslated > height)
                {
                    height += heightTranslated - height;
                }
            }
        }
        else if (!(height > viewport.Height))
        {
            height += Abs(transformed.Position.Y);
        }

        extent = new Size(width, height);

        var ox = transformed.Position.X;
        var oy = transformed.Position.Y;

        var offsetX = ox < 0 ? Abs(ox) : 0;
        var offsetY = oy < 0 ? Abs(oy) : 0;

        offset = new Vector(offsetX, offsetY);

        Log($"[CalculateScrollable] Extent: {extent} | Offset: {offset} | Viewport: {viewport}");
    }

    /// <inheritdoc/>
    Size IScrollable.Extent => _extent;

    /// <inheritdoc/>
    Vector IScrollable.Offset
    {
        get => _offset;
        set
        {
            Log($"[Offset] offset value: {value}");
            if (_updating)
            {
                return;
            }
            _updating = true;

            var (x, y) = _offset;
            var dx = x - value.X;
            var dy = y - value.Y;

            _offset = value;

            Log($"[Offset] offset: {_offset}, dx: {dx}, dy: {dy}");

            _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, _matrix.M31 + dx, _matrix.M32 + dy);
            Invalidate(!this.IsPointerOver);

            _updating = false;
        }
    }

    /// <inheritdoc/>
    Size IScrollable.Viewport => _viewport;

    bool ILogicalScrollable.CanHorizontallyScroll
    {
        get => _canHorizontallyScroll;
        set
        {
            _canHorizontallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.CanVerticallyScroll
    {
        get => _canVerticallyScroll;
        set
        {
            _canVerticallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => _scrollInvalidated += value;
        remove => _scrollInvalidated -= value;
    }

    Size ILogicalScrollable.ScrollSize => new Size(1, 1);

    Size ILogicalScrollable.PageScrollSize => new Size(10, 10);

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
    {
        // Get offset in original coord (X, Y)
        // Get offset in scaled coord (zX, zY)
        // Get rect in Zcoord = (zX, zY, targetRect.X*scale1, targetRect.Y*scale1);
        // If Zcoord in VP return
        // If  Zcoord.Width <= VP.Widch
        //   & Zcoord.Heigth <= VP.Height
        // Calc the invisible x-proj = Zcoord.X-Segment \ VP.X-Segment
        //                    y-proj = Zcoord.Y-Segment \ VP.Y-Segment
        // {
        //   If y-proj is above VP
        //    shift.Y = - |y-proj|
        // else shift.Y = |y-proj|
        // If x-proj is left to VP
        //    shift.X = - |x-proj|
        // else shift.X = |x-proj|
        // }
        // Else scale = Min(VP.Widch / Zcoord.Width, VP.Heigth / Zcoord.Heigth)
        // {
        //    ZcoordD = (zX * scale, zY*scale,  targetRect.X*scale1*scale, targetRect.Y*scale1*scale)
        //    VPD = (vX*scale, vY*scale, vWidth, vHeight)
        //    calc shift.X = vX(scale - 1)+scaleD
        // }
        // ********************************
        // *                              *
        // *               V+             *
        // *               ---------------+-----
        // ****************|*******Z+******    |
        //                 ---------------------
        // 
        if (_element == null)
        {
            return false;
        }
        if (_viewport.Width < 40 || _viewport.Height < 40)
            return false;
        /* var offsetX = target.IsSet(Canvas.LeftProperty)?
              Canvas.GetLeft(target) : 0.0;
        var offsetY = target.IsSet(Canvas.TopProperty)?
              Canvas.GetTop(target) : 0.0;
        var mx = MatrixHelper.Translate(offsetX, offsetY);*/
        var mx = target.TransformToVisual(this);
        if (!mx.HasValue)
            return false;
        var zCoord = targetRect.TransformToAABB(mx.Value).Inflate(10);
        /* var zCoord = new Rect(
              MatrixHelper.TransformPoint(_matrix, targetX.TopLeft),
              new Size(targetX.Width*_zoomX, targetX.Height*_zoomY)); */
        var VP = new Rect(new Point(), _viewport);
        if (VP.Contains(zCoord))
            return true;
        if (zCoord.Width <= _viewport.Width && zCoord.Height <= _viewport.Height)
        {
            // No zoom-out is required
            var shift = GetShift(zCoord, VP);
            _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, OffsetX - shift.X, OffsetY - shift.Y);
        }
        else 
        {
            double scale = Min(VP.Width / zCoord.Width, VP.Height / zCoord.Height);
            Matrix matrixD = new Matrix(_zoomX*scale, 0, 0, _zoomY*scale, _matrix.M31, _matrix.M32);
            Rect zCoordD = new Rect(zCoord.X*scale, zCoord.Y*scale, zCoord.Width*scale, zCoord.Height*scale);
            var shift = GetShift(zCoordD, VP);
            _matrix =  MatrixHelper.ScaleAndTranslate(_zoomX*scale, _zoomY*scale, OffsetX + shift.X, OffsetY + shift.Y);
        }
        Invalidate(false);
        return true;
    }
    public static Vector GetShift(Rect zCoord, Rect VP)
    {
        var xProj = zCoord.XProj().SubTract(VP.XProj());
        var yProj = zCoord.YProj().SubTract(VP.YProj());
        var shiftX = !xProj.HasValue? 0
          :  xProj.Value.From < VP.X? xProj.Value.From - VP.X
          :  xProj.Value.To - VP.BottomRight.X;
        var shiftY = !yProj.HasValue? 0
          : yProj.Value.From < VP.Y? yProj.Value.From - VP.Y
          : yProj.Value.To - VP.BottomRight.Y;
        return new Vector (shiftX, shiftY);
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        _scrollInvalidated?.Invoke(this, e);
    }

    private void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        if (_element == null)
        {
            return;
        }

        CalculateScrollable(_element.Bounds, _matrix, out var extent, out var viewport, out var offset);

        Log($"[InvalidateScrollable] _element.Bounds: {_element.Bounds}, _matrix: {_matrix}");
        Log($"[InvalidateScrollable] _extent: {_extent}, extent: {extent}, diff: {extent - _extent}");
        Log($"[InvalidateScrollable] _offset: {_offset}, offset: {offset}, diff: {offset - _offset}");
        Log($"[InvalidateScrollable] _viewport: {_viewport}, viewport: {viewport}, diff: {viewport - _viewport}");

        _extent = extent;
        _offset = offset;
        _viewport = viewport;

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
    }
}
