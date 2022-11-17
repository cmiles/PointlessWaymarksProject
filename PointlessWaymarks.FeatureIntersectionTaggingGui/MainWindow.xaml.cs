﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LoggingTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using Serilog;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class MainWindow
    {
        [ObservableProperty] private string _infoTitle;
        [ObservableProperty] private StatusControlContext _statusContext;
        [ObservableProperty] private WindowIconStatus _windowStatus;

        public MainWindow()
        {
            InitializeComponent();


            JotServices.Tracker.Track(this);

            if (Width < 900) Width = 900;
            if (Height < 650) Height = 650;

            WindowInitialPositionHelpers.EnsureWindowIsVisible(this);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse for generated ThisAssembly.Git.IsDirty
            // ReSharper disable once HeuristicUnreachableCode
            //.Git IsDirty can change at runtime
#pragma warning disable CS0162
            _infoTitle = WindowTitleTools.StandardAppInformationString(Assembly.GetExecutingAssembly(),
                "Pointless Waymarks Feature Intersection Tagger"); ;
#pragma warning restore CS0162

            DataContext = this;

            _statusContext = new StatusControlContext();

            _windowStatus = new WindowIconStatus();

            StatusContext.RunBlockingTask(LoadData);
        }

        private async Task LoadData()
        {
        }


    }
}