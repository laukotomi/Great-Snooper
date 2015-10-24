using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GreatSnooper.CustomUI
{
    public class CustomSlider : Slider
    {
        public static readonly DependencyProperty IsDraggingProperty =
             DependencyProperty.Register("IsDragging", typeof(bool), typeof(CustomSlider));

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            IsDragging = true;
            base.OnThumbDragStarted(e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            IsDragging = false;
            base.OnThumbDragCompleted(e);
        }
    }
}
