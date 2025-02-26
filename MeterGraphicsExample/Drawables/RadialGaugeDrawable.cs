﻿
namespace MeterGraphicsExample.Drawables;

public class RadialGaugeDrawable : BaseDrawable, IDrawable
{
    private double _emptyAngle = 0d;
    private double _removeCirclePercentage = 0d; 
    
    public int MaxValue { get; set; }
    public int Steps { get; set; } = 48;
    public float GaugeThickness { get; set; } = 1f;

    public int FillValue { get; set; }

    public int NeedleThickness { get; set; }

    public Color NeedleColor { get; set; }

    public Color MiddleAreaColor { get; set; } = Colors.White;

    public bool GradiantFill { get; set; } = false;


    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.FillColor = Colors.Green;


        float limitingDim = dirtyRect.Width < dirtyRect.Height ? dirtyRect.Width : dirtyRect.Height;

        var circleCenter = new PointF((dirtyRect.Width / 2), (dirtyRect.Height / 2));

        if (GradiantFill)
        {
            LinearGradientPaint lgp = new LinearGradientPaint
            {
                StartColor = Colors.Green,
                EndColor = Colors.Red,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            canvas.SetFillPaint(lgp, dirtyRect);
        }
        else
            canvas.FillColor = Colors.Green;

        DrawNumDisplay(canvas, circleCenter);

        // This is the best way to do remove the bottom side of the gauge
        // BUT unfortunately this isn't implemented yet on Windows
        // So if you try to do ClipPath on windows, it'll crash.
        // Github issue here - https://github.com/dotnet/Microsoft.Maui.Graphics/issues/250
        var path = new PathF();
        
        path.MoveTo(circleCenter.X, circleCenter.Y);
        path.LineTo(dirtyRect.X + 5, dirtyRect.Height);
        path.LineTo(dirtyRect.X, dirtyRect.Y - 10);
        path.LineTo(dirtyRect.Width, dirtyRect.Y - 10);
        path.LineTo(dirtyRect.Width, dirtyRect.Height);
        path.LineTo(circleCenter.X, circleCenter.Y);
        
        canvas.ClipPath(path);

        canvas.FillCircle(circleCenter.X, circleCenter.Y, limitingDim / 2);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(circleCenter.X, circleCenter.Y, limitingDim / 2); 

        canvas.SetFillPaint(new SolidPaint(Colors.White), dirtyRect);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 3;
        canvas.FillCircle(circleCenter.X, circleCenter.Y, limitingDim / (GaugeThickness + 2));
        canvas.DrawCircle(circleCenter.X, circleCenter.Y, limitingDim / (GaugeThickness + 2));

        // Use a Path to cancel out the bottom part of the circle, finishing the gauge

        var top = new Point(dirtyRect.Width / 2, dirtyRect.Height / 2);
        var bottomLeft = new Point(dirtyRect.X, dirtyRect.Height + 3);
        var bottomRight = new Point(dirtyRect.Width, dirtyRect.Height + 3);

        _emptyAngle = GetAngleDegrees(top, bottomLeft, bottomRight);
        _removeCirclePercentage = (180 - (_emptyAngle / 2)) / 360;


        


        if (FillValue > MaxValue)
            FillValue = MaxValue;

        if (FillValue < 0)
            FillValue = 0;

        DrawNeedle(canvas, dirtyRect, FillValue);
        DrawTickMarks(canvas, dirtyRect, Steps);

    }

    private void DrawNeedle(ICanvas canvas, RectF dirtyRect, int fillAmount)
    {


        var circleCenter = new PointF(dirtyRect.Width / 2, dirtyRect.Height / 2);
        

        var limitingDim = dirtyRect.Width < dirtyRect.Height ? dirtyRect.Width : dirtyRect.Height;
        canvas.StrokeSize = 3;

        canvas.SetFillPaint(new SolidPaint(Colors.Black), dirtyRect);
        canvas.FillColor = NeedleColor;
        canvas.FillCircle(dirtyRect.Width / 2, dirtyRect.Height / 2, limitingDim / 30);

        // You wouldn't believe how long it took me to figure this math out lmao

        // This normalizes the fill amount from 0 - MaxValue down to -1 to 1
        var zeroPos = ((fillAmount - 0.0) / ( MaxValue - 0.0) * 2.0) - 1.0;
        var angleDegrees = ((zeroPos * 100) * 360.0) / MaxValue;

        //reduce the angle down from -360 - 360 to our 'non-removed circle' angles
        angleDegrees *= _removeCirclePercentage;

        var angleRadians = (Math.PI / 180.0) * angleDegrees;

        var radius = (limitingDim / (GaugeThickness + 2) * 1.5);
        PointF outerPoint = new((float)(radius * Math.Sin(angleRadians)) + (dirtyRect.Width / 2), (float)(-radius * Math.Cos(angleRadians)) + (dirtyRect.Height / 2)); 
        canvas.DrawLine(circleCenter, outerPoint);
        
    }

    private void DrawTickMarks(ICanvas canvas, RectF dirtyRect, int steps)
    {

        canvas.FontSize = 10;
        for (int i = 0; i < steps; i++)
        {
            var stepScale = (double)i / steps;

            var tickSize = .9;

            if (i == (steps / 2))
                tickSize = .7;
            else if (i % (steps / 4) == 0)
                tickSize = .8;

            if (i == 0)
                tickSize = 1.0;

            var zeroPos = ((stepScale * MaxValue - 0.0) / ( MaxValue - 0.0) * 2.0) - 1.0;
            var angleDegrees = ((zeroPos * 100) * 360.0) / MaxValue;

            angleDegrees *= _removeCirclePercentage;
            var angleRadians = (Math.PI / 180.0) * angleDegrees;

            var limitingDim = dirtyRect.Width < dirtyRect.Height ? dirtyRect.Width : dirtyRect.Height;
            var radius = (limitingDim / 2);
            PointF outerPoint = new((float)(radius * Math.Sin(angleRadians)) + (dirtyRect.Width / 2), (float)(-radius * Math.Cos(angleRadians)) + (dirtyRect.Height / 2));
            PointF innerPoint = new((float)((radius * tickSize) * Math.Sin(angleRadians)) + (dirtyRect.Width / 2), (float)(-(radius * tickSize) * Math.Cos(angleRadians)) + (dirtyRect.Height / 2));
            canvas.DrawLine(outerPoint, innerPoint);

            var scaleDir = (i);
            var percentOfMax = (int)(((double)MaxValue / steps) * scaleDir);

            tickSize = 1.075f;
            PointF stringPoint = new((float)((radius * tickSize) * Math.Sin(angleRadians)) + (dirtyRect.Width / 2), (float)(-(radius * tickSize) * Math.Cos(angleRadians)) + (dirtyRect.Height / 2));

            canvas.DrawString(percentOfMax.ToString(), stringPoint.X, stringPoint.Y, HorizontalAlignment.Center); 
        }
    }

    private void DrawNumDisplay(ICanvas canvas, PointF centerPoint)
    {
        var fillString = FillValue.ToString();

        var textLoc = centerPoint.Offset(0, 50);
        canvas.FontSize = 25;
        canvas.DrawString(fillString, textLoc.X, textLoc.Y, HorizontalAlignment.Center);
    }
    
    //A method to determine the angle of 2 vectors, P1 -> P2, and P1 -> P3
    private double GetAngleDegrees(PointF p1, PointF p2, PointF p3)
    {
        var angle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) - Math.Atan2(p3.Y - p1.Y, p3.X - p1.X);
        var degrees = (180 / Math.PI) * angle; 
        return degrees;
    }
}
