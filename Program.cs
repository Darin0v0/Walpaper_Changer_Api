using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pastel;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    private const string ApiKey = "aOAqwTWJ3bqA6D7JEqPTrgaSiwB97J9g";
    private const string BaseApiUrl = "https://wallhaven.cc/api/v1/";
    private static readonly HttpClient client = new HttpClient();
    private static readonly List<WallpaperHistoryItem> wallpaperHistory = new List<WallpaperHistoryItem>();
    private static bool isWindows = true;
    private static int currentPage = 1;
    private static int totalPages = 1;
    private static string wallpaperCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".wallpaper_cache"
    );
    private static SearchParameters currentSearchParams = new SearchParameters();
    private static List<Wallpaper> currentWallpapers = new List<Wallpaper>();
    private static readonly Random random = new Random();
    private static string currentWallpaperPath = "";
    private static string currentTheme = "ocean";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true;
            Console.WriteLine("\n\nExiting gracefully...");
            Environment.Exit(0);
        };

        PrintBanner();
        
        if (!Directory.Exists(wallpaperCachePath))
        {
            Directory.CreateDirectory(wallpaperCachePath);
        }

        LoadHistory();
        AutoDetectOS();

        while (true)
        {
            Console.WriteLine("\n1. 🔍 Search Wallpapers".Pastel(GetThemeColor("menu1")));
            Console.WriteLine("2. 🏞️ Set Default Wallpaper".Pastel(GetThemeColor("menu2")));
            Console.WriteLine("3. 📜 View History".Pastel(GetThemeColor("menu3")));
            Console.WriteLine("4. ⚙️ Settings".Pastel(GetThemeColor("menu4")));
            Console.WriteLine("5. 🎲 Random Wallpaper".Pastel(GetThemeColor("menu5")));
            Console.WriteLine("6. 🔄 Refresh Current".Pastel(GetThemeColor("menu6")));
            
            if (isWindows)
            {
                Console.WriteLine("7. 📺 VLC Video Wallpaper".Pastel(GetThemeColor("menu7")));
            }
            else
            {
                Console.WriteLine("7. 🎥 Video Wallpaper".Pastel(GetThemeColor("menu7")));
            }
            
            Console.WriteLine("8. 🚪 Exit".Pastel(GetThemeColor("menu8")));

            Console.Write("\nChoose option: ".Pastel(GetThemeColor("text")));
            string mainChoice = Console.ReadLine()?.Trim() ?? "1";

            switch (mainChoice)
            {
                case "1":
                    await SearchWallpapers();
                    break;
                case "2":
                    await SetDefaultWallpaper();
                    break;
                case "3":
                    ShowWallpaperHistory();
                    break;
                case "4":
                    await ShowSettings();
                    break;
                case "5":
                    await SetRandomWallpaper();
                    break;
                case "6":
                    RefreshCurrentWallpaper();
                    break;
                case "7":
                    if (isWindows)
                    {
                        await SetVLCVideoWallpaper();
                    }
                    else
                    {
                        await SetHanabiVideoWallpaper();
                    }
                    break;
                case "8":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.".Pastel(GetThemeColor("error")));
                    break;
            }
        }
    }

    private static string GetThemeColor(string element)
    {
        return currentTheme switch
        {
            "ocean" => element switch
            {
                "primary" => "#3498db",
                "background" => "#2c3e50",
                "menu1" => "#1abc9c",
                "menu2" => "#9b59b6",
                "menu3" => "#f1c40f",
                "menu4" => "#95a5a6",
                "menu5" => "#e74c3c",
                "menu6" => "#2ecc71",
                "menu7" => "#e67e22",
                "menu8" => "#9b59b6",
                "text" => "#ecf0f1",
                "success" => "#2ecc71",
                "error" => "#e74c3c",
                "warning" => "#f39c12",
                _ => "#ffffff"
            },
            "forest" => element switch
            {
                "primary" => "#27ae60",
                "background" => "#1e8449",
                "menu1" => "#2ecc71",
                "menu2" => "#16a085",
                "menu3" => "#f39c12",
                "menu4" => "#7f8c8d",
                "menu5" => "#c0392b",
                "menu6" => "#27ae60",
                "menu7" => "#d35400",
                "menu8" => "#9b59b6",
                "text" => "#ecf0f1",
                "success" => "#27ae60",
                "error" => "#c0392b",
                "warning" => "#f39c12",
                _ => "#ffffff"
            },
            "sunset" => element switch
            {
                "primary" => "#e67e22",
                "background" => "#d35400",
                "menu1" => "#e74c3c",
                "menu2" => "#9b59b6",
                "menu3" => "#f1c40f",
                "menu4" => "#95a5a6",
                "menu5" => "#c0392b",
                "menu6" => "#e67e22",
                "menu7" => "#3498db",
                "menu8" => "#9b59b6",
                "text" => "#ecf0f1",
                "success" => "#e67e22",
                "error" => "#c0392b",
                "warning" => "#f39c12",
                _ => "#ffffff"
            },
            _ => "#3498db"
        };
    }

private static async Task SetHanabiVideoWallpaper()
{
    Console.WriteLine("\n🎥 Video Wallpaper (Linux)".Pastel(GetThemeColor("primary")));
    
    try
    {
        // Sprawdź czy folder animee istnieje
        string animePath = "/home/dawid/Obrazy/animee";
        if (!Directory.Exists(animePath))
        {
            Console.WriteLine($"Folder {animePath} not found.".Pastel(GetThemeColor("error")));
            return;
        }

        // Znajdź wszystkie pliki MP4 w folderze animee i podfolderach
        var mp4Files = Directory.GetFiles(animePath, "*.mp4", SearchOption.AllDirectories).ToList();
        
        if (mp4Files.Count == 0)
        {
            Console.WriteLine($"No MP4 files found in {animePath}".Pastel(GetThemeColor("error")));
            return;
        }

        Console.WriteLine($"\nFound {mp4Files.Count} MP4 files:".Pastel(GetThemeColor("success")));
        for (int i = 0; i < mp4Files.Count; i++)
        {
            string fileName = Path.GetFileName(mp4Files[i]);
            string folderName = Path.GetFileName(Path.GetDirectoryName(mp4Files[i]));
            Console.WriteLine($"{i + 1}. {fileName} (in {folderName})".Pastel(GetThemeColor("text")));
        }

        Console.Write($"\nSelect video (1-{mp4Files.Count}): ".Pastel(GetThemeColor("text")));
        if (!int.TryParse(Console.ReadLine(), out int selectedIndex) || selectedIndex < 1 || selectedIndex > mp4Files.Count)
        {
            Console.WriteLine("Invalid selection.".Pastel(GetThemeColor("error")));
            return;
        }

        string selectedVideo = mp4Files[selectedIndex - 1];

        // Ustaw ścieżkę wideo w Hanabi przez gsettings
        try
        {
            Process.Start("/bin/bash", $"-c \"gsettings set io.github.jeffshee.hanabi-extension video-path '{selectedVideo}'\"");
            Console.WriteLine($"Video path set to: {selectedVideo}".Pastel(GetThemeColor("success")));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting Hanabi video: {ex.Message}".Pastel(GetThemeColor("error")));
        }

        // Uruchom Hanabi w tle
        string fullCommand = "flatpak run --socket=wayland io.github.jeffshee.Hanabi >/dev/null 2>&1 &";
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{fullCommand}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();

        // Zapisz informację o video wallpaper w historii
        wallpaperHistory.Add(new WallpaperHistoryItem {
            Id = "hanabi_video_" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Path = selectedVideo,
            Url = "",
            Resolution = "Video Wallpaper",
            SetDate = DateTime.Now,
            Tags = new List<string> { "video", "mp4", "animated", "hanabi" }
        });
        
        SaveHistory();
        
        Console.WriteLine("\n🎥 Video wallpaper started!".Pastel(GetThemeColor("success")));
        Console.WriteLine($"• Playing: {Path.GetFileName(selectedVideo)}".Pastel(GetThemeColor("text")));
        Console.WriteLine("• Video is looping automatically".Pastel(GetThemeColor("text")));
        Console.WriteLine("• Running in background".Pastel(GetThemeColor("text")));
        Console.WriteLine("• Close from system tray to stop".Pastel(GetThemeColor("text")));
        Console.WriteLine("\nPress any key to return to menu...".Pastel(GetThemeColor("text")));
        Console.ReadKey();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error starting Hanabi: {ex.Message}".Pastel(GetThemeColor("error")));
    }
}


    private static async Task SetVLCVideoWallpaper()
    {
        try
        {
            if (!IsVLCInstalled())
            {
                Console.WriteLine("VLC not found. Please install VLC media player first.".Pastel(GetThemeColor("error")));
                Console.WriteLine("Download from: https://www.videolan.org/vlc/".Pastel(GetThemeColor("text")));
                return;
            }

            Console.WriteLine("\n📺 VLC Video Wallpaper".Pastel(GetThemeColor("primary")));
            Console.WriteLine("1. Select video file".Pastel(GetThemeColor("menu1")));
            Console.WriteLine("2. Enter video URL".Pastel(GetThemeColor("menu2")));
            Console.WriteLine("3. Back to main menu".Pastel(GetThemeColor("menu4")));
            Console.Write("\nChoose option: ".Pastel(GetThemeColor("text")));

            string choice = Console.ReadLine()?.Trim() ?? "1";

            string videoPath = "";

            switch (choice)
            {
                case "1":
                    Console.Write("Enter path to video file: ");
                    videoPath = Console.ReadLine()?.Trim() ?? "";
                    if (!File.Exists(videoPath))
                    {
                        Console.WriteLine("Video file not found.".Pastel(GetThemeColor("error")));
                        return;
                    }
                    break;
                case "2":
                    Console.Write("Enter video URL: ");
                    string videoUrl = Console.ReadLine()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        Console.WriteLine("Invalid URL.".Pastel(GetThemeColor("error")));
                        return;
                    }
                    videoPath = videoUrl;
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid choice.".Pastel(GetThemeColor("error")));
                    return;
            }

            Console.WriteLine("\nVLC Options:".Pastel(GetThemeColor("primary")));
            Console.WriteLine("1. Play once".Pastel(GetThemeColor("menu1")));
            Console.WriteLine("2. Loop video".Pastel(GetThemeColor("menu2")));
            Console.WriteLine("3. Loop with audio".Pastel(GetThemeColor("menu3")));
            Console.Write("Choose option: ".Pastel(GetThemeColor("text")));

            string vlcChoice = Console.ReadLine()?.Trim() ?? "2";

            string vlcArgs = "";
            switch (vlcChoice)
            {
                case "1":
                    vlcArgs = $"\"{videoPath}\" --video-wallpape --no-video-title-show --no-audio --no-loop";
                    break;
                case "2":
                    vlcArgs = $"\"{videoPath}\" --video-wallpaper ---no-video-title-show --no-audio --loop";
                    break;
                case "3":
                    vlcArgs = $"\"{videoPath}\" --video-wallpaper--no-video-title-show --loop";
                    break;
                default:
                    vlcArgs = $"\"{videoPath}\" --video-wallpaper --no-video-title-show --no-audio --loop";
                    break;
            }

            string vlcPath = GetVLCPath();
            if (string.IsNullOrEmpty(vlcPath))
            {
                Console.WriteLine("VLC path not found.".Pastel(GetThemeColor("error")));
                return;
            }

            Console.WriteLine($"Starting VLC with: {vlcArgs}".Pastel(GetThemeColor("text")));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = vlcPath,
                    Arguments = vlcArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            
            // Zapisz informację o video wallpaper w historii
            wallpaperHistory.Add(new WallpaperHistoryItem {
                Id = "vlc_video_" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Path = videoPath,
                Url = videoPath.StartsWith("http") ? videoPath : "",
                Resolution = "Video Wallpaper",
                SetDate = DateTime.Now,
                Tags = new List<string> { "video", "vlc", "animated" }
            });
            
            SaveHistory();
            
            Console.WriteLine("\n🎥 VLC video wallpaper started!".Pastel(GetThemeColor("success")));
            Console.WriteLine("💡 Tips:".Pastel(GetThemeColor("text")));
            Console.WriteLine("• VLC will run in background".Pastel(GetThemeColor("text")));
            Console.WriteLine("• Close VLC to stop the wallpaper".Pastel(GetThemeColor("text")));
            Console.WriteLine("• Right-click VLC in system tray for options".Pastel(GetThemeColor("text")));
            Console.WriteLine("\nPress any key to return to menu...".Pastel(GetThemeColor("text")));
            Console.ReadKey();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting VLC: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static bool IsVLCInstalled()
    {
        return !string.IsNullOrEmpty(GetVLCPath());
    }

    private static string GetVLCPath()
    {
        string[] possiblePaths = {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\VideoLAN\VLC\vlc.exe"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\VideoLAN\VLC\vlc.exe")
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private static void RefreshCurrentWallpaper()
    {
        if (!string.IsNullOrEmpty(currentWallpaperPath) && File.Exists(currentWallpaperPath))
        {
            SetWallpaperFile(currentWallpaperPath);
            Console.WriteLine("Current wallpaper refreshed!".Pastel(GetThemeColor("success")));
        }
        else
        {
            Console.WriteLine("No current wallpaper set".Pastel(GetThemeColor("error")));
        }
    }

    private static string? GetAvailableImageViewer()
    {
        string[] viewers = {"feh", "imv", "sxiv", "xdg-open"};
        
        foreach (var viewer in viewers)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = viewer,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(500);
                if (process.ExitCode == 0)
                    return viewer;
            }
            catch { /* ignore */ }
        }
        return null;
    }

    private static void ShowWallpaperPreview(string imageUrl)
    {
        if (isWindows)
        {
            ShowWallpaperPreviewWindows(imageUrl);
        }
        else
        {
            ShowWallpaperPreviewLinux(imageUrl);
        }
    }

    private static void ShowWallpaperPreviewWindows(string imageUrl)
    {
        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"wallpreview_{Guid.NewGuid()}.jpg");
            
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(imageUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    File.WriteAllBytes(tempFile, response.Content.ReadAsByteArrayAsync().Result);
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Console.WriteLine("Could not download image for preview".Pastel(GetThemeColor("error")));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Preview error: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static void ShowWallpaperPreviewLinux(string imageUrl)
    {
        string? viewer = GetAvailableImageViewer();
        if (viewer == null)
        {
            Console.WriteLine("No image viewer found. Install feh, imv or sxiv.".Pastel(GetThemeColor("error")));
            return;
        }

        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"wallpreview_{Guid.NewGuid()}.jpg");
            
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(imageUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    File.WriteAllBytes(tempFile, response.Content.ReadAsByteArrayAsync().Result);
                }
                else
                {
                    Console.WriteLine("Could not download image for preview".Pastel(GetThemeColor("error")));
                    return;
                }
            }

            string terminal = Environment.GetEnvironmentVariable("TERMINAL") ?? "xterm";
            string viewerCommand = viewer switch
            {
                "feh" => $"feh --scale-down --geometry 800x600 --title 'Wallpaper Preview' \"{tempFile}\"",
                "imv" => $"imv \"{tempFile}\"",
                "sxiv" => $"sxiv \"{tempFile}\"",
                "xdg-open" => $"xdg-open \"{tempFile}\"",
                _ => $"feh \"{tempFile}\""
            };

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = terminal,
                    Arguments = $"-e {viewerCommand}",
                    UseShellExecute = false
                }
            };
            process.Start();

            process.Exited += (sender, e) => 
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Preview error: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static async Task SetDefaultWallpaper()
    {
        Console.Write("\nEnter path to default wallpaper: ");
        string path = Console.ReadLine()?.Trim() ?? "";
        
        if (File.Exists(path))
        {
            try
            {
                SetWallpaperFile(path);
                Console.WriteLine("Default wallpaper set!".Pastel(GetThemeColor("success")));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting wallpaper: {ex.Message}".Pastel(GetThemeColor("error")));
            }
        }
        else
        {
            Console.WriteLine("File not found".Pastel(GetThemeColor("error")));
        }
    }

    private static void PrintBanner()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine(@"███████╗ █████╗ ██╗     ██╗██████╗  █████╗ ███████╗███████╗".Pastel(GetThemeColor("primary")));
        Console.WriteLine(@"██╔════╝██╔══██╗██║     ██║██╔══██╗██╔══██╗██╔════╝██╔════╝".Pastel(GetThemeColor("primary")));
        Console.WriteLine(@"█████╗  ███████║██║     ██║██████╔╝███████║███████╗█████╗  ".Pastel(GetThemeColor("primary")));
        Console.WriteLine(@"██╔══╝  ██╔══██║██║     ██║██╔═══╝ ██╔══██║╚════██║██╔══╝  ".Pastel(GetThemeColor("primary")));
        Console.WriteLine(@"██║     ██║  ██║███████╗██║██║     ██║  ██║███████║███████╗".Pastel(GetThemeColor("primary")));
        Console.WriteLine(@"╚═╝     ╚═╝  ╚═╝╚══════╝╚═╚╚═╝     ╚═╝  ╚═╝╚══════╝╚══════╝".Pastel(GetThemeColor("primary")));
        Console.WriteLine();
        Console.WriteLine("=".Repeat(60).Pastel(GetThemeColor("background")));
        Console.WriteLine($"🌄 Wallpaper Changer v3.0 | Cache: {wallpaperCachePath}".Pastel(GetThemeColor("text")));
        Console.WriteLine("=".Repeat(60).Pastel(GetThemeColor("background")));
    }

    private static void AutoDetectOS()
    {
        isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        Console.WriteLine($"\nDetected OS: {(isWindows ? "Windows" : "Linux")}".Pastel(GetThemeColor("success")));
    }

    private static void LoadHistory()
    {
        string historyFile = Path.Combine(wallpaperCachePath, "history.json");
        if (File.Exists(historyFile))
        {
            try
            {
                var json = File.ReadAllText(historyFile);
                var history = JsonConvert.DeserializeObject<List<WallpaperHistoryItem>>(json);
                if (history != null)
                {
                    wallpaperHistory.AddRange(history);
                    if (wallpaperHistory.Count > 0)
                    {
                        currentWallpaperPath = wallpaperHistory[^1].Path;
                    }
                    Console.WriteLine($"\nLoaded {wallpaperHistory.Count} items from history".Pastel(GetThemeColor("text")));
                }
            }
            catch
            {
                Console.WriteLine("Could not load history".Pastel(GetThemeColor("error")));
            }
        }
    }

    private static void SaveHistory()
    {
        string historyFile = Path.Combine(wallpaperCachePath, "history.json");
        try
        {
            File.WriteAllText(historyFile, JsonConvert.SerializeObject(wallpaperHistory));
        }
        catch
        {
            Console.WriteLine("Could not save history".Pastel(GetThemeColor("error")));
        }
    }

    private static async Task ShowSettings()
    {
        while (true)
        {
            Console.WriteLine("\n" + "SETTINGS".Pastel(GetThemeColor("primary")).PastelBg(GetThemeColor("background")));
            Console.WriteLine("1. 🔄 Change Cache Location".Pastel(GetThemeColor("menu1")));
            Console.WriteLine("2. 🖼️ Clear Wallpaper Cache".Pastel(GetThemeColor("menu5")));
            Console.WriteLine("3. 📜 Clear History".Pastel(GetThemeColor("menu3")));
            Console.WriteLine("4. 🌈 Change UI Theme".Pastel(GetThemeColor("menu2")));
            Console.WriteLine("5. ⬅️ Back to Main Menu".Pastel(GetThemeColor("menu4")));
            Console.Write("\nChoose option: ".Pastel(GetThemeColor("text")));

            string choice = Console.ReadLine()?.Trim() ?? "1";

            switch (choice)
            {
                case "1":
                    Console.Write("\nEnter new cache path: ");
                    var newPath = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        if (!Directory.Exists(newPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(newPath);
                                wallpaperCachePath = newPath;
                                Console.WriteLine($"Cache location updated to: {newPath}".Pastel(GetThemeColor("success")));
                            }
                            catch
                            {
                                Console.WriteLine("Invalid path or access denied".Pastel(GetThemeColor("error")));
                            }
                        }
                        else
                        {
                            wallpaperCachePath = newPath;
                            Console.WriteLine($"Cache location updated to: {newPath}".Pastel(GetThemeColor("success")));
                        }
                    }
                    break;
                case "2":
                    Console.Write("\nAre you sure you want to clear the cache? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        try
                        {
                            var files = Directory.GetFiles(wallpaperCachePath);
                            foreach (var file in files)
                            {
                                if (!file.EndsWith("history.json"))
                                {
                                    File.Delete(file);
                                }
                            }
                            Console.WriteLine("Cache cleared successfully".Pastel(GetThemeColor("success")));
                        }
                        catch
                        {
                            Console.WriteLine("Failed to clear cache".Pastel(GetThemeColor("error")));
                        }
                    }
                    break;
                case "3":
                    Console.Write("\nAre you sure you want to clear history? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        wallpaperHistory.Clear();
                        SaveHistory();
                        Console.WriteLine("History cleared".Pastel(GetThemeColor("success")));
                    }
                    break;
                case "4":
                    Console.WriteLine("\nColor themes: ");
                    Console.WriteLine("1. Ocean Blue (Default)");
                    Console.WriteLine("2. Forest Green");
                    Console.WriteLine("3. Sunset Orange");
                    Console.Write("Choose theme: ");
                    var themeChoice = Console.ReadLine()?.Trim();
                    switch (themeChoice)
                    {
                        case "1":
                            currentTheme = "ocean";
                            Console.WriteLine("Theme changed to Ocean Blue!".Pastel("#3498db"));
                            PrintBanner();
                            break;
                        case "2":
                            currentTheme = "forest";
                            Console.WriteLine("Theme changed to Forest Green!".Pastel("#2ecc71"));
                            PrintBanner();
                            break;
                        case "3":
                            currentTheme = "sunset";
                            Console.WriteLine("Theme changed to Sunset Orange!".Pastel("#e67e22"));
                            PrintBanner();
                            break;
                        default:
                            Console.WriteLine("Invalid theme choice".Pastel(GetThemeColor("error")));
                            break;
                    }
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid choice".Pastel(GetThemeColor("error")));
                    break;
            }
        }
    }

    private static async Task SearchWallpapers()
    {
        currentSearchParams = new SearchParameters();
        
        Console.WriteLine("\n" + "SEARCH OPTIONS".Pastel(GetThemeColor("primary")).PastelBg(GetThemeColor("background")));
        Console.WriteLine("1. 🔖 By Category".Pastel(GetThemeColor("menu1")));
        Console.WriteLine("2. 🏷️ By Tags".Pastel(GetThemeColor("menu2")));
        Console.WriteLine("3. 🎨 By Color".Pastel(GetThemeColor("menu3")));
        Console.WriteLine("4. 🎲 Random".Pastel(GetThemeColor("menu5")));
        Console.WriteLine("5. 🔥 Popular".Pastel(GetThemeColor("menu7")));
        Console.WriteLine("6. ⚙️ Advanced Search".Pastel(GetThemeColor("menu4")));
        Console.WriteLine("7. ⬅️ Back".Pastel(GetThemeColor("menu6")));
        Console.Write("\nChoose search method: ".Pastel(GetThemeColor("text")));

        string choice = Console.ReadLine()?.Trim() ?? "1";

        switch (choice)
        {
            case "1":
                currentSearchParams.Categories = GetCategories();
                currentSearchParams.Sorting = "toplist";
                break;
            case "2":
                Console.Write("\nEnter tags (separate with commas): ");
                currentSearchParams.Query = Console.ReadLine()?.Trim() ?? "";
                break;
            case "3":
                Console.Write("\nEnter color code (e.g., 660000, 0066cc, ffffff): ");
                currentSearchParams.Colors = Console.ReadLine()?.Trim() ?? "";
                break;
            case "4":
                currentSearchParams.Sorting = "random";
                currentSearchParams.Seed = GenerateSeed();
                break;
            case "5":
                currentSearchParams.Sorting = "toplist";
                currentSearchParams.TopRange = "1M";
                break;
            case "6":
                await ConfigureAdvancedSearch();
                break;
            case "7":
                return;
            default:
                Console.WriteLine("Invalid choice".Pastel(GetThemeColor("error")));
                return;
        }

        currentSearchParams.Purity = GetPurity();
        currentSearchParams.Ratios = GetRatios();
        currentSearchParams.Resolutions = GetResolutions();

        await SearchAndDisplayWallpapers();
    }

    private static async Task ConfigureAdvancedSearch()
    {
        Console.WriteLine("\n" + "ADVANCED SEARCH".Pastel(GetThemeColor("primary")).PastelBg(GetThemeColor("background")));
        
        currentSearchParams.Categories = GetCategories();
        currentSearchParams.Purity = GetPurity();
        
        Console.Write("\nEnter tags (optional, separate with commas): ");
        currentSearchParams.Query = Console.ReadLine()?.Trim() ?? "";
        
        Console.Write("Enter color code (optional, e.g., 660000): ");
        currentSearchParams.Colors = Console.ReadLine()?.Trim() ?? "";
        
        currentSearchParams.Ratios = GetRatios();
        currentSearchParams.Resolutions = GetResolutions();
        
        Console.WriteLine("\nSorting Options:");
        Console.WriteLine("1. Date Added");
        Console.WriteLine("2. Relevance");
        Console.WriteLine("3. Random");
        Console.WriteLine("4. Views");
        Console.WriteLine("5. Favorites");
        Console.WriteLine("6. Toplist");
        Console.Write("Choose sorting: ");
        
        switch (Console.ReadLine()?.Trim() ?? "1")
        {
            case "1": currentSearchParams.Sorting = "date_added"; break;
            case "2": currentSearchParams.Sorting = "relevance"; break;
            case "3": currentSearchParams.Sorting = "random"; break;
            case "4": currentSearchParams.Sorting = "views"; break;
            case "5": currentSearchParams.Sorting = "favorites"; break;
            case "6": currentSearchParams.Sorting = "toplist"; break;
            default: currentSearchParams.Sorting = "date_added"; break;
        }

        if (currentSearchParams.Sorting == "toplist")
        {
            Console.WriteLine("\nTop Range:");
            Console.WriteLine("1. Last Day");
            Console.WriteLine("2. Last Week");
            Console.WriteLine("3. Last Month");
            Console.WriteLine("4. Last Year");
            Console.WriteLine("5. All Time");
            Console.Write("Choose range: ");
            
            switch (Console.ReadLine()?.Trim() ?? "3")
            {
                case "1": currentSearchParams.TopRange = "1d"; break;
                case "2": currentSearchParams.TopRange = "1w"; break;
                case "3": currentSearchParams.TopRange = "1M"; break;
                case "4": currentSearchParams.TopRange = "1y"; break;
                case "5": currentSearchParams.TopRange = "all"; break;
                default: currentSearchParams.TopRange = "1M"; break;
            }
        }
    }

    private static string GetCategories()
    {
        Console.WriteLine("\nSelect categories:");
        Console.WriteLine("1. General");
        Console.WriteLine("2. Anime");
        Console.WriteLine("3. People");
        Console.WriteLine("4. All Categories");
        Console.Write("Choose option: ");

        string choice = Console.ReadLine()?.Trim() ?? "4";

        return choice switch
        {
            "1" => "100",
            "2" => "010",
            "3" => "001",
            _ => "111"
        };
    }

    private static string GetPurity()
    {
        Console.WriteLine("\nSelect content purity:");
        Console.WriteLine("1. SFW (Safe)");
        Console.WriteLine("2. Sketchy");
        Console.WriteLine("3. NSFW (Unsafe)");
        Console.Write("Choose option: ");

        string choice = Console.ReadLine()?.Trim() ?? "1";

        return choice switch
        {
            "1" => "100",
            "2" => "110",
            "3" => "111",
            _ => "100"
        };
    }

    private static string GetRatios()
    {
        Console.WriteLine("\nSelect aspect ratios:");
        Console.WriteLine("1. All Ratios");
        Console.WriteLine("2. 16:9");
        Console.WriteLine("3. 16:10");
        Console.WriteLine("4. 21:9");
        Console.WriteLine("5. 4:3");
        Console.Write("Choose option: ");

        string choice = Console.ReadLine()?.Trim() ?? "2";

        return choice switch
        {
            "1" => "",
            "2" => "16x9",
            "3" => "16x10",
            "4" => "21x9",
            "5" => "4x3",
            _ => "16x9"
        };
    }

    private static string GetResolutions()
    {
        Console.WriteLine("\nSelect minimum resolution:");
        Console.WriteLine("1. 1920x1080 (Full HD)");
        Console.WriteLine("2. 2560x1440 (2K)");
        Console.WriteLine("3. 3840x2160 (4K)");
        Console.WriteLine("4. No minimum");
        Console.Write("Choose option: ");

        string choice = Console.ReadLine()?.Trim() ?? "1";

        return choice switch
        {
            "1" => "1920x1080",
            "2" => "2560x1440",
            "3" => "3840x2160",
            _ => ""
        };
    }

    private static async Task SearchAndDisplayWallpapers()
    {
        try
        {
            currentPage = 1;
            await FetchWallpapers();
            await WallpaperResultsMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static async Task FetchWallpapers()
    {
        var apiUrl = new StringBuilder($"{BaseApiUrl}search?apikey={ApiKey}");
        
        apiUrl.Append($"&categories={currentSearchParams.Categories}");
        apiUrl.Append($"&purity={currentSearchParams.Purity}");
        apiUrl.Append($"&sorting={currentSearchParams.Sorting}");
        apiUrl.Append($"&order={currentSearchParams.Order}");
        
        if (!string.IsNullOrEmpty(currentSearchParams.Query))
            apiUrl.Append($"&q={Uri.EscapeDataString(currentSearchParams.Query)}");
        
        if (!string.IsNullOrEmpty(currentSearchParams.Colors))
            apiUrl.Append($"&colors={currentSearchParams.Colors}");
        
        if (!string.IsNullOrEmpty(currentSearchParams.Ratios))
            apiUrl.Append($"&ratios={currentSearchParams.Ratios}");
        
        if (!string.IsNullOrEmpty(currentSearchParams.Resolutions))
            apiUrl.Append($"&resolutions={currentSearchParams.Resolutions}");
        
        if (!string.IsNullOrEmpty(currentSearchParams.Seed))
            apiUrl.Append($"&seed={currentSearchParams.Seed}");
        
        if (currentSearchParams.Sorting == "toplist")
            apiUrl.Append($"&topRange={currentSearchParams.TopRange}");
        
        apiUrl.Append($"&page={currentPage}");

        Console.WriteLine($"\nFetching wallpapers...".Pastel(GetThemeColor("primary")));
        
        var response = await client.GetAsync(apiUrl.ToString());
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var data = JObject.Parse(json);

        if (data["data"] == null || !data["data"].Any())
        {
            Console.WriteLine("No wallpapers found. Try different search parameters.".Pastel(GetThemeColor("error")));
            return;
        }

        totalPages = data["meta"]?["last_page"]?.ToObject<int>() ?? 1;
        currentWallpapers = data["data"]?.ToObject<List<Wallpaper>>() ?? new List<Wallpaper>();
        DisplayWallpaperGrid(currentWallpapers);
    }

    private static void DisplayWallpaperGrid(List<Wallpaper> wallpapers)
    {
        Console.WriteLine($"\nPage {currentPage}/{totalPages} | Found {wallpapers.Count} wallpapers".Pastel(GetThemeColor("primary")));
        Console.WriteLine("=".Repeat(80).Pastel(GetThemeColor("background")));
        
        for (int i = 0; i < wallpapers.Count; i++)
        {
            var wp = wallpapers[i];
            string purityBadge = wp.Purity == "sfw" ? "🟢" : 
                               wp.Purity == "sketchy" ? "🟠" : "🔴";
            
            string ratioIcon = "🖼️";
            if (wp.Resolution.Contains("1920x1080") || wp.Resolution.Contains("2560x1440") || 
                wp.Resolution.Contains("3840x2160"))
            {
                ratioIcon = "📺";
            }
            else if (wp.Resolution.Contains("1920x1200") || wp.Resolution.Contains("2560x1600"))
            {
                ratioIcon = "💻";
            }
            else if (wp.Resolution.Contains("2560x1080") || wp.Resolution.Contains("3440x1440"))
            {
                ratioIcon = "🎬";
            }
            else if (wp.Resolution.Contains("1600x1200") || wp.Resolution.Contains("2048x1536"))
            {
                ratioIcon = "📱";
            }
            
            Console.WriteLine($"{i + 1}. {wp.Id} {purityBadge} {ratioIcon} | {wp.Resolution} | 🌟 {wp.Favorites} | 👁️ {wp.Views}");
            Console.WriteLine($"   🏷️ {string.Join(", ", wp.Tags?.Select(t => t.Name)?.Take(5) ?? new List<string>())}");
            
            if (!string.IsNullOrEmpty(wp.Path))
            {
                string domain = new Uri(wp.Path).Host;
                string path = new Uri(wp.Path).AbsolutePath;
                Console.WriteLine($"   🔗 {domain}{path.Substring(0, Math.Min(40, path.Length))}...");
            }
            
            if (i < wallpapers.Count - 1)
                Console.WriteLine("-".Repeat(80).Pastel(GetThemeColor("background")));
        }
        
        Console.WriteLine("=".Repeat(80).Pastel(GetThemeColor("background")));
    }

    private static async Task WallpaperResultsMenu()
    {
        while (true)
        {
            Console.WriteLine("\n" + "WALLPAPER OPTIONS".Pastel(GetThemeColor("primary")).PastelBg(GetThemeColor("background")));
            Console.WriteLine("1-9: Set wallpaper".Pastel(GetThemeColor("menu1")));
            Console.WriteLine("P: Preview wallpaper".Pastel(GetThemeColor("menu2")));
            Console.WriteLine("D: Download without setting".Pastel(GetThemeColor("menu3")));
            Console.WriteLine("N: Next page".Pastel(GetThemeColor("menu6")));
            Console.WriteLine("B: Previous page".Pastel(GetThemeColor("menu6")));
            Console.WriteLine("S: Back to search".Pastel(GetThemeColor("menu4")));
            Console.WriteLine("M: Main menu".Pastel(GetThemeColor("menu7")));
            Console.Write("\nChoose option: ".Pastel(GetThemeColor("text")));

            var selection = Console.ReadLine()?.Trim().ToLower();

            if (int.TryParse(selection, out int index) && index > 0 && index <= currentWallpapers.Count)
            {
                var selectedWallpaper = currentWallpapers[index - 1];
                await DownloadAndSetWallpaper(selectedWallpaper);
                return;
            }

            switch (selection)
            {
                case "p":
                    Console.Write("Enter wallpaper number to preview: ");
                    if (int.TryParse(Console.ReadLine(), out int previewIndex) && 
                        previewIndex > 0 && previewIndex <= currentWallpapers.Count)
                    {
                        ShowWallpaperPreview(currentWallpapers[previewIndex - 1].Path);
                    }
                    break;
                case "d":
                    Console.Write("Enter wallpaper number to download: ");
                    if (int.TryParse(Console.ReadLine(), out int dlIndex) && 
                        dlIndex > 0 && dlIndex <= currentWallpapers.Count)
                    {
                        await DownloadWallpaper(currentWallpapers[dlIndex - 1], false);
                    }
                    break;
                case "n":
                    if (currentPage < totalPages)
                    {
                        currentPage++;
                        await FetchWallpapers();
                    }
                    else
                    {
                        Console.WriteLine("Already on last page".Pastel(GetThemeColor("error")));
                    }
                    break;
                case "b":
                    if (currentPage > 1)
                    {
                        currentPage--;
                        await FetchWallpapers();
                    }
                    else
                    {
                        Console.WriteLine("Already on first page".Pastel(GetThemeColor("error")));
                    }
                    break;
                case "s":
                    await SearchWallpapers();
                    return;
                case "m":
                    return;
                default:
                    Console.WriteLine("Invalid selection".Pastel(GetThemeColor("error")));
                    break;
            }
        }
    }

    private static async Task SetRandomWallpaper()
    {
        try
        {
            Console.WriteLine("\nFetching random wallpaper...".Pastel(GetThemeColor("primary")));
            
            var apiUrl = $"{BaseApiUrl}search?apikey={ApiKey}&sorting=random&categories=111&purity=100";
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);
            var wallpapers = data["data"]?.ToObject<List<Wallpaper>>() ?? new List<Wallpaper>();

            if (wallpapers.Count > 0)
            {
                var randomWallpaper = wallpapers[random.Next(wallpapers.Count)];
                await DownloadAndSetWallpaper(randomWallpaper);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static async Task DownloadAndSetWallpaper(Wallpaper wallpaper)
    {
        if (await DownloadWallpaper(wallpaper, true))
        {
            SetWallpaper(wallpaper);
        }
    }

    private static async Task<bool> DownloadWallpaper(Wallpaper wallpaper, bool setAsWallpaper)
    {
        try
        {
            string imageUrl = wallpaper.Path;
            if (string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("No valid image URL found".Pastel(GetThemeColor("error")));
                return false;
            }

            string wallpaperPath = Path.Combine(wallpaperCachePath, $"wallhaven-{wallpaper.Id}.jpg");
            if (File.Exists(wallpaperPath))
            {
                Console.WriteLine("Using cached wallpaper".Pastel(GetThemeColor("text")));
                if (setAsWallpaper)
                {
                    SetWallpaper(wallpaper);
                }
                return true;
            }

            Console.WriteLine($"\nDownloading: {wallpaper.Id} ({wallpaper.Resolution})".Pastel(GetThemeColor("primary")));
            
            using (var response = await client.GetAsync(imageUrl))
            {
                response.EnsureSuccessStatusCode();
                
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(wallpaperPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            } 
            
            wallpaperHistory.Add(new WallpaperHistoryItem {
                Id = wallpaper.Id,
                Path = wallpaperPath,
                Url = imageUrl,
                Resolution = wallpaper.Resolution,
                SetDate = DateTime.Now,
                Tags = wallpaper.Tags?.Select(t => t.Name).ToList() ?? new List<string>()
            });
            
            SaveHistory();
            currentWallpaperPath = wallpaperPath;

            if (setAsWallpaper)
            {
                SetWallpaper(wallpaper);
            }
            
            Console.WriteLine("Download complete!".Pastel(GetThemeColor("success")));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download error: {ex.Message}".Pastel(GetThemeColor("error")));
            return false;
        }
    }

    private static void SetWallpaper(Wallpaper wallpaper)
    {
        string wallpaperPath = Path.Combine(wallpaperCachePath, $"wallhaven-{wallpaper.Id}.jpg");
        
        if (!File.Exists(wallpaperPath))
        {
            Console.WriteLine("Wallpaper file not found".Pastel(GetThemeColor("error")));
            return;
        }

        SetWallpaperFile(wallpaperPath);
        
        Console.WriteLine("\n🎉 Wallpaper set successfully!".Pastel(GetThemeColor("success")));
        Console.WriteLine($"🆔 ID: {wallpaper.Id}".Pastel(GetThemeColor("primary")));
        Console.WriteLine($"📏 Resolution: {wallpaper.Resolution}".Pastel(GetThemeColor("menu2")));
        Console.WriteLine($"⭐ Favorites: {wallpaper.Favorites}".Pastel(GetThemeColor("menu3")));
        Console.WriteLine($"👀 Views: {wallpaper.Views}\n".Pastel(GetThemeColor("menu1")));
    }

    private static void SetWallpaperFile(string wallpaperPath)
    {
        if (isWindows)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
        else
        {
            try
            {
                string desktopSession = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.ToLower() ?? "";

                if (desktopSession.Contains("gnome") || desktopSession.Contains("ubuntu") || desktopSession.Contains("unity"))
                {
                    ExecuteCommand($"gsettings set org.gnome.desktop.background picture-uri \"file://{wallpaperPath}\"");
                    ExecuteCommand($"gsettings set org.gnome.desktop.background picture-uri-dark \"file://{wallpaperPath}\"");
                }
                else if (desktopSession.Contains("xfce"))
                {
                    ExecuteCommand($"xfconf-query -c xfce4-desktop -p /backdrop/screen0/monitor0/workspace0/last-image -s \"{wallpaperPath}\"");
                }
                else if (desktopSession.Contains("kde"))
                {
                    ExecuteCommand(
                        "dbus-send --session --dest=org.kde.plasmashell --type=method_call /PlasmaShell " +
                        "org.kde.PlasmaShell.evaluateScript 'string:var Desktops = desktops();" +
                        "for (i=0;i<Desktops.length;i++) {" +
                        "d = Desktops[i];" +
                        "d.wallpaperPlugin = \"org.kde.image\";" +
                        "d.currentConfigGroup = Array(\"Wallpaper\", \"org.kde.image\", \"General\");" +
                        $"d.writeConfig(\"Image\", \"file:{wallpaperPath}\");}}'");
                }
                else
                {
                    ExecuteCommand($"feh --bg-fill \"{wallpaperPath}\"");
                }
                
                string currentLink = Path.Combine(wallpaperCachePath, "current_wallpaper");
                if (File.Exists(currentLink)) File.Delete(currentLink);
                File.CreateSymbolicLink(currentLink, wallpaperPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting wallpaper: {ex.Message}".Pastel(GetThemeColor("error")));
            }
        }
    }

    private static void ShowWallpaperHistory()
    {
        if (wallpaperHistory.Count == 0)
        {
            Console.WriteLine("\nNo wallpaper history found".Pastel(GetThemeColor("text")));
            return;
        }

        Console.WriteLine("\n" + $"WALLPAPER HISTORY ({wallpaperHistory.Count})".Pastel(GetThemeColor("primary")).PastelBg(GetThemeColor("background")));
        int displayCount = Math.Min(10, wallpaperHistory.Count);
        
        for (int i = 0; i < displayCount; i++)
        {
            var item = wallpaperHistory[wallpaperHistory.Count - 1 - i];
            Console.WriteLine($"{i + 1}. 🆔 {item.Id} | 📅 {item.SetDate:g} | 📏 {item.Resolution}");
            Console.WriteLine($"   {string.Join(", ", item.Tags.Take(5))}");
            Console.WriteLine($"   {item.Path}");
            if (i < displayCount - 1)
                Console.WriteLine("-".Repeat(80).Pastel(GetThemeColor("background")));
        }

        Console.WriteLine("\n1-10: Set wallpaper from history".Pastel(GetThemeColor("menu1")));
        Console.WriteLine("C: Clear history".Pastel(GetThemeColor("menu5")));
        Console.WriteLine("B: Back".Pastel(GetThemeColor("menu4")));
        Console.Write("Choose option: ".Pastel(GetThemeColor("text")));

        var choice = Console.ReadLine()?.Trim().ToLower();
        if (int.TryParse(choice, out int index) && index > 0 && index <= displayCount)
        {
            var wallpaper = wallpaperHistory[wallpaperHistory.Count - index];
            if (File.Exists(wallpaper.Path))
            {
                SetWallpaperFile(wallpaper.Path);
                currentWallpaperPath = wallpaper.Path;
                Console.WriteLine("Wallpaper set from history".Pastel(GetThemeColor("success")));
            }
            else
            {
                Console.WriteLine("Wallpaper file not found".Pastel(GetThemeColor("error")));
            }
        }
        else if (choice == "c")
        {
            Console.Write("Are you sure? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                wallpaperHistory.Clear();
                SaveHistory();
                Console.WriteLine("History cleared".Pastel(GetThemeColor("success")));
            }
        }
    }

    private static void ExecuteCommand(string command)
    {
        try
        {
            var escapedArgs = command.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command error: {ex.Message}".Pastel(GetThemeColor("error")));
        }
    }

    private static string GenerateSeed()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

class SearchParameters
{
    public string Query { get; set; } = "";
    public string Categories { get; set; } = "111";
    public string Purity { get; set; } = "100";
    public string Sorting { get; set; } = "date_added";
    public string Order { get; set; } = "desc";
    public string TopRange { get; set; } = "1M";
    public string Colors { get; set; } = "";
    public string Ratios { get; set; } = "16x9";
    public string Resolutions { get; set; } = "1920x1080";
    public string Seed { get; set; } = "";
}

class Wallpaper
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";
    
    [JsonProperty("url")]
    public string Url { get; set; } = "";
    
    [JsonProperty("short_url")]
    public string ShortUrl { get; set; } = "";
    
    [JsonProperty("purity")]
    public string Purity { get; set; } = "";
    
    [JsonProperty("category")]
    public string Category { get; set; } = "";
    
    [JsonProperty("resolution")]
    public string Resolution { get; set; } = "";
    
    [JsonProperty("colors")]
    public List<string> Colors { get; set; } = new List<string>();
    
    [JsonProperty("path")]
    public string Path { get; set; } = "";
    
    [JsonProperty("tags")]
    public List<Tag> Tags { get; set; } = new List<Tag>();
    
    [JsonProperty("favorites")]
    public int Favorites { get; set; }
    
    [JsonProperty("views")]
    public int Views { get; set; }
}

class Tag
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; } = "";
}

class WallpaperHistoryItem
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public string Url { get; set; } = "";
    public string Resolution { get; set; } = "";
    public DateTime SetDate { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
}

public static class StringExtensions
{
    public static string Repeat(this string s, int count)
    {
        return new StringBuilder(s.Length * count).Insert(0, s, count).ToString();
    }
}
