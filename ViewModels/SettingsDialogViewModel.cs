using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
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

        public SettingsDialogViewModel()
        {
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void Save()
        {
            DialogParameters param = new DialogParameters
            {
                { nameof(IdCode), IdCode }
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
            string idCode;
            if (parameters.TryGetValue<string>(nameof(IdCode), out idCode))
            {
                IdCode = idCode;
            }
        }
    }
}
