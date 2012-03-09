using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Web;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.Office.Server.Search.Query;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;

namespace mAdcOW.SharePoint.Search
{
    [ComVisible(false), XmlRoot(Namespace = "urn:schemas-microsoft-com:HighConfidencePartialMatchWebPart"), AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal), SharePointPermission(SecurityAction.LinkDemand, ObjectModel = true)]
    public class HighConfidencePartialMatchWebPart : SearchResultsBaseWebPart
    {
        // Fields
        private int _bbLimit = 3;
        private bool _displayBestBetTitle = true;
        private bool _displayDefinition = true;
        private bool _displayDescription = true;
        private bool _displayHCDescription;
        private bool _displayHCImage = true;
        private bool _displayHCProps = true;
        private bool _displayHCTitle = true;
        private bool _displayTerm = true;
        private bool _displayUrl = true;
        private QueryId _qryId;
        private int _resultsPerTypeLimit = 1;
        private int _sharedPropertiesVersion = 3;
        private bool _forceOnInit = true;
        private bool _isSrhdcError;
        private QueryManager _qdra;

        // Methods
        private void AssignOOBXsl()
        {
            if (this.IsBestBetsOnly)
            {
                this.Xsl = string.Format(CultureInfo.InstalledUICulture, "<xsl:stylesheet version=\"1.0\" \r\n    xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" \r\n    xmlns:srwrt=\"http://schemas.microsoft.com/WebParts/v3/searchresults/runtime\"\r\n    xmlns:ddwrt=\"http://schemas.microsoft.com/WebParts/v2/DataView/runtime\">\r\n<xsl:output method=\"xml\" indent=\"no\"/>\r\n<xsl:param name=\"BBLimit\">3</xsl:param>\r\n<xsl:param name=\"DisplayDefinition\">True</xsl:param>\r\n<xsl:param name=\"DisplayDescription\">False</xsl:param>\r\n<xsl:param name=\"DisplayHCDescription\">False</xsl:param>\r\n<xsl:param name=\"DisplayHCImage\">True</xsl:param>\r\n<xsl:param name=\"DisplayHCProps\">True</xsl:param>\r\n<xsl:param name=\"DisplayHCTitle\">True</xsl:param>\r\n<xsl:param name=\"DisplayTerm\">True</xsl:param>\r\n<xsl:param name=\"DisplayTitle\">True</xsl:param>\r\n<xsl:param name=\"DisplayUrl\">True</xsl:param>\r\n<xsl:param name=\"ResultsPerTypeLimit\">1</xsl:param>\r\n<xsl:param name=\"DisplayST\">True</xsl:param>\r\n<xsl:param name=\"DisplayBB\">True</xsl:param>\r\n<xsl:param name=\"DisplayHC\">True</xsl:param>\r\n<xsl:param name=\"ResponsibilityText\" />\r\n<xsl:param name=\"SkillsText\" />\r\n<xsl:param name=\"HighConfTitle\" />\r\n<xsl:param name=\"IsFirstPage\">True</xsl:param>\r\n<xsl:param name=\"BestBetTitle\"></xsl:param>\r\n<xsl:param name=\"RecommendationText\"></xsl:param>\r\n<xsl:param name=\"IsDesignMode\">True</xsl:param>\r\n\r\n<xsl:template match=\"All_Results/SpecialTermInformation\">\r\n <xsl:variable name=\"keyword\" select=\"Keyword\" />\r\n <xsl:if test=\"$DisplayST = 'True'\" >\r\n   <xsl:if test=\"($DisplayTerm = 'True'and string-length($keyword) &gt; 0) or ($DisplayDefinition = 'True' and string-length(Description) &gt; 0)\" >\r\n     <div class=\"srch-BB-Result\"> \r\n       <xsl:if test=\"$DisplayTerm = 'True'and string-length($keyword) &gt; 0\">  \r\n         <span class=\"srch-Title\">\r\n           <img src=\"/_layouts/images/star.png\" title=\"{{$RecommendationText}}\" />\r\n           <span class=\"srch-BBTitle\">\r\n               <strong>\r\n               <xsl:value-of select=\"$keyword\"/>\r\n               </strong>\r\n           </span>\r\n         </span>\r\n       </xsl:if>\r\n       <xsl:if test=\"$DisplayDefinition = 'True'\" >\r\n         <div class=\"srch-BB-Description2\">      \r\n         <xsl:value-of disable-output-escaping=\"yes\" select=\"Description\"/>\r\n         </div>\r\n       </xsl:if>   \r\n     </div>  \r\n   </xsl:if>   \r\n </xsl:if>\r\n</xsl:template>\r\n\r\n\r\n<xsl:template match=\"All_Results/BestBetResults/Result\"> \r\n <xsl:if test=\"$DisplayBB = 'True'\" >\r\n  <xsl:if test=\"position() &lt;= $BBLimit\" >\r\n  <xsl:variable name=\"url\" select=\"url\"/>\r\n  <xsl:variable name=\"id\" select=\"id\" />\r\n  <div class=\"srch-BB-Result\">\r\n  <xsl:if test=\"$DisplayTitle = 'True'\" >\r\n    <span class=\"srch-Title\"> \r\n     <img src=\"/_layouts/images/star.png\" title=\"{{$RecommendationText}}\" />\r\n     <span class=\"srch-BBTitle\">\r\n        <!-- links with the file scheme only work in ie if they are unescaped. For  \r\n             this reason here we will render the link using disable-output-escaping if the url \r\n             begins with file.-->\r\n        <xsl:choose>\r\n          <xsl:when test=\"substring($url,1,5) = 'file:' and $IsDesignMode = 'False'\">\r\n            <xsl:text     disable-output-escaping=\"yes\">&lt;a href=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode($url)\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\" id=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode(concat('BBR_',$id))\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\" title=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode(title)\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\"&gt;</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlEncode(title)\"/>\r\n            <xsl:text     disable-output-escaping=\"yes\">&lt;/a&gt;</xsl:text>\r\n          </xsl:when>\r\n          <xsl:otherwise>\r\n            <a id=\"{{concat('BBR_',$id)}}\">\r\n              <xsl:attribute name=\"href\">\r\n                <xsl:value-of  select=\"$url\"/>\r\n              </xsl:attribute>\r\n              <xsl:attribute name=\"title\">\r\n                <xsl:value-of select=\"title\"/>\r\n              </xsl:attribute>\r\n              <xsl:value-of select=\"title\"/> \r\n            </a> \r\n          </xsl:otherwise>\r\n        </xsl:choose>\r\n     </span>\r\n    </span>\r\n  </xsl:if>\r\n\r\n  <xsl:if test=\"$DisplayDescription = 'True' and description[. != '']\" >\r\n      <div class=\"srch-BB-Description2\">\r\n      <xsl:value-of select=\"description\"/> \r\n      </div>\r\n  </xsl:if>\r\n  <xsl:if test=\"$DisplayUrl = 'True'\" >\r\n     <div class=\"srch-BB-URL3\">\r\n     <span class=\"srch-BB-URL2\">\r\n      <xsl:value-of select=\"$url\"/> \r\n     </span>\r\n     </div>\r\n  </xsl:if>\r\n  </div>\r\n  </xsl:if>\r\n </xsl:if>   \r\n</xsl:template>\r\n\r\n<xsl:template match=\"All_Results/HighConfidenceResults/Result\"> \r\n <xsl:if test=\"$DisplayHC = 'True' and $IsFirstPage = 'True'\" >\r\n  <xsl:variable name=\"prefix\">IMNRC('</xsl:variable>\r\n  <xsl:variable name=\"suffix\">')</xsl:variable>\r\n  <xsl:variable name=\"url\" select=\"url\"/>\r\n  <xsl:variable name=\"id\" select=\"id\"/>\r\n  <xsl:variable name=\"pictureurl\" select=\"highconfidenceimageurl\"/>\r\n  <xsl:variable name=\"jobtitle\" select=\"highconfidencedisplayproperty1\"/>\r\n  <xsl:variable name=\"workphone\" select=\"highconfidencedisplayproperty2\"/>\r\n  <xsl:variable name=\"department\" select=\"highconfidencedisplayproperty3\"/>\r\n  <xsl:variable name=\"officenumber\" select=\"highconfidencedisplayproperty4\"/>\r\n  <xsl:variable name=\"preferredname\" select=\"highconfidencedisplayproperty5\"/>\r\n  <xsl:variable name=\"aboutme\" select=\"highconfidencedisplayproperty8\"/>\r\n  <xsl:variable name=\"responsibility\" select=\"highconfidencedisplayproperty9\"/>\r\n  <xsl:variable name=\"skills\" select=\"highconfidencedisplayproperty10\"/>\r\n  <xsl:variable name=\"workemail\" select=\"highconfidencedisplayproperty11\"/>\r\n  <xsl:variable name=\"imgid\" select=\"concat('HSR_IMG_',$id)\"/>\r\n\r\n  <div class=\"srch-HCMain \">\r\n  <span class=\"srch-HCSocDistTitle\">\r\n    <xsl:value-of select=\"$HighConfTitle\" />\r\n  </span> \r\n  <table class=\"psrch-HCresult\" CELLPADDING=\"0\" CELLSPACING=\"0\" BORDER=\"0\" width=\"100%\">\r\n    <tr>\r\n      <td class=\"psrch-imgcell\" width=\"0%\">\r\n        <xsl:if test = \"$DisplayHCImage = 'True'\">\r\n          <table class=\"psrch-profimg\" CELLPADDING=\"0\" CELLSPACING=\"0\" BORDER=\"0\" WIDTH=\"77px\" HEIGHT=\"77px\">\r\n            <tr>\r\n              <td align=\"middle\" valign=\"middle\">\r\n                <a href=\"{{$url}}\" id=\"{{concat('HSR_IMGL_',$id)}}\" title=\"{{$url}}\">\r\n                  <img id=\"{{$imgid}}\" alt=\"{{$preferredname}}\" border=\"0\" onload=\"resizeProfileImage('{{$imgid}}')\">\r\n                    <xsl:attribute name=\"src\">\r\n                    <xsl:choose>\r\n                      <xsl:when test = \"string-length($pictureurl) &gt; 0\"><xsl:value-of select=\"$pictureurl\" /></xsl:when>\r\n                      <xsl:otherwise>/_layouts/images/no_pic.gif</xsl:otherwise>\r\n\t\t\t\t    </xsl:choose>                    \r\n\t\t\t\t    </xsl:attribute>\r\n                  </img>\r\n                  <script>\r\n                    window.setTimeout(\"resizeProfileImage('<xsl:value-of select=\"$imgid\"/>')\", 1)\r\n                  </script>\r\n                </a>\r\n              </td>\r\n            </tr>\r\n          </table>\r\n        </xsl:if>\r\n      </td>\r\n      <td valign=\"top\" class=\"psrch-propcell\" width=\"100%\">\r\n        <span class=\"psrch-Title\">\r\n          <img border=\"0\" height=\"12\" width=\"12\" src=\"/_layouts/images/imnhdr.gif\" onload=\"{{concat($prefix, $workemail, $suffix)}}\" ShowOfflinePawn=\"1\" id=\"{{concat('HSRP_',$id)}}\" />\r\n          <a href=\"{{$url}}\" id=\"{{concat('HSR_',$id)}}\">\r\n            <xsl:value-of select=\"$preferredname\"/>\r\n          </a>\r\n        </span>\r\n        <br/>\r\n        <div class=\"psrch-Description\">\r\n          <xsl:call-template name=\"DisplayOfficeProfile\">\r\n            <xsl:with-param name=\"title\" select=\"$jobtitle\" />\r\n            <xsl:with-param name=\"dep\" select=\"$department\" />\r\n            <xsl:with-param name=\"phone\" select=\"$workphone\" />\r\n            <xsl:with-param name=\"office\" select=\"$officenumber\" />\r\n          </xsl:call-template>\r\n        </div>\r\n        <div class=\"psrch-Description\">\r\n          <xsl:choose>\r\n            <xsl:when test=\"$aboutme[. != '']\">\r\n              <xsl:value-of disable-output-escaping=\"yes\" select=\"$aboutme\"/>\r\n              <br/>\r\n            </xsl:when>\r\n          </xsl:choose>\r\n          <xsl:choose>\r\n            <xsl:when test=\"$responsibility[. != ''] or $skills[. != '']\">\r\n              <xsl:choose>\r\n                <xsl:when test=\"$responsibility[. != '']\">\r\n                  <span class=\"psrch-PropLabel\">\r\n                    <xsl:value-of disable-output-escaping=\"yes\" select=\"$ResponsibilityText\"/>\r\n                  </span>\r\n                  <span class=\"psrch-PropValue\">\r\n                    <xsl:value-of select=\"translate($responsibility,';',',')\"/>\r\n                    <xsl:text> </xsl:text>\r\n                  </span>\r\n                </xsl:when>\r\n              </xsl:choose>\r\n              <xsl:choose>\r\n                <xsl:when test=\"$skills[. != '']\">\r\n                  <xsl:if test=\"$responsibility[. != ''] and $skills[. != '']\">\r\n                    <br/>\r\n                  </xsl:if>\r\n                  <span class=\"psrch-PropLabel\">\r\n                    <xsl:value-of disable-output-escaping=\"yes\" select=\"$SkillsText\"/>\r\n                  </span>\r\n                  <span class=\"psrch-PropValue\">\r\n                    <xsl:value-of select=\"translate($skills,';',',')\"/>\r\n                  </span>\r\n                </xsl:when>\r\n              </xsl:choose>\r\n              <br/>\r\n            </xsl:when>\r\n            <xsl:otherwise><span /></xsl:otherwise>\r\n          </xsl:choose>\r\n        </div>\r\n      </td>\r\n    </tr>\r\n  </table>          \r\n  </div>\r\n </xsl:if>   \r\n</xsl:template>\r\n\r\n<!-- XSL transformation starts here -->\r\n<xsl:template match=\"/\">\r\n    <xsl:call-template name=\"dvt_1.body\"/>     \r\n</xsl:template> \r\n\r\n<xsl:template name=\"DisplayOfficeProfile\">\r\n  <xsl:param name=\"title\" />\r\n  <xsl:param name=\"dep\" />\r\n  <xsl:param name=\"phone\" />\r\n  <xsl:param name=\"office\" />\r\n\r\n  <span class=\"psrch-Metadata\">\r\n  <xsl:if test='string-length($title) &gt; 0'>   \r\n   <xsl:value-of select=\"$title\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($dep) &gt; 0'>   \r\n   <xsl:value-of select=\"$dep\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($phone) &gt; 0'>   \r\n   <xsl:value-of select=\"$phone\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($office) &gt; 0'>   \r\n   <xsl:value-of select=\"$office\" />  \r\n  </xsl:if>\r\n  </span>\r\n  <br/>\r\n</xsl:template>\r\n\r\n\r\n<xsl:template name=\"dvt_1.body\">\r\n<xsl:if test=\"(/*/*)\">\r\n  <div class=\"srch-BestBets\">\r\n    <xsl:if test=\"$IsFirstPage = 'True'\" >\r\n      <xsl:apply-templates />\r\n      <xsl:if test=\"string-length($BestBetTitle) &gt; 0\">\r\n        <div class=\"srch-BestBetsBottom\">\r\n        <div class=\"srch-BestBetsBottom2\">\r\n           <img alt=\"\" src=\"/_layouts/images/blank.gif\" />\r\n        </div>\r\n        </div>\r\n      </xsl:if>\r\n    </xsl:if>\r\n  </div>\r\n</xsl:if>\r\n</xsl:template>\r\n\r\n\r\n<!-- End of Stylesheet -->\r\n</xsl:stylesheet>", new object[0]);
            }
            else
            {
                this.Xsl = string.Format(CultureInfo.InvariantCulture, "<xsl:stylesheet version=\"1.0\" \r\n    xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" \r\n    xmlns:srwrt=\"http://schemas.microsoft.com/WebParts/v3/searchresults/runtime\"\r\n    xmlns:ddwrt=\"http://schemas.microsoft.com/WebParts/v2/DataView/runtime\">\r\n<xsl:output method=\"xml\" indent=\"no\"/>\r\n<xsl:param name=\"BBLimit\">3</xsl:param>\r\n<xsl:param name=\"DisplayDefinition\">True</xsl:param>\r\n<xsl:param name=\"DisplayDescription\">False</xsl:param>\r\n<xsl:param name=\"DisplayHCDescription\">False</xsl:param>\r\n<xsl:param name=\"DisplayHCImage\">True</xsl:param>\r\n<xsl:param name=\"DisplayHCProps\">True</xsl:param>\r\n<xsl:param name=\"DisplayHCTitle\">True</xsl:param>\r\n<xsl:param name=\"DisplayTerm\">True</xsl:param>\r\n<xsl:param name=\"DisplayTitle\">True</xsl:param>\r\n<xsl:param name=\"DisplayUrl\">True</xsl:param>\r\n<xsl:param name=\"ResultsPerTypeLimit\">1</xsl:param>\r\n<xsl:param name=\"DisplayST\">True</xsl:param>\r\n<xsl:param name=\"DisplayBB\">True</xsl:param>\r\n<xsl:param name=\"DisplayHC\">True</xsl:param>\r\n<xsl:param name=\"ResponsibilityText\" />\r\n<xsl:param name=\"SkillsText\" />\r\n<xsl:param name=\"HighConfTitle\" />\r\n<xsl:param name=\"IsFirstPage\">True</xsl:param>\r\n<xsl:param name=\"BestBetTitle\"></xsl:param>\r\n<xsl:param name=\"RecommendationText\"></xsl:param>\r\n<xsl:param name=\"IsDesignMode\">True</xsl:param>\r\n\r\n<xsl:template match=\"All_Results/SpecialTermInformation\">\r\n <xsl:variable name=\"keyword\" select=\"Keyword\" />\r\n <xsl:if test=\"$DisplayST = 'True'\" >\r\n   <xsl:if test=\"($DisplayTerm = 'True'and string-length($keyword) &gt; 0) or ($DisplayDefinition = 'True' and string-length(Description) &gt; 0)\" >\r\n     <div class=\"srch-BB-Result\"> \r\n       <xsl:if test=\"$DisplayTerm = 'True'and string-length($keyword) &gt; 0\">  \r\n         <span class=\"srch-Title\">\r\n           <img src=\"/_layouts/images/star.png\" title=\"{{$RecommendationText}}\" />\r\n           <span class=\"srch-BBTitle\">\r\n               <strong>\r\n               <xsl:value-of select=\"$keyword\"/>\r\n               </strong>\r\n           </span>\r\n         </span>\r\n       </xsl:if>\r\n       <xsl:if test=\"$DisplayDefinition = 'True'\" >\r\n         <div class=\"srch-BB-Description2\">      \r\n         <xsl:value-of disable-output-escaping=\"yes\" select=\"Description\"/>\r\n         </div>\r\n       </xsl:if>   \r\n     </div>  \r\n   </xsl:if>   \r\n </xsl:if>\r\n</xsl:template>\r\n\r\n\r\n<xsl:template match=\"All_Results/BestBetResults/Result\"> \r\n <xsl:if test=\"$DisplayBB = 'True'\" >\r\n  <xsl:if test=\"position() &lt;= $BBLimit\" >\r\n  <xsl:variable name=\"url\" select=\"url\"/>\r\n  <xsl:variable name=\"id\" select=\"id\" />\r\n  <div class=\"srch-BB-Result\">\r\n  <xsl:if test=\"$DisplayTitle = 'True'\" >\r\n    <span class=\"srch-Title\"> \r\n     <img src=\"/_layouts/images/star.png\" title=\"{{$RecommendationText}}\" />\r\n     <span class=\"srch-BBTitle\">\r\n        <!-- links with the file scheme only work in ie if they are unescaped. For  \r\n             this reason here we will render the link using disable-output-escaping if the url \r\n             begins with file.-->\r\n        <xsl:choose>\r\n          <xsl:when test=\"substring($url,1,5) = 'file:' and $IsDesignMode = 'False'\">\r\n            <xsl:text     disable-output-escaping=\"yes\">&lt;a href=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode($url)\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\" id=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode(concat('BBR_',$id))\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\" title=\"</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlAttributeEncode(title)\" />\r\n            <xsl:text     disable-output-escaping=\"yes\">\"&gt;</xsl:text>\r\n            <xsl:value-of disable-output-escaping=\"yes\" select=\"srwrt:HtmlEncode(title)\"/>\r\n            <xsl:text     disable-output-escaping=\"yes\">&lt;/a&gt;</xsl:text>\r\n          </xsl:when>\r\n          <xsl:otherwise>\r\n            <a id=\"{{concat('BBR_',$id)}}\">\r\n              <xsl:attribute name=\"href\">\r\n                <xsl:value-of  select=\"$url\"/>\r\n              </xsl:attribute>\r\n              <xsl:attribute name=\"title\">\r\n                <xsl:value-of select=\"title\"/>\r\n              </xsl:attribute>\r\n              <xsl:value-of select=\"title\"/> \r\n            </a> \r\n          </xsl:otherwise>\r\n        </xsl:choose>\r\n     </span>\r\n    </span>\r\n  </xsl:if>\r\n\r\n  <xsl:if test=\"$DisplayDescription = 'True' and description[. != '']\" >\r\n      <div class=\"srch-BB-Description2\">\r\n      <xsl:value-of select=\"description\"/> \r\n      </div>\r\n  </xsl:if>\r\n  <xsl:if test=\"$DisplayUrl = 'True'\" >\r\n     <div class=\"srch-BB-URL3\">\r\n     <span class=\"srch-BB-URL2\">\r\n      <xsl:value-of select=\"$url\"/> \r\n     </span>\r\n     </div>\r\n  </xsl:if>\r\n  </div>\r\n  </xsl:if>\r\n </xsl:if>   \r\n</xsl:template>\r\n\r\n<xsl:template match=\"All_Results/HighConfidenceResults/Result\"> \r\n <xsl:if test=\"$DisplayHC = 'True' and $IsFirstPage = 'True'\" >\r\n  <xsl:variable name=\"prefix\">IMNRC('</xsl:variable>\r\n  <xsl:variable name=\"suffix\">')</xsl:variable>\r\n  <xsl:variable name=\"url\" select=\"url\"/>\r\n  <xsl:variable name=\"id\" select=\"id\"/>\r\n  <xsl:variable name=\"pictureurl\" select=\"highconfidenceimageurl\"/>\r\n  <xsl:variable name=\"jobtitle\" select=\"highconfidencedisplayproperty1\"/>\r\n  <xsl:variable name=\"workphone\" select=\"highconfidencedisplayproperty2\"/>\r\n  <xsl:variable name=\"department\" select=\"highconfidencedisplayproperty3\"/>\r\n  <xsl:variable name=\"officenumber\" select=\"highconfidencedisplayproperty4\"/>\r\n  <xsl:variable name=\"preferredname\" select=\"highconfidencedisplayproperty5\"/>\r\n  <xsl:variable name=\"aboutme\" select=\"highconfidencedisplayproperty8\"/>\r\n  <xsl:variable name=\"responsibility\" select=\"highconfidencedisplayproperty9\"/>\r\n  <xsl:variable name=\"skills\" select=\"highconfidencedisplayproperty10\"/>\r\n  <xsl:variable name=\"workemail\" select=\"highconfidencedisplayproperty11\"/>\r\n  <xsl:variable name=\"imgid\" select=\"concat('HSR_IMG_',$id)\"/>\r\n\r\n  <div class=\"srch-HCMain \">\r\n  <span class=\"srch-HCSocDistTitle\">\r\n    <xsl:value-of select=\"$HighConfTitle\" />\r\n  </span> \r\n  <table class=\"psrch-HCresult\" CELLPADDING=\"0\" CELLSPACING=\"0\" BORDER=\"0\" width=\"100%\">\r\n    <tr>\r\n      <td class=\"psrch-imgcell\" width=\"0%\">\r\n        <xsl:if test = \"$DisplayHCImage = 'True'\">\r\n          <table class=\"psrch-profimg\" CELLPADDING=\"0\" CELLSPACING=\"0\" BORDER=\"0\" WIDTH=\"77px\" HEIGHT=\"77px\">\r\n            <tr>\r\n              <td align=\"middle\" valign=\"middle\">\r\n                <a href=\"{{$url}}\" id=\"{{concat('HSR_IMGL_',$id)}}\" title=\"{{$url}}\">\r\n                  <img id=\"{{$imgid}}\" alt=\"{{$preferredname}}\" border=\"0\" onload=\"resizeProfileImage('{{$imgid}}')\">\r\n                    <xsl:attribute name=\"src\">\r\n                    <xsl:choose>\r\n                      <xsl:when test = \"string-length($pictureurl) &gt; 0\"><xsl:value-of select=\"$pictureurl\" /></xsl:when>\r\n                      <xsl:otherwise>/_layouts/images/no_pic.gif</xsl:otherwise>\r\n\t\t\t\t    </xsl:choose>                    \r\n\t\t\t\t    </xsl:attribute>\r\n                  </img>\r\n                  <script>\r\n                    window.setTimeout(\"resizeProfileImage('<xsl:value-of select=\"$imgid\"/>')\", 1)\r\n                  </script>\r\n                </a>\r\n              </td>\r\n            </tr>\r\n          </table>\r\n        </xsl:if>\r\n      </td>\r\n      <td valign=\"top\" class=\"psrch-propcell\" width=\"100%\">\r\n        <span class=\"psrch-Title\">\r\n          <img border=\"0\" height=\"12\" width=\"12\" src=\"/_layouts/images/imnhdr.gif\" onload=\"{{concat($prefix, $workemail, $suffix)}}\" ShowOfflinePawn=\"1\" id=\"{{concat('HSRP_',$id)}}\" />\r\n          <a href=\"{{$url}}\" id=\"{{concat('HSR_',$id)}}\">\r\n            <xsl:value-of select=\"$preferredname\"/>\r\n          </a>\r\n        </span>\r\n        <br/>\r\n        <div class=\"psrch-Description\">\r\n          <xsl:call-template name=\"DisplayOfficeProfile\">\r\n            <xsl:with-param name=\"title\" select=\"$jobtitle\" />\r\n            <xsl:with-param name=\"dep\" select=\"$department\" />\r\n            <xsl:with-param name=\"phone\" select=\"$workphone\" />\r\n            <xsl:with-param name=\"office\" select=\"$officenumber\" />\r\n          </xsl:call-template>\r\n        </div>\r\n        <div class=\"psrch-Description\">\r\n          <xsl:choose>\r\n            <xsl:when test=\"$aboutme[. != '']\">\r\n              <xsl:value-of disable-output-escaping=\"yes\" select=\"$aboutme\"/>\r\n              <br/>\r\n            </xsl:when>\r\n          </xsl:choose>\r\n          <xsl:choose>\r\n            <xsl:when test=\"$responsibility[. != ''] or $skills[. != '']\">\r\n              <xsl:choose>\r\n                <xsl:when test=\"$responsibility[. != '']\">\r\n                  <span class=\"psrch-PropLabel\">\r\n                    <xsl:value-of disable-output-escaping=\"yes\" select=\"$ResponsibilityText\"/>\r\n                  </span>\r\n                  <span class=\"psrch-PropValue\">\r\n                    <xsl:value-of select=\"translate($responsibility,';',',')\"/>\r\n                    <xsl:text> </xsl:text>\r\n                  </span>\r\n                </xsl:when>\r\n              </xsl:choose>\r\n              <xsl:choose>\r\n                <xsl:when test=\"$skills[. != '']\">\r\n                  <xsl:if test=\"$responsibility[. != ''] and $skills[. != '']\">\r\n                    <br/>\r\n                  </xsl:if>\r\n                  <span class=\"psrch-PropLabel\">\r\n                    <xsl:value-of disable-output-escaping=\"yes\" select=\"$SkillsText\"/>\r\n                  </span>\r\n                  <span class=\"psrch-PropValue\">\r\n                    <xsl:value-of select=\"translate($skills,';',',')\"/>\r\n                  </span>\r\n                </xsl:when>\r\n              </xsl:choose>\r\n              <br/>\r\n            </xsl:when>\r\n            <xsl:otherwise><span /></xsl:otherwise>\r\n          </xsl:choose>\r\n        </div>\r\n      </td>\r\n    </tr>\r\n  </table>          \r\n  </div>\r\n </xsl:if>   \r\n</xsl:template>\r\n\r\n<!-- XSL transformation starts here -->\r\n<xsl:template match=\"/\">\r\n    <xsl:call-template name=\"dvt_1.body\"/>     \r\n</xsl:template> \r\n\r\n<xsl:template name=\"DisplayOfficeProfile\">\r\n  <xsl:param name=\"title\" />\r\n  <xsl:param name=\"dep\" />\r\n  <xsl:param name=\"phone\" />\r\n  <xsl:param name=\"office\" />\r\n\r\n  <span class=\"psrch-Metadata\">\r\n  <xsl:if test='string-length($title) &gt; 0'>   \r\n   <xsl:value-of select=\"$title\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($dep) &gt; 0'>   \r\n   <xsl:value-of select=\"$dep\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($phone) &gt; 0'>   \r\n   <xsl:value-of select=\"$phone\" />  \r\n   -\r\n  </xsl:if>\r\n  <xsl:if test='string-length($office) &gt; 0'>   \r\n   <xsl:value-of select=\"$office\" />  \r\n  </xsl:if>\r\n  </span>\r\n  <br/>\r\n</xsl:template>\r\n\r\n\r\n<xsl:template name=\"dvt_1.body\">\r\n\r\n  <xsl:apply-templates />\r\n</xsl:template>\r\n\r\n\r\n<!-- End of Stylesheet -->\r\n</xsl:stylesheet>", new object[0]);
            }
        }

        protected override bool ConnectToDataSourceControl()
        {
            return false;
        }

        protected override void CreateChildControls()
        {
            using (new SPMonitoredScope(this.Title + " CreateChildControls"))
            {
                base.CreateChildControls();
            }
        }

        protected override void CreateDataSource()
        {
            this.DataSource = new HighConfidenceResultsDataSource(this);            
        }

        public override ToolPart[] GetToolParts()
        {
            ToolPart[] toolParts = base.GetToolParts();
            if (toolParts.Length == 3)
            {
                ToolPart part = toolParts[0];
                toolParts[0] = toolParts[2];
                toolParts[2] = toolParts[1];
                toolParts[1] = part;
            }
            return toolParts;
        }

        protected override XPathNavigator GetXPathNavigator(string viewPath)
        {
            base.EnsureWebpartReady();

            //TODO: build XML with best bets here
            HighConfidenceResultsDataSource dataSource = this.DataSource as HighConfidenceResultsDataSource;
            return (dataSource.GetView() as HighConfidenceResultsDataSourceView).GetXPathNavigator(null);
        }

        protected override void ModifyXsltArgumentList(ArgumentClassWrapper argList)
        {
            argList.SetParameter("BBLimit", string.Empty, this._bbLimit);
            argList.SetParameter("DisplayDefinition", string.Empty, this._displayDefinition);
            argList.SetParameter("DisplayDescription", string.Empty, this._displayDescription);
            argList.SetParameter("DisplayHCDescription", string.Empty, this._displayHCDescription);
            argList.SetParameter("DisplayHCImage", string.Empty, this._displayHCImage);
            argList.SetParameter("DisplayHCProps", string.Empty, this._displayHCProps);
            argList.SetParameter("DisplayHCTitle", string.Empty, this._displayHCTitle);
            argList.SetParameter("DisplayTerm", string.Empty, this._displayTerm);
            argList.SetParameter("DisplayTitle", string.Empty, this._displayBestBetTitle);
            argList.SetParameter("DisplayUrl", string.Empty, this._displayUrl);
            argList.SetParameter("ResultsPerTypeLimit", string.Empty, this._resultsPerTypeLimit);
            argList.SetParameter("DisplayST", string.Empty, this._displayTerm | this._displayDefinition);
            argList.SetParameter("DisplayBB", string.Empty, (this._displayBestBetTitle | this._displayUrl) | this._displayDescription);
            argList.SetParameter("DisplayHC", string.Empty, ((this._displayHCDescription | this._displayHCImage) | this._displayHCProps) | this._displayHCTitle);
            argList.SetParameter("IsDesignMode", string.Empty, base.DesignMode ? "True" : "False");
            bool hasSpecialTermInformation = false;
            bool hasBestBetResults = false;
            HighConfidenceResultsDataSource dataSource = this.DataSource as HighConfidenceResultsDataSource;
            if (dataSource == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            HighConfidenceResultsDataSourceView view = dataSource.GetView() as HighConfidenceResultsDataSourceView;
            if (view == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (view.ResultsXml != null)
            {
                XmlNode node = view.ResultsXml.SelectSingleNode("/All_Results/SpecialTermInformation");
                if ((node != null) && node.HasChildNodes)
                {
                    hasSpecialTermInformation = true;
                }
                node = view.ResultsXml.SelectSingleNode("/All_Results/BestBetResults");
                if ((node != null) && node.HasChildNodes)
                {
                    hasBestBetResults = true;
                }
            }
            if (((this.DisplayTerm || this.DisplayDefinition) && hasSpecialTermInformation) || (((this.DisplayBestBetTitle || this.DisplayDescription) || this.DisplayUrl) && hasBestBetResults))
            {
                argList.SetParameter("BestBetTitle", string.Empty, "SearchBestBetResult_Title");
            }

            bool isfirstPage = _qdra[0].StartItem == 0;
            argList.SetParameter("IsFirstPage", string.Empty, isfirstPage);
        }

        protected override void OnInit(EventArgs e)
        {
            using (new SPMonitoredScope(this.Title + " OnInit"))
            {
                this._qdra = SharedQueryManager.GetInstance(this.Page, this.QueryID).QueryManager;
                this._forceOnInit = false;
                base.OnInit(e);
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            SPMonitoredScope scope = new SPMonitoredScope(this.Title + " OnPreRender");
            try
            {
                base.ShouldLogQuery = false;
                if (!this._isSrhdcError)
                {
                    if (SPContext.Current.Web.UIVersion == 4)
                    {
                        CssRegistration.Register("Themable/search.css");
                    }
                    else
                    {
                        CssRegistration.Register("portal.css");
                    }
                    if (!this.Page.ClientScript.IsClientScriptBlockRegistered("resizeprofileimg_peoplesearch"))
                    {
                        this.Page.ClientScript.RegisterClientScriptBlock(base.GetType(), "resizeprofileimg_peoplesearch", "\r\n<script>\r\nfunction resizeProfileImageCore(objid, maxWidth, maxHeight) {\r\n    var obj = document.getElementById(objid);\r\n    var oldResize=obj.onresize;\r\n    obj.onresize=null;\r\n    if ((obj != null) && (obj.height > 0) && (obj.width > 0)) {\r\n        try {\r\n            var ratiomax = maxHeight/maxWidth;\r\n            var ratioobj = obj.height/obj.width;\r\n\r\n            if (ratiomax > ratioobj) { // too wide\r\n                obj.width = maxWidth;\r\n            }\r\n            else { // too tall\r\n                obj.height = maxHeight;\r\n            }\r\n        }\r\n        catch (e) {\r\n        }\r\n    }\r\n    obj.onresize=oldResize;\r\n}\r\nfunction resizeProfileImage(objid) {\r\n    resizeProfileImageCore(objid, 75, 75);\r\n}\r\n</script>\r\n");
                    }
                    base.OnPreRender(e);
                }
            }
            finally
            {
                if (scope != null)
                {
                    scope.Dispose();
                }
            }
        }

        protected override void SetVisualization()
        {
            if (string.IsNullOrEmpty(this.Xsl))
            {
                this.AssignOOBXsl();
            }
        }

        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true), Resources("SearchResults_HCBBLimit", "SearchResults_HCBB", "SearchResults_HCBBLimit_ToolTip")]
        public int BestBetsLimit
        {
            get
            {
                return this._bbLimit;
            }
            set
            {
                if ((value < 0) || (value > 15))
                {
                    throw new WebPartPageUserException("Limit is between 0 and 15");
                }
                this._bbLimit = value;
                this._forceOnInit = true;
            }
        }

        [Resources("SearchResults_HCBBTitle", "SearchResults_HCBB", "SearchResults_HCBBTitle_ToolTip"), Personalizable(PersonalizationScope.Shared), WebBrowsable(true)]
        public bool DisplayBestBetTitle
        {
            get
            {
                return this._displayBestBetTitle;
            }
            set
            {
                this._displayBestBetTitle = value;
                this._forceOnInit = true;
            }
        }

        [Resources("SearchResults_HCDefinition", "SearchResults_HCSt", "SearchResults_HCDefinition_ToolTip"), WebBrowsable(true), Personalizable(PersonalizationScope.Shared)]
        public bool DisplayDefinition
        {
            get
            {
                return this._displayDefinition;
            }
            set
            {
                this._displayDefinition = value;
                this._forceOnInit = true;
            }
        }

        [WebBrowsable(true), Resources("SearchResults_HCBBDesc", "SearchResults_HCBB", "SearchResults_HCBBDesc_ToolTip"), Personalizable(PersonalizationScope.Shared)]
        public bool DisplayDescription
        {
            get
            {
                return this._displayDescription;
            }
            set
            {
                this._displayDescription = value;
                this._forceOnInit = true;
            }
        }

        [Personalizable(PersonalizationScope.Shared), Resources("SearchResults_HCDesc", "SearchResults_HC", "SearchResults_HCDesc_ToolTip"), WebBrowsable(true)]
        public bool DisplayHCDescription
        {
            get
            {
                return this._displayHCDescription;
            }
            set
            {
                this._displayHCDescription = value;
                this._forceOnInit = true;
            }
        }

        [Personalizable(PersonalizationScope.Shared), Resources("SearchResults_HCImage", "SearchResults_HC", "SearchResults_HCImage_ToolTip"), WebBrowsable(true)]
        public bool DisplayHCImage
        {
            get
            {
                return this._displayHCImage;
            }
            set
            {
                this._displayHCImage = value;
                this._forceOnInit = true;
            }
        }

        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true), Resources("SearchResults_HCProp", "SearchResults_HC", "SearchResults_HCProp_ToolTip")]
        public bool DisplayHCProps
        {
            get
            {
                return this._displayHCProps;
            }
            set
            {
                this._displayHCProps = value;
                this._forceOnInit = true;
            }
        }

        [WebBrowsable(true), Resources("SearchResults_HCTitle", "SearchResults_HC", "SearchResults_HCTitle_ToolTip"), Personalizable(PersonalizationScope.Shared)]
        public bool DisplayHCTitle
        {
            get
            {
                return this._displayHCTitle;
            }
            set
            {
                this._displayHCTitle = value;
                this._forceOnInit = true;
            }
        }

        [Personalizable(PersonalizationScope.Shared), Resources("SearchResults_HCTerm", "SearchResults_HCSt", "SearchResults_HCTerm_ToolTip"), WebBrowsable(true)]
        public bool DisplayTerm
        {
            get
            {
                return this._displayTerm;
            }
            set
            {
                this._displayTerm = value;
                this._forceOnInit = true;
            }
        }

        [Resources("SearchResults_HCURL", "SearchResults_HCBB", "SearchResults_HCURL_ToolTip"), WebBrowsable(true), Personalizable(PersonalizationScope.Shared)]
        public bool DisplayUrl
        {
            get
            {
                return this._displayUrl;
            }
            set
            {
                this._displayUrl = value;
                this._forceOnInit = true;
            }
        }

        private bool IsBestBetsOnly
        {
            get
            {
                return (((!this._displayHCTitle && !this._displayHCImage) && !this._displayHCDescription) && !this._displayHCProps);
            }
        }

        private bool NeedHighConfidenceResults
        {
            get
            {
                if ((!this._displayHCImage && !this._displayHCDescription) && !this._displayHCProps)
                {
                    return this._displayHCTitle;
                }
                return true;
            }
        }

        private bool NeedSpecialTerms
        {
            get
            {
                if ((!this._displayDefinition && !this._displayDescription) && (!this._displayBestBetTitle && !this._displayUrl))
                {
                    return this._displayTerm;
                }
                return true;
            }
        }

        [Resources("SearchResults_QueryId", "SearchResults_HCResults", "SearchResults_QueryId_ToolTip"), WebBrowsable(true), Personalizable(PersonalizationScope.Shared)]
        public QueryId QueryID
        {
            get
            {
                return this._qryId;
            }
            set
            {
                this._qryId = value;
                this._forceOnInit = true;
            }
        }

        [Resources("SearchResults_HCLimit", "SearchResults_HC", "SearchResults_HCLimit_ToolTip"), Personalizable(PersonalizationScope.Shared), WebBrowsable(true)]
        public int ResultsPerTypeLimit
        {
            get
            {
                return this._resultsPerTypeLimit;
            }
            set
            {
                if ((value < 0) || (value > 15))
                {
                    throw new WebPartPageUserException("Limit is between 0 and 15");
                }
                this._resultsPerTypeLimit = value;
                this._forceOnInit = true;
            }
        }

        [WebPartStorage(Storage.Shared), DefaultValue(3), WebBrowsable(false)]
        public int SharedPropertiesVersion
        {
            get
            {
                return this._sharedPropertiesVersion;
            }
            set
            {
                this._sharedPropertiesVersion = value;
            }
        }

        protected override bool ShouldToolPartShowDataSourceID
        {
            get
            {
                return false;
            }
        }

        protected override bool ShouldToolPartShowParameterBindings
        {
            get
            {
                return false;
            }
        }

        protected override ToolPart ToolPart
        {
            get
            {
                return null;
            }
        }
    }
}
