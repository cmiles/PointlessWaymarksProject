using System.Windows;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

/// <summary>
///     Interaction logic for SearchBuilderWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class SearchBuilderWindow : Window
{
    public SearchBuilderWindow(SearchBuilderContext context)
    {
        SearchContext = context;
        StatusContext = context.StatusContext;

        RunSearchCommand = new RelayCommand(RunSearch);
        CancelSearchCommand = new RelayCommand(CancelSearch);

        DataContext = this;
        InitializeComponent();
    }

    public RelayCommand CancelSearchCommand { get; set; }
    public RelayCommand RunSearchCommand { get; set; }
    public SearchBuilderContext SearchContext { get; set; }
    public string SearchString { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public SearchBuilderWindowExitType WindowExitType { get; set; } = SearchBuilderWindowExitType.NotSet;

    public void CancelSearch()
    {
        WindowExitType = SearchBuilderWindowExitType.Cancel;
        Close();
    }

    public static async Task<SearchBuilderWindow> CreateInstance(SearchBuilderContext searchContext)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderWindow(searchContext);
    }

    public void RunSearch()
    {
        SearchString = SearchContext.CreateSearch();
        WindowExitType = SearchBuilderWindowExitType.RunSearch;
        Close();
    }
}

public enum SearchBuilderWindowExitType
{
    NotSet,
    Cancel,
    RunSearch
}