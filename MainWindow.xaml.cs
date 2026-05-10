using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CircuitForge;

public partial class MainWindow : Window
{
    private const string DefaultMainSource = """
        // Starter sketch: blink without delay
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
        """;

    private const string DefaultNotes = """
        # Project notes

        - millis() lets the loop keep running.
        - delay() blocks every other task.
        - Serial output is the fastest first debugging tool.
        """;

    private sealed record BoardProfile(
        string Family,
        string Name,
        string Platform,
        string BoardId,
        string Framework,
        string Clock,
        string Flash,
        string Description);

    private readonly string _projectPath = Path.Combine(
        AppContext.BaseDirectory,
        "Projects",
        "StarterKit");

    private readonly string? _pioPath = FindPlatformIo();

    private string _activeFile = "main.cpp";

    private readonly Dictionary<string, string> _files = new()
    {
        ["main.cpp"] = DefaultMainSource,
        ["platformio.ini"] = """
        [env:uno]
        platform = atmelavr
        board = uno
        framework = arduino
        monitor_speed = 115200
        """,
        ["notes.md"] = DefaultNotes
    };

    private readonly List<BoardProfile> _boards =
    [
        new("ESP32", "ESP32 DevKit v1", "espressif32", "esp32dev", "arduino", "240 MHz", "4 MB", "General ESP32-WROOM development board."),
        new("ESP32", "ESP32-S3 DevKitC-1", "espressif32", "esp32-s3-devkitc-1", "arduino", "240 MHz", "8 MB", "ESP32-S3 board with USB and stronger AI/vector instructions."),
        new("ESP32", "ESP32-C3 DevKitM-1", "espressif32", "esp32-c3-devkitm-1", "arduino", "160 MHz", "4 MB", "RISC-V ESP32-C3 board for Wi-Fi and Bluetooth LE projects."),
        new("ESP32", "WEMOS LOLIN32", "espressif32", "lolin32", "arduino", "240 MHz", "4 MB", "Compact ESP32 board with battery-friendly maker layout."),
        new("ESP32", "M5Stack Core ESP32", "espressif32", "m5stack-core-esp32", "arduino", "240 MHz", "4 MB", "ESP32 device with screen, enclosure, and Grove ecosystem."),

        new("ESP8266", "NodeMCU 1.0 ESP-12E", "espressif8266", "nodemcuv2", "arduino", "80 MHz", "4 MB", "Classic ESP8266 learning board for Wi-Fi projects."),
        new("ESP8266", "WEMOS D1 Mini", "espressif8266", "d1_mini", "arduino", "80 MHz", "4 MB", "Tiny ESP8266 board with many shields."),
        new("ESP8266", "Adafruit Feather HUZZAH ESP8266", "espressif8266", "huzzah", "arduino", "80 MHz", "4 MB", "Feather-format ESP8266 board."),

        new("Arduino AVR", "Arduino Uno R3", "atmelavr", "uno", "arduino", "16 MHz", "32 KB", "The standard first microcontroller board."),
        new("Arduino AVR", "Arduino Nano ATmega328", "atmelavr", "nanoatmega328", "arduino", "16 MHz", "32 KB", "Breadboard-friendly Nano board."),
        new("Arduino AVR", "Arduino Mega 2560", "atmelavr", "megaatmega2560", "arduino", "16 MHz", "256 KB", "Large AVR board with many GPIO pins."),
        new("Arduino AVR", "Arduino Leonardo", "atmelavr", "leonardo", "arduino", "16 MHz", "32 KB", "ATmega32U4 board with native USB."),
        new("Arduino AVR", "SparkFun Pro Micro 5V", "atmelavr", "sparkfun_promicro16", "arduino", "16 MHz", "32 KB", "Small ATmega32U4 board for USB HID projects."),

        new("Arduino SAMD", "Arduino MKR WiFi 1010", "atmelsam", "mkrwifi1010", "arduino", "48 MHz", "256 KB", "SAMD21 board with Wi-Fi for IoT projects."),
        new("Arduino SAMD", "Arduino Zero", "atmelsam", "zero", "arduino", "48 MHz", "256 KB", "SAMD21 Cortex-M0+ board."),
        new("Arduino SAMD", "Adafruit Feather M0", "atmelsam", "adafruit_feather_m0", "arduino", "48 MHz", "256 KB", "Feather-format SAMD21 board."),
        new("Arduino SAMD", "Seeeduino XIAO", "atmelsam", "seeed_xiao", "arduino", "48 MHz", "256 KB", "Very small SAMD21 board."),

        new("RP2040 / Pico", "Raspberry Pi Pico", "raspberrypi", "pico", "arduino", "133 MHz", "2 MB", "RP2040 board with low cost and strong learning value."),
        new("RP2040 / Pico", "Raspberry Pi Pico W", "raspberrypi", "rpipicow", "arduino", "133 MHz", "2 MB", "RP2040 board with wireless support."),
        new("RP2040 / Pico", "Adafruit Feather RP2040", "raspberrypi", "adafruit_feather_rp2040", "arduino", "133 MHz", "8 MB", "Feather-format RP2040 board."),
        new("RP2040 / Pico", "SparkFun Pro Micro RP2040", "raspberrypi", "sparkfun_promicro_rp2040", "arduino", "133 MHz", "16 MB", "Tiny RP2040 board for USB and embedded projects."),

        new("STM32", "STM32 Nucleo F401RE", "ststm32", "nucleo_f401re", "arduino", "84 MHz", "512 KB", "ST Nucleo board with onboard debugger."),
        new("STM32", "STM32 Blue Pill F103C8", "ststm32", "bluepill_f103c8", "arduino", "72 MHz", "64 KB", "Low-cost STM32F103 board."),
        new("STM32", "STM32 BlackPill F411CE", "ststm32", "blackpill_f411ce", "arduino", "100 MHz", "512 KB", "Modern compact STM32F411 board."),
        new("STM32", "STM32 Nucleo L432KC", "ststm32", "nucleo_l432kc", "arduino", "80 MHz", "256 KB", "Small low-power Nucleo board."),
        new("STM32", "STM32F4 Discovery", "ststm32", "disco_f407vg", "stm32cube", "168 MHz", "1 MB", "Discovery board for STM32Cube workflows."),

        new("Teensy", "Teensy 4.1", "teensy", "teensy41", "arduino", "600 MHz", "8 MB", "Very fast ARM Cortex-M7 board."),
        new("Teensy", "Teensy 4.0", "teensy", "teensy40", "arduino", "600 MHz", "2 MB", "Compact high-performance Teensy."),
        new("Teensy", "Teensy 3.2", "teensy", "teensy31", "arduino", "72 MHz", "256 KB", "Classic Teensy board."),
        new("Teensy", "Teensy LC", "teensy", "teensylc", "arduino", "48 MHz", "62 KB", "Low-cost Teensy for learning."),

        new("nRF52 / Bluetooth", "Adafruit Feather nRF52840 Express", "nordicnrf52", "adafruit_feather_nrf52840", "arduino", "64 MHz", "1 MB", "Bluetooth LE board with Feather ecosystem."),
        new("nRF52 / Bluetooth", "Nordic nRF52840 DK", "nordicnrf52", "nrf52840_dk", "arduino", "64 MHz", "1 MB", "Nordic development kit for BLE workflows."),
        new("nRF52 / Bluetooth", "BBC micro:bit V2", "nordicnrf52", "bbcmicrobit_v2", "arduino", "64 MHz", "512 KB", "Education board with sensors and BLE."),

        new("Linux SBC", "Raspberry Pi 3 Model B", "linux_arm", "raspberrypi_3b", "native", "1.2 GHz", "SD", "Linux single-board computer target."),
        new("Linux SBC", "Raspberry Pi 4 Model B", "linux_arm", "raspberrypi_4b", "native", "1.5 GHz", "SD", "Linux SBC for native projects."),
        new("Linux SBC", "BeagleBone Black", "linux_arm", "bb_black", "native", "1 GHz", "eMMC/SD", "Linux board with strong GPIO and PRU features."),

        new("Specialty / RISC-V", "SiFive HiFive1 Rev B", "sifive", "hifive1-revb", "freedom-e-sdk", "320 MHz", "16 MB", "RISC-V development board."),
        new("Specialty / 3D Printer", "RUMBA32 F446VE", "ststm32", "rumba32_f446ve", "arduino", "180 MHz", "512 KB", "3D printer controller board."),
        new("Specialty / Robotics", "nicai-systems NIBO 2", "atmelavr", "nibo2", "arduino", "16 MHz", "128 KB", "Robot controller supported by PlatformIO.")
    ];

    public MainWindow()
    {
        InitializeComponent();
        PopulateDeviceTypes();
        EnsureStarterProject();
        ProjectPathText.Text = _projectPath;
        PlatformIoStatusText.Text = _pioPath is not null ? "PIO" : "NO PIO";
        OpenFile("main.cpp");
        _ = LoadPlatformIoBoardCatalogAsync();
    }

    private void OpenMainFile(object sender, RoutedEventArgs e) => OpenFile("main.cpp");

    private void OpenConfigFile(object sender, RoutedEventArgs e) => OpenFile("platformio.ini");

    private void OpenNotesFile(object sender, RoutedEventArgs e) => OpenFile("notes.md");

    private void WorkspaceClicked(object sender, RoutedEventArgs e)
    {
        OpenFile("main.cpp");
        ShowConsole("Workspace", [
            $"Project: {_projectPath}",
            $"Active file: {ActiveFileText.Text}",
            "Use Verify to build, Upload to flash a connected board, or Serial to open the monitor."
        ]);
    }

    private void BoardsClicked(object sender, RoutedEventArgs e)
    {
        ShowConsole("Boards", _boards
            .GroupBy(board => board.Family)
            .Select(group => $"{group.Key}: {group.Count()} boards")
            .OrderBy(line => line)
            .ToArray());
    }

    private void HelpClicked(object sender, RoutedEventArgs e)
    {
        ShowConsole("Help", [
            "Circuit Forge is a native PlatformIO front end.",
            "1. Pick a device type and board.",
            "2. Edit src/main.cpp or platformio.ini.",
            "3. Use Verify, Upload, and Serial from the top bar.",
            "The + button resets the starter sketch files."
        ]);
    }

    private void ProblemsClicked(object sender, RoutedEventArgs e)
    {
        ShowConsole("Problems", [
            "No diagnostics yet.",
            "Run Verify to ask PlatformIO to compile the current project."
        ]);
    }

    private void OutputClicked(object sender, RoutedEventArgs e)
    {
        ShowConsole("Output", [
            $"PlatformIO: {(_pioPath ?? "not found")}",
            $"Project: {_projectPath}",
            $"Selected board: {GetSelectedBoard().Name}",
            $"Loaded boards: {_boards.Count}"
        ]);
    }

    private void SettingsClicked(object sender, RoutedEventArgs e)
    {
        ShowConsole("Settings", [
            $"PlatformIO executable: {(_pioPath ?? "not found")}",
            $"Project folder: {_projectPath}",
            "Upload port: auto",
            "Serial baud: 115200",
            "Project files are saved automatically when switching files or running commands."
        ]);
    }

    private void NewSketchClicked(object sender, RoutedEventArgs e)
    {
        _files["main.cpp"] = DefaultMainSource;
        _files["platformio.ini"] = BuildPlatformIoIni(GetSelectedBoard());
        _files["notes.md"] = DefaultNotes;

        Directory.CreateDirectory(Path.Combine(_projectPath, "src"));
        File.WriteAllText(Path.Combine(_projectPath, "src", "main.cpp"), _files["main.cpp"]);
        File.WriteAllText(Path.Combine(_projectPath, "platformio.ini"), _files["platformio.ini"]);
        File.WriteAllText(Path.Combine(_projectPath, "learning-notes.md"), _files["notes.md"]);
        EditorText.Text = _files[_activeFile];
        ActiveFileText.Text = _activeFile == "main.cpp" ? "src/main.cpp" : _activeFile;
        LineNumbersText.Text = string.Join(Environment.NewLine, Enumerable.Range(1, _files[_activeFile].Split('\n').Length));
        ShowConsole("New Sketch", [
            "Starter sketch files were reset.",
            "src/main.cpp, platformio.ini, and learning-notes.md were written to the project folder."
        ]);
    }

    private void OpenFile(string fileName)
    {
        SaveActiveFile();
        _activeFile = fileName;
        LoadFilesFromDisk();
        EditorText.Text = _files[fileName];
        ActiveFileText.Text = fileName == "main.cpp" ? "src/main.cpp" : fileName;
        LineNumbersText.Text = string.Join(Environment.NewLine, Enumerable.Range(1, _files[fileName].Split('\n').Length));
    }

    private void DeviceTypeSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BoardSelector is null || DeviceTypeSelector.SelectedItem is not string family)
        {
            return;
        }

        var boards = _boards
            .Where(board => board.Family == family)
            .OrderBy(board => board.Name)
            .ToList();

        BoardSelector.ItemsSource = boards;
        BoardSelector.SelectedIndex = 0;
    }

    private void BoardSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClockText is null || FlashText is null || BoardSelector.SelectedItem is not BoardProfile board)
        {
            return;
        }

        ClockText.Text = board.Clock;
        FlashText.Text = board.Flash;
        DeviceFamilyText.Text = board.Family;
        DeviceDescriptionText.Text = board.Description;
        PlatformText.Text = board.Platform;
        BoardIdText.Text = board.BoardId;
        BoardFrameworkText.Text = board.Framework;
        _files["platformio.ini"] = BuildPlatformIoIni(board);

        if (_activeFile == "platformio.ini")
        {
            EditorText.Text = _files["platformio.ini"];
        }
    }

    private void PopulateDeviceTypes()
    {
        var deviceTypeSelector = DeviceTypeSelector;
        var deviceCountText = DeviceCountText;
        if (deviceTypeSelector is null || deviceCountText is null)
        {
            return;
        }

        var selectedFamily = deviceTypeSelector.SelectedItem as string ?? "Arduino AVR";
        if (!_boards.Any(board => board.Family == selectedFamily))
        {
            selectedFamily = _boards.Select(board => board.Family).OrderBy(family => family).FirstOrDefault() ?? "";
        }

        deviceTypeSelector.ItemsSource = _boards
            .Select(board => board.Family)
            .Distinct()
            .OrderBy(family => family)
            .ToList();
        deviceCountText.Text = _boards.Count.ToString();
        deviceTypeSelector.SelectedItem = selectedFamily;
    }

    private BoardProfile GetSelectedBoard()
    {
        return BoardSelector?.SelectedItem as BoardProfile ?? _boards.First(board => board.BoardId == "uno");
    }

    private static string BuildPlatformIoIni(BoardProfile board) =>
        $"""
        [env:{board.BoardId}]
        platform = {board.Platform}
        board = {board.BoardId}
        framework = {board.Framework}
        monitor_speed = 115200
        """;

    private void ShowConsole(string title, IEnumerable<string> lines)
    {
        ConsoleText.Text = $"[{title}]{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        ConsoleText.ScrollToEnd();
    }

    private async Task LoadPlatformIoBoardCatalogAsync()
    {
        if (_pioPath is null)
        {
            return;
        }

        Dispatcher.Invoke(() => AppendConsoleLine("Loading full PlatformIO board catalog..."));

        var startInfo = new ProcessStartInfo
        {
            FileName = _pioPath,
            Arguments = "boards --json-output",
            WorkingDirectory = _projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                Dispatcher.Invoke(() => AppendConsoleLine($"PlatformIO board catalog failed: {error.Trim()}"));
                return;
            }

            var loadedBoards = ParsePlatformIoBoards(output);
            if (loadedBoards.Count == 0)
            {
                Dispatcher.Invoke(() => AppendConsoleLine("PlatformIO returned no boards; keeping the built-in fallback catalog."));
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var selectedBoardId = GetSelectedBoard().BoardId;
                _boards.Clear();
                _boards.AddRange(loadedBoards);
                PopulateDeviceTypes();
                if (CatalogStatusText is not null)
                {
                    CatalogStatusText.Text = "full";
                    CatalogStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                }

                var selectedBoard = _boards.FirstOrDefault(board => board.BoardId == selectedBoardId) ?? _boards.FirstOrDefault();
                if (selectedBoard is not null)
                {
                    DeviceTypeSelector.SelectedItem = selectedBoard.Family;
                    BoardSelector.SelectedItem = selectedBoard;
                }

                ShowConsole("Boards", [
                    $"Loaded {_boards.Count} boards from PlatformIO.",
                    "The searchable board browser now uses the full PlatformIO catalog reported by this installation."
                ]);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => AppendConsoleLine($"PlatformIO board catalog failed: {ex.Message}"));
        }
    }

    private static List<BoardProfile> ParsePlatformIoBoards(string json)
    {
        using var document = JsonDocument.Parse(json);
        var boards = new List<BoardProfile>();

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var id = GetString(element, "id");
            var name = GetString(element, "name");
            var platform = GetString(element, "platform");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(platform))
            {
                continue;
            }

            var frameworks = GetStringArray(element, "frameworks");
            var framework = frameworks.Contains("arduino") ? "arduino" : frameworks.FirstOrDefault() ?? "arduino";
            var vendor = GetString(element, "vendor");
            var mcu = GetString(element, "mcu");
            var clock = FormatFrequency(GetLong(element, "fcpu"));
            var flash = FormatBytes(GetLong(element, "rom"));
            var family = ClassifyFamily(platform, name, mcu, vendor);
            var descriptionParts = new[] { vendor, mcu, $"{platform} platform" }
                .Where(part => !string.IsNullOrWhiteSpace(part));

            boards.Add(new BoardProfile(
                family,
                name,
                platform,
                id,
                framework,
                clock,
                flash,
                string.Join(" · ", descriptionParts)));
        }

        return boards
            .GroupBy(board => board.BoardId)
            .Select(group => group.First())
            .OrderBy(board => board.Family)
            .ThenBy(board => board.Name)
            .ToList();
    }

    private static string ClassifyFamily(string platform, string name, string mcu, string vendor)
    {
        var haystack = $"{platform} {name} {mcu} {vendor}".ToLowerInvariant();

        if (haystack.Contains("esp32") || platform == "espressif32") return "ESP32";
        if (haystack.Contains("esp8266") || platform == "espressif8266") return "ESP8266";
        if (haystack.Contains("rp2040") || haystack.Contains("pico") || platform == "raspberrypi") return "RP2040 / Pico";
        if (haystack.Contains("stm32") || platform == "ststm32") return "STM32";
        if (platform == "teensy") return "Teensy";
        if (platform == "nordicnrf52" || haystack.Contains("nrf52") || haystack.Contains("micro:bit")) return "nRF52 / Bluetooth";
        if (platform == "atmelsam") return "Arduino SAMD / ARM";
        if (platform == "atmelavr" || haystack.Contains("atmega") || haystack.Contains("attiny")) return "Arduino AVR / 8-bit";
        if (platform.StartsWith("linux", StringComparison.OrdinalIgnoreCase)) return "Linux SBC";
        if (haystack.Contains("risc-v") || haystack.Contains("riscv") || platform == "sifive") return "RISC-V";

        return platform;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? "")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static long GetLong(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : 0;
    }

    private static string FormatFrequency(long hz)
    {
        if (hz <= 0) return "Unknown";
        if (hz >= 1_000_000) return $"{hz / 1_000_000d:0.#} MHz";
        if (hz >= 1_000) return $"{hz / 1_000d:0.#} kHz";
        return $"{hz} Hz";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "Unknown";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576d:0.#} MB";
        if (bytes >= 1024) return $"{bytes / 1024d:0.#} KB";
        return $"{bytes} B";
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
