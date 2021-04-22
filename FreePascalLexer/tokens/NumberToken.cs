using System;
using System.Linq;

namespace FreePascalLexer.tokens
{
    public class NumberToken : IToken
    {
        private static Char[] HEX = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'a', 'b', 'c', 'd', 'e'};
        private static Char[] DEC = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
        private static Char[] OCT = new[] {'0', '1', '2', '3', '4', '5', '6', '7'};
        private static Char[] BIN = new[] {'0', '1'};
        
        private int _from, _to;
        
        public Tuple<int, int> Position()
        {
            return new Tuple<int, int>(_from, _to);
        }

        public TokenType Type()
        {
            return TokenType.Number;
        }

        /**
         * Number parser generates token of type TokenType.Number.
         * If successful, 'true' is returned, 'result' contains token, 'next' contains further index information.
         * If not successful, 'false' is returned, other values can be arbitrary.
         */
        public static bool ParseNumber(string source, int index, out NumberToken result, out int next)
        {
            result = null;
            next = -1;
            
            var index2 = index;
            if (index2 >= source.Length)
            {
                return false;
            }
            // parse sign (optional)
            if (source[index] == '+' || source[index] == '-')
            {
                index2++;
            }
            if (index2 >= source.Length)
            {
                return false;
            }
            
            // parse unsigned number
            var ok =  ParseUnsignedNumber(source, index2, out result, out var nnext);
            // process result if OK
            if (ok)
            {
                next = nnext;
                result._from = index;
            }

            return ok;
        }


        private static bool ParseUnsignedNumber(string source, int index, out NumberToken result, out int next)
        {
            if (ParseUnsignedReal(source, index, out result, out next))
            {
                return true;
            }

            return ParseUnsignedInteger(source, index, out result, out next);
        }


        private static bool ParseUnsignedReal(string source, int index, out NumberToken result, out int next)
        {
            // parse digit sequence
            var ok = ParseDigitSequence(source, index, DEC, out result, out next);
            if (!ok)
            {
                return ok;
            }

            var nextIndex = result._to;
            // and maybe .digit sequence
            if (nextIndex >= source.Length)
            {
                return ok;
            }
            if (source[nextIndex] == '.')
            {
                var ok2 = ParseDigitSequence(source, nextIndex + 1, DEC, out var result2, out var nnext);
                if (ok2)
                {
                    next = nnext;
                    nextIndex = result2._to;
                }
            }
            // and maybe scale factor
            if (ParseScaleFactor(source, nextIndex, out var result3, out var nnnext))
            {
                next = nnnext;
            }

            return ok;
        }
    
        private static bool ParseScaleFactor(string source, int index, out NumberToken result, out int next) {
            result = null;
            next = -1;
            var index2 = index;
            if (index2 >= source.Length)
            {
                return false;
            }
            // parse 'E'
            if (source[index2] == 'E' || source[index2] == 'e')
            {
                index2++;
            }
            else
            {
                return false;
            }
            
            if (index2 >= source.Length)
            {
                return false;
            }
            // parse sign (optional)
            if (source[index2] == '+' || source[index2] == '-')
            {
                index2++;
            }
            if (index2 >= source.Length)
            {
                return false;
            }
            
            // parse unsigned number
            var ok = ParseDigitSequence(source, index2, DEC, out result, out next);
            // process result if OK
            if (ok)
            {
                result._from = index;
            }

            return ok;
        }


        public static bool ParseUnsignedInteger(string source, int index, out NumberToken result, out int next)
        {
            result = null;
            next = -1;
            
            if (index >= source.Length)
            {
                return false;
            }
            // parse sign (optional)
            var ok = false;
            if (source[index] == '$')
            {
                ok = ParseDigitSequence(source, index + 1, HEX, out result, out next);
            } else if (source[index] == '&')
            {
                ok = ParseDigitSequence(source, index + 1, OCT, out result, out next);
            } else if (source[index] == '%')
            {
                ok = ParseDigitSequence(source, index + 1, BIN, out result, out next);
            }
            else
            {
                ok = ParseDigitSequence(source, index, DEC, out result, out next);
            }

            if (ok)
            {
                result._from = index;
            }

            return ok;
        }

        private static bool ParseDigitSequence(string source, int index, Char[] allowed, out NumberToken result, out int next)
        {
            result = null;
            next = -1;
            if (index >= source.Length || !allowed.Contains(source[index]))
            {
                return false;
            }

            result = new NumberToken();
            result._from = index;
            while (index < source.Length && allowed.Contains(source[index]))
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