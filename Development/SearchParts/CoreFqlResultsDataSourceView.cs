using System;
using System.Linq;
using Microsoft.Office.Server.Search.Query;
using Microsoft.Office.Server.Search.WebControls;

namespace mAdcOW.SharePoint.Search
{
    internal class CoreFqlResultsDataSourceView : CoreResultsDatasourceView
    {
        private static readonly DynamicReflectionHelperforObject<Location>.GetPropertyFieldDelegate<ILocationRuntime>
            _internalGetHelper = DynamicReflectionHelperforObject<Location>.GetProperty<ILocationRuntime>("LocationRuntime");

        public CoreFqlResultsDataSourceView(SearchResultsBaseDatasource dataSourceOwner, string viewName)
            : base(dataSourceOwner, viewName)
        {
            CoreFqlResultsDataSource fqlDataSourceOwner = base.DataSourceOwner as CoreFqlResultsDataSource;

            if (fqlDataSourceOwner == null)
            {
                throw new ArgumentOutOfRangeException();
            }
        }
        public override void SetPropertiesOnQdra()
        {
            base.SetPropertiesOnQdra();
            // At this point the query has not yet been dispatched to a search 
            // location and we can set properties on that location, which will 
            // let it understand the FQL syntax.
            UpdateFastSearchLocation();
        }

        private void UpdateFastSearchLocation()
        {
            if (base.LocationList == null || 0 == base.LocationList.Count)
            {
                return;
            }

            foreach (var runtime in
                base.LocationList.Select(location => _internalGetHelper.Invoke(location)).OfType<FASTSearchRuntime>())
            {
                // This is a FAST Search runtime. We can now enable FQL.
                runtime.EnableFQL = true;
                break;
            }
        }
    }
}