using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.UI.WebControls.WebParts;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint;

namespace mAdcOW.SharePoint.Search
{
    /// <summary>
    /// FQL and synonym enabled web part
    /// Used for building fql with the correct data types
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// 
    /// The code is copied from http://neganov.blogspot.com/2011/01/extending-coreresultswebpart-to-handle.html
    /// </summary>
    [ToolboxItemAttribute(false)]
    public class FqlCoreResults : CoreResultsWebPart
    {
        private static Regex _reNonCharacter = new Regex(@"\W", RegexOptions.Compiled);
        private string _query;
        private Dictionary<string, List<string>> _synonymLookup;
        private bool _enableFql = true;
        private string _cacheKey;
        private int _cacheMinutes = 60;
        private int _boostValue = 500;
        private string _duplicateTrimProperty = "DocumentSignature";
        private SynonymHandling _synonymHandling = SynonymHandling.Include;

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Synonym handling")]
        [WebDescription("Choose to expand synonyms")]
        public SynonymHandling SynonymHandling
        {
            get { return _synonymHandling; }
            set { _synonymHandling = value; }
        }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Query Language")]
        [WebDescription("Kql or Fql")]
        [DefaultValue(QueryKind.Kql)]
        public QueryKind QueryKind { get; set; }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Original Query Boost Value")]
        [WebDescription("Boost the original entered query")]
        public int BoostValue
        {
            get { return _boostValue; }
            set { _boostValue = value; }
        }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Cache time for synonyms and scopes")]
        [WebDescription("Cache the values for specified minutes. 0=no caching")]
        public int CacheMinutes
        {
            get { return _cacheMinutes; }
            set { _cacheMinutes = value; }
        }

        [Personalizable(PersonalizationScope.Shared)]
        [WebBrowsable(true)]
        [Category("Advanced Query Options")]
        [WebDisplayName("Duplicate Trimming Property")]
        [WebDescription("Trim duplicates on a custom managed property")]
        public string DuplicateTrimProperty
        {
            get { return _duplicateTrimProperty; }
            set { _duplicateTrimProperty = value; }
        }


        protected override void ConfigureDataSourceProperties()
        {
            if (_enableFql)
            {
                // We use the FixedQuery parameter to pass inn fql
                this.FixedQuery = GetQuery();
            }            
            base.ConfigureDataSourceProperties();
        }

        protected override void CreateDataSource()
        {
            _query = HttpUtility.UrlDecode(HttpContext.Current.Request["k"]);
            _cacheKey = SPContext.Current.Site.Url;
            _synonymLookup = GetSynonymLookup(_cacheKey);
            if (IsSingleWordNoSynonyms())
            {
                // We can pass the query thru directly with no modifications
                // This will allow best bets to function
                _enableFql = false;
                this.FixedQuery = string.Empty;
            }
            else
            {
                _enableFql = true;
            }
            this.DataSource = new CoreFqlResultsDataSource(this, _enableFql, _duplicateTrimProperty);
        }

        private bool IsSingleWordNoSynonyms()
        {
            return string.IsNullOrEmpty(_query) || _query == "#" || (!_reNonCharacter.IsMatch(_query) && !_synonymLookup.ContainsKey(_query.ToLower()));
        }

        private string GetQuery()
        {
            if (string.IsNullOrEmpty(_query))
            {
                return null;
            }
            if (QueryKind == QueryKind.Fql && _enableFql) return _query;
            if (QueryKind == QueryKind.Kql && _query.ToLower().StartsWith("fql:"))
            {
                _query = _query.Substring(4);
                return _query;
            }

            return ConvertKqlToFql();
        }

        private string ConvertKqlToFql()
        {
            Dictionary<string, string> scopeLookup = GetScopeLookup(_cacheKey);
            Dictionary<string, string> managedPropertyTypeLookup = GetPropertyTypeLookup(_cacheKey);

            string scopeFilter = null;
            if (!string.IsNullOrEmpty(this.Scope)) scopeLookup.TryGetValue(this.Scope.ToLower(), out scopeFilter);

            FqlHelper helper = new FqlHelper(_synonymLookup, managedPropertyTypeLookup, scopeFilter);
            var fql = helper.GetFqlFromKql(_query, SynonymHandling, BoostValue);
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
