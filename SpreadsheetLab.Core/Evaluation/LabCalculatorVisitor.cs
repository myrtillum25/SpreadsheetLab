using System.Numerics;
using Antlr4.Runtime;
using SpreadsheetLab.Core.Grammar;
using SpreadsheetLab.Core.Models;

namespace SpreadsheetLab.Core.Evaluation;

// Calculation error with position (from 1)
public class EvaluationException : Exception
{
    public EvaluationException(string message, int position) : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}

// Stage 2: calculates the parse tree
public class LabCalculatorVisitor : LabCalculatorBaseVisitor<BigInteger>
{
    public const int MaxExponent = 1000;   // protects from huge numbers

    private readonly Calculator calculator;   // gives values of other cells

    public LabCalculatorVisitor(Calculator calculator)
    {
        this.calculator = calculator;
    }

    // Whole formula -> true / false
    public bool Evaluate(LabCalculatorParser.CompileUnitContext tree)
    {
        return Visit(tree) != 0;
    }

    public override BigInteger VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
    {
        return Visit(context.expression());
    }

    // ( a )
    public override BigInteger VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
    {
        return Visit(context.expression());
    }

    // 123
    public override BigInteger VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
    {
        return BigInteger.Parse(context.GetText());
    }

    // A1 -> 1 or 0
    public override BigInteger VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
    {
        string address = CellAddress.Normalize(context.GetText());
        return calculator.GetCellValue(address, Position(context.Start));
    }

    // a + b, a - b
    public override BigInteger VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
    {
        BigInteger left = WalkLeft(context);
        BigInteger right = WalkRight(context);

        if (context.operatorToken.Type == LabCalculatorLexer.ADD)
            return left + right;
        else
            return left - right;
    }

    // a * b, a / b
    public override BigInteger VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
    {
        BigInteger left = WalkLeft(context);
        BigInteger right = WalkRight(context);

        if (context.operatorToken.Type == LabCalculatorLexer.MULTIPLY)
            return left * right;

        if (right == 0)
            throw new EvaluationException("Ділення на нуль", Position(context.operatorToken));

        return left / right;   // integer division
    }

    // a ^ b
    public override BigInteger VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
    {
        BigInteger left = WalkLeft(context);
        BigInteger right = WalkRight(context);
        int position = Position(context.EXPONENT().Symbol);

        if (BigInteger.Abs(right) > MaxExponent)
            throw new EvaluationException($"Занадто великий показник степеня (від -{MaxExponent} до {MaxExponent})", position);

        if (right >= 0)
            return BigInteger.Pow(left, (int)right);

        // Negative power: a^(-n) = 1 / a^n (integer division, like 7 / 2 = 3)
        if (left == 0)
            throw new EvaluationException("Ділення на нуль: 0 у від'ємному степені", position);

        return BigInteger.One / BigInteger.Pow(left, (int)(-right));
    }

    // -a
    public override BigInteger VisitUnaryMinusExpr(LabCalculatorParser.UnaryMinusExprContext context)
    {
        return -Visit(context.expression());
    }

    // a = b, a <> b, a < b, a > b, a <= b, a >= b  ->  1 or 0
    public override BigInteger VisitComparisonExpr(LabCalculatorParser.ComparisonExprContext context)
    {
        BigInteger left = WalkLeft(context);
        BigInteger right = WalkRight(context);
        bool result;

        switch (context.operatorToken.Type)
        {
            case LabCalculatorLexer.EQUAL: result = left == right; break;
            case LabCalculatorLexer.NOT_EQUAL: result = left != right; break;
            case LabCalculatorLexer.LESS: result = left < right; break;
            case LabCalculatorLexer.GREATER: result = left > right; break;
            case LabCalculatorLexer.LESS_EQUAL: result = left <= right; break;
            default: result = left >= right; break;
        }

        return result ? 1 : 0;
    }

    // mmax(...), mmin(...)
    public override BigInteger VisitFunctionExpr(LabCalculatorParser.FunctionExprContext context)
    {
        BigInteger result = Visit(context._arguments[0]);

        for (int i = 1; i < context._arguments.Count; i++)
        {
            BigInteger value = Visit(context._arguments[i]);

            if (context.functionName.Type == LabCalculatorLexer.MMAX)
            {
                if (value > result) result = value;
            }
            else
            {
                if (value < result) result = value;
            }
        }

        return result;
    }

    // Left operand
    private BigInteger WalkLeft(ParserRuleContext context)
    {
        return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
    }

    // Right operand
    private BigInteger WalkRight(ParserRuleContext context)
    {
        return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
    }

    // ANTLR counts from 0, user from 1
    private static int Position(IToken token)
    {
        return token.Column + 1;
    }
}
