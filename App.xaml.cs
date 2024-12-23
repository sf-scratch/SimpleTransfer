using Prism.DryIoc;
using Prism.Ioc;
using Prism.Services.Dialogs;
using SimpleTransfer.ViewModels;
using SimpleTransfer.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleTransfer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<Main>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Main, MainViewModel>();
            containerRegistry.RegisterDialog<SettingsDialog, SettingsDialogViewModel>();
            containerRegistry.RegisterDialog<TransferProgressDialog, TransferProgressDialogViewModel>();
            containerRegistry.Register<IDialogWindow, SettingsDialogWindow>(nameof(SettingsDialogWindow));
            containerRegistry.Register<IDialogWindow, TransferProgressDialogWindow>(nameof(TransferProgressDialogWindow));
        }
    }
}
