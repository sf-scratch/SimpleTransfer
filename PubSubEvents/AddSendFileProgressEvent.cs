using Prism.Events;
using SimpleTransfer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.PubSubEvents
{
    internal class AddSendFileProgressEvent : PubSubEvent<SendFileProgress>
    {
    }
}
