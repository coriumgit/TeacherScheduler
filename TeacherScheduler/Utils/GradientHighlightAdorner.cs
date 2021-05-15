using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TeacherScheduler
{
    public class GradientHighlightAdorner : Adorner
    {
        private readonly Rect adornedElementRect;     
        private readonly LinearGradientBrush hightlightBrush;        

        // Be sure to call the base class constructor.
        public GradientHighlightAdorner(UIElement adornedLmnt, Color highlightColor) : base(adornedLmnt)
        {
            adornedElementRect = new Rect(AdornedElement.RenderSize);
            GradientStopCollection stops = new GradientStopCollection();
            Color highlightColorWithAlpha1 = Color.FromRgb(highlightColor.R, highlightColor.G, highlightColor.B);
            stops.Add(new GradientStop(highlightColorWithAlpha1, 0.0));
            stops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.25));
            stops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.75));
            stops.Add(new GradientStop(highlightColorWithAlpha1, 1.0));
            hightlightBrush = new LinearGradientBrush(stops, 0.0f);            
        }
                    
        protected override void OnRender(DrawingContext drawingContext)
        {                 
            drawingContext.DrawRectangle(hightlightBrush, new Pen(), adornedElementRect);
        }
    }
}