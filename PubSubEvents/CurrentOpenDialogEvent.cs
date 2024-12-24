using Prism.Events;
using SimpleTransfer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.PubSubEvents
{
    //通知所有对话框，若不是需要打开的对话框，则关闭
    internal class CurrentOpenDialogEvent : PubSubEvent<string>
    {
    }
}
