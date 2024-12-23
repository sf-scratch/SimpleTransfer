using Prism.Events;
using Prism.Services.Dialogs;
using SimpleTransfer.PubSubEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleTransfer.Views
{
    /// <summary>
    /// TransferProgressDialogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TransferProgressDialogWindow : Window, IDialogWindow
    {
        private IEventAggregator _eventAggregator;

        public TransferProgressDialogWindow(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<UpdateWindowLeftTopEvent>().Subscribe(UpdateWindowLeftTop);
        }

        private void UpdateWindowLeftTop(WindowLeftTop win)
        {
            Left = win.Left + 70;
            Top = win.Top;
        }

        public IDialogResult Result { get; set; }
    }
}
