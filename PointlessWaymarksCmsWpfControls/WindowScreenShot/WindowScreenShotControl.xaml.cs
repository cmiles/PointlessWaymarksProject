using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.ScreenShot;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.WindowScreenShot
{
    /// <summary>
    /// Interaction logic for WindowScreenShotControl.xaml
    /// </summary>
    public partial class WindowScreenShotControl : INotifyPropertyChanged
    {
        public WindowScreenShotControl()
        {
            InitializeComponent();

            WindowScreenShotCommand = new Command<Window>(async x =>
            {
                if (x == null) return;

                StatusControlContext statusContext = null;

                try
                {
                    statusContext = (StatusControlContext) ((dynamic) x.DataContext).StatusContext;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                var result = await NativeCapture.TryWindowScreenShotToClipboardAsync(x);

                if (statusContext != null)
                {
                    if (result)
                        statusContext.ToastSuccess("Window copied to Clipboard");
                    else
                        statusContext.ToastError("Problem Copying Window to Clipboard");
                }
            });

            DataContext = this;
        }

        public Command WindowScreenShotCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}