using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace SimpleTransfer.Behaviors
{
    /// <summary>
    /// 实现窗口拖动
    /// </summary>
    public class DraggableWindowBehavior
    {
        public static bool GetIsDraggable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDraggableProperty);
        }

        public static void SetIsDraggable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDraggableProperty, value);
        }

        public static readonly DependencyProperty IsDraggableProperty =
            DependencyProperty.RegisterAttached("IsDraggable", typeof(bool), typeof(DraggableWindowBehavior), new UIPropertyMetadata(false, OnIsDraggableChanged));

        private static void OnIsDraggableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element == null) { return; }

            if ((bool)e.NewValue)
            {
                element.PreviewMouseLeftButtonDown += UIElement_PreviewMouseLeftButtonDown;
                element.PreviewMouseMove += UIElement_PreviewMouseMove;
            }
            else
            {
                element.PreviewMouseLeftButtonDown -= UIElement_PreviewMouseLeftButtonDown;
                element.PreviewMouseMove -= UIElement_PreviewMouseMove;
            }
        }

        private static void UIElement_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var dependencyObject = sender as DependencyObject;
                if (dependencyObject == null) { return; }

                Window window = Window.GetWindow(dependencyObject);
                if (window != null)
                {
                    window.DragMove();
                }
            }
        }

        private static void UIElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This method is necessary to ensure the window is dragged
            // when the mouse is over a UIElement in the window.
            e.Handled = true;
        }
    }
}
