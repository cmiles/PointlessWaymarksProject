using System.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.CmsWpfControls.AllContentList
{
    /// <summary>
    ///     Interaction logic for AllItemsWithActionsWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class AllItemsWithActionsWindow : Window
    {
        [ObservableProperty] private AllItemsWithActionsContext _allItemsListContext;
        [ObservableProperty] private string _windowTitle = "Content List";

        public AllItemsWithActionsWindow()
        {
            InitializeComponent();

            DataContext = this;
        }
    }
}