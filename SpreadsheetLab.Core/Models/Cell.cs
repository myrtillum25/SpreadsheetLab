namespace SpreadsheetLab.Core.Models;

// One table cell
public class Cell
{
    public const string TrueText = "ІСТИНА";
    public const string FalseText = "ХИБНІСТЬ";
    public const string ErrorText = "#ПОМИЛКА";

    public Cell(string address)
    {
        Address = address;
    }

    public string Address { get; }                          // e.g. "B1"
    public string Expression { get; private set; } = "";    // what the user typed
    public bool Value { get; private set; }                 // calculated result
    public bool HasError { get; private set; }
    public string ErrorMessage { get; private set; } = "";
    public int ErrorPosition { get; private set; }          // char number, from 1

    public bool IsEmpty => string.IsNullOrWhiteSpace(Expression);

    public void SetExpression(string text)
    {
        Expression = text?.Trim() ?? "";
    }

    public void SetValue(bool value)
    {
        Value = value;
        HasError = false;
        ErrorMessage = "";
        ErrorPosition = 0;
    }

    public void SetError(string message, int position = 0)
    {
        Value = false;
        HasError = true;
        ErrorMessage = message;
        ErrorPosition = position;
    }

    // Empty cell = false
    public void ClearResult()
    {
        SetValue(false);
    }

    // Text shown in the grid (EXPRESSION / VALUE mode)
    public string GetDisplayText(bool isExpressionMode)
    {
        if (isExpressionMode) return Expression;
        if (IsEmpty) return "";
        if (HasError) return ErrorText;
        return Value ? TrueText : FalseText;
    }
}