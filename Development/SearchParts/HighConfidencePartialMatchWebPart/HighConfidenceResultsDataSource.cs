using System.Security.Permissions;
using System.Web;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint.Security;

namespace mAdcOW.SharePoint.Search
{
    [AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal), SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true), AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal), SharePointPermission(SecurityAction.InheritanceDemand, ObjectModel = true)]
    public class HighConfidenceResultsDataSource : SearchResultsBaseDatasource
    {
        // Fields
        private const string HighConfidenceViewName = "HighConfidence";

        // Methods
        public HighConfidenceResultsDataSource(HighConfidencePartialMatchWebPart parentWebPart)
            : base(parentWebPart)
        {
            base.View = new HighConfidenceResultsDataSourceView(this, HighConfidenceViewName);
        }
    }
}
