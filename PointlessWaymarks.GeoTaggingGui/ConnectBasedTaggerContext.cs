using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.GeoTaggingGui
{
    [ObservableObject]
    public partial class ConnectBasedTaggerContext
    {
        [ObservableProperty] private StatusControlContext _statusContext;
        [ObservableProperty] private WindowIconStatus? _windowStatus;

        public ConnectBasedTaggerContext(StatusControlContext? statusContext, WindowIconStatus? windowStatus)
        {
            _statusContext = statusContext ?? new StatusControlContext();
            _windowStatus = windowStatus;
        }

        public static async Task<ConnectBasedTaggerContext> CreateInstance(StatusControlContext? statusContext,
            WindowIconStatus windowStatus)
        {
            var control = new ConnectBasedTaggerContext(statusContext, windowStatus);
            await control.LoadData();
            return control;
        }

        public async System.Threading.Tasks.Task LoadData()
        {

        }
    }
}
