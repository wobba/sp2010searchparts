using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.SharePoint.Search.Extended.Administration.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KQLParserTest
{
    /// <summary>
    /// Test cases for conversion
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// </summary>

    [TestClass]
    public class Kql2FqlTest
    {
        private FqlHelper _helper;

        [TestInitialize]
        public void Initialize()
        {
            var synonymLookup = new Dictionary<string, List<string>>();
            synonymLookup["contoso"] = new List<string> { "microsoft" };
            synonymLookup["microsoft"] = new List<string> { "contoso" };
            synonymLookup["pepsi"] = new List<string> { "cola" };
            synonymLookup["coca cola"] = new List<string> { "pepsi max" };

            var managedTypes = new Dictionary<string, string>();
            managedTypes["size"] = "int";
            managedTypes["write"] = "datetime";

            _helper = new FqlHelper(synonymLookup, managedTypes, null);

        }

        [TestMethod]
        public void PropertyWord()
        {
            string query = @"author:mikael";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void PropertyPhrase()
        {
            string query = @"author:""mikael svenson""";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void MultiplePropertyPhraseSame1()
        {
            string query = @"author:""mikael svenson"" author:""svenson mikael""";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void MultiplePropertyPhraseSame2()
        {
            string query = @"author:""mikael svenson"" author:""svenson mikael"" ANY(c d)";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void MultiplePropertyPhraseSame3()
        {
            string query = @"ANY(author:""mikael svenson"" author:""svenson mikael"") c OR d size<1000";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void MultiplePropertyPhraseSame4()
        {
            string query = @"author:a author:b author:c c OR d";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void Synonym1()
        {
            string query = @"-pepsi solo";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void Synonym2()
        {
            string query = @"test NONE(pepsi)";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }
        
        [TestMethod]
        public void MultiplePropertyPhraseDifferent()
        {
            string query = @"author:""mikael svenson"" name:""svenson mikael""";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void MultiTest1()
        {
            string query = "(\"SharePoint Search\" OR \"Live Search\") AND title:\"Keyword Syntax\" NOT (sug OR svelg)";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void Simple1()
        {
            string query = "test test2";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void Simple2()
        {
            string query = "test AND test2";
            string result = _helper.GetFqlFromKql(query);
        }

        [TestMethod]
        public void SimpleSynonym1()
        {
            string query = "test contoso";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void PhraseSynonym1()
        {
            string query = "test \"coca cola\"";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void NumberPropertyEQ()
        {
            string query = "size=100";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void NumberPropertyGT()
        {
            string query = "size>100";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void NumberPropertyLT()
        {
            string query = "size<100";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void NumberPropertyAND()
        {
            string query = "size<100 AND size>50";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimeProperty()
        {
            string query = "write=2011-1-1";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimePropertyGreaterThan()
        {
            string query = "write>2011-1-1";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimePropertyGreaterThanEqual()
        {
            string query = "write>=2011-1-1";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimePropertyLEGE()
        {
            string query = "(Write>=2011-1-1 AND Write<=2012-1-1)";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimePropertyOR()
        {
            string query = "Write>=2011-1-1 Write<=2012-1-1";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }

        [TestMethod]
        public void DateTimePropertySecRes()
        {
            string query = "write<=\"2009-10-15 23:59:59\"";
            string result = _helper.GetFqlFromKql(query, SynonymHandling.Include, 100);
        }
        
        //("SharePoint Search" OR "Live Search") AND title:"Keyword Syntax" NOT (sug OR svelg)
        //string query = @"(""SharePoint Search"" OR ""Live Search"") AND title:""title title"" NOT (sug OR svelg) ANY(test test2) ""mikael svenson"" ";
        //string query = @"(""SharePoint Search"" OR ""Live Search"") title:""title title"" NOT (sug OR svelg) ANY(test test2) ""mikael svenson"" ";
        //string query = @"ANY(test test2) ""mikael svenson"" ";
        //string query = @"NOT(a) ANY(test test2)";

    }
}
