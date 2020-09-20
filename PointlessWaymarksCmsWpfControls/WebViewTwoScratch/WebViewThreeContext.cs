using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.WebViewTwoScratch
{
    public class WebViewThreeContext : INotifyPropertyChanged
    {
        private int _counter = 1;
        private string _generatedHtml;
        private Command _rotateHtmlCommand;

        private StatusControlContext _statusContext;

        private WebViewThreeContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            RotateHtmlCommand = StatusContext.RunBlockingTaskCommand(RotateHtml);
        }

        public string GeneratedHtml
        {
            get => _generatedHtml;
            set
            {
                if (value == _generatedHtml) return;
                _generatedHtml = value;
                OnPropertyChanged();
            }
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

        public static WebViewThreeContext CreateInstance(StatusControlContext statusContext)
        {
            return new WebViewThreeContext(statusContext);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task RotateHtml()
        {
            _counter++;

            GeneratedHtml = $@"<html>
<head>
<title>Test {_counter}</title>
<body>
<p>Counter {_counter}</p>
<p>{Guid.NewGuid().ToString()}</p>
</body>
</html>
";
        }
    }
}