using System;
using System.Linq;

namespace FreePascalLexer.tokens
{
    public class StringToken : IToken
    {
        private int _from, _to;
        
        public Tuple<int, int> Position()
        {
            return new Tuple<int, int>(_from, _to);
        }

        public TokenType Type()
        {
            return TokenType.String;
        }

        /**
         * String parser generates token of type TokenType.String.
         * If successful, 'true' is returned, 'result' contains token, 'next' contains further index information.
         * If not successful, 'false' is returned, other values can be arbitrary.
         */
        public static bool ParseString(string source, int index, out StringToken result, out int next)
        {
            if (ParseControlString(source, index, out result, out next))
            {
                return true;
            }

            return ParseQuotedString(source, index, out result, out next);
        }

        private static bool ParseControlString(string source, int index, out StringToken result, out int next)
        {
            
            if (index >= source.Length || source[index] != '#')
            {
                result = null;
                next = -1;
                return false;
            }
            var ok = NumberToken.ParseUnsignedInteger(source, index + 1, out var numberToken, out var nnext);
            if (ok)
            {
                result = new StringToken();
                result._from = index;
                result._to = nnext;
                next = nnext;
                return true;
            }

            result = null;
            next = -1;
            return false;

        }


        private static bool ParseQuotedString(string source, int index, out StringToken result, out int next)
        {
            result = null;
            next = -1;
            if (index >= source.Length || source[index] != '\'')
            {
                return false;
            }

            result = new StringToken();
            result._from = index;
            index++;
            while (index < source.Length && (source[index] != '\n' && source[index] != '\''))
            {
                index++;
                next = index;
            }

            if (index >= source.Length || source[index] != '\'')
            {
                return false;
            }
            // advance index to the end of string definition
            index++;
            // else success
            result._to = index;
            next = index;
            return true;
        }
    }
}