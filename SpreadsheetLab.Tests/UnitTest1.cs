using SpreadsheetLab.Core.Evaluation;
using SpreadsheetLab.Core.Models;
using Xunit;

namespace SpreadsheetLab.Tests;

public class SpreadsheetTests
{
    // ТЕСТ 1: Перевірка функцій (mmax, mmin, степінь ^ та логічне порівняння)
    [Fact]
    public void Evaluate_MmaxAndPower_CalculatesCorrectLogicalResult()
    {
        // 1. Arrange (Підготовка таблиці)
        var table = new Spreadsheet(3, 3);
        var calculator = new Calculator(table);

        table.GetCell("A1")!.SetExpression("mmax(2, 8, 3) = 2 ^ 3");

        // 2. Act (Дія - перераховуємо таблицю)
        calculator.RecalculateAll();

        // 3. Assert (Перевірка результату)
        Cell cellA1 = table.GetCell("A1")!;
        Assert.False(cellA1.HasError, "Клітинка не повинна містити помилок");
        Assert.True(cellA1.Value, "Вираз mmax(2, 8, 3) = 2 ^ 3 має бути TRUE");
    }

    // ТЕСТ 2: Перевірка посилання між клітинками
    [Fact]
    public void Evaluate_CellReferences_CalculatesDependentCells()
    {
        // 1. Arrange
        var table = new Spreadsheet(3, 3);
        var calculator = new Calculator(table);

        // A1 має значення true (бо 10 > 5)
        table.GetCell("A1")!.SetExpression("10 > 5");
        // B1 бере значення A1 (якщо A1 істина, то 1 = 1 -> true)
        table.GetCell("B1")!.SetExpression("A1 = 1");

        // 2. Act
        calculator.RecalculateAll();

        // 3. Assert
        Cell cellB1 = table.GetCell("B1")!;
        Assert.False(cellB1.HasError, "Клітинка B1 не повинна містити помилок");
        Assert.True(cellB1.Value, "B1 має бути TRUE, бо посилається на істинну A1");
    }

    // ТЕСТ 3: Виявлення циклічних посилань
    [Fact]
    public void Evaluate_CircularReference_SetsErrorOnCell()
    {
        // 1. Arrange: створюємо замкнене коло (A1 посилається на B1, а B1 на A1)
        var table = new Spreadsheet(3, 3);
        var calculator = new Calculator(table);

        table.GetCell("A1")!.SetExpression("B1 = 1");
        table.GetCell("B1")!.SetExpression("A1 = 1");

        // 2. Act
        calculator.RecalculateAll();

        // 3. Assert: обидві клітинки повинні мати помилку
        Cell cellA1 = table.GetCell("A1")!;
        Cell cellB1 = table.GetCell("B1")!;

        Assert.True(cellA1.HasError, "A1 повинна мати помилку");
        Assert.True(cellB1.HasError, "B1 повинна мати помилку");
        // Переконуємося, що хоча б одна з клітинок прямо вказала на циклічне посилання
        Assert.True(cellA1.ErrorMessage.Contains("Циклічне") || cellB1.ErrorMessage.Contains("Циклічне"),
            "Має бути зафіксоване саме циклічне посилання");
    }
}