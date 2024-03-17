using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;

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
        DataContext = this;
        InitializeComponent();
    }

    public SearchBuilderContext SearchContext { get; set; }

    public static async Task<SearchBuilderWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new SearchBuilderWindow(await SearchBuilderContext.CreateInstance());
    }
}