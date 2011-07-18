using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.Caching;
using System.Web.UI.WebControls.WebParts;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.Office.Server.Search.Query;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint;

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

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Cache time for synonyms and scopes")]
        [WebDescription("Cache the values for specified minutes. 0=no caching")]
        public int CacheMinutes { get; set; }

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

            string cacheKey = SPContext.Current.Site.Url;
            Dictionary<string, List<string>> synonymLookup = GetSynonymLookup(cacheKey);
            Dictionary<string, string> scopeLookup = GetScopeLookup(cacheKey);
            Dictionary<string, string> managedPropertyTypeLookup = GetPropertyTypeLookup(cacheKey);


            string scopeFilter = null;
            if (!string.IsNullOrEmpty(this.Scope)) scopeLookup.TryGetValue(this.Scope.ToLower(), out scopeFilter);

            FqlHelper helper = new FqlHelper(synonymLookup, managedPropertyTypeLookup, scopeFilter);
            var fql = helper.GetFqlFromKql(query, SynonymHandling, BoostValue);
            return fql;
        }

        private Dictionary<string, string> GetPropertyTypeLookup(string uniqueKey)
        {
            Dictionary<string, string> propertyLookup;
            if (CacheMinutes == 0 || HttpContext.Current.Cache["props" + uniqueKey] == null)
            {
                propertyLookup = new Dictionary<string, string>();
                FastManagedPropertyReader.PopulateManagedProperties(propertyLookup);
                HttpContext.Current.Cache.Add("scopes" + uniqueKey, propertyLookup, null, DateTime.UtcNow.AddMinutes(5), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
            }
            else
            {
                propertyLookup = (Dictionary<string, string>)HttpContext.Current.Cache["props" + uniqueKey];
            }
            return propertyLookup;
        }

        private Dictionary<string, string> GetScopeLookup(string uniqueKey)
        {
            Dictionary<string, string> scopeLookup;
            if (CacheMinutes == 0 || HttpContext.Current.Cache["scopes" + uniqueKey] == null)
            {
                scopeLookup = new Dictionary<string, string>();
                FastScopeReader.PopulateScopes(scopeLookup);    
                HttpContext.Current.Cache.Add("scopes" + uniqueKey, scopeLookup, null, DateTime.UtcNow.AddMinutes(5), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
            }
            else
            {
                scopeLookup = (Dictionary<string, string>)HttpContext.Current.Cache["scopes" + uniqueKey];
            }
            return scopeLookup;
        }

        private Dictionary<string, List<string>> GetSynonymLookup(string uniqueKey)
        {
            Dictionary<string, List<string>> synonymLookup;
            if (CacheMinutes == 0 || HttpContext.Current.Cache["synonyms" + uniqueKey] == null)
            {
                synonymLookup = new Dictionary<string, List<string>>();
                FastSynonymReader.PopulateSynonyms(synonymLookup);
                HttpContext.Current.Cache.Add("synonyms" + uniqueKey, synonymLookup, null, DateTime.UtcNow.AddMinutes(5), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);                
            }
            else
            {
                synonymLookup = (Dictionary<string, List<string>>)HttpContext.Current.Cache["synonyms" + uniqueKey];
            }
            return synonymLookup;
        }
    }
}
