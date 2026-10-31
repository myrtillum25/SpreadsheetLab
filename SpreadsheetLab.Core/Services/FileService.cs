using System.Text.Encodings.Web;
using System.Text.Json;
using SpreadsheetLab.Core.Models;

namespace SpreadsheetLab.Core.Services;

// Save / open table as JSON
public class FileService
{
    public const string FileExtension = ".json";

    public string? LastFilePath { get; private set; }      // null = new table
    public bool HasUnsavedChanges { get; private set; }

    public void MarkChanged()
    {
        HasUnsavedChanges = true;
    }

    // For a new empty table
    public void Reset()
    {
        LastFilePath = null;
        HasUnsavedChanges = false;
    }

    public void SaveToFile(Spreadsheet table, string path)
    {
        // Size + non-empty expressions
        var data = new TableFile
        {
            Rows = table.RowCount,
            Columns = table.ColumnCount
        };

        foreach (Cell cell in table.GetAllCells())
        {
            if (!cell.IsEmpty)
                data.Cells[cell.Address] = cell.Expression;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,                                   // readable file
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // keep < and > as is
        };

        string json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(path, json);

        LastFilePath = path;
        HasUnsavedChanges = false;
    }

    public Spreadsheet LoadFromFile(string path)
    {
        string json = File.ReadAllText(path);

        TableFile? data;
        try
        {
            data = JsonSerializer.Deserialize<TableFile>(json);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Файл пошкоджений або не є файлом таблиці");
        }

        if (data == null || data.Rows < 1 || data.Rows > Spreadsheet.MaxRows ||
            data.Columns < 1 || data.Columns > Spreadsheet.MaxColumns)
        {
            throw new InvalidDataException("У файлі неправильний розмір таблиці");
        }

        var table = new Spreadsheet(data.Rows, data.Columns);
        table.LoadExpressions(data.Cells ?? new Dictionary<string, string>());

        LastFilePath = path;
        HasUnsavedChanges = false;
        return table;
    }

    // "table" -> "C:\Users\...\Documents\table.json"
    public static string ResolvePath(string input, string defaultFolder)
    {
        string path = input.Trim().Trim('"');

        if (!Path.HasExtension(path))
            path += FileExtension;

        if (!Path.IsPathRooted(path))
            path = Path.Combine(defaultFolder, path);

        return path;
    }

    // File structure
    private class TableFile
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new();
    }
}