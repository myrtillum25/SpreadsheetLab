using SpreadsheetLab.Core.Evaluation;

namespace SpreadsheetLab.Core.Models;

// The table: cells + size operations (no UI here)
public class Spreadsheet
{
    public const int DefaultRows = 10;
    public const int DefaultColumns = 8;   // A..H
    public const int MaxRows = 50;
    public const int MaxColumns = 26;      // A..Z

    // "B3" -> cell
    private readonly Dictionary<string, Cell> cells = new();
    private readonly Calculator calculator;

    public Spreadsheet(int rows = DefaultRows, int columns = DefaultColumns)
    {
        if (rows < 1 || rows > MaxRows || columns < 1 || columns > MaxColumns)
            throw new ArgumentException($"Розмір таблиці має бути від 1×1 до {MaxRows}×{MaxColumns}");

        RowCount = rows;
        ColumnCount = columns;
        calculator = new Calculator(this);

        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
                AddCell(row, column);
    }

    public int RowCount { get; private set; }
    public int ColumnCount { get; private set; }

    // null if no such cell
    public Cell? GetCell(string address)
    {
        cells.TryGetValue(CellAddress.Normalize(address), out Cell? cell);
        return cell;
    }

    public Cell? GetCell(int row, int column)
    {
        return GetCell(CellAddress.Make(row, column));
    }

    public IEnumerable<Cell> GetAllCells()
    {
        return cells.Values;
    }

    // Set expression and recalculate; true if no error
    public bool SetExpression(string address, string text)
    {
        Cell? cell = GetCell(address);
        if (cell == null)
            return false;

        cell.SetExpression(text);
        calculator.RecalculateAll();
        return !cell.HasError;
    }

    // Many expressions at once (file open)
    public void LoadExpressions(Dictionary<string, string> expressions)
    {
        foreach (var pair in expressions)
            GetCell(pair.Key)?.SetExpression(pair.Value);

        calculator.RecalculateAll();
    }

    // ---------- Size ----------

    public void AddRow()
    {
        if (RowCount >= MaxRows)
            throw new InvalidOperationException($"Максимальна кількість рядків — {MaxRows}");

        for (int column = 0; column < ColumnCount; column++)
            AddCell(RowCount, column);

        RowCount++;
        calculator.RecalculateAll();
    }

    public void AddColumn()
    {
        if (ColumnCount >= MaxColumns)
            throw new InvalidOperationException($"Максимальна кількість стовпців — {MaxColumns}");

        for (int row = 0; row < RowCount; row++)
            AddCell(row, ColumnCount);

        ColumnCount++;
        calculator.RecalculateAll();
    }

    // Removes the last row
    public void RemoveRow()
    {
        if (RowCount <= 1)
            throw new InvalidOperationException("У таблиці має залишитися хоча б один рядок");

        int lastRow = RowCount - 1;
        for (int column = 0; column < ColumnCount; column++)
            cells.Remove(CellAddress.Make(lastRow, column));

        RowCount--;
        calculator.RecalculateAll();
    }

    // Removes the last column
    public void RemoveColumn()
    {
        if (ColumnCount <= 1)
            throw new InvalidOperationException("У таблиці має залишитися хоча б один стовпець");

        int lastColumn = ColumnCount - 1;
        for (int row = 0; row < RowCount; row++)
            cells.Remove(CellAddress.Make(row, lastColumn));

        ColumnCount--;
        calculator.RecalculateAll();
    }

    // ---------- Checks before delete ----------

    public bool RowHasData(int row)
    {
        for (int column = 0; column < ColumnCount; column++)
        {
            if (!GetCell(row, column)!.IsEmpty)
                return true;
        }
        return false;
    }

    public bool ColumnHasData(int column)
    {
        for (int row = 0; row < RowCount; row++)
        {
            if (!GetCell(row, column)!.IsEmpty)
                return true;
        }
        return false;
    }

    // Cells from other rows that use this row
    public List<string> FindCellsReferringToRow(int row)
    {
        var result = new List<string>();

        foreach (Cell cell in cells.Values)
        {
            CellAddress.TryParse(cell.Address, out int cellRow, out _);
            if (cellRow == row)
                continue; // will be deleted itself

            foreach (string reference in calculator.GetReferences(cell))
            {
                if (CellAddress.TryParse(reference, out int referenceRow, out _) && referenceRow == row)
                {
                    result.Add(cell.Address);
                    break;
                }
            }
        }

        return result;
    }

    // Cells from other columns that use this column
    public List<string> FindCellsReferringToColumn(int column)
    {
        var result = new List<string>();

        foreach (Cell cell in cells.Values)
        {
            CellAddress.TryParse(cell.Address, out _, out int cellColumn);
            if (cellColumn == column)
                continue;

            foreach (string reference in calculator.GetReferences(cell))
            {
                if (CellAddress.TryParse(reference, out _, out int referenceColumn) && referenceColumn == column)
                {
                    result.Add(cell.Address);
                    break;
                }
            }
        }

        return result;
    }

    private void AddCell(int row, int column)
    {
        string address = CellAddress.Make(row, column);
        cells[address] = new Cell(address);
    }
}