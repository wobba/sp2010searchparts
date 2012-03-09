using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Search.Extended.Administration;
using Microsoft.SharePoint.Search.Extended.Administration.Keywords;
using Microsoft.SharePoint.Utilities;
using BestBet = Microsoft.SharePoint.Search.Extended.Administration.Keywords.BestBet;
using Keyword = Microsoft.SharePoint.Search.Extended.Administration.Keywords.Keyword;
using Synonym = Microsoft.SharePoint.Search.Extended.Administration.Keywords.Synonym;

namespace mAdcOW.SharePoint.Search
{
    class FastBestBetsReader
    {
        public static string CreateBestBetXml(List<string> words)
        {
            StringBuilder termBuilder = new StringBuilder();
            StringBuilder bestBetBuilder = new StringBuilder();

            SPSecurity.RunWithElevatedPrivileges(
                delegate
                {
                    try
                    {
                        var ssaProxy = (SearchServiceApplicationProxy)SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);
                        if (ssaProxy.FASTAdminProxy != null)
                        {
                            var fastProxy = ssaProxy.FASTAdminProxy;

                            KeywordContext keywordContext = fastProxy.KeywordContext;
                            SearchSettingGroupCollection searchSettingGroupCollection = keywordContext.SearchSettingGroups;

                            DateTime currentDate = DateTime.Now;

                            int bbCount = 1;

                            foreach (SearchSettingGroup searchSettingGroup in searchSettingGroupCollection)
                            {
                                foreach (Keyword keyword in searchSettingGroup.Keywords)
                                {
                                    if (words.Contains(keyword.Term.ToLower()))
                                    {
                                        termBuilder.AppendFormat(
                                            "<SpecialTermInformation><Keyword>{0}</Keyword><Description>{1}</Description></SpecialTermInformation>",
                                            keyword.Term, SPHttpUtility.HtmlEncode(keyword.Definition));

                                        foreach (BestBet bestBet in keyword.BestBets)
                                        {
                                            if (bestBet.StartDate < currentDate || bestBet.EndDate > currentDate)
                                                continue;
                                            bestBetBuilder.AppendFormat(
                                                @"
                                            <Result>
			<id>{0}</id>
			<title>{1}</title>
			<description>{2}</description>
			<url>{3}</url>
			<urlEncoded>{3}</urlEncoded>
			<teaserContentType/>
			<FS14Description/>
			<keyword>KD[{4}]</keyword>
		</Result>", bbCount++, bestBet.Name, bestBet.Description, bestBet.Uri.OriginalString, keyword.Term);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (SecurityException secEx)
                    {
                        throw;
                    }
                }
                );

            return "<All_Results>" + termBuilder + "<BestBetResults>" + bestBetBuilder + "</BestBetResults></All_Results>";
        }
    }
}
