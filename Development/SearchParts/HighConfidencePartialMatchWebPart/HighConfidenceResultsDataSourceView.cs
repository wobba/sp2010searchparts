using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using System.Web.UI;
using System.Xml;
using System.Xml.XPath;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint.Security;

namespace mAdcOW.SharePoint.Search
{
    public class HighConfidenceResultsDataSourceView : SearchResultsBaseDatasourceView
    {
        // Fields
        private bool _resultsFetched;
        private XmlDocument _resultsXmlDoc;
        public bool ExactTermMatching { get; set; }

        // Methods
        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true),
         AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
        public HighConfidenceResultsDataSourceView(HighConfidenceResultsDataSource dataSourceOwner, string viewName)
            : base(dataSourceOwner, viewName)
        {
            if (dataSourceOwner == null)
            {
                throw new ArgumentNullException("dataSourceOwner");
            }
            HighConfidenceResultsDataSource source = base.DataSourceOwner as HighConfidenceResultsDataSource;
            if (source == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            HighConfidencePartialMatchWebPart parentWebpart = source.ParentWebpart as HighConfidencePartialMatchWebPart;
            if (parentWebpart == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            base.QueryManager = SharedQueryManager.GetInstance(parentWebpart.Page, parentWebpart.QueryID).QueryManager;
        }

        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true),
         AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
        public override XPathNavigator GetXPathNavigator(DataSourceSelectArguments selectArguments)
        {
            if (ResultsXml == null)
            {
                return null;
            }
            return ResultsXml.CreateNavigator();
        }

        [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true),
         AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
        public override void SetPropertiesOnQdra()
        {
        }

        // Properties
        public XmlDocument ResultsXml
        {
            [SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true),
             AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
            get
            {
                if (_resultsFetched)
                {
                    return _resultsXmlDoc;
                }
                _resultsFetched = true;
                const TokenType allowedTokenTypes = TokenType.Phrase | TokenType.Word;
                TokenBuilder builder = new TokenBuilder(base.QueryManager.UserQuery, allowedTokenTypes);
                builder.Build();
                List<string> words = new List<string>();
                words.AddRange(builder.AndExpr.Select(t => t.Text.Trim('"').ToLower()));
                
                words.Add(string.Join(" ", builder.AndExpr.Select(t => t.Text.Trim('"').ToLower()).ToArray()));
                words.AddRange(builder.OrExpr.Select(t => t.Text.Trim('"').ToLower()));
                string bestBetXml = FastBestBetsReader.CreateBestBetXml(words, ExactTermMatching);
                _resultsXmlDoc = new XmlDocument();
                _resultsXmlDoc.LoadXml(bestBetXml);
                return _resultsXmlDoc;
            }
        }
    }
}