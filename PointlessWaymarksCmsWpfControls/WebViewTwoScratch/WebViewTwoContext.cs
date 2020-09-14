using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.WebViewTwoScratch
{
    public class WebViewTwoContext : INotifyPropertyChanged
    {
        private int _counter = 1;
        private Command _rotateHtmlCommand;

        private StatusControlContext _statusContext;

        private WebViewTwoContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            RotateHtmlCommand = StatusContext.RunBlockingTaskCommand(RotateHtml);
        }

        public Command RotateHtmlCommand
        {
            get => _rotateHtmlCommand;
            set
            {
                if (Equals(value, _rotateHtmlCommand)) return;
                _rotateHtmlCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal static WebViewTwoContext CreateInstance(StatusControlContext statusContext)
        {
            return new WebViewTwoContext(statusContext);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnThresholdReached(HtmlUpdatedEventArgs eventArgs)
        {
            ThresholdReached?.Invoke(this, eventArgs);
        }

        public async Task RotateHtml()
        {
            _counter++;

            OnThresholdReached(new HtmlUpdatedEventArgs
            {
                HtmlString = $@"<html>
<head>
<title>Test {_counter}</title>
<body>
<p>Counter {_counter}</p>
<p>{Guid.NewGuid().ToString()}</p>
</body>
</html>
"
            });
        }

        public event EventHandler<HtmlUpdatedEventArgs> ThresholdReached;

        public class HtmlUpdatedEventArgs : EventArgs
        {
            public string HtmlString { get; set; }
        }
    }
}