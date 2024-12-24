using DryIoc;
using ImTools;
using Microsoft.Win32;
using Prism.Commands;
using Prism.DryIoc;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
using SimpleTransfer.Utils;
using SimpleTransfer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static ImTools.ImMap;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SimpleTransfer.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private static readonly string RegisterKeyName = @"Software\SimpleTransfer";
        public DelegateCommand<Tuple<object, DragEventArgs>> DragOverCommand { get; set; }
        public DelegateCommand<Tuple<object, DragEventArgs>> DropCommand { get; set; }
        public DelegateCommand OpenSettingCommand { get; set; }
        public DelegateCommand OpenTransferFilesCommand { get; set; }
        public DelegateCommand SetStartupLocationCommand { get; set; }
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
            SetStartupLocationCommand = new DelegateCommand(SetStartupLocation);
        }

        private void InitAppConfig()
        {
            try
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(RegisterKeyName);
                if (subKey == null)
                {
                    MessageBox.Show("注册表打开异常");
                    Environment.Exit(0);
                    return;
                }
                if (subKey.GetValue(nameof(IdCode)) == null)
                {
                    subKey.SetValue(nameof(IdCode), Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper());
                }
                if (subKey.GetValue(nameof(SaveFolder)) == null)
                {
                    string saveFolder = string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimpleTransfer");
                    if (!Directory.Exists(saveFolder))
                    {
                        Directory.CreateDirectory(saveFolder);
                    }
                    subKey.SetValue(nameof(SaveFolder), saveFolder);
                }
                if (subKey.GetValue(nameof(Left)) == null)
                {
                    subKey.SetValue(nameof(Left), "200");
                }
                if (subKey.GetValue(nameof(Top)) == null)
                {
                    subKey.SetValue(nameof(Top), "200");
                }

                IdCode = subKey.GetValue(nameof(IdCode)).ToString();
                SaveFolder = subKey.GetValue(nameof(SaveFolder)).ToString();
                string leftValue = subKey.GetValue(nameof(Left)).ToString();
                if (double.TryParse(leftValue, out double left))
                {
                    _left = left;
                }
                string topValue = subKey.GetValue(nameof(Top)).ToString();
                if (double.TryParse(topValue, out double top))
                {
                    _top = top;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void InitTransferServer()
        {
            _transferServer = new TransferServer(SaveFolder, IdCode);
            _transferServer.Start();
            _transferServer.AddReceiveFileProgressEvent += TransferServer_AddReceiveFileProgressEvent;
            _transferServer.AddSendFileProgressEvent += TransferServer_AddSendFileProgressEvent;

        }

        private readonly List<SendFileProgress> _SendFileProgressCollection = new List<SendFileProgress>();
        private void TransferServer_AddSendFileProgressEvent(SendFileProgress sendFileProgress)
        {
            _eventAggregator.GetEvent<CloseDialogEvent>().Publish();//通知关闭对话框
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                OpenTransferFiles();//打开文件传输对话框（需要UI线程）
            });
            _eventAggregator.GetEvent<AddSendFileProgressEvent>().Publish(sendFileProgress);
            _SendFileProgressCollection.Add(sendFileProgress);
        }

        private readonly List<ReceiveFileProgress> _ReceiveFileProgressCollection = new List<ReceiveFileProgress>();
        private void TransferServer_AddReceiveFileProgressEvent(ReceiveFileProgress receiveFileProgress)
        {
            _eventAggregator.GetEvent<CloseDialogEvent>().Publish();//通知关闭对话框
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                OpenTransferFiles();//打开文件传输对话框（需要UI线程）
            });
            _eventAggregator.GetEvent<AddReceiveFileProgressEvent>().Publish(receiveFileProgress);
            _ReceiveFileProgressCollection.Add(receiveFileProgress);
        }

        private void SetStartupLocation()
        {
            try
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(RegisterKeyName);
                if (subKey == null)
                {
                    MessageBox.Show("注册表打开异常");
                    return;
                }
                subKey.SetValue(nameof(Left), Left.ToString());
                subKey.SetValue(nameof(Top), Top.ToString());
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
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

                //将文件传输的记录再发给对话框
                foreach (var item in _SendFileProgressCollection)
                {
                    _eventAggregator.GetEvent<AddSendFileProgressEvent>().Publish(item);
                }
                foreach (var item in _ReceiveFileProgressCollection)
                {
                    _eventAggregator.GetEvent<AddReceiveFileProgressEvent>().Publish(item);
                }
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
                    { nameof(SaveFolder), SaveFolder },
                    { "IsTransferLocal", _transferServer.IsTransferLocal }
                };
                _dialogService.Show(nameof(SettingsDialog), param, (res) =>
                {
                    //点击保存按钮
                    if (res.Result == ButtonResult.OK)
                    {
                        if (res.Parameters.TryGetValue(nameof(IdCode), out string idCode))
                        {
                            IdCode = idCode;
                            _transferServer.IdCode = idCode;
                            RegisterSetValue(nameof(IdCode), idCode);
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
                                    RegisterSetValue(nameof(SaveFolder), saveFolder);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            }
                            else
                            {
                                SaveFolder = saveFolder;
                                _transferServer.SaveFolder = saveFolder;
                                RegisterSetValue(nameof(SaveFolder), saveFolder);
                            }
                        }
                        if (res.Parameters.TryGetValue("IsTransferLocal", out bool isTransferLocal))
                        {
                            _transferServer.IsTransferLocal = isTransferLocal;
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

        private void RegisterSetValue(string key, string value)
        {
            try
            {
                RegistryKey subKey = Registry.CurrentUser.CreateSubKey(RegisterKeyName);
                if (subKey == null)
                {
                    MessageBox.Show("注册表打开异常");
                    return;
                }
                subKey.SetValue(key, value);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
