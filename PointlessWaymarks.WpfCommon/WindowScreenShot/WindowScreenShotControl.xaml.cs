using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using PointlessWaymarks.WpfCommon.Commands;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.WindowScreenShot
{
    /// <summary>
    ///     Interaction logic for WindowScreenShotControl.xaml
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