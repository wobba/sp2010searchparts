using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using Microsoft.Office.Server.Search.ContentProcessingEnrichment;
using Microsoft.Office.Server.Search.ContentProcessingEnrichment.PropertyTypes;

//http://msdn.microsoft.com/en-us/library/jj163982(v=office.15).aspx
namespace ContentProcessingEnrichmentService
{
    public class ContentProcessingEnrichmentService : IContentProcessingEnrichmentService
    {
        static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        static Dictionary<string, string> _cache = new Dictionary<string, string>();

        // Defines the name of the managed property 'Author'
        private const string AuthorProperty = "Author";

        // Defines the error code for managed properties with an unexpected type.
        private const int UnexpectedType = 1;

        // Defines the error code for encountering unexpected exceptions.
        private const int UnexpectedError = 2;

        private readonly ProcessedItem _processedItemHolder = new ProcessedItem
                                                                  {
                                                                      ItemProperties = new List<AbstractProperty>()
                                                                  };

        #region IContentProcessingEnrichmentService Members

        public ProcessedItem ProcessItem(Item item)
        {
            _processedItemHolder.ErrorCode = 0;
            _processedItemHolder.ItemProperties.Clear();
            try
            {
                //var path = item.ItemProperties.FirstOrDefault(p => p.Name.Equals("path", StringComparison.OrdinalIgnoreCase)) as Property<string>;
                //if (!string.IsNullOrEmpty(path.Value) && path.Value.ToLower().Contains("chapter"))
                //{
                //    int a = 0;
                //}

                var author = item.ItemProperties.FirstOrDefault(p => p.Name.Equals(AuthorProperty, StringComparison.OrdinalIgnoreCase)) as Property<List<string>>;
                if (author == null)
                {
                    // The author property was not of the expected type.
                    // Update the error code and return. 
                    //_processedItemHolder.ErrorCode = UnexpectedType;
                    return _processedItemHolder;
                }
                string authorString = author.Value.First();

                string department;
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (!_cache.TryGetValue(authorString, out department))
                    {
                        department = GetDepartmentFromAuthor(authorString);
                        _lock.EnterWriteLock();
                        _cache.Add(authorString, department);
                        _lock.ExitWriteLock();
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                    
                }

                Property<string> departmentProperty = new Property<string> {Name = "Department", Value = department};
                _processedItemHolder.ItemProperties.Add(departmentProperty);
            }
            catch (Exception)
            {
                _processedItemHolder.ErrorCode = UnexpectedError;
            }
            return _processedItemHolder;
        }

        public static string GetDepartmentFromAuthor(string authorString)
        {
            PrincipalContext principalContext = new PrincipalContext(ContextType.Domain);

            UserPrincipal user = new UserPrincipal(principalContext) {DisplayName = authorString};
            string department = FindDepartmentForPrincipal(user);

            if (string.IsNullOrEmpty(department))
            {
                user = new UserPrincipal(principalContext) {Name = authorString};
                department = FindDepartmentForPrincipal(user);
            }

            if (string.IsNullOrEmpty(department))
            {
                user = new UserPrincipal(principalContext) {SamAccountName = authorString};
                department = FindDepartmentForPrincipal(user);
            }

            return department;
        }

        private static string FindDepartmentForPrincipal(UserPrincipal user)
        {
            string department = string.Empty;
            PrincipalSearcher searcher = new PrincipalSearcher(user);
            var directorySearcher = (DirectorySearcher)searcher.GetUnderlyingSearcher();
            directorySearcher.PropertiesToLoad.Add("department");
            foreach (UserPrincipal found in searcher.FindAll())
            {
                department = found.GetDepartment();
                break;
            }
            return department;
        }

        #endregion
    }

    public static class AccountManagementExtensions
    {

        public static String GetProperty(this Principal principal, String property)
        {
            DirectoryEntry directoryEntry = (DirectoryEntry)principal.GetUnderlyingObject();

            return directoryEntry.Properties.Contains(property) ? directoryEntry.Properties[property].Value.ToString() : String.Empty;
        }

        public static String GetCompany(this Principal principal)
        {
            return principal.GetProperty("company");
        }

        public static String GetDepartment(this Principal principal)
        {
            return principal.GetProperty("department");
        }

    }
}