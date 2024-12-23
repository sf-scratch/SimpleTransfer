using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.PubSubEvents
{
    public class UpdateWindowLeftTopEvent : PubSubEvent<WindowLeftTop>
    {
    }

    public class WindowLeftTop
    {
        public WindowLeftTop(double left, double top)
        {
            Left = left;
            Top = top;
        }
        public double Left {  get; set; }
        public double Top {  get; set; }
    }
}
