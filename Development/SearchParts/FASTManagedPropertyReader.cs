using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Search.Extended.Administration;
using Microsoft.SharePoint.Search.Extended.Administration.Schema;
using ManagedProperty = Microsoft.SharePoint.Search.Extended.Administration.Schema.ManagedProperty;

namespace mAdcOW.SharePoint.Search
{
    class FastManagedPropertyReader
    {
        public static void PopulateManagedProperties(Dictionary<string, string> propertyLookup)
        {
            SPSecurity.RunWithElevatedPrivileges(
                delegate
                {
                    try
                    {
                        var ssaProxy = (SearchServiceApplicationProxy)SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);
                        if (ssaProxy.FASTAdminProxy != null)
                        {
                            var fastProxy = ssaProxy.FASTAdminProxy;

                            SchemaContext schemaContext = fastProxy.SchemaContext;
                            foreach (ManagedProperty property in schemaContext.Schema.AllManagedProperties)
                            {
                                propertyLookup.Add(property.Name.ToLower(), GetFqlType(property.Type));
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

        private static string GetFqlType(ManagedType type)
        {
            switch (type)
            {                
                case ManagedType.Boolean:
                case ManagedType.Integer:
                    return "int";
                case ManagedType.Datetime:
                    return "datetime";
                case ManagedType.Text:
                default:
                    return "string";
            }
        }
    }
}
