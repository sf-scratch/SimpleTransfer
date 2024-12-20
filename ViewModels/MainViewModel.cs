using DryIoc;
using ImTools;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.Utils;
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
        private readonly IDialogService _dialogService;
        private TransferServer _transferServer;

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

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            _idCode = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            DragOverCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(DragOver);
            DropCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(Drop);
            OpenSettingCommand = new DelegateCommand(OpenSetting);
            Init();
        }

        private void Init()
        {
            _transferServer = new TransferServer("E:\\TestData\\ReceiveFileFolder");
            _transferServer.Start(IdCode);
        }

        private void OpenSetting()
        {
            DialogParameters param = new DialogParameters
            {
                { nameof(IdCode), IdCode }
            };
            _dialogService.Show("SettingsDialog", param, (res) =>
            {
                if (res.Result == ButtonResult.OK)
                {
                    string idCode;
                    if (res.Parameters.TryGetValue<string>(nameof(IdCode), out idCode))
                    {
                        IdCode = idCode;
                    }
                }
            });
        }

        private void Drop(Tuple<object, DragEventArgs> tuple)
        {
            DragEventArgs eventArgs = tuple.Item2;
            if (eventArgs == null) { return; }
            string[] files = (string[])eventArgs.Data.GetData(DataFormats.FileDrop);
            if (files == null || HasDirectory(files)) { return; }
            _transferServer.TransferFilePathQueue.Append(files);
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
