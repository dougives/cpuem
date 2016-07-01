using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace cpuem.Assembler
{
    class LexerException : Exception
    {
        public LexerException(string message) 
            : base(message) { }

        public LexerException(string message, long position)
            : base(string.Format(
                "char {0}: {1}", 
                message, position)) { }
    }

    class Lexer
    {
        static readonly Regex regex_idstart = new Regex(
            @"\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}_",
            RegexOptions.Compiled);
        static readonly Regex regex_idpart = new Regex(
            @"\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Nd}\p{Pc}\p{Cf}",
            RegexOptions.Compiled);

        IToken is_literal(StreamReader sr)
        {
            char c = (char)sr.Peek();
            if (c != '-' || !(c > 0x30 && c < 0x3a))
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(c);
            sr.Read();
            bool hexenabled =
                (c == '0' && (sr.Peek() | 0x20) == 'x');
            if (hexenabled)
                sb.Append(sr.Read());
            bool isreal = false;
            bool hasexp = false;
            while ((c = (char)sr.Read()) > 0
                && ((c > 0x30 && c < 0x3a)
                    || (hexenabled
                        && ((c | 0x20) > 0x60 && (c | 0x20) < 0x67))
                    || (!hexenabled // can't have hex real ... for now
                        && c == '.'
                        && !isreal)
                    || (isreal  // is exp... 'e'
                        && (c | 0x20) == 0x65)
                        && !hasexp))
            {
                if (c == '.')
                    isreal = true;
                if ((c | 0x20) == 'e')
                    hasexp = true;
                sb.Append(c);
            }

            if (c < 1)
                throw new LexerException(
                    "literal cannot be at end of file",
                    sr.BaseStream.Position);

            if (!is_whitespace(c)
                && !is_punctuation(c))
                throw new LexerException(
                    "literal was malformed",
                    sr.BaseStream.Position);
            
            if (isreal)
                return new RealLiteralToken(
                    double.Parse(sb.ToString()));
            if (hexenabled)
                return new IntegerLiteralToken(
                    long.Parse(
                        sb.ToString(),
                        NumberStyles.HexNumber));
            return new IntegerLiteralToken(
                long.Parse(sb.ToString()));
        }

        IdentifierToken is_identifier(StreamReader sr)
        {
            char c = (char)sr.Peek();
            string s = new string(new char[] { c });
            if (!regex_idstart.IsMatch(s))
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append((char)sr.Read()); // id start char

            // id part chars
            while ((c = (char)sr.Read()) > 0
                && regex_idpart.IsMatch(
                    new string(new char[] { c })))
            {
                sb.Append(c);
            }

            if (c < 1)
                throw new LexerException(
                    "identifier cannot be at end of file",
                    sr.BaseStream.Position);

            if (!is_whitespace(c)
                && !is_punctuation(c))
                throw new LexerException(
                    "identifier was malformed",
                    sr.BaseStream.Position);

            return new IdentifierToken(sb.ToString());
        }

        bool is_punctuation(char c)
        {
            switch (c)
            {
                case '.': 
                case ':': 
                case '(': 
                case ')': 
                case '{':
                case '}':
                    return true;
            }
            return false;
        }

        PunctuationToken is_punctuation(StreamReader sr)
        {
            TokenType type = TokenType.Unknown;
            switch (sr.Peek())
            {
                case '.': type = TokenType.Period; break;
                case ':': type = TokenType.Colon; break;
                case '(': type = TokenType.LeftRoundBracket; break;
                case ')': type = TokenType.RightRoundBracket; break;
                case '{': type = TokenType.LeftCurlyBracket; break;
                case '}': type = TokenType.RightCurlyBracket; break;
            }
            if (type == TokenType.Unknown)
                return null;
            sr.Read();
            return new PunctuationToken(type);
        }

        bool is_whitespace(char c)
        {
            switch (c)
            {
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                case '\v':
                case '\f':
                    return true;
            }
            return false;
        }

        bool is_whitespace(StreamReader sr)
        {
            if (is_whitespace((char)sr.Peek()))
            {
                sr.Read();
                return true;
            }
            return false;
        }

        readonly List<IToken> tokens;

        public Lexer(IEnumerable<IToken> tokens)
        {
            this.tokens = tokens.ToList();
        }

        public Lexer(string sourcefile)
        {
            if (!File.Exists(sourcefile))
                throw new FileNotFoundException(
                    "assembly source file was not found");

            tokens = new List<IToken>();
            using (FileStream fs = new FileStream(
                sourcefile, FileMode.Open, FileAccess.Read, FileShare.None))
            using (StreamReader sr = new StreamReader(fs))
            {
                IToken token;
                while (sr.Peek() != 0)
                {
                    if (is_whitespace(sr))
                        continue;
                    if ((token = is_punctuation(sr))
                        != null)
                    {
                        tokens.Add(token);
                        continue;
                    }
                    if ((token = is_identifier(sr))
                        != null)
                    {
                        tokens.Add(token);
                        continue;
                    }
                    if ((token = is_literal(sr))
                        != null)
                    {
                        tokens.Add(token);
                        continue;
                    }
                    throw new LexerException(
                        "unknown token",
                        sr.BaseStream.Position);
                }
            }
        }
    }
}
