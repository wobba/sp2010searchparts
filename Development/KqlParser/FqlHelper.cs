using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mAdcOW.SharePoint.KqlParser
{
    public class FqlHelper
    {
        private readonly Dictionary<string, List<string>> _synonymLookup = new Dictionary<string, List<string>>();
        private bool _synonymAdded;
        private SynonymHandling _synonymHandling;

        public FqlHelper()
        {
            _synonymLookup["contoso"] = new List<string> { "microsoft" };
            _synonymLookup["microsoft"] = new List<string> { "contoso" };
            _synonymLookup["pepsi"] = new List<string> { "cola" };
            _synonymLookup["coca cola"] = new List<string> { "pepsi max" };
        }

        public string GetFqlFromKql( string kql )
        {
            const TokenType allowedTokenTypes = TokenType.Group | TokenType.Phrase | TokenType.Property | TokenType.Word | TokenType.Operator;
            TokenBuilder builder = new TokenBuilder(kql, allowedTokenTypes);
            builder.Build();
            List<string> includes = new List<string>();
            List<string> excludes = new List<string>();
            CreateTokenFql(builder, includes, excludes, SynonymHandling.None);
            return Build(includes, excludes);
        }

        public string GetFqlFromKql( string kql, SynonymHandling synonymHandling, int boostValue )
        {
            _synonymHandling = synonymHandling;
            const TokenType allowedTokenTypes = TokenType.Group | TokenType.Phrase | TokenType.Property | TokenType.Word | TokenType.Operator;
            TokenBuilder builder = new TokenBuilder(kql, allowedTokenTypes);
            builder.Build();
            List<string> includes = new List<string>();
            List<string> excludes = new List<string>();
            CreateTokenFql(builder, includes, excludes, synonymHandling);
            string resultFql = Build(includes, excludes);
            if(_synonymAdded && boostValue > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("xrank(");
                sb.Append(resultFql);
                sb.Append(",");
                if(includes.Count > 1 ) sb.Append("and(");
                includes.Clear();
                excludes.Clear();
                CreateTokenFql(builder, includes, excludes, SynonymHandling.None);
                sb.Append(Build(includes, new List<string>()).Replace("annotation_class=\"user\",", "") );
                if(includes.Count > 1 ) sb.Append(")");
                sb.AppendFormat(",boost={0})", boostValue); // close xrank
                return sb.ToString();
            }
            return resultFql;
        }

        private string JoinTokens(List<Token> tokens, string operand)
        {
            if (tokens.Count == 0) return string.Empty;
            if (tokens.Count == 1) return tokens[0].GetFql(_synonymHandling);
            var innerQueries = new List<string>();
            foreach (Token token in tokens)
            {
                innerQueries.Add(token.GetFql(_synonymHandling));
            }
            var innerSb = new StringBuilder();
            innerSb.Append(operand);
            innerSb.Append("(");
            innerSb.Append(string.Join(",", innerQueries.ToArray()));
            innerSb.Append(")");
            return innerSb.ToString();
        }

        internal static string GetFqlQueryForTerm(Token token)
        {
            bool isUserClass = token.ParentOperator == "AND" || token.ParentOperator == "ALL";
            string term = token.Text;
            if (token.Type == TokenType.Phrase && term.Contains(' '))
            {
                token.Text = token.Text.Trim(new[] {'"'});
                if (isUserClass)
                    return string.Format("string({0}, annotation_class=\"user\", mode=\"phrase\")", term);
                return string.Format("string({0}, mode=\"phrase\")", term);
            }
            if (token.Type == TokenType.Property)
            {
                char splitChar = term.Contains('=') ? '=' : ':';
                string[] internalPair = term.Split(splitChar);
                if (splitChar == ':')
                {
                    string fqlQueryMode = GetFqlQueryMode(internalPair[1]);
                    if (fqlQueryMode == "phrase")
                        return string.Format("\"{0}\":string(\"{1}\", mode=\"phrase\")", internalPair[0],
                                             internalPair[1].Trim('"'));
                    return
                        string.Format("\"{0}\":string(\"{1}\")", internalPair[0], internalPair[1].Trim('"'));
                }
                return string.Format("\"{0}\":equals(\"{1}\")", internalPair[0], internalPair[1].Trim('"'));
            }
            if (isUserClass)
                return string.Format("string(\"{0}\", annotation_class=\"user\", mode=\"simpleall\")", term);
            return string.Format("string(\"{0}\", mode=\"simpleall\")", term);
        }

        private static string GetFqlQueryMode(string s)
        {
            if (s.Contains("\"")) return "phrase";
            return "simpleall";
        }

        internal string Build(List<string> includes, List<string> excludes)
        {
            var sb = new StringBuilder();
            if (excludes.Count > 0) sb.Append("andnot(");
            if (includes.Count > 1) sb.Append("and(");
            sb.Append(string.Join(",", includes.ToArray()));
            if (includes.Count > 1) sb.Append(")"); // close and

            if (excludes.Count > 0)
            {
                sb.Append(",");
                sb.Append(string.Join(",", excludes.ToArray()));
                sb.Append(")"); // close andnot
            }
            return sb.ToString();
        }

        internal void CreateTokenFql(TokenBuilder builder, List<string> includes, List<string> excludes, SynonymHandling synonymHandling)
        {
            string ors = JoinTokens(builder.OrExpr, "or");

            if (!string.IsNullOrEmpty(ors))
                includes.Add(ors);
            foreach (Token token in builder.AndExpr)
            {
                string fql = token.GetFql(synonymHandling);
                if (synonymHandling == SynonymHandling.Include && (token.Type == TokenType.Phrase || token.Type == TokenType.Word))
                {
                    fql = GetSynonymsFql(token.Text, fql);
                }
                includes.Add(fql);
            }
            foreach (Token token in builder.NotExpr)
            {
                excludes.Add(token.GetFql(SynonymHandling.None));
            }
        }

        private string GetSynonymsFql(string innerTerm, string fql)
        {
            List<string> synonyms = BuildSynonymQuery(innerTerm);
            if (synonyms.Count > 0)
            {
                _synonymAdded = true;
                StringBuilder sb = new StringBuilder();
                sb.Append("or(");
                sb.Append(fql);
                sb.Append(",");
                if (synonyms.Count > 1) sb.Append("or(");
                sb.Append(string.Join(",", synonyms.ToArray()));
                sb.Append(")");
                if (synonyms.Count > 1) sb.Append(")");
                fql = sb.ToString();
            }
            return fql;
        }

        private List<string> BuildSynonymQuery(string term)
        {
            term = term.Trim(' ', '"');
            List<string> synonyms = new List<string>(5);
            List<string> wordSynonyms;
            if (_synonymLookup.TryGetValue(term, out wordSynonyms))
            {
                string w = term;
                synonyms.AddRange(
                    wordSynonyms.Select(
                        synonym =>
                        string.Format("string(\"{0}\", mode=\"simpleall\")", term.Replace(w, synonym))).Where(
                            syn => !synonyms.Contains(syn)));
            }
            return synonyms;
        }
    }
}