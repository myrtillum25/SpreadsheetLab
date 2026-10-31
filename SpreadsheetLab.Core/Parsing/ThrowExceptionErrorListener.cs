using Antlr4.Runtime;
using SpreadsheetLab.Core.Grammar;

namespace SpreadsheetLab.Core.Parsing;

// Syntax error with position (from 1)
public class SyntaxErrorException : Exception
{
    public SyntaxErrorException(string message, int position) : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}

// Turns ANTLR errors into Ukrainian messages
public class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    // Lexer error: unknown symbol
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        // msg looks like: token recognition error at: '#'
        string symbol = msg.Substring(msg.IndexOf('\''));

        string message = $"Недопустимий символ {symbol}";
        if (symbol.Length > 1 && char.IsLetter(symbol[1]))
            message += " — імена клітинок і функцій пишуться латиницею";

        throw new SyntaxErrorException(message, charPositionInLine + 1);
    }

    // Parser error: token in the wrong place
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        string message;

        if (offendingSymbol.Type == TokenConstants.EOF)
            message = "Вираз обірвався: у кінці чогось не вистачає (числа, клітинки або дужки ')')";
        else if (offendingSymbol.Type == LabCalculatorLexer.NAME)
            message = $"Невідоме ім'я '{offendingSymbol.Text}'. Функції: mmax, mmin. Клітинки пишуться так: A1, B12";
        else
            message = $"Неочікуваний символ '{offendingSymbol.Text}'";

        throw new SyntaxErrorException(message, charPositionInLine + 1);
    }
}
