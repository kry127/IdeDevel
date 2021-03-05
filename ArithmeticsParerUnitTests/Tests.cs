using System;
using ArithmeticsParser;
using NUnit.Framework;

namespace ArithmeticsParerUnitTests
{
    
    [TestFixture]
    public class ParsingTests
    {
        private static int RawEval(string input) {
            var iex = BaseExpression.Parse(input);
            var reduced = iex.EvaluateBe();
            Assert.True(reduced.ExtractRawInteger(out var result));
            return result;
        }

        private static BaseExpression Minimize(string input)
        {
            var iex = BaseExpression.Parse(input);
            var reduced = iex.Normalize().EvaluateBe();
            return reduced;
        }
        
        [Test]
        public void SimpleTest()
        {
            Assert.AreEqual(5, RawEval("2+3"));
        }
        
        [Test]
        public void AssocTest()
        {
            Assert.AreEqual(9, RawEval("2 + (3 + 4)"));
            Assert.AreEqual(9, RawEval("(2 + 3) + 4"));
        }
        
        [Test]
        public void SimpleSumWithNegativeTest()
        {
            Assert.AreEqual(-7, RawEval("2 + -9"));
        }
        
        [Test]
        public void ExtraBracketAround()
        {
            Assert.AreEqual(3, RawEval("(2 + 1)"));
        }
        
        
        [Test]
        public void BracketsAroundEveryOperand()
        {
            Assert.AreEqual(4, RawEval("((13) + (-9))"));
        }
        
        
        [Test]
        public void TestMultiply()
        {
            Assert.AreEqual(120, RawEval("1*2*3*4*5"));
        }
        
        
        [Test]
        public void PolynomialTest()
        {
            Assert.AreEqual(-4830, RawEval("1*2*3 + 2*3*4 + 3*4*5 + 4*5*6 + -7*8*9*10"));
        }
        
        [Test]
        public void ValidDoubleMinus()
        {
            Assert.AreEqual(12, RawEval("7 - -5"));
        }
        
        [Test]
        public void AssocInvalidUnbalancedOpen()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("(4 + 1 + 5");
            });
        }
 
        [Test]
        public void AssocInvalidUnbalancedClosed()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("(4 + 1) + 5)");
            });
        }
        
        
        [Test]
        public void EmptyInvalid()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("");
            });
        }
        
        [Test]
        public void UnitInvalid()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("()");
            });
        }
        
        [Test]
        public void InvalidDoublePlus()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("3 + + 5");
            });
        }
        
        [Test]
        public void InvalidDoubleMinus()
        {
            Assert.Throws<ParseException>(() =>
            {
                BaseExpression.Parse("7 - - 5");
            });
        }
        
        
        // variables
        [Test]
        public void ExprWithVarTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BaseExpression.Parse("x + 4");
            });
        }
        [Test]
        public void SimpleMinimizationTest()
        {
            Assert.AreEqual("x+9", Minimize("x + (4 + 5)").ToString());
        }
        
        [Test]
        public void RegroupOperatorsMinimizationTest()
        {
            Assert.AreEqual("x+9-y", Minimize("(x + 4) - (y - 5)").ToString());
        }
        
        
        [Test]
        public void FuzzyTest()
        {
            var 
        }
    }
}