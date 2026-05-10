using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CircuitForge;

public partial class MainWindow : Window
{
    private readonly string _projectPath = Path.Combine(
        AppContext.BaseDirectory,
        "Projects",
        "StarterKit");

    private readonly string? _pioPath = FindPlatformIo();

    private string _activeFile = "main.cpp";

    private readonly Dictionary<string, string> _files = new()
    {
        ["main.cpp"] = """
        // Lesson 2: blink without delay
        #include <Arduino.h>

        const int ledPin = 13;
        unsigned long previousMillis = 0;
        const long interval = 500;
        bool ledState = LOW;

        void setup() {
          pinMode(ledPin, OUTPUT);
          Serial.begin(115200);
        }

        void loop() {
          unsigned long currentMillis = millis();

          if (currentMillis - previousMillis >= interval) {
            previousMillis = currentMillis;
            ledState = !ledState;
            digitalWrite(ledPin, ledState);
            Serial.println(ledState ? "LED on" : "LED off");
          }
        }
        """,
        ["platformio.ini"] = """
        [env:uno]
        platform = atmelavr
        board = uno
        framework = arduino
        monitor_speed = 115200
        """,
        ["notes.md"] = """
        # Learning notes

        - millis() lets the loop keep running.
        - delay() blocks every other task.
        - Serial output is the fastest first debugging tool.
        """
    };

    private readonly Dictionary<string, (string Clock, string Flash, string Environment, string Platform, string Board)> _boards = new()
    {
        ["Arduino Uno R3"] = ("16 MHz", "32 KB", "uno", "atmelavr", "uno"),
        ["ESP32 DevKit v1"] = ("240 MHz", "4 MB", "esp32dev", "espressif32", "esp32dev"),
        ["Raspberry Pi Pico"] = ("133 MHz", "2 MB", "pico", "raspberrypi", "pico"),
        ["STM32 Nucleo F401RE"] = ("84 MHz", "512 KB", "nucleo_f401re", "ststm32", "nucleo_f401re")
    };

    public MainWindow()
    {
        InitializeComponent();
        EnsureStarterProject();
        ProjectPathText.Text = _projectPath;
        PlatformIoStatusText.Text = _pioPath is not null ? "PIO" : "NO PIO";
        OpenFile("main.cpp");
    }

    private void OpenMainFile(object sender, RoutedEventArgs e) => OpenFile("main.cpp");

    private void OpenConfigFile(object sender, RoutedEventArgs e) => OpenFile("platformio.ini");

    private void OpenNotesFile(object sender, RoutedEventArgs e) => OpenFile("notes.md");

    private void OpenFile(string fileName)
    {
        SaveActiveFile();
        _activeFile = fileName;
        LoadFilesFromDisk();
        EditorText.Text = _files[fileName];
        ActiveFileText.Text = fileName == "main.cpp" ? "src/main.cpp" : fileName;
        LineNumbersText.Text = string.Join(Environment.NewLine, Enumerable.Range(1, _files[fileName].Split('\n').Length));
    }

    private void BoardSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClockText is null || FlashText is null)
        {
            return;
        }

        if (BoardSelector.SelectedItem is not ComboBoxItem item || item.Content is not string boardName)
        {
            return;
        }

        var board = _boards[boardName];
        ClockText.Text = board.Clock;
        FlashText.Text = board.Flash;
        _files["platformio.ini"] = $"""
        [env:{board.Environment}]
        platform = {board.Platform}
        board = {board.Board}
        framework = arduino
        monitor_speed = 115200
        """;

        if (_activeFile == "platformio.ini")
        {
            EditorText.Text = _files["platformio.ini"];
        }
    }

    private async void VerifyClicked(object sender, RoutedEventArgs e)
    {
        await RunPlatformIoAsync("run");
    }

    private async void UploadClicked(object sender, RoutedEventArgs e)
    {
        await RunPlatformIoAsync("run --target upload");
    }

    private async void SerialClicked(object sender, RoutedEventArgs e)
    {
        await RunPlatformIoAsync("device monitor --baud 115200");
    }

    private void EnsureStarterProject()
    {
        Directory.CreateDirectory(Path.Combine(_projectPath, "src"));
        WriteIfMissing(Path.Combine(_projectPath, "src", "main.cpp"), _files["main.cpp"]);
        WriteIfMissing(Path.Combine(_projectPath, "platformio.ini"), _files["platformio.ini"]);
        WriteIfMissing(Path.Combine(_projectPath, "learning-notes.md"), _files["notes.md"]);
        LoadFilesFromDisk();
    }

    private static void WriteIfMissing(string path, string content)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }
    }

    private void LoadFilesFromDisk()
    {
        _files["main.cpp"] = File.ReadAllText(Path.Combine(_projectPath, "src", "main.cpp"));
        _files["platformio.ini"] = File.ReadAllText(Path.Combine(_projectPath, "platformio.ini"));
        _files["notes.md"] = File.ReadAllText(Path.Combine(_projectPath, "learning-notes.md"));
    }

    private void SaveActiveFile()
    {
        if (EditorText is null || string.IsNullOrWhiteSpace(_activeFile))
        {
            return;
        }

        _files[_activeFile] = EditorText.Text;
        var path = _activeFile switch
        {
            "main.cpp" => Path.Combine(_projectPath, "src", "main.cpp"),
            "platformio.ini" => Path.Combine(_projectPath, "platformio.ini"),
            "notes.md" => Path.Combine(_projectPath, "learning-notes.md"),
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, EditorText.Text);
        }
    }

    private async Task RunPlatformIoAsync(string arguments)
    {
        SaveActiveFile();

        if (_pioPath is null)
        {
            ConsoleText.Text = "PlatformIO was not found. Install PlatformIO Core, then restart Circuit Forge.";
            return;
        }

        ConsoleText.Text = $"> pio {arguments}{Environment.NewLine}";

        var startInfo = new ProcessStartInfo
        {
            FileName = _pioPath,
            Arguments = arguments,
            WorkingDirectory = _projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => AppendConsoleLine(e.Data);
        process.ErrorDataReceived += (_, e) => AppendConsoleLine(e.Data);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            AppendConsoleLine(process.ExitCode == 0 ? "SUCCESS: PlatformIO command finished." : $"FAILED: PlatformIO exited with code {process.ExitCode}.");
        }
        catch (Exception ex)
        {
            AppendConsoleLine($"FAILED: {ex.Message}");
        }
    }

    private void AppendConsoleLine(string? line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            ConsoleText.AppendText(line + Environment.NewLine);
            ConsoleText.ScrollToEnd();
        });
    }

    private static string? FindPlatformIo()
    {
        var pathCandidate = FindOnPath("pio.exe") ?? FindOnPath("pio");
        if (pathCandidate is not null)
        {
            return pathCandidate;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var knownStorePythonPath = Path.Combine(
            localAppData,
            "Packages",
            "PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0",
            "LocalCache",
            "local-packages",
            "Python313",
            "Scripts",
            "pio.exe");

        return File.Exists(knownStorePythonPath) ? knownStorePythonPath : null;
    }

    private static string? FindOnPath(string fileName)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var path in paths)
        {
            var candidate = Path.Combine(path, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
