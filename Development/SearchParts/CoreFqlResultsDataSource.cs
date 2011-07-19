using Microsoft.Office.Server.Search.WebControls;

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
    /// 
    /// The code is based on code from http://neganov.blogspot.com/2011/01/extending-coreresultswebpart-to-handle.html
    /// </summary>
    public class CoreFqlResultsDataSource : CoreResultsDatasource
    {
        private const string CoreFqlResultsViewName = "CoreFqlResults";

        public CoreFqlResultsDataSource(CoreResultsWebPart parentWebPart)
            : base(parentWebPart)
        {
            // Replace default view with a custom view.
            base.View = new CoreFqlResultsDataSourceView(this, CoreFqlResultsViewName);            
        }
    }
}

