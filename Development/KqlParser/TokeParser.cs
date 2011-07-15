using System.Collections;
using System.Collections.Generic;

namespace mAdcOW.SharePoint.KqlParser
{
    internal class TokeParser : IEnumerable<Token>
    {
        private readonly string _query;
        private int _pos;


        public TokeParser(string query)
        {
            _query = query;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            while (_pos < _query.Length)
            {
                if (_query[_pos] == ' ')
                {
                    _pos++;
                }
                else
                {
                    int skipChars;
                    if (_query[_pos] == '(')
                    {
                        Token t = new Token
                                      {
                                          Text = ParseGroup(_query.Substring(_pos), out skipChars),
                                          Type = TokenType.Group
                                      };
                        yield return t;
                        _pos += skipChars + 1;
                    }
                    else if (_query[_pos] == '"')
                    {
                        Token t = new Token
                                      {
                                          Text = ParsePhrase(_query.Substring(_pos), out skipChars),
                                          Type = TokenType.Phrase
                                      };
                        yield return t;
                        _pos += skipChars + 1;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'O' && _query[_pos + 1] == 'R')
                    {
                        Token t = new Token {Text = "OR", Type = TokenType.Operator};
                        yield return t;
                        _pos += 3;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'N' && _query[_pos + 2] == 'D')
                    {
                        Token t = new Token {Text = "AND", Type = TokenType.Operator};
                        yield return t;
                        _pos += 4;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'N' && _query[_pos + 2] == 'Y')
                    {
                        Token t = new Token {Text = "ANY", Type = TokenType.Operator};
                        yield return t;
                        _pos += 3;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'L' && _query[_pos + 2] == 'L')
                    {
                        Token t = new Token {Text = "ALL", Type = TokenType.Operator};
                        yield return t;
                        _pos += 3;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'N' && _query[_pos + 1] == 'O' && _query[_pos + 2] == 'T')
                    {
                        Token t = new Token {Text = "NOT", Type = TokenType.Operator};
                        yield return t;
                        _pos += 3;
                    }
                    else if (_pos < _query.Length - 1 && _query[_pos] == 'N' && _query[_pos + 1] == 'O' && _query[_pos + 2] == 'N' && _query[_pos + 3] == 'E')
                    {
                        Token t = new Token {Text = "NONE", Type = TokenType.Operator};
                        yield return t;
                        _pos += 4;
                    }
                    else
                    {
                        Token t = new Token
                                      {
                                          Text = NormalToken(_query.Substring(_pos), out skipChars)
                                      };                        
                        t.Type = t.Text.EndsWith(":") || t.Text.EndsWith("=") ? TokenType.Property : TokenType.Word;
                        yield return t;
                        _pos += skipChars + 1;
                    }
                }
            }
        }

        private static string ParseGroup(string query, out int skipPos)
        {
            skipPos = query.IndexOf(')', 1);
            if (skipPos < 0)
            {
                skipPos = query.Length - 1;
                return string.Empty;
            }
            return query.Substring(0, skipPos + 1);
        }

        private static string ParsePhrase(string query, out int skipPos)
        {
            skipPos = query.IndexOf('"', 1);
            if (skipPos < 0)
            {
                skipPos = query.Length - 1;
                return string.Empty;
            }
            return query.Substring(0, skipPos + 1);
        }

        private static string NormalToken(string query, out int skipPos)
        {
            skipPos = query.IndexOfAny(new[] { ' ', ':', '=' }, 1);
            if (skipPos < 0) skipPos = query.Length - 1;
            return query.Substring(0, skipPos + 1);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}