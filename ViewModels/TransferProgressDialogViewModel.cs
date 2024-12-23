using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.ViewModels
{
    public class TransferProgressDialogViewModel : BindableBase, IDialogAware
    {
        public string Title => "文件传输列表";

        public event Action<IDialogResult> RequestClose;
        //发布订阅传递数据
        private readonly IEventAggregator _eventAggregator;

        public TransferProgressDialogViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
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
