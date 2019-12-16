using System;
using System.Windows;

namespace TheLemmonWorkshopWpfControls.Utility
{
    public static class UiThreadHelper
    {
        public static void CheckBeginInvokeOnUi(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess())
                action();
            else
                Application.Current.Dispatcher.BeginInvoke(action);
        }
    }
}