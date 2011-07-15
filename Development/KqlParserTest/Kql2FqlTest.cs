using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using mAdcOW.SharePoint.KqlParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KQLParserTest
{
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
            _helper = new FqlHelper(synonymLookup);

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
            string query = @"ANY(author:""mikael svenson"" author:""svenson mikael"") c OR d";
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
        //("SharePoint Search" OR "Live Search") AND title:"Keyword Syntax" NOT (sug OR svelg)
        //string query = @"(""SharePoint Search"" OR ""Live Search"") AND title:""title title"" NOT (sug OR svelg) ANY(test test2) ""mikael svenson"" ";
        //string query = @"(""SharePoint Search"" OR ""Live Search"") title:""title title"" NOT (sug OR svelg) ANY(test test2) ""mikael svenson"" ";
        //string query = @"ANY(test test2) ""mikael svenson"" ";
        //string query = @"NOT(a) ANY(test test2)";

    }
}
