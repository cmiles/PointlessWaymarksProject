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
using System.Windows.Shapes;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Models;
using PointlessWaymarksCmsWpfControls.Status;

namespace PointlessWaymarksCmsWpfControls.NoteContentEditor
{
    /// <summary>
    /// Interaction logic for NoteContentEditorWindow.xaml
    /// </summary>
    public partial class NoteContentEditorWindow : INotifyPropertyChanged
    {
        private NoteContentEditorContext _noteContent;
        private StatusControlContext _statusContext;

        public NoteContentEditorWindow(NoteContent toLoad)
        {
            InitializeComponent();
            StatusContext = new StatusControlContext();
            NoteContent = new NoteContentEditorContext(StatusContext, toLoad);

            DataContext = this;
        }

        public NoteContentEditorContext NoteContent
        {
            get => _noteContent;
            set
            {
                if (Equals(value, _noteContent)) return;
                _noteContent = value;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
