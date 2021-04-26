using System;
using FreePascalLexer;
using NUnit.Framework;

namespace FreePascalLexerUnitTests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void ParseNumber()
        {
            foreach ((var shouldFail, var numberAsString, var from, var to) in new[]
            {
                (false, "5829", 0, 4),
                (false, "+60214", 0, 6),
                (false, "-2233", 0, 5),
                (false, "  48\r\n", 2, 4),
                (false, "2.5", 0, 3),
                (false, "+33.6", 0, 5),
                (false, "-35E93", 0, 6),
                (false, "+6e30", 0, 5),
                (false, "+25e-3", 0, 6),
                (false, "-3E+6", 0, 5),
                (false, "$ABC123", 0, 7),
                (false, "&01234567", 0, 9),
                (false, "&01234567", 0, 9),
                (false, "%101", 0, 4),
                (true, "%", 0, 0),
                (true, "&", 0, 0),
                (true, "$", 0, 0),
                (true, "5e", 0, 0),
                (true, ".3", 0, 0),
            })
            {
                if (shouldFail)
                {
                    Assert.Throws<Exception>(() =>
                        TokenParser.Parse(numberAsString)
                    );
                }
                else
                {
                    var tokens = TokenParser.Parse(numberAsString);
                    Assert.AreEqual(1, tokens.Length);
                    var token = tokens[0];
                    Assert.AreEqual(TokenType.Number, token.Type());
                    Assert.AreEqual(from, token.Position().Item1);
                    Assert.AreEqual(to, token.Position().Item2);
                }
            }
        }

        [Test]
        public void ParseString()
        {
            foreach ((var shouldFail, var pascalString, var from, var to) in new[]
            {
                (false, "'abc'", 0, 5),
                (false, "#127", 0, 4),
                (false, "''", 0, 2),
                (true, "#", 0, 0),
                (true, "'I just want to", 0, 0),
            })
            {
                if (shouldFail)
                {
                    Assert.Throws<Exception>(() =>
                        TokenParser.Parse(pascalString)
                    );
                }
                else
                {
                    var tokens = TokenParser.Parse(pascalString);
                    Assert.AreEqual(1, tokens.Length);
                    var token = tokens[0];
                    Assert.AreEqual(TokenType.String, token.Type());
                    Assert.AreEqual(from, token.Position().Item1);
                    Assert.AreEqual(to, token.Position().Item2);
                }
            }
        }

        [Test]
        public void ParseComments()
        {
            foreach ((var shouldFail, var pascalString, var listOfPositions) in new[]
            {
                (false, "  { my beautiful curly braced comment! }", new[] {(2, 40)}),
                (false, "     // another comment", new[] {(5, 23)}),
                (false, "(* kek *)", new[] {(0, 9)}),
                (false, "(* {foo} {bar} *)", new[] {(3, 8), (9, 14), (0, 17)}),
                (false, "{ {even} (* MOAR *) // comments }", new[] {(2, 8), (9, 19), (20, 32), (0, 33)}),
                (false, "{ (* some *) // body { once } }", new[] {(2, 12), (21, 29), (13, 30), (0, 31)}),
                (false, "// dropie \n", new[] {(0, 10)}),
                (true, "\\ oh \n no", new [] {(0, 0)}),
                (true, "\\ oh {no} \n oh", new [] {(0, 0)}),
            })
            {
                if (shouldFail)
                {
                    Assert.Throws<Exception>(() =>
                        TokenParser.Parse(pascalString)
                    );
                }
                else
                {
                    var tokens = TokenParser.Parse(pascalString);
                    Assert.AreEqual(listOfPositions.Length, tokens.Length);
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        var token = tokens[i];
                        (var from, var to) = listOfPositions[i];
                        Assert.AreEqual(TokenType.Comment, token.Type());
                        Assert.AreEqual(from, token.Position().Item1);
                        Assert.AreEqual(to, token.Position().Item2);
                    }
                }
            }
        }
        
        
        [Test]
        public void ParseIdentifier()
        {
            foreach ((var shouldFail, var pascalString, var from, var to) in new[]
            {
                (false, "abc", 0, 3),
                (false, "Vengardium_Leviossa", 0, 19),
                (false, "Привет", 0, 6),
                (true, "53va", 0, 0),
                (true, "'==492-", 0, 0),
            })
            {
                if (shouldFail)
                {
                    Assert.Throws<Exception>(() =>
                        TokenParser.Parse(pascalString)
                    );
                }
                else
                {
                    var tokens = TokenParser.Parse(pascalString);
                    Assert.AreEqual(1, tokens.Length);
                    var token = tokens[0];
                    Assert.AreEqual(TokenType.Identifier, token.Type());
                    Assert.AreEqual(from, token.Position().Item1);
                    Assert.AreEqual(to, token.Position().Item2);
                }
            }
        }
        
        
        [Test]
        public void ParseSymbol()
        {
            foreach ((var shouldFail, var pascalString, var from, var to) in new[]
            {
                (false, "<=", 0, 2),
                (false, ">=", 0, 2),
                (false, "<>", 0, 2),
                (false, "><", 0, 2),
            })
            {
                if (shouldFail)
                {
                    Assert.Throws<Exception>(() =>
                        TokenParser.Parse(pascalString)
                    );
                }
                else
                {
                    var tokens = TokenParser.Parse(pascalString);
                    Assert.AreEqual(1, tokens.Length);
                    var token = tokens[0];
                    Assert.AreEqual(TokenType.Symbol, token.Type());
                    Assert.AreEqual(from, token.Position().Item1);
                    Assert.AreEqual(to, token.Position().Item2);
                }
            }
        }
    }
}