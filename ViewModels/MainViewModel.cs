using ImTools;
using Prism.Commands;
using Prism.Mvvm;
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
        public MainViewModel()
        {
            DragOverCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(DragOver);
            DropCommand = new DelegateCommand<Tuple<object, DragEventArgs>>(Drop);
        }

        private void Drop(Tuple<object, DragEventArgs> tuple)
        {
            DragEventArgs eventArgs = tuple.Item2;
            if (eventArgs == null)
            {
                return;
            }
            var files = (string[])eventArgs.Data.GetData(DataFormats.FileDrop);
            if (files == null || HasDirectory(files))
                return;
            MessageBox.Show(string.Join("\r", files));
        }

        private void DragOver(Tuple<object, DragEventArgs> tuple)
        {
            DragEventArgs eventArgs = tuple.Item2;
            if (eventArgs == null)
            {
                return;
            }
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
