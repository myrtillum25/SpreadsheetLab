namespace SpreadsheetLab.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Main window: title and size
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Електронні таблиці — ЛР №1, варіант 46",
            Width = 1280,
            Height = 820
        };
    }
}