using SpreadsheetLab.Core.Services;
#if WINDOWS
using Windows.Storage.Pickers;
#endif

namespace SpreadsheetLab.App;

// Explorer windows for opening and saving files
public static class FileDialogs
{
    // Open window: path of the chosen .json file (null if cancelled)
    public static async Task<string?> PickFileToOpen()
    {
        // Show only .json files
        var jsonFiles = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { FileService.FileExtension } }
        });

        FileResult? file = await FilePicker.Default.PickAsync(new PickOptions { FileTypes = jsonFiles });

        return file?.FullPath;
    }

    // Save window: path for the file (null if cancelled)
    public static async Task<string?> PickFileToSave(string suggestedName)
    {
#if WINDOWS
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedName
        };
        picker.FileTypeChoices.Add("Таблиця (JSON)", new List<string> { FileService.FileExtension });

        // Attach the window to our program
        IntPtr windowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

        var file = await picker.PickSaveFileAsync();

        return file?.Path;
#else
        await Task.CompletedTask;
        return null;    // only Windows is supported
#endif
    }
}
