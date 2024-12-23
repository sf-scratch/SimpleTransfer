using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
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

        public SettingsDialogViewModel(IEventAggregator eventAggregator)
        {
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
            _eventAggregator = eventAggregator;
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
            IdCode = parameters.GetValue<string>(nameof(IdCode));
            double left = parameters.GetValue<double>("Left");
            double top = parameters.GetValue<double>("Top");
            _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Publish(new WindowLeftTop(left, top));
        }
    }
}
