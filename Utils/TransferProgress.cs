using Prism.DryIoc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SimpleTransfer.Utils
{
    public class TransferProgress : BindableBase
    {
        private DispatcherTimer _timer;
        private readonly int _timerInterval = 1000;

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

        private long _totalBytesLength;
        /// <summary>
        /// 传输文件总数据长度
        /// </summary>
        public long TotalBytesLength
        {
            get { return _totalBytesLength; }
            set
            {
                _totalBytesLength = value;
            }
        }

        private long _transferredBytesLength;
        /// <summary>
        /// 当前已传输文件的数据长度
        /// </summary>
        public long TransferredBytesLength
        {
            get { return _transferredBytesLength; }
            set
            {
                _transferredBytesLength = value;
                int progressValue = (int)(_transferredBytesLength * 1.0 / _totalBytesLength * 100);
                if (progressValue != Value)
                {
                    Value = progressValue;
                }
                if (Value == 100)
                {
                    _timer.Stop();
                    Task.Delay(_timerInterval + 100).Wait();
                    TransferRate = "完成";
                }
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

        private string _transferRate;
        /// <summary>
        /// 传输文件进度 0 - 100
        /// </summary>
        public string TransferRate
        {
            get { return _transferRate; }
            set
            {
                _transferRate = value;
                RaisePropertyChanged();
            }
        }

        public TransferProgress(string transferIP, string transferFileName)
        {
            _transferIP = transferIP;
            _transferFileName = transferFileName;
            _value = 0;
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += ChangeFileTransferRate;
                _timer.Start();
            });
        }

        private long _preTransferredBytesLength = 0;

        private void ChangeFileTransferRate(object sender, EventArgs e)
        {
            long bytesLenth = _transferredBytesLength - _preTransferredBytesLength;
            _preTransferredBytesLength = _transferredBytesLength;
            int GB = 1024 * 1024 * 1024;//定义GB的计算常量
            int MB = 1024 * 1024;//定义MB的计算常量
            int KB = 1024;//定义KB的计算常量
            double transferRate = 0;
            string rateUnit = "b/s";
            if ((int)(transferRate = Math.Round(bytesLenth / (double)GB, 2)) != 0)
            {
                rateUnit = "Gb/s";
            }
            else if ((int)(transferRate = Math.Round(bytesLenth / (double)MB, 2)) != 0)
            {
                rateUnit = "Mb/s";
            }
            else if ((int)(transferRate = Math.Round(bytesLenth / (double)KB, 2)) != 0)
            {
                rateUnit = "Kb/s";
            }
            else if (bytesLenth >= TransferServer.BufferSize) 
            {
                transferRate = bytesLenth;
                rateUnit = "b/s";
            }
            else
            {
                PrismApplication.Current.Dispatcher.Invoke(() =>
                {
                    TransferRate = string.Format("低于{0} {1}", TransferServer.BufferSize / 1024, "b/s");
                });
            }
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                TransferRate = string.Format("{0} {1}", transferRate, rateUnit);
            });
        }
    }
}
