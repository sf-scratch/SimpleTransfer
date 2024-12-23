using DryIoc;
using ImTools;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
using SimpleTransfer.Utils;
using SimpleTransfer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleTransfer.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public DelegateCommand<Tuple<object, DragEventArgs>> DragOverCommand { get; set; }
        public DelegateCommand<Tuple<object, DragEventArgs>> DropCommand { get; set; }
        public DelegateCommand OpenSettingCommand { get; set; }
        public DelegateCommand OpenTransferFilesCommand { get; set; }
        private readonly IDialogService _dialogService;
        private TransferServer _transferServer;
        //发布订阅传递数据
        private readonly IEventAggregator _eventAggregator;

        private string _idCode;

        public string IdCode
        {
            get { return _idCode; }
            set
            {
                _idCode = value;
                RaisePropertyChanged();
            }
        }

        private double _left;

        public double Left
        {
            get { return _left; }
            set
            {
                _left = value;
                RaisePropertyChanged();
                _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Publish(new WindowLeftTop(Left, Top));
            }
        }

        private double _top;

        public double Top
        {
            get { return _top; }
            set
            {
                _top = value;
                RaisePropertyChanged();
                _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Publish(new WindowLeftTop(Left, Top));
            }
        }


        public MainViewModel(IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            //_idCode = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            _idCode = "6401";
            DragOverCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(DragOver);
            DropCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(Drop);
            OpenSettingCommand = new DelegateCommand(OpenSetting);
            OpenTransferFilesCommand = new DelegateCommand(OpenTransferFiles);
            Init();
        }

        private void Init()
        {
            _transferServer = new TransferServer("C:\\MySpace\\Dev\\Project\\TestData");
            _transferServer.Start(IdCode);
            _transferServer.AddReceiveFileProgressEvent += TransferServer_AddReceiveFileProgressEvent;
            _transferServer.AddSendFileProgressEvent += TransferServer_AddSendFileProgressEvent;

        }

        private void TransferServer_AddSendFileProgressEvent(SendFileProgress obj)
        {
            
        }

        private void TransferServer_AddReceiveFileProgressEvent(ReceiveFileProgress obj)
        {

        }

        private void OpenTransferFiles()
        {
            DialogParameters param = new DialogParameters
            {
                { nameof(Left), Left },
                { nameof(Top), Top }
            };
            _dialogService.Show(nameof(TransferProgressDialog), param, (res) =>
            {
                if (res.Result == ButtonResult.OK)
                {
                    string idCode;
                    if (res.Parameters.TryGetValue<string>(nameof(IdCode), out idCode))
                    {
                        IdCode = idCode;
                        _transferServer.IdCode = idCode;
                    }
                }
            }, nameof(TransferProgressDialogWindow));
        }

        private void OpenSetting()
        {
            DialogParameters param = new DialogParameters
            {
                { nameof(Left), Left },
                { nameof(Top), Top },
                { nameof(IdCode), IdCode }
            };
            _dialogService.Show(nameof(SettingsDialog), param, (res) =>
            {
                if (res.Result == ButtonResult.OK)
                {
                    string idCode;
                    if (res.Parameters.TryGetValue<string>(nameof(IdCode), out idCode))
                    {
                        IdCode = idCode;
                        _transferServer.IdCode = idCode;
                    }
                }
            }, nameof(SettingsDialogWindow));
        }

        private void Drop(Tuple<object, DragEventArgs> tuple)
        {
            DragEventArgs eventArgs = tuple.Item2;
            if (eventArgs == null) { return; }
            string[] files = (string[])eventArgs.Data.GetData(DataFormats.FileDrop);
            if (files == null || HasDirectory(files)) { return; }
            _transferServer.SendFiles(files);
        }

        private void DragOver(Tuple<object, DragEventArgs> tuple)
        {
            DragEventArgs eventArgs = tuple.Item2;
            if (eventArgs == null) { return; }
            var files = (string[])eventArgs.Data.GetData(DataFormats.FileDrop);
            if (HasDirectory(files))
            {
                eventArgs.Effects = DragDropEffects.None;// 抛弃这一次的拖拽源
                eventArgs.Handled = true;
            }
        }

        /// <summary>
        /// 包含目录文件判断
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private bool HasDirectory(string[] files)
        {
            if (files == null)
                return false;
            for (int i = 0; i < files.Length; i++)
            {
                if (System.IO.Directory.Exists(files[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
