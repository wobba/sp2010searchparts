using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Office.Server.Search.Administration;
using Microsoft.Office.Server.Search.WebControls;
using Microsoft.SharePoint;

namespace mAdcOW.SharePoint.Search.RefinementPanelValueMappingSupport
{
    [ToolboxItem(false)]
    public class RefinementPanelValueMappingSupport : RefinementWebPart
    {
        private Dictionary<string, XmlNode> _valueReplacements = new Dictionary<string, XmlNode>();

        protected override void OnInit(EventArgs e)
        {
            if (!string.IsNullOrEmpty(FilterCategoriesDefinition) && IsRunningFast())
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(FilterCategoriesDefinition);
                var nodes = document.SelectNodes("//CustomFilters[@MappingType='ValueMapping']");
                if (nodes != null)
                {
                    // remove all custom filters with value mapping as they are not supported
                    // values will be replaced on output
                    foreach (XmlNode node in nodes)
                    {
                        if (node.ParentNode == null || node.ParentNode.Attributes == null) continue;
                        _valueReplacements.Add(node.ParentNode.Attributes["MappedProperty"].Value, node);
                        node.ParentNode.RemoveChild(node);
                    }
                    FilterCategoriesDefinition = document.OuterXml;
                }
            }
            base.OnInit(e);
        }

        protected override XPathNavigator GetXPathNavigator(string viewPath)
        {
            var defaultNav = base.GetXPathNavigator(viewPath);
            if (!IsRunningFast()) return defaultNav;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(defaultNav.OuterXml);
            foreach (KeyValuePair<string, XmlNode> valuePair in _valueReplacements)
            {
                bool keepAllValues = true;
                if (valuePair.Value.Attributes != null && valuePair.Value.Attributes["ShowAllInMore"] != null)
                {
                    bool.TryParse(valuePair.Value.Attributes["ShowAllInMore"].Value, out keepAllValues);
                }

                XmlNode refiner =
                    doc.SelectSingleNode(string.Format("//FilterCategory[@ManagedProperty='{0}']", valuePair.Key));
                if (refiner == null) continue;
                XmlNodeList nodeList = refiner.SelectNodes("Filters/Filter");
                if (nodeList == null) continue;
                int count = nodeList.Count;
                foreach (XmlNode refinement in nodeList)
                {
                    XmlNode valueNode = refinement.SelectSingleNode("Value");
                    XmlNode toolTipNode = refinement.SelectSingleNode("Tooltip");
                    if (valueNode == null || toolTipNode == null) continue;
                    string xpath = string.Format("//OriginalValue[text()='{0}']", toolTipNode.InnerText);
                    XmlNode lookupNode = valuePair.Value.SelectSingleNode(xpath);
                    if (lookupNode != null && lookupNode.ParentNode != null && lookupNode.ParentNode.Attributes != null)
                    {
                        string lookupValue = lookupNode.ParentNode.Attributes["CustomValue"].InnerText;
                        if (lookupValue.Length > NumberOfCharsToDisplay)
                        {
                            valueNode.InnerText = lookupValue.Substring(0, NumberOfCharsToDisplay) + "...";
                        }
                        else
                        {
                            valueNode.InnerText = lookupValue;
                        }
                        toolTipNode.InnerText = lookupValue;
                    }
                    else if (!keepAllValues && refinement.ParentNode != null)
                    {
                        // Nodes with count==null are remove filter nodes and should be kept
                        refinement.ParentNode.RemoveChild(refinement);
                        count--;
                    }
                }
                if (count == 0 && refiner.ParentNode != null) refiner.ParentNode.RemoveChild(refiner);
            }
            return doc.CreateNavigator();
        }

        private bool IsRunningFast()
        {
            var ssaProxy =
                (SearchServiceApplicationProxy)SearchServiceApplicationProxy.GetProxy(SPServiceContext.Current);
            return ssaProxy.FASTAdminProxy != null;
        }
    }
}