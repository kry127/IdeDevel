namespace FreePascalLexer.tokens
{
    public class TokenParseUtility
    {
        public static bool HasPrefix(string source, int index, string prefix)
        {
            return source.Substring(index).StartsWith(prefix);
        }
    }
}