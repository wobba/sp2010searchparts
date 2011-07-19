using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Search.Extended.Administration;
using Microsoft.SharePoint.Search.Extended.Administration.Schema;
using ManagedProperty = Microsoft.SharePoint.Search.Extended.Administration.Schema.ManagedProperty;

namespace mAdcOW.SharePoint.Search
{
    /// <summary>
    /// Read in all managed properties and store the data type
    /// Used for building fql with the correct data types
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// </summary>
    internal class FastManagedPropertyReader
    {
        public static void PopulateManagedProperties(Dictionary<string, string> propertyLookup)
        {
            SPSecurity.RunWithElevatedPrivileges(
                delegate
                {
                    var ssaProxy =
                        (SearchServiceApplicationProxy)
                        SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);
                    if (ssaProxy.FASTAdminProxy != null)
                    {
                        var fastProxy = ssaProxy.FASTAdminProxy;

                        SchemaContext schemaContext = fastProxy.SchemaContext;
                        foreach (ManagedProperty property in
                            schemaContext.Schema.AllManagedProperties.Where(property => property.Queryable))
                        {
                            propertyLookup.Add(property.Name.ToLower(), GetFqlType(property.Type));
                        }
                    }
                }
                );
        }

        private static string GetFqlType(ManagedType type)
        {
            switch (type)
            {
                case ManagedType.Boolean:
                case ManagedType.Integer:
                    return "int";
                case ManagedType.Datetime:
                    return "datetime";
                default:
                    return "string";
            }
        }
    }
}