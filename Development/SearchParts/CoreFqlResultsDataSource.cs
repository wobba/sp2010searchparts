using Microsoft.Office.Server.Search.WebControls;

namespace mAdcOW.SharePoint.Search
{
    public class CoreFqlResultsDataSource : CoreResultsDatasource
    {
        private const string CoreFqlResultsViewName = "CoreFqlResults";

        public CoreFqlResultsDataSource(CoreResultsWebPart parentWebPart)
            : base(parentWebPart)
        {
            // Replace default view with a custom view.
            base.View = new CoreFqlResultsDataSourceView(this, CoreFqlResultsViewName);            
        }
    }
}

