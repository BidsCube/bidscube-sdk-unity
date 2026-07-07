using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BidscubeSDK.OpenRTB
{
    internal static class OpenRtbJson
    {
        internal static bool TryParseObject(string json, out Dictionary<string, object> root)
        {
            root = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var parser = new Parser(json);
                var value = parser.ParseValue();
                if (value is Dictionary<string, object> dict)
                {
                    if (!parser.HasOnlyWhitespaceRemaining())
                        return false;

                    root = dict;
                    return true;
                }
            }
            catch
            {
                // Production flow must not throw.
            }

            return false;
        }

        sealed class Parser
        {
            readonly string _text;
            int _index;

            internal Parser(string text)
            {
                _text = text ?? string.Empty;
                _index = 0;
            }

            internal object ParseValue()
            {
                SkipWhitespace();
                if (_index >= _text.Length)
                    return null;

                char c = _text[_index];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': return ParseLiteral("true", true);
                    case 'f': return ParseLiteral("false", false);
                    case 'n': return ParseLiteral("null", null);
                    default: return ParseNumber();
                }
            }

            Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                Expect('{');
                SkipWhitespace();
                if (TryConsume('}'))
                    return result;

                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var value = ParseValue();
                    if (!string.IsNullOrEmpty(key))
                        result[key] = value;
                    SkipWhitespace();
                    if (TryConsume('}'))
                        break;
                    Expect(',');
                }

                return result;
            }

            List<object> ParseArray()
            {
                var result = new List<object>();
                Expect('[');
                SkipWhitespace();
                if (TryConsume(']'))
                    return result;

                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                        break;
                    Expect(',');
                }

                return result;
            }

            string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_index < _text.Length)
                {
                    char c = _text[_index++];
                    if (c == '"')
                        return sb.ToString();
                    if (c == '\\' && _index < _text.Length)
                    {
                        char esc = _text[_index++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_index + 4 <= _text.Length)
                                {
                                    var hex = _text.Substring(_index, 4);
                                    if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                                    {
                                        sb.Append((char)code);
                                        _index += 4;
                                    }
                                }
                                break;
                            default:
                                sb.Append(esc);
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }

            object ParseNumber()
            {
                int start = _index;
                if (_text[_index] == '-')
                    _index++;
                while (_index < _text.Length && char.IsDigit(_text[_index]))
                    _index++;
                bool isFloat = false;
                if (_index < _text.Length && _text[_index] == '.')
                {
                    isFloat = true;
                    _index++;
                    while (_index < _text.Length && char.IsDigit(_text[_index]))
                        _index++;
                }

                if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
                {
                    isFloat = true;
                    _index++;
                    if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-'))
                        _index++;
                    while (_index < _text.Length && char.IsDigit(_text[_index]))
                        _index++;
                }

                var token = _text.Substring(start, _index - start);
                if (isFloat)
                {
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    {
                        if (double.IsNaN(d) || double.IsInfinity(d))
                            return null;
                        return d;
                    }
                    return null;
                }

                if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
                return null;
            }

            object ParseLiteral(string literal, object value)
            {
                if (_text.Length - _index >= literal.Length &&
                    string.Compare(_text, _index, literal, 0, literal.Length, StringComparison.Ordinal) == 0)
                {
                    _index += literal.Length;
                    return value;
                }
                return null;
            }

            internal bool HasOnlyWhitespaceRemaining()
            {
                SkipWhitespace();
                return _index >= _text.Length;
            }

            void SkipWhitespace()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }

            void Expect(char expected)
            {
                SkipWhitespace();
                if (_index >= _text.Length || _text[_index] != expected)
                    throw new FormatException($"Expected '{expected}' at {_index}");
                _index++;
            }

            bool TryConsume(char expected)
            {
                if (_index < _text.Length && _text[_index] == expected)
                {
                    _index++;
                    return true;
                }
                return false;
            }
        }
    }
}
