using Prism.Commands;
using Prism.DryIoc;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
using SimpleTransfer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace SimpleTransfer.ViewModels
{
    public class TransferProgressDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "文件传输列表";

        public event Action<IDialogResult> RequestClose;
        //发布订阅传递数据
        private readonly IEventAggregator _eventAggregator;
        public DelegateCommand CloseDialogCommand { get; set; }
        public ObservableCollection<SendFileProgress> SendFileProgressCollection { get; set; }
        public ObservableCollection<ReceiveFileProgress> ReceiveFileProgressCollection { get; set; }

        public TransferProgressDialogViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            CloseDialogCommand = new DelegateCommand(CloseDialog);
            SendFileProgressCollection = new ObservableCollection<SendFileProgress>();
            ReceiveFileProgressCollection = new ObservableCollection<ReceiveFileProgress>();
            _eventAggregator.GetEvent<AddSendFileProgressEvent>().Subscribe(AddSendFileProgress);
            _eventAggregator.GetEvent<AddReceiveFileProgressEvent>().Subscribe(AddReceiveFileProgress);
        }

        private void CloseDialog()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private void AddReceiveFileProgress(ReceiveFileProgress progress)
        {
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                ReceiveFileProgressCollection.Add(progress);
            });
        }

        private void AddSendFileProgress(SendFileProgress progress)
        {
            PrismApplication.Current.Dispatcher.Invoke(() =>
            {
                SendFileProgressCollection.Add(progress);
            });
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
            double left = parameters.GetValue<double>("Left");
            double top = parameters.GetValue<double>("Top");
            _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Publish(new WindowLeftTop(left, top));
        }
    }
}
