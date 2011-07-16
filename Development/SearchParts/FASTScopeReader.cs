using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.Query;
using Microsoft.SharePoint;

namespace mAdcOW.SharePoint.Search
{
    class FastScopeReader
    {
        public static void PopulateScopes(Dictionary<string, string> scopeLookup)
        {
            SPSecurity.RunWithElevatedPrivileges(
                delegate
                {
                    try
                    {
                        var ssaProxy = (SearchServiceApplicationProxy)SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);

                        var searchApplictionInfo = ssaProxy.GetSearchServiceApplicationInfo();
                        var searchApplication = SearchService.Service.SearchApplications.GetValue<SearchServiceApplication>(searchApplictionInfo.SearchServiceApplicationId);
                        Scopes scopes = new Scopes(searchApplication);
                        foreach (Scope scope in scopes.GetScopesForSite(null))
                        {
                            if (!string.IsNullOrEmpty(scope.Filter))
                            {
                                scopeLookup[scope.Name.ToLower()] = scope.Filter;
                            }
                        }
                    }
                    catch (SecurityException secEx)
                    {
                        throw;
                    }
                }
                );

        }
    }
}
