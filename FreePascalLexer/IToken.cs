using System;

namespace FreePascalLexer
{
    public enum TokenType
    {
        Symbol, Comment, Identifier, Number, String
    }
    public interface IToken
    {
        Tuple<int, int> Position();
        TokenType Type();
    }
}