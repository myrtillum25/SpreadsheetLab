using SpreadsheetLab.Core.Models;
using SpreadsheetLab.Core.Services;
// MAUI already has a class named Cell
using TableCell = SpreadsheetLab.Core.Models.Cell;

namespace SpreadsheetLab.App;

// Main window: UI only, logic is in SpreadsheetLab.Core
public partial class MainPage : ContentPage
{
    // Shown in "About"
    private const string AuthorInfo = "Автор: Іхонкіна Уляна, група К-26";

    private Spreadsheet table = new Spreadsheet();
    private readonly FileService fileService = new FileService();

    // "A1" -> its Entry on screen
    private readonly Dictionary<string, Entry> cellEntries = new();

    private bool isExpressionMode = true;   // false = VALUE mode
    private string selectedAddress = "A1";

    public MainPage()
    {
        InitializeComponent();
        BuildGrid();
    }

    // ==== Table on screen ====

    // Rebuild the whole grid (new table, open, resize)
    private void BuildGrid()
    {
        TableGrid.Children.Clear();
        TableGrid.RowDefinitions.Clear();
        TableGrid.ColumnDefinitions.Clear();
        cellEntries.Clear();

        // Columns: row numbers + table columns
        TableGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(45)));
        for (int column = 0; column < table.ColumnCount; column++)
            TableGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(130)));

        // Rows: column letters + table rows
        TableGrid.RowDefinitions.Add(new RowDefinition(new GridLength(30)));
        for (int row = 0; row < table.RowCount; row++)
            TableGrid.RowDefinitions.Add(new RowDefinition(new GridLength(40)));

        // A, B, C ...
        for (int column = 0; column < table.ColumnCount; column++)
            TableGrid.Add(CreateHeader(CellAddress.ColumnName(column)), column + 1, 0);

        for (int row = 0; row < table.RowCount; row++)
        {
            // 1, 2, 3 ...
            TableGrid.Add(CreateHeader((row + 1).ToString()), 0, row + 1);

            // Cells
            for (int column = 0; column < table.ColumnCount; column++)
            {
                string address = CellAddress.Make(row, column);
                Entry entry = CreateCellEntry(address);
                cellEntries[address] = entry;
                TableGrid.Add(entry, column + 1, row + 1); // (view, column, row)
            }
        }

        if (table.GetCell(selectedAddress) == null)
            selectedAddress = "A1";

        RefreshGrid();
    }

    private Label CreateHeader(string text)
    {
        return new Label
        {
            Text = text,
            BackgroundColor = Colors.Gainsboro,
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
    }

    private Entry CreateCellEntry(string address)
    {
        var entry = new Entry
        {
            TextColor = Colors.Black,
            BackgroundColor = Colors.White
        };

        // Click -> select
        entry.Focused += (sender, e) => SelectCell(address);
        // Enter or leave -> save expression
        entry.Completed += (sender, e) => OnCellEdited(address, entry.Text);
        entry.Unfocused += (sender, e) => OnCellEdited(address, entry.Text);

        return entry;
    }

    // Update texts, colors, formula bar, status
    private void RefreshGrid()
    {
        foreach (var pair in cellEntries)
        {
            TableCell cell = table.GetCell(pair.Key)!;
            Entry entry = pair.Value;

            entry.Text = cell.GetDisplayText(isExpressionMode);
            entry.IsReadOnly = !isExpressionMode;
            UpdateCellColor(pair.Key);
        }

        SelectedCellLabel.Text = selectedAddress;
        FormulaEntry.Text = table.GetCell(selectedAddress)!.Expression;
        UpdateStatus();
    }

    private void SelectCell(string address)
    {
        string previous = selectedAddress;
        selectedAddress = address;

        UpdateCellColor(previous);
        UpdateCellColor(address);

        SelectedCellLabel.Text = address;
        FormulaEntry.Text = table.GetCell(address)!.Expression;
        UpdateStatus();
    }

    // Red = error, blue = selected, white = normal
    private void UpdateCellColor(string address)
    {
        if (!cellEntries.ContainsKey(address))
            return;

        Entry entry = cellEntries[address];
        TableCell cell = table.GetCell(address)!;

        if (cell.HasError)
            entry.BackgroundColor = Colors.MistyRose;
        else if (address == selectedAddress)
            entry.BackgroundColor = Colors.LightBlue;
        else
            entry.BackgroundColor = Colors.White;
    }

    // Bottom line
    private void UpdateStatus()
    {
        TableCell cell = table.GetCell(selectedAddress)!;

        if (cell.HasError)
            CellInfoLabel.Text = $"{selectedAddress}: {cell.ErrorMessage}";
        else if (cell.IsEmpty)
            CellInfoLabel.Text = $"{selectedAddress}: порожня клітинка";
        else
            CellInfoLabel.Text = $"{selectedAddress} = {cell.GetDisplayText(false)}";

        string mode = isExpressionMode ? "ВИРАЗ" : "ЗНАЧЕННЯ";
        string file = fileService.LastFilePath == null ? "нова таблиця" : Path.GetFileName(fileService.LastFilePath);
        string unsaved = fileService.HasUnsavedChanges ? " (є незбережені зміни)" : "";

        FileInfoLabel.Text = $"Режим: {mode}  |  {table.RowCount} × {table.ColumnCount}  |  {file}{unsaved}";
    }

    // ==== Editing ====

    // Cell edited in the grid
    private async void OnCellEdited(string address, string? text)
    {
        if (!isExpressionMode)
            return; // VALUE mode shows values, not expressions

        string newText = (text ?? "").Trim();
        if (newText == table.GetCell(address)!.Expression)
            return; // nothing changed

        await ApplyExpression(address, newText);
    }

    // "Apply" button or Enter in formula bar
    private async void OnApplyClicked(object? sender, EventArgs e)
    {
        await ApplyExpression(selectedAddress, FormulaEntry.Text ?? "");
    }

    private async Task ApplyExpression(string address, string text)
    {
        bool ok = table.SetExpression(address, text);
        fileService.MarkChanged();
        RefreshGrid();

        if (!ok)
            await ShowError(address);
    }

    // Error message with position
    private async Task ShowError(string address)
    {
        TableCell cell = table.GetCell(address)!;
        string message = cell.ErrorMessage;

        int position = cell.ErrorPosition;
        if (position > 0)
        {
            int index = Math.Min(position - 1, cell.Expression.Length);
            string marked = cell.Expression.Insert(index, "▶");
            message += $"\n\nПозиція {position}:\n{marked}";
        }

        await DisplayAlertAsync($"Помилка в клітинці {address}", message, "OK");

        // Put cursor on the error
        SelectCell(address);
        if (position > 0)
        {
            int length = cell.Expression.Length;
            int index = Math.Min(position - 1, length);

            FormulaEntry.Focus();
            FormulaEntry.CursorPosition = index;
            FormulaEntry.SelectionLength = index < length ? 1 : 0;
        }
    }

    // ==== Rows and columns ====

    private async void OnAddRowClicked(object? sender, EventArgs e)
    {
        try
        {
            table.AddRow();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Додавання рядка", ex.Message, "OK");
            return;
        }

        fileService.MarkChanged();
        BuildGrid();
    }

    private async void OnAddColumnClicked(object? sender, EventArgs e)
    {
        try
        {
            table.AddColumn();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Додавання стовпця", ex.Message, "OK");
            return;
        }

        fileService.MarkChanged();
        BuildGrid();
    }

    private async void OnDeleteRowClicked(object? sender, EventArgs e)
    {
        if (table.RowCount <= 1)
        {
            await DisplayAlertAsync("Видалення рядка", "У таблиці має залишитися хоча б один рядок.", "OK");
            return;
        }

        int lastRow = table.RowCount - 1;
        bool confirmed = await ConfirmDelete($"Рядок {lastRow + 1}",
            table.RowHasData(lastRow), table.FindCellsReferringToRow(lastRow));
        if (!confirmed)
            return;

        table.RemoveRow();
        fileService.MarkChanged();
        BuildGrid();
    }

    private async void OnDeleteColumnClicked(object? sender, EventArgs e)
    {
        if (table.ColumnCount <= 1)
        {
            await DisplayAlertAsync("Видалення стовпця", "У таблиці має залишитися хоча б один стовпець.", "OK");
            return;
        }

        int lastColumn = table.ColumnCount - 1;
        bool confirmed = await ConfirmDelete($"Стовпець {CellAddress.ColumnName(lastColumn)}",
            table.ColumnHasData(lastColumn), table.FindCellsReferringToColumn(lastColumn));
        if (!confirmed)
            return;

        table.RemoveColumn();
        fileService.MarkChanged();
        BuildGrid();
    }

    // Ask before deleting data / referenced cells
    private async Task<bool> ConfirmDelete(string what, bool hasData, List<string> references)
    {
        if (!hasData && references.Count == 0)
            return true; // nothing to warn about

        string message = "";
        if (hasData)
            message += $"{what} містить дані — їх буде втрачено.\n\n";
        if (references.Count > 0)
            message += $"На нього посилаються клітинки: {string.Join(", ", references)}. " +
                       "Після видалення в них буде помилка.\n\n";
        message += "Видалити?";

        return await DisplayAlertAsync("Попередження", message, "Видалити", "Скасувати");
    }

    // ==== EXPRESSION / VALUE mode ====

    private void OnToggleModeClicked(object? sender, EventArgs e)
    {
        isExpressionMode = !isExpressionMode;
        ModeButton.Text = isExpressionMode ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
        RefreshGrid();
    }

    // ==== Files ====

    private async void OnNewClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmUnsavedChanges())
            return;

        table = new Spreadsheet();
        fileService.Reset();
        selectedAddress = "A1";
        BuildGrid();
    }

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmUnsavedChanges())
            return;

        string? path = await AskFilePath("Відкриття таблиці", "Відкрити", fileService.LastFilePath ?? "");
        if (path == null)
            return; // cancelled

        if (!File.Exists(path))
        {
            await DisplayAlertAsync("Відкриття таблиці", $"Файл не знайдено:\n{path}", "OK");
            return;
        }

        try
        {
            table = fileService.LoadFromFile(path);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Не вдалося відкрити файл", ex.Message, "OK");
            return;
        }

        selectedAddress = "A1";
        BuildGrid();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await SaveTable();
    }

    // true if saved
    private async Task<bool> SaveTable()
    {
        string suggested = fileService.LastFilePath ?? Path.Combine(DefaultFolder(), "таблиця.json");

        string? path = await AskFilePath("Збереження таблиці", "Зберегти", suggested);
        if (path == null)
            return false;

        try
        {
            fileService.SaveToFile(table, path);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Не вдалося зберегти", ex.Message, "OK");
            return false;
        }

        UpdateStatus();
        await DisplayAlertAsync("Збережено", $"Таблицю збережено у файл:\n{path}", "OK");
        return true;
    }

    // Ask file name / path (null if cancelled)
    private async Task<string?> AskFilePath(string title, string acceptButton, string initialValue)
    {
        string? input = await DisplayPromptAsync(title,
            "Вкажіть ім'я файлу або повний шлях.\nЯкщо вказати лише ім'я, файл буде в папці «Документи».",
            acceptButton, "Скасувати", initialValue: initialValue);

        if (string.IsNullOrWhiteSpace(input))
            return null;

        return FileService.ResolvePath(input, DefaultFolder());
    }

    private static string DefaultFolder()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    // true = can continue
    private async Task<bool> ConfirmUnsavedChanges()
    {
        if (!fileService.HasUnsavedChanges)
            return true;

        string choice = await DisplayActionSheetAsync(
            "У таблиці є незбережені зміни. Зберегти їх?",
            "Скасувати", null, "Зберегти", "Не зберігати");

        if (choice == "Зберегти")
            return await SaveTable();

        return choice == "Не зберігати";
    }

    // ==== Help, About, Exit ====

    private async void OnHelpClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new HelpPage());
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Про програму",
            "Електронні таблиці\n" +
            "Лабораторна робота №1, варіант 46\n\n" +
            "Клітинки містять логічні вирази, у яких можуть бути: \n - Цілі числа довільної довжини \n - Імена клітинок \n - Дужки \n - Операції: +, -, *, /, ^ \n - Функції mmax і mmin \n - Порівняння: =, <, >, <=, >=, <>\n\n" +
            "Розбір виразів — ANTLR 4. Інтерфейс — .NET MAUI.\n\n" +
            AuthorInfo,
            "OK");
    }

    private async void OnExitClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmUnsavedChanges())
            return;

        Application.Current?.Quit();
    }
}