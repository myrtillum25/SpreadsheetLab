using System.Numerics;
using SpreadsheetLab.Core.Grammar;
using SpreadsheetLab.Core.Models;
using SpreadsheetLab.Core.Parsing;

namespace SpreadsheetLab.Core.Evaluation;

// Recalculates all cells of the table
public class Calculator
{
    private readonly Spreadsheet table;
    private readonly ExpressionParser parser = new();
    private readonly LabCalculatorVisitor visitor;

    private readonly HashSet<string> calculated = new();   // already done in this pass
    private readonly HashSet<string> inProgress = new();   // being calculated now (cycle check)

    public Calculator(Spreadsheet table)
    {
        this.table = table;
        visitor = new LabCalculatorVisitor(this);
    }

    public void RecalculateAll()
    {
        calculated.Clear();
        inProgress.Clear();

        foreach (Cell cell in table.GetAllCells())
            CalculateCell(cell);
    }

    // Value of another cell: 1 or 0 (called by the visitor)
    public BigInteger GetCellValue(string address, int position)
    {
        Cell? cell = table.GetCell(address);

        if (cell == null)
            throw new EvaluationException($"Клітинки {address} немає в таблиці", position);

        if (inProgress.Contains(address))
            throw new EvaluationException($"Циклічне посилання на клітинку {address}", position);

        CalculateCell(cell);   // calculate it first

        if (cell.HasError)
            throw new EvaluationException($"Клітинка {address} містить помилку", position);

        return cell.Value ? 1 : 0;
    }

    // Cells used in the cell's expression
    public List<string> GetReferences(Cell cell)
    {
        if (cell.IsEmpty)
            return new List<string>();

        LabCalculatorParser.CompileUnitContext? tree = parser.Parse(cell.Expression);
        if (tree == null)
            return new List<string>();

        return ExpressionParser.GetReferences(tree);
    }

    // Calculate once per pass
    private void CalculateCell(Cell cell)
    {
        if (calculated.Contains(cell.Address))
            return;

        inProgress.Add(cell.Address);
        Evaluate(cell);
        inProgress.Remove(cell.Address);

        calculated.Add(cell.Address);
    }

    // Two stages: syntax, then value
    private void Evaluate(Cell cell)
    {
        if (cell.IsEmpty)
        {
            cell.ClearResult();
            return;
        }

        // Stage 1
        LabCalculatorParser.CompileUnitContext? tree = parser.Parse(cell.Expression);
        if (tree == null)
        {
            cell.SetError("Синтаксична помилка: " + parser.ErrorMessage, parser.ErrorPosition);
            return;
        }

        // Stage 2
        try
        {
            bool value = visitor.Evaluate(tree);
            cell.SetValue(value);
        }
        catch (EvaluationException ex)
        {
            cell.SetError("Помилка обчислення: " + ex.Message, ex.Position);
        }
    }
}
