using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CircuitForge;

public partial class MainWindow : Window
{
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

    private void DeviceTypeSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BoardSelector is null || DeviceTypeSelector.SelectedItem is not string family)
        {
            return;
        }

        BoardSelector.ItemsSource = _boards
            .Where(board => board.Family == family)
            .Select(board => board.Name)
            .ToList();
        BoardSelector.SelectedIndex = 0;
    }

    private void BoardSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClockText is null || FlashText is null || BoardSelector.SelectedItem is not string boardName)
        {
            return;
        }

        var board = _boards.FirstOrDefault(candidate => candidate.Name == boardName);
        if (board is null)
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
        _files["platformio.ini"] = $"""
        [env:{board.BoardId}]
        platform = {board.Platform}
        board = {board.BoardId}
        framework = {board.Framework}
        monitor_speed = 115200
        """;

        if (_activeFile == "platformio.ini")
        {
            EditorText.Text = _files["platformio.ini"];
        }
    }

    private void PopulateDeviceTypes()
    {
        DeviceTypeSelector.ItemsSource = _boards
            .Select(board => board.Family)
            .Distinct()
            .ToList();
        DeviceCountText.Text = _boards.Count.ToString();
        DeviceTypeSelector.SelectedItem = "Arduino AVR";
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
