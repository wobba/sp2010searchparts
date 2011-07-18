// Copyright - Mikael Svenson - mAdcOW deZign
// Under MIT license
// E-mail: miksvenson@gmail.com
// Twitter: @mikaelsvenson
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mAdcOW.SharePoint.KqlParser
{
    public class FqlHelper
    {
        private readonly Dictionary<string, List<string>> _synonymLookup;
        private bool _synonymAdded;
        private SynonymHandling _synonymHandling;
        private readonly string _scopeFilter;
        private readonly Dictionary<string, string> _managedTypes;

        public FqlHelper(Dictionary<string, List<string>> synonymLookup, Dictionary<string, string> managedTypes, string scopeFilter)
        {
            _synonymLookup = synonymLookup;
            _scopeFilter = scopeFilter;
            _managedTypes = managedTypes;
        }

        public string GetFqlFromKql(string kql)
        {
            const TokenType allowedTokenTypes = TokenType.Group | TokenType.Phrase | TokenType.Property | TokenType.Word | TokenType.Operator;
            TokenBuilder builder = new TokenBuilder(kql, allowedTokenTypes);
            builder.Build();
            List<string> includes = new List<string>();
            List<string> excludes = new List<string>();
            CreateTokenFql(builder, includes, excludes, SynonymHandling.None);
            return Build(includes, excludes);
        }

        public string GetFqlFromKql(string kql, SynonymHandling synonymHandling, int boostValue)
        {
            _synonymHandling = synonymHandling;
            const TokenType allowedTokenTypes = TokenType.Group | TokenType.Phrase | TokenType.Property | TokenType.Word | TokenType.Operator;
            TokenBuilder builder = new TokenBuilder(kql, allowedTokenTypes);
            builder.Build();
            List<string> includes = new List<string>();
            List<string> excludes = new List<string>();
            CreateTokenFql(builder, includes, excludes, synonymHandling);
            string resultFql = Build(includes, excludes);
            StringBuilder sb = new StringBuilder();
            if (_synonymAdded && boostValue > 0)
            {
                sb.Append("xrank(");
                sb.Append(resultFql);
                sb.Append(",");
                if (includes.Count > 1) sb.Append("and(");
                includes.Clear();
                excludes.Clear();
                CreateTokenFql(builder, includes, excludes, SynonymHandling.None);
                sb.Append(Build(includes, new List<string>()).Replace("annotation_class=\"user\",", ""));
                if (includes.Count > 1) sb.Append(")");
                sb.AppendFormat(",boost={0})", boostValue); // close xrank
                resultFql = sb.ToString();
            }
            if (!string.IsNullOrEmpty(_scopeFilter))
            {
                resultFql += " AND filter(" + _scopeFilter + ")";
            }
            return resultFql;
        }

        private string JoinTokens(List<Token> tokens, string operand)
        {
            if (tokens.Count == 0) return string.Empty;
            if (tokens.Count == 1) return tokens[0].GetFql(_synonymHandling, _synonymLookup, _managedTypes);
            var innerQueries = new List<string>();
            foreach (Token token in tokens)
            {
                innerQueries.Add(token.GetFql(_synonymHandling, _synonymLookup, _managedTypes));
            }
            var innerSb = new StringBuilder();
            innerSb.Append(operand);
            innerSb.Append("(");
            innerSb.Append(string.Join(",", innerQueries.ToArray()));
            innerSb.Append(")");
            return innerSb.ToString();
        }

        internal string GetFqlQueryForTerm(Token token)
        {
            bool isUserClass = token.ParentOperator == "AND" || token.ParentOperator == "ALL";
            string term = token.Text;
            if (token.Type == TokenType.Phrase && term.Contains(' '))
            {
                token.Text = token.Text.Trim(new[] { '"' });
                if (isUserClass)
                    return string.Format("string({0}, annotation_class=\"user\", mode=\"phrase\")", term);
                return string.Format("string({0}, mode=\"phrase\")", term);
            }
            if (token.Type == TokenType.Property)
            {
                string split = ":";
                string from = "ge";
                string to = "lt";
                if (term.Contains("<="))
                {
                    split = "<=";
                    from = "ge";
                    to = "le";
                }
                else if (term.Contains(">="))
                {
                    split = ">=";
                    from = "ge";
                    to = "le";
                }
                else if (term.Contains(">"))
                {
                    split = ">";
                    from = "gt";
                    to = "le";
                }
                else if (term.Contains("<"))
                {
                    split = "<";
                    from = "ge";
                    to = "lt";
                }
                else if (term.Contains("="))
                {
                    split = "=";
                    from = "ge";
                    to = "lt";
                }
                string[] internalPair = term.Split( new []{split}, StringSplitOptions.RemoveEmptyEntries);

                string fqlType;
                _managedTypes.TryGetValue(internalPair[0].ToLower(), out fqlType);
                if (string.IsNullOrEmpty(fqlType)) fqlType = "string";
                if (fqlType == "string" && split == "=")
                    fqlType = "equals";

                string propVal = internalPair[1].Trim('"');
                if( fqlType == "datetime" || fqlType == "int")
                {
                    if (fqlType == "datetime")
                    {
                        DateTime dateTime = DateTime.Parse(propVal);
                        propVal = dateTime.ToString("yyyy-MM-ddTHH:mm:ss") + "z";
                        if(split == "=")
                            return string.Format("\"{0}\":range({1}(\"{2}\"),{1}(\"{3}\"),from=\"{4}\",to=\"{5}\")", internalPair[0], fqlType, dateTime.ToString("yyyy-MM-ddTHH:mm:ss") + "z", dateTime.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss") + "z", from, to);
                    }                    
                    if (split == "=")
                        return string.Format("\"{0}\":{1}(\"{2}\")", internalPair[0], fqlType, propVal);
                    if (split == "<" || split == "<=")
                        return string.Format("\"{0}\":range(min,{1}(\"{2}\"),from=\"{3}\",to=\"{4}\")", internalPair[0], fqlType, propVal, from, to);
                    if (split == ">" || split == ">=")
                        return string.Format("\"{0}\":range({1}(\"{2}\"),max,from=\"{3}\",to=\"{4}\")", internalPair[0], fqlType, propVal, from, to);
                }
                else
                {
                    string fqlQueryMode = GetFqlQueryMode(internalPair[1]);
                    if (fqlQueryMode == "phrase")
                        return string.Format("\"{0}\":{1}(\"{2}\", mode=\"phrase\")", internalPair[0], fqlType, internalPair[1].Trim('"'));
                    return
                        string.Format("\"{0}\":{1}(\"{2}\")", internalPair[0], fqlType, internalPair[1].Trim('"'));                    
                }
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
                string fql = token.GetFql(synonymHandling, _synonymLookup, _managedTypes);
                if (synonymHandling == SynonymHandling.Include && (token.Type == TokenType.Phrase || token.Type == TokenType.Word))
                {
                    fql = GetSynonymsFql(token.Text, fql);
                }
                includes.Add(fql);
            }
            foreach (Token token in builder.NotExpr)
            {
                excludes.Add(token.GetFql(SynonymHandling.None, null, _managedTypes));
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