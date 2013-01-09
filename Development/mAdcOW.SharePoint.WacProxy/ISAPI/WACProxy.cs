using System.Web;

using Microsoft.SharePoint;

namespace mAdcOW.SharePoint
{
    public class WACProxy : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                var proxy = new Microsoft.Office.Server.Search.Extended.Query.Internal.UI.WACProxy();
                proxy.ProcessRequest(context);
            });

        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
