using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TeacherScheduler
{
    public class DragDropHightlightedCloneAdorner : Adorner
    {
        public bool IsBeingDragged { get; set;} = false;

        private readonly Rect adornedElementRect;
        private readonly VisualBrush cloneBrush;
        private readonly SolidColorBrush hightlightBrush;        
        private double leftOffset;
        private double topOffset;
        // Be sure to call the base class constructor.
        public DragDropHightlightedCloneAdorner(UIElement adornedLmnt, Color highlightColor) : base(adornedLmnt)
        {
            adornedElementRect = new Rect(AdornedElement.RenderSize);
            cloneBrush = new VisualBrush(AdornedElement);
            hightlightBrush = new SolidColorBrush(highlightColor);            
        }

        public double LeftOffset
        {
            get { return leftOffset; }
            set
            {
                leftOffset = value;
                UpdatePosition();
            }
        }

        public double TopOffset
        {
            get { return topOffset; }
            set
            {
                topOffset = value;
                UpdatePosition();
            }
        }
       
        protected override void OnRender(DrawingContext drawingContext)
        {     
            if (IsBeingDragged)
                drawingContext.DrawRectangle(cloneBrush, new Pen(), adornedElementRect);
            drawingContext.DrawRectangle(hightlightBrush, new Pen(), adornedElementRect);
        }
 
        private void UpdatePosition()
        {
            var adornerLayer = Parent as AdornerLayer;
            adornerLayer?.Update(AdornedElement);
        }
        
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(leftOffset, topOffset));
            return result;
        }
    }
}