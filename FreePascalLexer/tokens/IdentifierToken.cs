using System;
using System.Linq;

namespace FreePascalLexer.tokens
{
    public class IdentifierToken : IToken
    {
        private int _from, _to;
        
        public Tuple<int, int> Position()
        {
            return new Tuple<int, int>(_from, _to);
        }

        public TokenType Type()
        {
            return TokenType.Identifier;
        }

        /**
         * Identifier parser generates token of type TokenType.Identifier.
         * If successful, 'true' is returned, 'result' contains token, 'next' contains further index information.
         * If not successful, 'false' is returned, other values can be arbitrary.
         */
        public static bool ParseIdentifier(string source, int index, out IdentifierToken result, out int next)
        {
            result = null;
            next = -1;
            if (index >= source.Length || !(Char.IsLetter(source[index]) || source[index] == '_'))
            {
                return false;
            }

            result = new IdentifierToken();
            result._from = index;
            index++;
            while (index < source.Length && (Char.IsLetter(source[index]) || source[index] == '_' || Char.IsDigit(source[index])))
            {
                index++;
                next = index;
            }
            result._to = index;
            next = index;
            return true;
        }
    }
}