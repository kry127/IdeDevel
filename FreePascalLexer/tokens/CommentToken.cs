using System;
using System.Collections.Generic;
using System.Linq;

namespace FreePascalLexer.tokens
{
    public class CommentToken : IToken
    {
        private int _from, _to;

        public Tuple<int, int> Position()
        {
            return new Tuple<int, int>(_from, _to);
        }

        public TokenType Type()
        {
            return TokenType.Comment;
        }

        /**
         * Comment parser generates token of type TokenType.Comment.
         * If successful, 'true' is returned, 'result' contains token, 'next' contains further index information.
         * If not successful, 'false' is returned, other values can be arbitrary.
         *
         * Note, that list of comment tokens are returned!
         */
        public static bool ParseComment(string source, int index, out List<CommentToken> result, out int next)
        {
            
            LinkedList<(int, string)> closeStack = new LinkedList<(int, string)>();
            bool singleLine = false;
            var startIndex = index;
            
            if (TokenParseUtility.HasPrefix(source, index, "(*"))
            {
                index += 2;
                closeStack.AddFirst((index, "*)"));
            }
            else if (TokenParseUtility.HasPrefix(source, index, "{"))
            {
                index += 1;
                closeStack.AddFirst((index, "}"));
            }
            else if (TokenParseUtility.HasPrefix(source, index, "//"))
            {
                index += 2;
                singleLine = true;
            }
            else
            {
                result = null;
                next = -1;
                return false;
            }
            
            result = new List<CommentToken>();

            while (index < source.Length)
            {
                if (singleLine && source[index] == '\n')
                {
                    // single line comment ended
                    var singleCommentToken = new CommentToken();
                    singleCommentToken._from = startIndex;
                    singleCommentToken._to = index;
                    result.Add(singleCommentToken);
                    next = index;
                    return true;
                }
                
                
                if (closeStack.Count > 0)
                {
                    var i = 0;
                    var prevCommType = "\n";
                    foreach (var pair in closeStack)
                    {
                        if (pair.Item2 == "\n")
                        {
                            i++;
                            continue;
                        }
                        prevCommType = pair.Item2;
                        break;
                    }
                    
                    if (TokenParseUtility.HasPrefix(source, index, prevCommType))
                    {
                        // prefix elimination
                        for (var k = 0; k <= i; k++)
                        {
                            // put all disposed inner comments (newlined mostly)
                            var pref = closeStack.First();
                            closeStack.RemoveFirst();
                            CommentToken ct = new CommentToken();
                            ct._from = pref.Item1;
                            ct._to = index;
                            result.Add(ct);
                        }
                        index += prevCommType.Length;
                        // put intermediate comment
                        if (closeStack.Count == 0 && !singleLine)
                        {
                            // that's all ended
                            var multiCommentToken = new CommentToken();
                            multiCommentToken._from = startIndex;
                            multiCommentToken._to = index;
                            result.Add(multiCommentToken);
                            next = index;
                            return true;
                        }
                        continue; // don't forget to continue main loop with new index
                    }
                }
                
                if (TokenParseUtility.HasPrefix(source, index, "(*"))
                {
                    index += 2;
                    closeStack.AddFirst((index, "*)"));
                    continue;
                }
                else if (TokenParseUtility.HasPrefix(source, index, "{"))
                {
                    index += 1;
                    closeStack.AddFirst((index, "}"));
                    continue;
                }
                else if (TokenParseUtility.HasPrefix(source, index, "//"))
                {
                    index += 2;
                    closeStack.AddFirst((index, "\n"));
                    continue;
                }
                
                // else this is just the char, skip...
                index++;
            }

            next = index;
            return closeStack.Count == 0;
        }
    }
}