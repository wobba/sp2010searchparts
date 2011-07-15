using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KQLParser
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
            int skipChars = 0;
            while (_pos < _query.Length)
            {
                if (_query[_pos] == ' ')
                {
                    _pos++;
                }
                else if (_query[_pos] == '(')
                {
                    Token t = new Token();
                    t.Text = ParseGroup(_query.Substring(_pos), out skipChars);
                    t.Type = TokenType.Group;
                    yield return t;
                    _pos += skipChars + 1;
                }
                else if (_query[_pos] == '"')
                {
                    Token t = new Token();
                    t.Text = ParsePhrase(_query.Substring(_pos), out skipChars);
                    t.Type = TokenType.Phrase;
                    yield return t;
                    _pos += skipChars + 1;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'O' && _query[_pos + 1] == 'R')
                {
                    Token t = new Token();
                    t.Text = "OR";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 3;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'N' && _query[_pos + 2] == 'D')
                {
                    Token t = new Token();
                    t.Text = "AND";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 4;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'N' && _query[_pos + 2] == 'Y')
                {
                    Token t = new Token();
                    t.Text = "ANY";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 3;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'A' && _query[_pos + 1] == 'L' && _query[_pos + 2] == 'L')
                {
                    Token t = new Token();
                    t.Text = "ALL";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 3;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'N' && _query[_pos + 1] == 'O' && _query[_pos + 2] == 'T')
                {
                    Token t = new Token();
                    t.Text = "NOT";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 3;
                }
                else if (_pos < _query.Length - 1 && _query[_pos] == 'N' && _query[_pos + 1] == 'O' && _query[_pos + 2] == 'N' && _query[_pos + 3] == 'E')
                {
                    Token t = new Token();
                    t.Text = "NONE";
                    t.Type = TokenType.Operator;
                    yield return t;
                    _pos += 4;
                }
                else
                {
                    Token t = new Token();
                    t.Text = NormalToken(_query.Substring(_pos), out skipChars); ;
                    t.Type = t.Text.EndsWith(":") || t.Text.EndsWith("=") ? TokenType.Property : TokenType.Word;
                    yield return t;
                    _pos += skipChars + 1;
                }
            }
        }

        private string ParseGroup(string query, out int skipPos)
        {
            skipPos = query.IndexOf(')', 1);
            if (skipPos < 0)
            {
                skipPos = query.Length - 1;
                return string.Empty;
            }
            return query.Substring(0, skipPos + 1);
        }

        private string ParsePhrase(string query, out int skipPos)
        {
            skipPos = query.IndexOf('"', 1);
            if (skipPos < 0)
            {
                skipPos = query.Length - 1;
                return string.Empty;
            }
            return query.Substring(0, skipPos + 1);
        }

        private string NormalToken(string query, out int skipPos)
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