using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MySnooper.CustomUIThings
{
    public class SliderWithDraggingEvents : Slider
    {
        public delegate void ThumbDragStartedHandler(object sender, DragStartedEventArgs e);
        public event ThumbDragStartedHandler ThumbDragStarted;


        public delegate void ThumbDragCompletedHandler(object sender, DragCompletedEventArgs e);
        public event ThumbDragCompletedHandler ThumbDragCompleted;

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            if (ThumbDragStarted != null) ThumbDragStarted(this, e);
            base.OnThumbDragStarted(e);
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            if (ThumbDragCompleted != null) ThumbDragCompleted(this, e);
            base.OnThumbDragCompleted(e);
        }
    }
}
