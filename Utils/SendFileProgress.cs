using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.Utils
{
    public class SendFileProgress : TransferProgress
    {
        public SendFileProgress(string transferIP, string transferFileName) : base(transferIP, transferFileName)
        {
        }
    }
}
