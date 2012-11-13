using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.Office.Server.Social;
using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;

namespace SearchExtension.CustomSearchWebPart
{
    [ToolboxItemAttribute(false)]
    public class CustomSearchWebPart : WebPart
    {
        protected override void OnInit(EventArgs e)
        {
            if (SPContext.Current.FormContext.FormMode.Equals(SPControlMode.Display))
            {
                WireUpDataProvider();
            }
            base.OnInit(e);
        }
        private void WireUpDataProvider()
        {
            var scriptapp = ScriptApplicationManager.GetCurrent(Page);
            var dataprovider = scriptapp.QueryGroups["Default"].DataProvider;
            dataprovider.BeforeSerializeToClient += DataProvider_BeforeSerializeToClient;
        }

        void DataProvider_BeforeSerializeToClient(object sender, ScriptWebPart.BeforeSerializeToClientEventArgs e)
        {
            DataProviderScriptWebPart dp = sender as DataProviderScriptWebPart;
            if (dp == null) return;

            List<string> authorsIFollow = new List<string>();
            List<string> sitesIFollow = new List<string>();
            SPUser targetUser = SPContext.Current.Web.CurrentUser;

            try
            {
                SPServiceContext serverContext = SPServiceContext.GetContext(SPContext.Current.Site);
                UserProfileManager profileManager = new UserProfileManager(serverContext);
                if (profileManager.UserExists(targetUser.LoginName))
                {

                    UserProfile profile = profileManager.GetUserProfile(targetUser.LoginName);
                    SPSocialFollowingManager manager = new SPSocialFollowingManager(profile);
                    SPSocialActor[] followedUsers = manager.GetFollowed(SPSocialActorTypes.Users);
                    authorsIFollow.AddRange(followedUsers.Select(f => f.Name));

                    SPSocialActor[] followedSites = manager.GetFollowed(SPSocialActorTypes.Sites);
                    sitesIFollow.AddRange(followedSites.Select(f => f.Uri.AbsoluteUri));
                }
            }
            catch (SPSocialException)
            {
                //Silence error if the user don't have a personal site
            }

            if (authorsIFollow.Count > 0)
            {
                dp.Properties["FollowedUsers"] = authorsIFollow;
            }

            if (sitesIFollow.Count > 0)
            {
                dp.Properties["FollowedSites"] = sitesIFollow;
            }
        }
    }
}
