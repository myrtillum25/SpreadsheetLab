namespace SpreadsheetLab.Core.Models;

// Helpers for cell names like "B3"
public static class CellAddress
{
    // 0 -> "A", 1 -> "B", ...
    public static string ColumnName(int column)
    {
        char letter = (char)('A' + column);
        return letter.ToString();
    }

    // (row 0, column 1) -> "B1"
    public static string Make(int row, int column)
    {
        return ColumnName(column) + (row + 1);
    }

    // "B3" -> row 2, column 1
    public static bool TryParse(string address, out int row, out int column)
    {
        row = -1;
        column = -1;

        if (string.IsNullOrEmpty(address) || address.Length < 2)
            return false;

        // Column letter
        char letter = char.ToUpper(address[0]);
        if (letter < 'A' || letter > 'Z')
            return false;

        // Row number
        string digits = address.Substring(1);
        if (!int.TryParse(digits, out int number) || number < 1)
            return false;

        row = number - 1;
        column = letter - 'A';
        return true;
    }

    // " b3 " -> "B3"
    public static string Normalize(string address)
    {
        return address.Trim().ToUpper();
    }
}