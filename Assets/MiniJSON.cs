using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class MiniJSON
{
    public static object Deserialize(string json)
    {
        if (json == null)
            return null;

        return Parser.Parse(json);
    }

    sealed class Parser : IDisposable
    {
        const string WORD_BREAK = "{}[],:\"";

        public static bool IsWordBreak(char c)
        {
            return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
        }

        StringReader json;

        Parser(string jsonString)
        {
            json = new StringReader(jsonString);
        }

        public static object Parse(string jsonString)
        {
            using (var instance = new Parser(jsonString))
            {
                return instance.ParseValue();
            }
        }

        public void Dispose()
        {
            json.Dispose();
            json = null;
        }

        Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> table = new Dictionary<string, object>();

            json.Read(); // {

            while (true)
            {
                switch (NextToken)
                {
                    case TOKEN.CURLY_CLOSE:
                        return table;

                    case TOKEN.NONE:
                        return null;

                    default:
                        string name = ParseString();
                        if (name == null)
                            return null;

                        if (NextToken != TOKEN.COLON)
                            return null;

                        json.Read(); // :

                        table[name] = ParseValue();
                        break;
                }
            }
        }

        List<object> ParseArray()
        {
            List<object> array = new List<object>();

            json.Read(); // [

            bool parsing = true;
            while (parsing)
            {
                TOKEN nextToken = NextToken;

                switch (nextToken)
                {
                    case TOKEN.SQUARE_CLOSE:
                        parsing = false;
                        break;

                    case TOKEN.NONE:
                        return null;

                    default:
                        array.Add(ParseValue());
                        break;
                }
            }

            return array;
        }

        object ParseValue()
        {
            switch (NextToken)
            {
                case TOKEN.STRING:
                    return ParseString();

                case TOKEN.NUMBER:
                    return ParseNumber();

                case TOKEN.CURLY_OPEN:
                    return ParseObject();

                case TOKEN.SQUARE_OPEN:
                    return ParseArray();

                case TOKEN.TRUE:
                    json.Read();
                    return true;

                case TOKEN.FALSE:
                    json.Read();
                    return false;

                case TOKEN.NULL:
                    json.Read();
                    return null;

                default:
                    return null;
            }
        }

        string ParseString()
        {
            StringBuilder s = new StringBuilder();
            char c;

            json.Read(); // skip "

            bool parsing = true;
            while (parsing)
            {
                if (json.Peek() == -1)
                    break;

                c = NextChar;
                switch (c)
                {
                    case '"':
                        parsing = false;
                        break;

                    case '\\':
                        if (json.Peek() == -1)
                            parsing = false;

                        c = NextChar;
                        switch (c)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                s.Append(c);
                                break;
                            case 'b':
                                s.Append('\b');
                                break;
                            case 'f':
                                s.Append('\f');
                                break;
                            case 'n':
                                s.Append('\n');
                                break;
                            case 'r':
                                s.Append('\r');
                                break;
                            case 't':
                                s.Append('\t');
                                break;
                            case 'u':
                                char[] hex = new char[4];
                                for (int i = 0; i < 4; i++)
                                    hex[i] = NextChar;

                                s.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                        break;

                    default:
                        s.Append(c);
                        break;
                }
            }

            return s.ToString();
        }

        object ParseNumber()
        {
            string number = NextWord;

            if (number.IndexOf('.') == -1)
                return Int64.Parse(number);

            return Double.Parse(number);
        }

        void EatWhitespace()
        {
            while (Char.IsWhiteSpace(PeekChar))
            {
                json.Read();
                if (json.Peek() == -1)
                    break;
            }
        }

        char PeekChar => Convert.ToChar(json.Peek());

        char NextChar => Convert.ToChar(json.Read());

        string NextWord
        {
            get
            {
                StringBuilder word = new StringBuilder();

                while (!IsWordBreak(PeekChar))
                {
                    word.Append(NextChar);
                    if (json.Peek() == -1)
                        break;
                }

                return word.ToString();
            }
        }

        TOKEN NextToken
        {
            get
            {
                EatWhitespace();

                if (json.Peek() == -1)
                    return TOKEN.NONE;

                switch (PeekChar)
                {
                    case '{': return TOKEN.CURLY_OPEN;
                    case '}': json.Read(); return TOKEN.CURLY_CLOSE;
                    case '[': return TOKEN.SQUARE_OPEN;
                    case ']': json.Read(); return TOKEN.SQUARE_CLOSE;
                    case ',': json.Read(); return TOKEN.COMMA;
                    case '"': return TOKEN.STRING;
                    case ':': json.Read(); return TOKEN.COLON;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-': return TOKEN.NUMBER;
                }

                string word = NextWord;

                switch (word)
                {
                    case "false": return TOKEN.FALSE;
                    case "true": return TOKEN.TRUE;
                    case "null": return TOKEN.NULL;
                }

                return TOKEN.NONE;
            }
        }

        enum TOKEN
        {
            NONE,
            CURLY_OPEN,
            CURLY_CLOSE,
            SQUARE_OPEN,
            SQUARE_CLOSE,
            COMMA,
            COLON,
            STRING,
            NUMBER,
            TRUE,
            FALSE,
            NULL
        }
    }

    sealed class StringReader : IDisposable
    {
        readonly string str;
        int position;

        public StringReader(string s) => str = s;

        public int Peek() => (position == str.Length) ? -1 : str[position];

        public int Read() => (position == str.Length) ? -1 : str[position++];

        public void Dispose() { }
    }
}
