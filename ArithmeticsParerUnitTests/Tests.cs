using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ArithmeticsParser;
using NUnit.Framework;

namespace ArithmeticsParerUnitTests
{
    
    [TestFixture]
    public class ParsingTests
    {
        // Utilitiy functions
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

        private static BaseExpression GenerateRandomExpression(Random rnd, int lvl)
        {
            if (lvl <= 1)
            {
                var leafKind = rnd.Next(0, 1);
                if (leafKind == 0)
                {
                    return new IntegerExpression(rnd.Next());
                }
                else
                {
                    return new VarExpression("v" + rnd.Next(0, 10));
                }
            }

            var p = rnd.NextDouble();
            if (p < 0.5)
            {
                var lhs = GenerateRandomExpression(rnd, rnd.Next(1, lvl - 1));
                var rhs = GenerateRandomExpression(rnd, rnd.Next(1, lvl - 1));
                BinopExpression.BinopType op = (BinopExpression.BinopType) rnd.Next(0, 5);
                return new BinopExpression(lhs, rhs, op);
            } else if (p < 0.75) {
                return new IntegerExpression(rnd.Next());
            } else {
                return new VarExpression("v" + rnd.Next(0, 10));
            }
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
        public void SubstitutionTestId()
        {
            BaseExpression expr = BaseExpression.Parse("x + y + z");
            BaseExpression expr2 = expr.Subst(new Dictionary<string, BaseExpression>());
            Assert.AreEqual(expr.ToString(), expr2.ToString());
        }
        
        
        [Test]
        public void SubstitutionTestId2()
        {
            BaseExpression expr = BaseExpression.Parse("1 + 2 + x + y");
            var subst = new Dictionary<string, BaseExpression>();
            subst["a"] = new IntegerExpression(3);
            subst["b"] = new IntegerExpression(9);
            BaseExpression expr2 = expr.Subst(subst);
            Assert.AreEqual(expr.ToString(), expr2.ToString());
        }
        
        [Test]
        public void SubstitutionSimpleTest()
        {
            BaseExpression exprExpected = BaseExpression.Parse("3 + (-1 * -4) / 2 - 0 % 17");
            
            BaseExpression expr = BaseExpression.Parse("x + (y * z) / w - k % l");
            var subst = new Dictionary<string, BaseExpression>();
            subst["x"] = new IntegerExpression(3);
            subst["y"] = new IntegerExpression(-1);
            subst["z"] = new IntegerExpression(-4);
            subst["w"] = new IntegerExpression(2);
            subst["k"] = new IntegerExpression(0);
            subst["l"] = new IntegerExpression(17);
            BaseExpression exprSubst = expr.Subst(subst);

            exprExpected.EvaluateBe().ExtractRawInteger(out var expected);
            exprSubst.EvaluateBe().ExtractRawInteger(out var actual);
            Assert.AreEqual(expected, actual);
        }
        
        
        [Test]
        public void RandomTest_ExprToStringAndParse()
        {
            var maxTreeLvl = 50;
            var N = 10000;
            var rnd = new Random();
            for (var i = 0; i < N; i++)
            {
                var rand = GenerateRandomExpression(rnd, maxTreeLvl);
                var randAsString = rand.ToString();
                var rand2 = BaseExpression.Parse(randAsString);
                var rand2AsString = rand2.ToString();
                Assert.AreEqual(randAsString, rand2AsString, "wrong value at iteration " + i);
            }
        }

        [Test]
        public void GenerateLibrary_ThreeArgsExpression()
        {
            var xyzExpr = BaseExpression.Parse("(66 + (x * -4)) / z + y / 9 + z * (x - y)");
            xyzExpr.Compile("Test_GenerateLibrary_LaunchFunction");
            
            Assembly assembly = Assembly.LoadFrom("Test_GenerateLibrary_LaunchFunction.dll");

            Type type = assembly.GetType("Evaluator");
            MethodInfo evalMethod = type.GetMethod("Evaluate");
            Assert.NotNull(evalMethod);

            int x = 2, y = 3, z = 5;
            var subst = new Dictionary<string, BaseExpression>();
            subst["x"] = new IntegerExpression(x);
            subst["y"] = new IntegerExpression(y);
            subst["z"] = new IntegerExpression(z);
            var substExpr = xyzExpr.Subst(subst);
            
            object evaluatorObject = Activator.CreateInstance(type);
            int result = (int) evalMethod.Invoke(evaluatorObject, new object[] { x, y, z });
            substExpr.EvaluateBe().ExtractRawInteger(out int expected);
            Assert.AreEqual(expected, result);
        }
        
        
        [Test]
        public void GenerateLibrary_RandomTestExpression()
        {
            
            var maxTreeLvl = 150;
            var N = 200;
            var rnd = new Random();
            for (var i = 0; i < N; i++)
            {
                var assemblyName = "Test_GenerateLibrary_RandomTestExpression" + i;
                var assemblyPath = assemblyName + ".dll";
                
                var rand = GenerateRandomExpression(rnd, maxTreeLvl);
                rand.Compile(assemblyName);

                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                Type type = assembly.GetType("Evaluator");
                MethodInfo evalMethod = type.GetMethod("Evaluate");
                Assert.NotNull(evalMethod);

                var freeVars = rand.GetFreeVars();
                var subst = new Dictionary<string, BaseExpression>();
                var substObjects = new object[freeVars.Length];
                
                for (var k = 0; k < freeVars.Length; k++)
                {
                    var varValue = rnd.Next();
                    subst[freeVars[k]] = new IntegerExpression(varValue);
                    substObjects[k] = varValue;
                }
                var substExpr = rand.Subst(subst);
            
                object evaluatorObject = Activator.CreateInstance(type);
                
                int expected = 0, actual = 0;
                bool substThrownDivideByZero = false, dllThrownDivideByZero = false;
                try
                {
                    substExpr.EvaluateBe().ExtractRawInteger(out expected);
                }
                catch (DivideByZeroException)
                {
                    substThrownDivideByZero = true;
                }

                try
                {
                    actual = (int) evalMethod.Invoke(evaluatorObject, substObjects);
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is DivideByZeroException)
                    {
                        dllThrownDivideByZero = true;
                    }
                    else
                    {
                        throw ex;
                    }
                }
                Assert.AreEqual(substThrownDivideByZero, dllThrownDivideByZero);
                if (!substThrownDivideByZero)
                {
                    Assert.AreEqual(expected, actual, "result mismatch at iteration " + i);
                }
                
                if(File.Exists(assemblyPath))
                {
                    File.Delete(assemblyPath);
                }
            }
        }
    }
}