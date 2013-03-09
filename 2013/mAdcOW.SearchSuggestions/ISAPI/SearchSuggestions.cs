using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Claims;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Query;
using Microsoft.SharePoint;

namespace mAdcOW.SearchSuggestions
{
    [Guid("10E2F9EC-CDEB-4F09-BB1A-9ECEAF360F40")]
    public class QuerySuggestionsHttpHandler : IHttpHandler
    {
        static readonly Regex ReCleanTags = new Regex("<.*?>", RegexOptions.Compiled);

        private static bool IsUserAnonymous
        {
            get
            {
                HttpContext current = HttpContext.Current;
                if (current != null && current.User != null && current.User.Identity is WindowsIdentity)
                {
                    var identity = (WindowsIdentity)current.User.Identity;
                    return identity.IsAnonymous;
                }
                return current != null && current.User != null && current.User.Identity is IClaimsIdentity && !current.User.Identity.IsAuthenticated;
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod != "GET") return;

            string relativeUri = context.Request.QueryString["url"];
            string query = context.Request.QueryString["query"];
            string language = context.Request.QueryString["language"];
            string sourceId = context.Request.QueryString["sourceId"];
            int numberOfQuerySuggestions;
            int.TryParse(context.Request.QueryString["numberOfQuerySuggestions"], out numberOfQuerySuggestions);
            int numberOfResultSuggestions;
            int.TryParse(context.Request.QueryString["numberOfResultSuggestions"], out numberOfResultSuggestions);
            bool preQuerySuggestions;
            bool.TryParse(context.Request.QueryString["preQuerySuggestions"], out preQuerySuggestions);
            bool hitHighlighting;
            bool.TryParse(context.Request.QueryString["hitHighlighting"], out hitHighlighting);
            bool showPeopleNameSuggestions;
            bool.TryParse(context.Request.QueryString["showPeopleNameSuggestions"], out showPeopleNameSuggestions);
            bool capitalizeFirstLetters;
            bool.TryParse(context.Request.QueryString["capitalizeFirstLetters"], out capitalizeFirstLetters);
            bool prefixMatchAllTerms;
            bool.TryParse(context.Request.QueryString["prefixMatchAllTerms"], out prefixMatchAllTerms);

            var uri = new Uri(context.Request.Url, relativeUri);

            using (var site = new SPSite(uri.AbsoluteUri))
            {
                ISearchServiceApplication ssaProxy = SearchServiceApplicationProxy.GetProxy(SPServiceContext.GetContext(site));
                
                if(string.IsNullOrWhiteSpace(relativeUri))
                {
                    relativeUri = "/";
                }

                using (SPWeb web = site.OpenWeb(relativeUri))
                {
                    if (!string.IsNullOrWhiteSpace(context.Request.QueryString["trimsuggestions"]))
                    {
                        bool trim;
                        bool.TryParse(context.Request.QueryString["trimsuggestions"], out trim);

                        web.SetProperty("mAdcOWQuerySuggestions_TrimSuggestions", trim);
                        context.Response.Write("Security trimming og search suggestions: " + trim);
                        return;
                    }

                    // Make sure SPContect.Current works from ajax
                    if (SPContext.Current == null) HttpContext.Current.Items["HttpHandlerSPWeb"] = web;

                    var qp = GetQueryProperties(query, showPeopleNameSuggestions, sourceId, language);

                    if (IsUserAnonymous)
                    {
                        numberOfResultSuggestions = 0;
                    }
                    QuerySuggestionResults results = ssaProxy.GetQuerySuggestionsWithResults(qp,
                                                                                             numberOfQuerySuggestions,
                                                                                             numberOfResultSuggestions,
                                                                                             preQuerySuggestions,
                                                                                             hitHighlighting,
                                                                                             capitalizeFirstLetters,
                                                                                             prefixMatchAllTerms);

                    bool trimSuggestions;
                    bool.TryParse(web.GetProperty("mAdcOWQuerySuggestions_TrimSuggestions") + "", out trimSuggestions);
                    if (trimSuggestions)
                    {
                        results.Queries = SecurityTrimSearchSuggestions(results.Queries, web, qp.SourceId, qp.Culture);
                    }

                    var serializer = new JavaScriptSerializer();
                    context.Response.ContentType = "application/json";
                    context.Response.Write(serializer.Serialize(results));
                }
            }
        }

        private static QuerySuggestionQuery[] SecurityTrimSearchSuggestions(QuerySuggestionQuery[] queries, SPWeb web, Guid sourceId, CultureInfo culture)
        {
            Dictionary<string, Query> keywordQueries = new Dictionary<string, Query>();
            try
            {
                foreach (var suggestion in queries)
                {
                    KeywordQuery q = new KeywordQuery(web)
                    {
                        QueryText = ReCleanTags.Replace(suggestion.Query, string.Empty),
                        SourceId = sourceId,
                        RowLimit = 1,
                        EnableStemming = true,
                        UserContextGroupID = web.ID.ToString(),
                        Culture = culture,
                        EnableQueryRules = true
                    };
                    q.SelectProperties.Clear();
                    keywordQueries.Add(suggestion.Query, q);
                }
                SearchExecutor se = new SearchExecutor();
                var results = se.ExecuteQueries(keywordQueries, true);

                List<QuerySuggestionQuery> securedQueries = new List<QuerySuggestionQuery>(queries);
                foreach (KeyValuePair<string, ResultTableCollection> result in results)
                {
                    // No result tables
                    if (result.Value.Count == 0)
                    {
                        RemoveResult(securedQueries, result);
                        continue;
                    }

                    // All tables show empty results
                    if (result.Value.All(resultTable => resultTable.RowCount == 0))
                    {
                        RemoveResult(securedQueries, result);
                    }
                }
                return securedQueries.ToArray();
            }
            finally
            {
                foreach (var query in keywordQueries)
                {
                    query.Value.Dispose();
                }
            }
        }

        private static QueryProperties GetQueryProperties(string query, bool showPeopleNameSuggestions, string sourceId, string language)
        {
            QueryProperties qp = new KeywordQueryProperties();
            qp.QueryText = query;
            qp.ShowPeopleNameSuggestions = showPeopleNameSuggestions;

            Guid guid;
            if (Guid.TryParse(sourceId, out guid))
            {
                qp.SourceId = guid;
            }

            int lcid;
            if (int.TryParse(language, out lcid))
            {
                qp.Culture = new CultureInfo(lcid);
            }
            return qp;
        }

        private static void RemoveResult(List<QuerySuggestionQuery> securedQueries, KeyValuePair<string, ResultTableCollection> result)
        {
            var removeQuery = securedQueries.Single(q => q.Query == result.Key);
            securedQueries.Remove(removeQuery);
        }
    }
}