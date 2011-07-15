using System.ComponentModel;
using System.Web;
using System.Web.UI.WebControls.WebParts;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.Office.Server.Search.Query;
using Microsoft.Office.Server.Search.WebControls;

namespace mAdcOW.SharePoint.Search
{
    [ToolboxItemAttribute(false)]
    public class FqlCoreResults : CoreResultsWebPart
    {
        QueryManager _queryManager;

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Query Mode")]
        [WebDescription("Kql or FQL")]
        public SynonymHandling SynonymHandling { get; set; }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Original Query Boost Value")]
        [WebDescription("Boost the original entered query")]
        public int BoostValue { get; set; }

        protected override void ConfigureDataSourceProperties()
        {
            this.FixedQuery = GetQuery();
            base.ConfigureDataSourceProperties();
        }

        protected override void CreateDataSource()
        {
            this.DataSource = new CoreFqlResultsDataSource(this);
        }

        private string GetQuery()
        {
            string query = HttpUtility.UrlDecode(HttpContext.Current.Request["k"]);
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }
            FqlHelper helper = new FqlHelper();
            var fql = helper.GetFqlFromKql(query, SynonymHandling, BoostValue);
            return fql;
        }
    }
}
