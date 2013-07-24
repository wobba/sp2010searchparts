using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        static readonly Regex _reNonChar = new Regex(@"\W", RegexOptions.Compiled);
        public static string CreateBestBetXml(List<string> queryWords, bool exactMatchOnTerms)
        {
            List<string> bestBets = new List<string>();
            List<string> termDefs = new List<string>();

            SPSecurity.RunWithElevatedPrivileges(
                delegate
                {
                    var ssaProxy = (SearchServiceApplicationProxy)SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);
                    if (ssaProxy.FASTAdminProxy != null)
                    {
                        var fastProxy = ssaProxy.FASTAdminProxy;

                        KeywordContext keywordContext = fastProxy.KeywordContext;
                        SearchSettingGroupCollection searchSettingGroupCollection = keywordContext.SearchSettingGroups;

                        DateTime currentDate = DateTime.Now;

                        string fullQuery = string.Join(" ", queryWords.ToArray());

                        foreach (SearchSettingGroup searchSettingGroup in searchSettingGroupCollection)
                        {
                            foreach (Keyword keyword in searchSettingGroup.Keywords)
                            {
                                List<string> terms = exactMatchOnTerms ? GetFullTermAndSynonymWords(keyword) : GetPartialTermAndSynonymWords(keyword);

                                foreach (string bestBetTerms in terms)
                                {
                                    //TODO: fullquery - check any combination with exact match on the best bet

                                    if( !_reNonChar.IsMatch(bestBetTerms)) //a-z only
                                    {
                                        Regex reBoundaryMatch = new Regex(@"\b" + bestBetTerms + @"\b");
                                        if( !reBoundaryMatch.IsMatch(fullQuery)) continue;
                                    }

                                    if (!queryWords.Contains(bestBetTerms)) continue;

                                    string termDef = GetTermDefXml(keyword);
                                    if (!string.IsNullOrEmpty(termDef) && !termDefs.Contains(termDef))
                                    {
                                        termDefs.Add(termDef);
                                    }

                                    foreach (BestBet bestBet in keyword.BestBets)
                                    {
                                        if (bestBet.StartDate < currentDate || bestBet.EndDate > currentDate)
                                            continue;

                                        string xml = BuildBestBetXml(keyword, bestBet);
                                        if (!bestBets.Contains(xml)) bestBets.Add(xml);
                                    }
                                }
                            }
                        }
                    }
                }
                );

            return "<All_Results>" + string.Join("", termDefs.ToArray()) + "<BestBetResults>" + string.Join("", bestBets.ToArray()) + "</BestBetResults></All_Results>";
        }

        private static string GetTermDefXml(Keyword keyword)
        {
            if (string.IsNullOrEmpty(keyword.Term) || string.IsNullOrEmpty(keyword.Definition)) return string.Empty;
            return string.Format(
                "<SpecialTermInformation><Keyword>{0}</Keyword><Description>{1}</Description></SpecialTermInformation>",
                SPHttpUtility.HtmlEncode(keyword.Term), SPHttpUtility.HtmlEncode(keyword.Definition));
        }

        private static string BuildBestBetXml(Keyword keyword, BestBet bestBet)
        {
            return string.Format(
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
		</Result>",
                1, SPHttpUtility.HtmlEncode(bestBet.Name), SPHttpUtility.HtmlEncode(bestBet.Description),
                bestBet.Uri.OriginalString, SPHttpUtility.HtmlEncode(keyword.Term));
        }

        private static List<string> GetFullTermAndSynonymWords(Keyword keyword)
        {
            List<string> terms = new List<string>(10);
            if (!string.IsNullOrEmpty(keyword.Term)) terms.Add(keyword.Term.ToLower());
            DateTime currentDate = DateTime.Now;
            foreach (Synonym synonym in keyword.Synonyms)
            {
                if (synonym.StartDate < currentDate || synonym.EndDate > currentDate) continue;
                if (!string.IsNullOrEmpty(synonym.Term)) terms.Add(synonym.Term.ToLower());
            }
            return terms;
        }

        private static List<string> GetPartialTermAndSynonymWords(Keyword keyword)
        {            
            List<string> individualTerms = new List<string>(10);
            if (!string.IsNullOrEmpty(keyword.Term)) AddSingleWordTerms(individualTerms, keyword.Term);
            DateTime currentDate = DateTime.Now;
            foreach (Synonym synonym in keyword.Synonyms)
            {
                if (synonym.StartDate < currentDate || synonym.EndDate > currentDate) continue;
                if( !string.IsNullOrEmpty(synonym.Term)) AddSingleWordTerms(individualTerms, synonym.Term);
            }
            return individualTerms;
        }

        private static void AddSingleWordTerms(List<string> individualTerms, string term)
        {
            term = term.ToLower();
            individualTerms.Add(term);
            var terms = term.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length > 1)
            {
                individualTerms.AddRange(terms);
            }
        }
    }
}
