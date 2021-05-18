using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PointlessWaymarks.CmsWpfControls.ContentList
{
    public class ContentListLoaderReport : ContentListLoaderBase
    {
        private readonly Func<Task<List<object>>> _loaderFunc;

        public ContentListLoaderReport(Func<Task<List<object>>> loaderFunc) : base(null)
        {
            _loaderFunc = loaderFunc;
        }

        public override async Task<bool> CheckAllItemsAreLoaded()
        {
            return true;
        }

        public override async Task<List<object>> LoadItems(IProgress<string> progress = null)
        {
            var listItems = new List<object>();

            if (_loaderFunc != null) listItems.AddRange(await _loaderFunc());

            AllItemsLoaded = true;
            
            return listItems;
        }
    }
}