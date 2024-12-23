using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.Utils
{
    public class TransferProgress : BindableBase
    {
        private string _transferIP;
        /// <summary>
        /// 与本机进行接收或发送的IP
        /// </summary>
        public string TransferIP
        {
            get { return _transferIP; }
            set { _transferIP = value; }
        }

        private string _transferFileName;
        /// <summary>
        /// 与本机进行接收或发送的文件名
        /// </summary>
        public string TransferFileName
        {
            get { return _transferFileName; }
            set
            {
                _transferFileName = value;
                RaisePropertyChanged();
            }
        }

        private int _value;
        /// <summary>
        /// 传输文件进度 0 - 100
        /// </summary>
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged();
            }
        }

        public TransferProgress(string transferIP, string transferFileName)
        {
            _transferIP = transferIP;
            _transferFileName = transferFileName;
            _value = 0;
        }
    }
}
