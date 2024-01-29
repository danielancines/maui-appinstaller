using Microsoft.Win32;
using System.ComponentModel;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Utils.Commands;

namespace Maui.AppInstaller.ViewModel;

public class MainPageViewModel : INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Fields

    private readonly HttpClient? _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion

    #region Constructor

    public MainPageViewModel(HttpClient httpClient, IHttpClientFactory httpClientFactory)
    {
        this._httpClient = httpClient;
        this._httpClientFactory = httpClientFactory;
        this.InitializeCommands();
    }

    #endregion

    #region Commands

    public ObservableCommand? DownloadCommand { get; private set; }
    public ObservableCommand?CancelCommand { get; private set; }

    #endregion

    #region Properties

    private double _progress;
    public double Progress
    {
        get { return this._progress; }
        set
        {
            if (this._progress == value)
                return;

            this._progress = value;
            this.OnPropertyChanged();
        }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get { return this._downloadProgress; }
        set
        {
            if (this._downloadProgress == value)
                return;

            this._downloadProgress = value;
            this.OnPropertyChanged();
        }
    }


    private string _downloadingFileName;
    public string DownloadingFileName
    {
        get { return this._downloadingFileName; }
        set
        {
            if (this._downloadingFileName == value)
                return;

            this._downloadingFileName = value;
            this.OnPropertyChanged();
        }
    }


    #endregion

    #region Private Methods

    private void InitializeCommands()
    {
        this.DownloadCommand = new ObservableCommand(this.OnDownloadCommand);
        this.CancelCommand = new ObservableCommand(this.OnCancelDownload);
    }
    private void OnCancelDownload(object? obj)
    {
        this._cancellationTokenSource.Cancel();
    }

    CancellationTokenSource _cancellationTokenSource;
    private async void OnDownloadCommand(object? parameter)
    {
        (string DownloadUri, string FileName)[] filesToDownload = new (string downloadUri, string fileName)[5]
        {
            ("http://212.183.159.230/5MB.zip", "C:\\Users\\danie\\Downloads\\200mb.bin"),
            ("http://212.183.159.230/5MB.zip", "C:\\Users\\danie\\Downloads\\200_1mb.bin"),
            ("http://212.183.159.230/5MB.zip", "C:\\Users\\danie\\Downloads\\200_2mb.bin"),
            ("http://212.183.159.230/5MB.zip", "C:\\Users\\danie\\Downloads\\200_3mb.bin"),
            ("http://212.183.159.230/5MB.zip", "C:\\Users\\danie\\Downloads\\200_4mb.bin")
        };

        //var filesToDownloadArray = new Task[5]
        //{
        //    Task.Run(() => _ = this.DownloadFile("http://212.183.159.230/200MB.zip", "C:\\Users\\danie\\Downloads\\200mb.bin")),
        //    Task.Run(() => _ = this.DownloadFile("http://212.183.159.230/200MB.zip", "C:\\Users\\danie\\Downloads\\200_1mb.bin")),
        //    Task.Run(() => _ = this.DownloadFile("http://212.183.159.230/200MB.zip", "C:\\Users\\danie\\Downloads\\200_2mb.bin")),
        //    Task.Run(() => _ = this.DownloadFile("http://212.183.159.230/200MB.zip", "C:\\Users\\danie\\Downloads\\200_3mb.bin")),
        //    Task.Run(() => _ = this.DownloadFile("http://212.183.159.230/200MB.zip", "C:\\Users\\danie\\Downloads\\200_4mb.bin")),
        //};

        var client = this._httpClientFactory.CreateClient();
        var file = await client.GetFromJsonAsync<InstallerVersion>("https://ancines-myapp.s3.amazonaws.com/production/installerConfig.json");

        //var zipFile = await client.GetStreamAsync("https://ancines-myapp.s3.amazonaws.com/production/v1.0.0.zip");


        this._cancellationTokenSource = new CancellationTokenSource();
        //await this.DownloadFile("https://ancines-myapp.s3.amazonaws.com/production/v1.0.0.zip", string.Empty, this._cancellationTokenSource.Token);

        this.WriteOnRegistry();


        //foreach (var task in filesToDownload)
        //{
        //    this._totalBytesRead = 0;
        //    this._totalFileSize = 0;
        //    await Task.Run(() => this.DownloadFile(task.DownloadUri, task.FileName, this._cancellationTokenSource.Token));
        //}

        //Task.WaitAll(filesToDownloadArray);

        //Parallel.ForEach(filesToDownload, file =>
        //{
        //    _ = this.DownloadFile(file.DownloadUri, file.FileName);
        //});
    }

    private void WriteOnRegistry()
    {
        var softwareKey = Registry.CurrentUser.OpenSubKey("Software", true);

        string registryKeyPath = @"MyAppInstaller";
        string valueName = "AppFullPath";

        // Define the value to write to the registry
        string valueData = "C:\\Users\\danie\\Downloads\\v1.0.0\\Maui.AppInstaller1.exe";

        try
        {
            // Open the registry key for writing
            using (var registryKey = softwareKey.CreateSubKey(registryKeyPath))
            {
                // Write the value to the registry
                registryKey.SetValue(valueName, valueData);
                Console.WriteLine("Value successfully written to the registry.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the registry: {ex.Message}");
        }
    }

    private long _totalFileSize, _totalBytesRead;
    private async Task DownloadFile(string downloadUri, string fileName, CancellationToken cancellationToken)
    {
        this.DownloadProgress = 0;

        try
        {
            var httpClient = this._httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 0);
            var getResponse = await httpClient.GetAsync(downloadUri);
            var totalFileSize = getResponse.Content.Headers.ContentRange?.Length ?? 0;

            httpClient.DefaultRequestHeaders.Range = null;
            var response = await httpClient.GetStreamAsync(downloadUri);
            if (response == null)
                return;

            _totalFileSize += totalFileSize;

            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[8192]; // Adjust buffer size as needed
                int bytesRead;

                while ((bytesRead = await response.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    //this.DownloadingFileName = Path.GetFileName(fileName);
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    _totalBytesRead += bytesRead;
                    this.Progress = (double)_totalBytesRead / _totalFileSize;
                    this.DownloadProgress = Math.Truncate(this.Progress * 100);
                }

                ZipFile.ExtractToDirectory(memoryStream, "C:\\Users\\danie\\Downloads");
            }

            //using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            //{
            //    byte[] buffer = new byte[8192]; // Adjust buffer size as needed
            //    int bytesRead;

            //    while ((bytesRead = await response.ReadAsync(buffer, 0, buffer.Length)) > 0)
            //    {
            //        if (cancellationToken.IsCancellationRequested) return;

            //        this.DownloadingFileName = Path.GetFileName(fileName);
            //        await fileStream.WriteAsync(buffer, 0, bytesRead);
            //        _totalBytesRead += bytesRead;
            //        this.Progress = (double)_totalBytesRead / _totalFileSize;
            //        this.DownloadProgress = Math.Truncate(this.Progress * 100);
            //    }

            //    ZipFile.ExtractToDirectory(fileStream, "C:\\Users\\danie\\Downloads");
            //}
        }
        catch (Exception ex)
        {
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

public class InstallerVersion
{
    [JsonPropertyName("version")]
    public Version? Version { get; set; }

    [JsonPropertyName("minVersion")]
    public Version? MinVersion { get; set; }

    [JsonPropertyName("versionEndpoint")]
    public string? FilesPath { get; set; }

    [JsonPropertyName("updaterVersion")]
    public Version? UpdaterVersion { get; set; }
}
