using System.Collections.Generic;

namespace mAdcOW.SharePoint.KqlParser
{
    /// <summary>
    /// Stores a kql token
    ///
    /// Author: Mikael Svenson - mAdcOW deZign    
    /// E-mail: miksvenson@gmail.com
    /// Twitter: @mikaelsvenson
    /// 
    /// This source code is released under the MIT license
    /// </summary>
    class Token
    {
        public string Text { get; set; }
        public TokenType Type { get; set; }
        public string ParentOperator { get; set; }

        public string GetFql(SynonymHandling synonymHandling, Dictionary<string, List<string>> synonymLookup, Dictionary<string,string> propertyTypeLookup)
        {
            if (Type == TokenType.Phrase || Type == TokenType.Word || Type == TokenType.Property)
            {
                FqlHelper helper = new FqlHelper(synonymLookup,propertyTypeLookup,null);
                return helper.GetFqlQueryForTerm(this);
            }
            else if (Type == TokenType.Group)
            {
                Text = Text.Trim(new char[] { '(', ')', ' ' });
                TokenType allowed;
                if (ParentOperator == "ANY" || ParentOperator == "ALL" && ParentOperator == "NONE" && ParentOperator == "NOT")
                    allowed = TokenType.Phrase | TokenType.Property | TokenType.Word;
                else
                    allowed = TokenType.Phrase | TokenType.Property | TokenType.Word | TokenType.Operator;
                TokenBuilder builder = new TokenBuilder(Text, allowed);
                builder.Build();

                if (ParentOperator == "ANY")
                {
                    builder.OrExpr.AddRange(builder.AndExpr);
                    builder.AndExpr.Clear();
                }

                List<string> includes = new List<string>();
                List<string> excludes = new List<string>();
                FqlHelper helper = new FqlHelper(synonymLookup, propertyTypeLookup, null);
                helper.CreateTokenFql(builder, includes, excludes, synonymHandling);
                return helper.Build(includes, excludes);
            }
            return string.Empty;
        }
    }
}
