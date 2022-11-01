using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.GeoTaggingGui
{
    [ObservableObject]
    public partial class DirectoryBasedTaggerContext
    {
        [ObservableProperty] private StatusControlContext _statusContext;
        [ObservableProperty] private WindowIconStatus? _windowStatus;

        public DirectoryBasedTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
        {
            _statusContext = statusContext ?? new StatusControlContext();
            _windowStatus = windowStatus;
        }

        public static async Task<DirectoryBasedTaggerContext> CreateInstance(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
        {
            var control = new DirectoryBasedTaggerContext(statusContext, windowStatus);
            await control.LoadData();
            return control;
        }

        private async System.Threading.Tasks.Task LoadData()
        {
        }
    }
}
