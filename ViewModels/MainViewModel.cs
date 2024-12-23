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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        //发布订阅传递数据
        private readonly IEventAggregator _eventAggregator;
        private TransferServer _transferServer;
        private int _isOpenedDialog = 0;// 0:未打开对话框  1:已打开对话框
        private string SaveFolder {  get; set; }

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
            InitAppConfig();
            InitTransferServer();
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            DragOverCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(DragOver);
            DropCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(Drop);
            OpenSettingCommand = new DelegateCommand(OpenSetting);
            OpenTransferFilesCommand = new DelegateCommand(OpenTransferFiles);
        }

        private void InitAppConfig()
        {
            if (ConfigurationManager.AppSettings[nameof(IdCode)] == null)
            {
                AppConfigUtils.AddItem(nameof(IdCode), Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper());
            }
            if (ConfigurationManager.AppSettings[nameof(SaveFolder)] == null)
            {
                string saveFolder = string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimpleTransfer");
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }
                AppConfigUtils.AddItem(nameof(SaveFolder), saveFolder);
            }
            if (ConfigurationManager.AppSettings[nameof(Left)] == null)
            {
                AppConfigUtils.AddItem(nameof(Left), "200");
            }
            if (ConfigurationManager.AppSettings[nameof(Top)] == null)
            {
                AppConfigUtils.AddItem(nameof(Top), "200");
            }
            IdCode = ConfigurationManager.AppSettings[nameof(IdCode)];
            SaveFolder = ConfigurationManager.AppSettings[nameof(SaveFolder)];
            if (double.TryParse(ConfigurationManager.AppSettings[nameof(Left)], out double left))
            {
                _left = left;
            }
            if (double.TryParse(ConfigurationManager.AppSettings[nameof(Top)], out double top))
            {
                _top = top;
            }
        }

        private void InitTransferServer()
        {
            _transferServer = new TransferServer(SaveFolder, IdCode);
            _transferServer.Start();
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
            if (Interlocked.CompareExchange(ref _isOpenedDialog, 1, 0) == 0)
            {
                DialogParameters param = new DialogParameters()
                {
                    { nameof(Left), Left },
                    { nameof(Top), Top }
                };
                _dialogService.Show(nameof(TransferProgressDialog), param, (res) =>
                {
                    if (res.Result == ButtonResult.OK)
                    {
                        if (res.Parameters.TryGetValue(nameof(IdCode), out string idCode))
                        {
                            IdCode = idCode;
                            _transferServer.IdCode = idCode;
                        }
                    }
                    Interlocked.Exchange(ref _isOpenedDialog, 0);//切换状态
                }, nameof(TransferProgressDialogWindow));
            }
        }

        private void OpenSetting()
        {
            if (Interlocked.CompareExchange(ref _isOpenedDialog, 1, 0) == 0)
            {
                DialogParameters param = new DialogParameters()
                {
                    { nameof(Left), Left },
                    { nameof(Top), Top },
                    { nameof(IdCode), IdCode },
                    { nameof(SaveFolder), SaveFolder }
                };
                _dialogService.Show(nameof(SettingsDialog), param, (res) =>
                {
                    if (res.Result == ButtonResult.OK)
                    {
                        if (res.Parameters.TryGetValue(nameof(IdCode), out string idCode))
                        {
                            IdCode = idCode;
                            _transferServer.IdCode = idCode;
                            AppConfigUtils.AddItem(nameof(IdCode), idCode);
                        }

                        if (res.Parameters.TryGetValue(nameof(SaveFolder), out string saveFolder))
                        {
                            //检查文件夹是否存在
                            if (!Directory.Exists(saveFolder))
                            {
                                try
                                {
                                    Directory.CreateDirectory(saveFolder);//文件夹路径格式不对会抛IO异常
                                    SaveFolder = saveFolder;
                                    _transferServer.SaveFolder = saveFolder;
                                    AppConfigUtils.AddItem(nameof(SaveFolder), saveFolder);
                                }
                                catch(Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            }
                            else
                            {
                                SaveFolder = saveFolder;
                                _transferServer.SaveFolder = saveFolder;
                                AppConfigUtils.AddItem(nameof(SaveFolder), saveFolder);
                            }
                        }
                    }
                    Interlocked.Exchange(ref _isOpenedDialog, 0);//切换状态
                }, nameof(SettingsDialogWindow));
            }
                
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
