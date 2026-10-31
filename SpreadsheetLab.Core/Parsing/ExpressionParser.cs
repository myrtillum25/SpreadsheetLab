using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SpreadsheetLab.Core.Grammar;
using SpreadsheetLab.Core.Models;

namespace SpreadsheetLab.Core.Parsing;

// Stage 1: syntax check
public class ExpressionParser
{
    public const int MaxLength = 1000;   // protects from stack overflow

    public string ErrorMessage { get; private set; } = "";
    public int ErrorPosition { get; private set; }

    public bool CheckSyntax(string text)
    {
        return Parse(text) != null;
    }

    // Text -> parse tree (null if error)
    public LabCalculatorParser.CompileUnitContext? Parse(string text)
    {
        ErrorMessage = "";
        ErrorPosition = 0;

        if (text.Length > MaxLength)
        {
            ErrorMessage = $"Вираз занадто довгий (більше {MaxLength} символів)";
            ErrorPosition = MaxLength + 1;
            return null;
        }

        LabCalculatorParser.CompileUnitContext tree;
        try
        {
            var errorListener = new ThrowExceptionErrorListener();

            // Text -> tokens
            var lexer = new LabCalculatorLexer(new AntlrInputStream(text));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);

            // Tokens -> tree
            var tokens = new CommonTokenStream(lexer);
            var parser = new LabCalculatorParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            tree = parser.compileUnit();
        }
        catch (SyntaxErrorException ex)
        {
            ErrorMessage = ex.Message;
            ErrorPosition = ex.Position;
            return null;
        }

        // Variant 46: result must be logical
        if (!IsComparison(tree.expression()))
        {
            ErrorMessage = "Вираз має бути логічним: додайте порівняння (=, <, >, <=, >=, <>). Наприклад: A1 + 2 > 5";
            ErrorPosition = text.Length + 1;
            return null;
        }

        return tree;
    }

    // Is the main operation a comparison? (skip outer brackets)
    private static bool IsComparison(LabCalculatorParser.ExpressionContext expression)
    {
        while (expression is LabCalculatorParser.ParenthesizedExprContext parenthesized)
            expression = parenthesized.expression();

        return expression is LabCalculatorParser.ComparisonExprContext;
    }

    // All cell names used in the tree
    public static List<string> GetReferences(IParseTree tree)
    {
        var result = new List<string>();
        CollectReferences(tree, result);
        return result;
    }

    // Recursive tree walk
    private static void CollectReferences(IParseTree node, List<string> result)
    {
        if (node is LabCalculatorParser.IdentifierExprContext identifier)
            result.Add(CellAddress.Normalize(identifier.GetText()));

        for (int i = 0; i < node.ChildCount; i++)
            CollectReferences(node.GetChild(i), result);
    }
}
