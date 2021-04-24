using System;
using System.Collections.Generic;
using System.Linq;
using FreePascalLexer.tokens;

namespace FreePascalLexer
{
    public class TokenParser
    {
        
        /**
         * This is the main lexing algorithm. It consumes source file as string and puts out token list.
         * Token consists of token type and range that token spans.
         */
        public static IToken[] Parse(string s)
        {
            LinkedList<IToken> ll = new LinkedList<IToken>();
            // use five kinds of token to produce token stream.
            var index = 0;
            var next = 0;
            while (index < s.Length)
            {
                // try to parse as number
                if (NumberToken.ParseNumber(s, index, out var tokenNumber, out next))
                {
                    ll.AddLast(tokenNumber);
                    index = next;
                    continue;
                }
                
                // then try to parse as string
                if (StringToken.ParseString(s, index, out var tokenString, out next))
                {
                    ll.AddLast(tokenString);
                    index = next;
                    continue;
                }
                
                // then try to parse as identifier
                if (IdentifierToken.ParseIdentifier(s, index, out var tokenIdentifier, out next))
                {
                    ll.AddLast(tokenIdentifier);
                    index = next;
                    continue;
                }
                
                // then try to parse as comment
                if (CommentToken.ParseComment(s, index, out var tokensComment, out next))
                {
                    foreach (var t in tokensComment)
                    {
                        ll.AddLast(t);
                    }
                    index = next;
                    continue;
                }
                
                // then try to parse as symbol token
                if (SymbolToken.ParseSymbol(s, index, out var tokenSymbol, out next))
                {
                    ll.AddLast(tokenSymbol);
                    index = next;
                    continue;
                }
                
                if (Char.IsWhiteSpace(s[index]))
                {
                    // skip spaces
                    index++;
                    continue;
                }
                
                // otherwise token is unknown
                throw new Exception("unknown token " + s[index] + " at position " + index);
            }
            
            // return collected tokens
            return ll.ToArray();
        }
    }
}