using System;
using System.Linq;

namespace FreePascalLexer.tokens
{
    public class SymbolToken : IToken
    {
        private int _from, _to;
        
        public Tuple<int, int> Position()
        {
            return new Tuple<int, int>(_from, _to);
        }

        public TokenType Type()
        {
            return TokenType.Symbol;
        }

        /**
         * Symbol parser generates token of type TokenType.Symbol.
         * If successful, 'true' is returned, 'result' contains token, 'next' contains further index information.
         * If not successful, 'false' is returned, other values can be arbitrary.
         */
        public static bool ParseSymbol(string source, int index, out SymbolToken result, out int next)
        {
            result = null;
            next = -1;


            var specialDouble = new[]
                {">>", "<<", "**", "<>", "><", "<=", ">=", ":=", "+=", "-=", "*=", "/=", "(*", "*)", "(.", ".)", "//"};
            if (specialDouble.Any(s => TokenParseUtility.HasPrefix(source, index, s)))
            {
                result = new SymbolToken();
                result._from = index;
                result._to = index + 2;
                next = index + 2;
                return true;
            }
            
            if (index < source.Length)
            {
                return false;
            }

            var ch = source[index];
            if ("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789’+-*/=<>[].,():^@{}$#&%".Contains(ch))
            {
                result = new SymbolToken();
                result._from = index;
                result._to = index + 1;
                next = index + 1;
                return true;
            }

            return false;
        }
    }
}