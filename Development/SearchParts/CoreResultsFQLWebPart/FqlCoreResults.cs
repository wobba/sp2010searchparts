using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.UI.WebControls.WebParts;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.Office.Server.Search.Query;
using Microsoft.Office.Server.Search.WebControls;

namespace mAdcOW.SharePoint.Search
{
    public enum QueryKind
    {
        Kql, Fql
    }

    [ToolboxItemAttribute(false)]
    public class FqlCoreResults : CoreResultsWebPart
    {
        QueryManager _queryManager;

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Synonym handling")]
        [WebDescription("Choose to expand synonyms")]
        public SynonymHandling SynonymHandling { get; set; }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Query Language")]
        [WebDescription("Kql or Fql")]
        public QueryKind QueryKind { get; set; }

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
            if (QueryKind == QueryKind.Fql) return query;

            Dictionary<string, List<string>> synonymLookup = new Dictionary<string, List<string>>();
            FastSynonymReader.PopulateSynonyms(synonymLookup);
            Dictionary<string, string> scopeLookup = new Dictionary<string, string>();

            FastScopeReader.PopulateScopes(scopeLookup);
            string scopeFilter = null;
            if (!string.IsNullOrEmpty(this.Scope)) scopeLookup.TryGetValue(this.Scope.ToLower(), out scopeFilter);

            FqlHelper helper = new FqlHelper(synonymLookup, scopeFilter);
            var fql = helper.GetFqlFromKql(query, SynonymHandling, BoostValue);
            return fql;
        }
    }
}
