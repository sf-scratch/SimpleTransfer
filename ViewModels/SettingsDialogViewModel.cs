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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleTransfer.ViewModels
{
    internal class SettingsDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "设置";
        public event Action<IDialogResult> RequestClose;
        public DelegateCommand SaveCommand {  get; set; }
        public DelegateCommand CancelCommand {  get; set; }
        public DelegateCommand SelectSaveFolderCommand { get; set; }
        public DelegateCommand OpenSaveFolderCommand { get; set; }
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

        private string _saveFolder;

        public string SaveFolder
        {
            get { return _saveFolder; }
            set
            {
                _saveFolder = value;
                RaisePropertyChanged();
            }
        }

        private bool _isNotTransferLocal;
        public bool IsNotTransferLocal
        {
            get { return _isNotTransferLocal; }
            set
            {
                _isNotTransferLocal = value;
                RaisePropertyChanged();
            }
        }

        public SettingsDialogViewModel(IEventAggregator eventAggregator)
        {
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
            SelectSaveFolderCommand = new DelegateCommand(SelectSaveFolder);
            OpenSaveFolderCommand = new DelegateCommand(OpenSaveFolder);
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<CurrentOpenDialogEvent>().Subscribe(CheckIsCloseDialog);
        }

        private void CheckIsCloseDialog(string openDialogName)
        {
            if (nameof(SettingsDialog) != openDialogName)
            {
                PrismApplication.Current.Dispatcher.Invoke(() =>
                {
                    RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
                });
            }
        }

        private void OpenSaveFolder()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = SaveFolder,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        private void SelectSaveFolder()
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "保存到文件夹";
                System.Windows.Forms.DialogResult dialogResult = folderBrowserDialog.ShowDialog();
                if (dialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    SaveFolder = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void Save()
        {
            DialogParameters param = new DialogParameters
            {
                { nameof(IdCode), IdCode },
                { nameof(SaveFolder), SaveFolder },
                { nameof(IsNotTransferLocal), IsNotTransferLocal }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, param));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            IdCode = parameters.GetValue<string>(nameof(IdCode));
            SaveFolder = parameters.GetValue<string>(nameof(SaveFolder));
            IsNotTransferLocal = parameters.GetValue<bool>(nameof(IsNotTransferLocal));
            double left = parameters.GetValue<double>("Left");
            double top = parameters.GetValue<double>("Top");
            _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Publish(new WindowLeftTop(left, top));
        }
    }
}
