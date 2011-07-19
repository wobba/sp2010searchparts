using System.Collections.Generic;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.SharePoint;

namespace mAdcOW.SharePoint.Search
{
    /// <summary>
    /// Read in all fql created scopes
    /// Used for building fql with the correct data types
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// </summary>
    internal class FastScopeReader
    {
        public static void PopulateScopes(Dictionary<string, string> scopeLookup)
        {
            SPSecurity.RunWithElevatedPrivileges(
                delegate
                    {
                        var ssaProxy =
                            (SearchServiceApplicationProxy)
                            SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);

                        var searchApplictionInfo = ssaProxy.GetSearchServiceApplicationInfo();
                        var searchApplication =
                            SearchService.Service.SearchApplications.GetValue<SearchServiceApplication>(
                                searchApplictionInfo.SearchServiceApplicationId);
                        Scopes scopes = new Scopes(searchApplication);
                        foreach (Scope scope in scopes.GetScopesForSite(null))
                        {
                            if (!string.IsNullOrEmpty(scope.Filter))
                            {
                                scopeLookup[scope.Name.ToLower()] = scope.Filter;
                            }
                        }
                    }
                );
        }
    }
}