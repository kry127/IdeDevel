using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ArithmeticsParser
{
    public class ParseException : Exception
    {
        public ParseException(string msg, int position) 
            : base("ParseException: " + msg + ", at position " + position + ".")
        {
        }
    }

    public class AstException : Exception
    {
        public AstException(string msg) : base(msg)
        {
        }

    }

    /**
     * Interface of expression. Every expression should be visitable
     */
    public interface IExpression
    {
        /**
         * Use method Visit to visit all nodes of the tree
         */
        void Visit(IExpressionVisitor visitor);

        /**
         * Use this method to evaluate (reduce) expression
         */
        IExpression Evaluate();
    }

    /**
     * This class is used as intermediate  representation to cache const, for instance
     */
    public abstract class BaseExpression : IExpression
    {
        private bool _evaluatedConst;
        private bool _cachedConstVal;

        public bool IsConst
        {
            get
            {
                if (_evaluatedConst) return _cachedConstVal;
                _cachedConstVal = EvalIsConst();
                _evaluatedConst = true;

                return _cachedConstVal;
            }
        }

        protected abstract bool EvalIsConst();
        public abstract void Visit(IExpressionVisitor visitor);

        // is there something like bridge methods in C#?
        public abstract BaseExpression EvaluateBe();

        public IExpression Evaluate()
        {
            return EvaluateBe();
        }

        /**
         * Use this method to extract integer from IntegerExpression class
         */
        public bool ExtractRawInteger(out int x)
        {
            switch (this)
            {
                case IntegerExpression ie:
                    x = ie.Value;
                    return true;
            }

            x = 0;
            return false;
        }


        /**
         * Pseudoexpressions for parsing
         */
        private class PseudoExpression : IExpression
        {
            public void Visit(IExpressionVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public IExpression Evaluate()
            {
                throw new NotImplementedException();
            }
        }
        
        private class OpenBracket : PseudoExpression
        {
        }

        private class OrphanOperator : PseudoExpression
        {
            public BinopExpression.BinopType op;
            public OrphanOperator(BinopExpression.BinopType op)
            {
                this.op = op;
            }
        }

        /**
         * Method that produces expression by the given string.
         * If parse is not ok, exception ParseException is thrown
         */
        public static BaseExpression Parse(string input)
        {
            List<IExpression> stack = new List<IExpression>();
            for (var i = 0; i < input.Length;)
            {
                switch (input[i])
                {
                    case '(':
                        stack.Add(new OpenBracket());
                        i++;
                        break;
                    case ')':
                        compactStackOnCloseBracket(ref stack, i + 1);
                        i++;
                        break;
                    case '+':
                        stack.Add(new OrphanOperator(BinopExpression.BinopType.Add));
                        i++;
                        break;
                    // case '-': // process minus separately, it can be integer
                    //     stack.Add(new OrphanOperator(BinopExpression.BinopType.Sub));
                    //     i++;
                    //     break;
                    case '*':
                        stack.Add(new OrphanOperator(BinopExpression.BinopType.Mul));
                        i++;
                        break;
                    case '/':
                        stack.Add(new OrphanOperator(BinopExpression.BinopType.Div));
                        i++;
                        break;
                    case '%':
                        stack.Add(new OrphanOperator(BinopExpression.BinopType.Mod));
                        i++;
                        break;
                    case ' ':
                    case '\t':
                    case '\v':
                    case '\r':
                    case '\n':
                        i++;
                        break; // no action to parse spaces
                    default:
                        IExpression stackTop = stack.Count == 0 ? null : stack.Last();
                        int dest;
                        if (input[i] == '-')
                        {
                            if (stackTop is BaseExpression)
                            {
                                // this is operator
                                stack.Add(new OrphanOperator(BinopExpression.BinopType.Sub));
                                i++;
                                break;
                            }
                            if (stackTop is OrphanOperator || stackTop is OpenBracket || stackTop is null)
                            {
                                int result = ParseInteger(input, i, out dest);
                                if (dest > i)
                                {
                                    stack.Add(new IntegerExpression(result));
                                    i = dest;
                                    compactStackOnOperand(ref stack);
                                    break; // ok, parsed as integer
                                }
                            }

                            throw new ParseException("Don't know what to do with this minus", i);
                        }
                        
                        // try to parse as variable
                        string asVarname = ParseVarname(input, i, out dest);
                        if (dest > i)
                        {
                            stack.Add(new VarExpression(asVarname));
                            i = dest;
                            compactStackOnOperand(ref stack);
                            break;
                        }
                        
                        // try to parse as integer
                        int asInt = ParseInteger(input, i, out dest);
                        if (dest > i)
                        {
                            stack.Add(new IntegerExpression(asInt));
                            i = dest;
                            compactStackOnOperand(ref stack);
                            break;
                        }
                        
                        throw new ParseException("variants of parsing exausted", i);
                        
                }
            }
            CompactTerminal(ref stack);
            if (stack.Count != 1)
            {
                throw new ParseException("not valid expression, maybe not enough brackets", input.Length);
            }

            IExpression top = stack[0];
            if (top is BaseExpression expression)
            {
                return expression;
            }
            else
            {
                throw new ParseException("error of evaluation to basic expression", input.Length);
            }

            
        }

        private static string ParseVarname(string input, int parseFrom, out int destination)
        {
            destination = parseFrom;
            if (parseFrom >= input.Length || !Char.IsLetter(input[parseFrom]))
            {
                return "";
            }

            destination = parseFrom + 1;
            while (destination < input.Length)
            {
                if (!Char.IsLetter(input[destination]) && !Char.IsDigit(input[destination]))
                {
                    break;
                }
                destination++;
            }

            return input.Substring(parseFrom, destination - parseFrom);
        }
        
        private static int ParseInteger(string input, int parseFrom, out int destination)
        {
            destination = parseFrom;
            if (parseFrom >= input.Length)
            {
                return 0;
            }
            
            int sign = 1;
            int acc = 0;
            destination = parseFrom;
            if (input[destination] == '-')
            {
                if (destination + 1 > input.Length || !Char.IsDigit(input[destination + 1]))
                {
                    // not ok, just minus... return
                    return 0;
                }
                sign = -1;
                destination++;
            }

            while (destination < input.Length)
            {
                if (!Char.IsDigit(input[destination]))
                {
                    break;
                }

                acc = 10 * acc + (input[destination] - '0');
                destination++;
            }

            return acc * sign;
        }


        // performs stack compaction on closing bracket if it can
        private static void compactStackOnCloseBracket(ref List<IExpression> stack, int position)
        {
            // compact all operators until there is open bracket on the left.
            // invariant is: all priorities are going with increasing priority
            // Example:
            //  ... 5 7 ( 0 2 4 8 9 
            // let's denote {4} -- a compact operation. It takes both his arguments and merge them into binop
            // for example: {4} === {[a] [*] [b]} => {[a * b]}
            // it converts three stack values into one binop value (where priority of '*' considered as 4.
            // let's add ')' virtually to the example (but there is no representation in memory of ')' on the stack:
            //  ... 5 7 ( 0 2 4 8 {9} ) => ... 5 7 ( 0 2 4 {8} ) => ... 5 7 ( 0 2 {4} ) =>
            // ... 5 7 ( 0 {2} ) =>... 5 7 ( {0} ) => 5 7
            // if invariant is preserved, then we compact operators in right order
            const string closeUnbalancedMsg = "more closing brackets than opening brackets";
            const string emptyBracketsMsg   = "brackets enclose no value";
            const string orphanedOpMsg   = "orphaned binary operation with empty right operand";
            const string noLeftOperandPresented = "No left operand presented";
            compactLoop: while (true)
            {
                if (stack.Count == 0)
                {
                    throw new ParseException(closeUnbalancedMsg, position);
                }

                var idRight = stack.Count - 1;
                var idOp = idRight - 1;
                var idLeft = idOp - 1;

                switch (stack[idRight])
                {
                    case OpenBracket _:
                        throw new ParseException(emptyBracketsMsg, position);
                    case OrphanOperator _:
                        throw new ParseException(orphanedOpMsg, position);
                    case BaseExpression rhs:
                        if (idOp < 0)
                        {
                            throw new ParseException(closeUnbalancedMsg, position);
                        }
                        // right op is ok, check middle op
                        switch (stack[idOp])
                        {
                            case OpenBracket _:
                                // ok, compact stack just with presented value
                                stack.RemoveAt(idOp);
                                return; // function ends on eliminated bracket
                            case OrphanOperator oop:
                                if (idLeft < 0)
                                {
                                    throw new ParseException(noLeftOperandPresented, position);
                                }
                                // ok, look  what is left operand
                                switch (stack[idLeft])
                                {
                                    case BaseExpression lhs:
                                        // ok, compact
                                        BaseExpression compacted = new BinopExpression(lhs, rhs, oop.op);
                                        stack.RemoveRange(idLeft, 3);
                                        stack.Add(compacted);
                                        goto compactLoop; // repeat compaction in cycle
                                    default:
                                        throw new ParseException(noLeftOperandPresented, position);
                                }
                            default:
                                throw new ParseException("No operation between operands", position);
                        }
                    default:
                        throw new ParseException("Unknown compaction stack Expression instance", position);
                }

            }
        }
    
        // performs stack when new operator has been added.
        private static void compactStackOnOperand(ref List<IExpression> stack) {
            // if stack is less than 5:
            //    [a] [*] [b] [+] [c]
            // it is not interesting
            // in the upper example we can compact:
            //    [a] [*] [b] [+] [c] -> [a * b] [+] [c]
            // because priority of * is more than priority of +
            // invariant is: all priorities are going with increasing priority
            //  ... 5 7 ( 0 2 4 8 9 
            // when value of priority 5 comes into the game that's what happens:
            // (curly brackets designate compact operation)
            //  ... 5 7 ( 0 2 4 8{9}5 => ... 5 7 ( 0 2 4{8}5 =>  ... 5 7 ( 0 2 4 5
            // if invariant is preserved, then we compact operators in right order
            while (stack.Count >= 5)
            {
                int opId = stack.Count - 2;
                int prevOpId = opId - 2;
                
                if (stack[opId] is OrphanOperator op
                   && stack[prevOpId] is OrphanOperator prevOp
                   && stack[opId + 1] is BaseExpression opnd3
                   && stack[prevOpId + 1] is BaseExpression opnd2
                   && stack[prevOpId - 1] is BaseExpression opnd1
                   // check if priority of new operand is less than previous, so we can safely compact them
                   && BinopExpression.Priority(prevOp.op) >= BinopExpression.Priority(op.op)
                )
                {
                    // compact
                    BaseExpression compaqed = new BinopExpression(opnd1, opnd2, prevOp.op);
                    stack.RemoveRange(prevOpId - 1, 5);
                    stack.Add(compaqed);
                    stack.Add(op);
                    stack.Add(opnd3);
                }
                else
                {
                    break;
                }

            }
        }
        
        
        // performs final stack compaction free of brackets
        private static void CompactTerminal(ref List<IExpression> stack) {
            // this is final stack compaction.
            // invariant is: all priorities are going with increasing priority
            //               + no brackets can occur during final compaction!
            // if invariant holds, than final compaction is correct.
            
            // terminal compaction consider this last three values of of stack:
            // [a] [*] [b]
            // If there are not three values, it is not interesting
            while (stack.Count >= 3)
            {
                int opId = stack.Count - 2;
                
                if (stack[opId] is OrphanOperator op
                      && stack[opId + 1] is BaseExpression rhs
                      && stack[opId - 1] is BaseExpression lhs
                )
                {
                    // compact
                    BaseExpression compaqed = new BinopExpression(lhs, rhs, op.op);
                    stack.RemoveRange(opId - 1, 3);
                    stack.Add(compaqed);
                }
                else
                {
                    break;
                }

            }
        }
        
        // parsing methods are over

        /**
         * Acquire free variables of the expression
         */
        public string[] GetFreeVars()
        {
            // use visitor pattern :)
            var fvv = new FreeVarsVisitor();
            Visit(fvv);
            return fvv.Vars.ToArray();
        }

        private class FreeVarsVisitor : IExpressionVisitor
        {
            // sorted set to define variable order
            public SortedSet<string> Vars { get; } = new SortedSet<string>();
            
            public void VisitInteger(IntegerExpression i)
            {
            }

            public void VisitBinop(BinopExpression bop)
            {
                bop.Lhs.Visit(this);
                bop.Rhs.Visit(this);
            }

            public void VisitVar(VarExpression v)
            {
                Vars.Add(v.Name);
            }
        }
        
        
        /**
         * Normalization regroups operands with
         */
        public BaseExpression Normalize()
        {
            if (this is BinopExpression be)
            {
                DualOperandVisitor opVisitor = null;
                BinopExpression.BinopType positiveOp, negativeOp;
                switch (be.Op)
                {
                    case BinopExpression.BinopType.Add:
                    case BinopExpression.BinopType.Sub:
                        positiveOp = BinopExpression.BinopType.Add;
                        negativeOp = BinopExpression.BinopType.Sub;
                        break;
                    case BinopExpression.BinopType.Mul:
                    case BinopExpression.BinopType.Div:
                        positiveOp = BinopExpression.BinopType.Mul;
                        negativeOp = BinopExpression.BinopType.Div;
                        break;
                    default:
                        be.Lhs = be.Lhs.Normalize();
                        be.Rhs = be.Rhs.Normalize();
                        return be;
                }

                opVisitor = new DualOperandVisitor(positiveOp, negativeOp);
                be.Visit(opVisitor);
                
                var positiveOperand = FoldListWithOp(opVisitor.List, positiveOp);
                var negativeOperand = FoldListWithOp(opVisitor.DualList, positiveOp);
                if (negativeOperand != null)
                {
                    return new BinopExpression(positiveOperand, negativeOperand, negativeOp);
                }
                return positiveOperand;
            }

            return this;
        }

        private static BaseExpression FoldListWithOp(List<BaseExpression> list, BinopExpression.BinopType op)
        {
            var operands = list.Select(x => x.EvaluateBe()); // fold constants immediately
            
            var baseExpressions = operands as BaseExpression[] ?? operands.ToArray();
            var constants = baseExpressions.Where(x => x.IsConst).ToArray();
            var variables = baseExpressions.Where(x => x is VarExpression).ToArray();
            var other = baseExpressions.Where(x => !(x.IsConst || x is VarExpression)).ToArray();
            if (other.Length + constants.Length + variables.Length != list.Count())
            {
                throw new AstException("Wrong split of operands");
            }

            // fold constants and reduce
            var reducedConst = FoldExpr(constants, null, op)?.EvaluateBe();
            if (op == BinopExpression.BinopType.Add)
            {
                // make such order for addition: [complex] + [var] + [const]
                return FoldExpr(other, FoldExpr(variables, reducedConst, op), op);
            }
            // for other operations use order: [const] * [var] * [complex]
            var varAndComplex = FoldExpr(variables, FoldExpr(other, null, op), op);
            if (reducedConst != null)
            {
                return new BinopExpression(reducedConst, varAndComplex, op);
            }
            return varAndComplex;
        }

        private static BaseExpression FoldExpr(BaseExpression[] constants, BaseExpression init, BinopExpression.BinopType op)
        {
            
            // fold consts
            BaseExpression expr = init;
            if (constants.Length > 0)
            {
                var i = 0;
                if (expr is null)
                {
                    expr = constants[0];
                    i = 1;
                }
                for (; i < constants.Length; i++)
                {
                    expr = new BinopExpression(constants[i],expr, op);
                }

                expr = expr.EvaluateBe();
            }

            return expr;
        }
        
        
        private class DualOperandVisitor : IExpressionVisitor
        {
            // save operator instance to collect all operands with the same action
            private BinopExpression.BinopType positive, negative;
            private DualOperandVisitor dualVisitor;

            public List<BaseExpression> List { get; } = new List<BaseExpression>();
            public List<BaseExpression> DualList => dualVisitor.List;

            private DualOperandVisitor(DualOperandVisitor prototype)
            {
                this.positive = prototype.positive;
                this.negative = prototype.negative;
                this.dualVisitor = prototype;
            }
            public DualOperandVisitor(BinopExpression.BinopType positive, BinopExpression.BinopType negative)
            {
                this.positive = positive;
                this.negative = negative;
                this.dualVisitor = new DualOperandVisitor(this);
            }
            
            public void VisitInteger(IntegerExpression i)
            {
                List.Add(i);
            }

            public void VisitBinop(BinopExpression bop)
            {
                if (bop.Op == positive)
                {
                    bop.Lhs.Visit(this);
                    bop.Rhs.Visit(this);
                } else if (bop.Op == negative)
                {
                    bop.Lhs.Visit(this);
                    bop.Rhs.Visit(dualVisitor); // the rhs is negative position for operators '-' and '/'
                }
                else
                {
                    List.Add(bop);
                }
            }

            public void VisitVar(VarExpression v)
            {
                List.Add(v);
            }
        }
    }
    


    public interface IExpressionVisitor
    {
        void VisitInteger(IntegerExpression i);
        void VisitBinop(BinopExpression bop);
        void VisitVar(VarExpression v);
    }

    public class IntegerExpression : BaseExpression
    {
        public int Value { get; }

        public IntegerExpression(int val)
        {
            Value = val;
        }

        public override void Visit(IExpressionVisitor visitor)
        {
            visitor.VisitInteger(this);
        }

        public override BaseExpression EvaluateBe()
        {
            return this;
        }

        protected override bool EvalIsConst()
        {
            return true;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class VarExpression : BaseExpression
    {
        public string Name { get; }

        public VarExpression(string name)
        {
            this.Name = name;
        }

        public override void Visit(IExpressionVisitor visitor)
        {
            visitor.VisitVar(this);
        }

        public override BaseExpression EvaluateBe()
        {
            return this;
        }

        protected override bool EvalIsConst()
        {
            return false;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class BinopExpression : BaseExpression
    {
        public enum BinopType
        {
            Add,
            Sub,
            Mul,
            Div,
            Mod
        }
        
        // BinopType priorities
        private static readonly int[] BinopPriorityArray = {5, 5, 7, 7, 7};
        private static readonly bool[] BinopCommutes = {true, false, true, false, false};

        public static int Priority(BinopType op)
        {
            int id = (int) op;
            if (id < 0 || id >= BinopPriorityArray.Length)
            {
                throw new ArgumentException("Invalid operator index");
            }
            return BinopPriorityArray[id];
        }        
        
        public static bool Commutes(BinopType op)
        {
            int id = (int) op;
            if (id < 0 || id >= BinopPriorityArray.Length)
            {
                throw new ArgumentException("Invalid operator index");
            }
            return BinopCommutes[id];
        }

        public static string AsString(BinopType op)
        {
            switch (op)
            {
                case BinopType.Add: return "+";
                case BinopType.Sub: return "-";
                case BinopType.Mul: return "*";
                case BinopType.Div: return "/";
                case BinopType.Mod: return "%";
            }

            throw new ArgumentException("You should provide BinopType enum!");
        }

        public int Priority()
        {
            return Priority(Op);
        }
        public bool Commutes()
        {
            return Commutes(Op);
        }

        public BaseExpression Lhs;
        public BaseExpression Rhs;
        public BinopType Op;

        public BinopExpression(BaseExpression lhs, BaseExpression rhs, BinopType op)
        {
            this.Lhs = lhs;
            this.Rhs = rhs;
            this.Op = op;
        }

        public override void Visit(IExpressionVisitor visitor)
        {
            visitor.VisitBinop(this);
        }

        protected override bool EvalIsConst()
        {
            return Lhs.IsConst && Rhs.IsConst;
        }

        public override BaseExpression EvaluateBe()
        {
            if (Lhs.IsConst)
            {
                Lhs = Lhs.EvaluateBe();
            }

            if (Rhs.IsConst)
            {
                Rhs = Rhs.EvaluateBe();
            }

            if (Lhs.IsConst && Rhs.IsConst)
            {
                // try to add them as integers
                int lhsi, rhsi;
                if (Lhs.ExtractRawInteger(out lhsi))
                {
                    if (Rhs.ExtractRawInteger(out rhsi))
                    {
                        return new IntegerExpression(ApplyOp(lhsi, rhsi));
                    }
                }
            }

            return this; // no luck
        }

        private int ApplyOp(int a, int b)
        {
            switch (Op)
            {
                case BinopType.Add: return a + b;
                case BinopType.Sub: return a - b;
                case BinopType.Mul: return a * b;
                case BinopType.Div: return a / b; // TODO?
                case BinopType.Mod: return a % b;
                default: throw new ArithmeticException("Can't apply operation #" + Op);
            }
        }

        public override string ToString()
        {
            var leftString = Lhs.ToString();
            var rightString = Rhs.ToString();

            if (Lhs is BinopExpression lhsOp && lhsOp.Priority() < Priority())
            {
                leftString = "(" + leftString + ")";
            }
            if (Rhs is BinopExpression rhsOp && rhsOp.Priority() < Priority())
            {
                rightString = "(" + rightString + ")";
            }

            return leftString + AsString(Op) + rightString;
        }
    }
}