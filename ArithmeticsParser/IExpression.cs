using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ArithmeticsParser
{
    public class ParseException : Exception
    {
        private string _msg;
        private int _position;

        public ParseException(string msg, int position)
        {
            _msg = msg;
            _position = position;
        }

        public override string ToString()
        {
            return "ParseException: " + _msg + ", at position " + _position + ".";
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
        private bool _evaluatedConst = false;
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
        public static BaseExpression parse(string input)
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
                        int dest = -1;
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
                                int result = parseInteger(input, i, out dest);
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
                        string asVarname = parseVarname(input, i, out dest);
                        if (dest > i)
                        {
                            stack.Add(new VarExpression(asVarname));
                            i = dest;
                            compactStackOnOperand(ref stack);
                            break;
                        }
                        
                        // try to parse as integer
                        int asInt = parseInteger(input, i, out dest);
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
            compactTerminal(ref stack);
            if (stack.Count != 1)
            {
                throw new ParseException("not valid expression, maybe not enough brackets", input.Length);
            }

            IExpression top = stack[0];
            if (top is BaseExpression)
            {
                return (BaseExpression)top;
            }
            else
            {
                throw new ParseException("error of evaluation to basic expression", input.Length);
            }

            
        }

        private static string parseVarname(string input, int parseFrom, out int destination)
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
        
        private static int parseInteger(string input, int parseFrom, out int destination)
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
            const string closeUnbalancedMsg = "more closing brackets than opening brackets";
            const string emptyBracketsMsg   = "brackets enclose no value";
            const string orphanedOpMsg   = "orphaned binary operation with empty right operand";
            var noLeftOperandPresented = "No left operand presented";
            compactLoop: while (true)
            {
                if (stack.Count == 0)
                {
                    throw new ParseException(closeUnbalancedMsg, position);
                }

                int idRight = stack.Count - 1;
                int idOp = idRight - 1;
                int idLeft = idOp - 1;

                switch (stack[idRight])
                {
                    case OpenBracket os:
                        throw new ParseException(emptyBracketsMsg, position);
                    case OrphanOperator os:
                        throw new ParseException(orphanedOpMsg, position);
                    case BaseExpression rhs:
                        if (idOp < 0)
                        {
                            throw new ParseException(closeUnbalancedMsg, position);
                        }
                        // right op is ok, check middle op
                        switch (stack[idOp])
                        {
                            case OpenBracket os:
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
    
        // performs stack compaction on closing bracket if it can
        private static void compactStackOnOperand(ref List<IExpression> stack) {
            // if stack is less than 5:
            // [a] [*] [b] [+] [c]
            // it is not interesting
            while (stack.Count >= 5)
            {
                int opId = stack.Count - 2;
                int prevOpId = opId - 2;
                
                if (stack[opId] is OrphanOperator && stack[prevOpId] is OrphanOperator
                   && stack[opId + 1] is BaseExpression
                   && stack[prevOpId + 1] is BaseExpression
                   && stack[prevOpId - 1] is BaseExpression
                )
                {
                    OrphanOperator op = (OrphanOperator)stack[opId];
                    OrphanOperator prevOp = (OrphanOperator)stack[prevOpId];
                    if (BinopExpression.Priority(prevOp.op) >= BinopExpression.Priority(op.op))
                    {
                        // compact
                        BaseExpression compaqed = new BinopExpression((BaseExpression) stack[prevOpId - 1],
                            (BaseExpression) stack[prevOpId + 1], prevOp.op);
                        IExpression top = stack[opId + 1];
                        stack.RemoveRange(prevOpId - 1, 5);
                        stack.Add(compaqed);
                        stack.Add(op);
                        stack.Add(top);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

            }
        }
        
        
        // performs stack compaction on closing bracket if it can
        private static void compactTerminal(ref List<IExpression> stack) {
            // terminal compaction consider this type of stack:
            // [a] [*] [b]
            // it is not interesting
            while (stack.Count >= 3)
            {
                int opId = stack.Count - 2;
                
                if (stack[opId] is OrphanOperator
                      && stack[opId + 1] is BaseExpression
                      && stack[opId - 1] is BaseExpression
                )
                {
                    OrphanOperator op = (OrphanOperator)stack[opId];
                    // compact
                    BaseExpression compaqed = new BinopExpression((BaseExpression) stack[opId - 1],
                        (BaseExpression) stack[opId + 1], op.op);
                    stack.RemoveRange(opId - 1, 3);
                    stack.Add(compaqed);
                }
                else
                {
                    break;
                }

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
    }

    public class VarExpression : BaseExpression
    {
        private string Name { get; }

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
        private static readonly int[] _priority = {5, 5, 7, 7, 7};

        public static int Priority(BinopType op)
        {
            int id = (int) op;
            if (id < 0 || id >= _priority.Length)
            {
                return -1;
            }
            return _priority[id];
        }

        private BaseExpression Lhs;
        private BaseExpression Rhs;
        private BinopType Op;

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
    }
}