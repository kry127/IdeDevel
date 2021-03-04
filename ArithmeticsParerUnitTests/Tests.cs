using System;
using ArithmeticsParser;
using NUnit.Framework;

namespace ArithmeticsParerUnitTests
{
    
    [TestFixture]
    public class ParsingTests
    {
        public static int rawEval(string input) {
            BaseExpression iex = BaseExpression.parse(input);
            BaseExpression reduced = iex.EvaluateBe();
            Assert.True(reduced.ExtractRawInteger(out var result));
            return result;
        }
        
        [Test]
        public void SimpleTest()
        {
            Assert.AreEqual(5, rawEval("2+3"));
        }
        
        [Test]
        public void AssocTest()
        {
            Assert.AreEqual(9, rawEval("2 + (3 + 4)"));
            Assert.AreEqual(9, rawEval("(2 + 3) + 4"));
        }
        
        [Test]
        public void SimpleSumWithNegativeTest()
        {
            Assert.AreEqual(-7, rawEval("2 + -9"));
        }
        
        [Test]
        public void ExtraBracketAround()
        {
            Assert.AreEqual(3, rawEval("(2 + 1)"));
        }
        
        
        [Test]
        public void BracketsAroundEveryOperand()
        {
            Assert.AreEqual(4, rawEval("((13) + (-9))"));
        }
        
        
        [Test]
        public void TestMultiply()
        {
            Assert.AreEqual(120, rawEval("1*2*3*4*5"));
        }
        
        
        [Test]
        public void PolynomialTest()
        {
            Assert.AreEqual(-4830, rawEval("1*2*3 + 2*3*4 + 3*4*5 + 4*5*6 + -7*8*9*10"));
        }
        
        [Test]
        public void ValidDoubleMinus()
        {
            Assert.AreEqual(12, rawEval("7 - -5"));
        }
        
        [Test]
        public void AssocInvalidUnbalancedOpen()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("(4 + 1 + 5");
            });
        }
 
        [Test]
        public void AssocInvalidUnbalancedClosed()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("(4 + 1) + 5)");
            });
        }
        
        
        [Test]
        public void EmptyInvalid()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("");
            });
        }
        
        [Test]
        public void UnitInvalid()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("()");
            });
        }
        
        [Test]
        public void InvalidDoublePlus()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("3 + + 5");
            });
        }
        
        [Test]
        public void InvalidDoubleMinus()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.parse("7 - - 5");
            });
        }
        
        
        // variables
        [Test]
        public void ExprWithVarTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BaseExpression.parse("x + 4");
            });
        }
    }
}