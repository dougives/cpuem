using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpuem.Assembler
{
    enum TokenType
    {
        Unknown,
        LeftRoundBracket,
        RightRoundBracket,
        LeftCurlyBracket,
        RightCurlyBracket,
        Colon,
        Period,
        Identifier,
        IntegerLiteral,
        RealLiteral,
    }

    class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message)
            : base(message) { }
    }

    interface IToken { }

    interface IToken<T> : IToken
    {
        T get_value();
        TokenType get_type();
    }

    abstract class Token<T> : IToken<T>
    {
        readonly TokenType type;
        readonly T value;

        public TokenType get_type()
            => type;
        public T get_value()
            => value;

        public Token(TokenType type, T value)
        {
            this.type = type;
            this.value = value;
        }
    }

    class PunctuationToken : Token<char>
    {
        public PunctuationToken(TokenType type)
            : base(type,
                  type == TokenType.Colon ? ':'
                  : type == TokenType.Period ? '.'
                  : type == TokenType.LeftCurlyBracket ? '{'
                  : type == TokenType.RightCurlyBracket ? '}'
                  : type == TokenType.LeftRoundBracket ? '('
                  : type == TokenType.RightRoundBracket ? ')'
                  : '\0')
        {
            if (get_value() == '\0')
                throw new InvalidTokenException(string.Format(
                    "{0} is not a valid punctuation type",
                    get_value()));
        }
    }

    class IdentifierToken : Token<string>
    {
        public IdentifierToken(string value)
            : base(TokenType.Identifier, value) { }
    }

    // todo need to allow full range of ulong
    class IntegerLiteralToken : Token<long>
    {
        public IntegerLiteralToken(long value)
            : base(TokenType.IntegerLiteral, value) { }
    }

    class RealLiteralToken : Token<double>
    {
        public RealLiteralToken(double value)
            : base(TokenType.RealLiteral, value) { }
    }

}
