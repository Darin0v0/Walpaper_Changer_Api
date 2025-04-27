using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    private const string ApiKey = "aOAqwTWJ3bqA6D7JEqPTrgaSiwB97J9g";
    private const string BaseApiUrl = "https://wallhaven.cc/api/v1/";
private const string DefaultWindowsWallpaperUrl = "https://w.wallhaven.cc/full/lq/wallhaven-lqye5q.png";
    private const string DefaultLinuxWallpaperUrl = "https://w.wallhaven.cc/full/ey/wallhaven-eyj8dk.png";
    private static readonly HttpClient client = new HttpClient();
    private static readonly List<string> downloadedWallpapers = new List<string>();
    private static bool isWindows = true;
    private static int currentPage = 1;
    private static string currentSeed = "";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("🌄 Wallpaper Changer");
        Console.WriteLine("===================");
        
        await SelectOperatingSystem();

        while (true)
        {
            Console.WriteLine("\nMain Menu:");
            Console.WriteLine("1. Search Wallpapers from Wallhaven");
            Console.WriteLine("2. Set Default System Wallpaper");
            Console.WriteLine("3. View Search History");
            Console.WriteLine("4. Exit");
            Console.Write("Choose option (1-4): ");

            string mainChoice = Console.ReadLine() ?? "1";

            switch (mainChoice)
            {
                case "1":
                    await SearchWallpapers();
                    break;
                case "2":
                    await SetDefaultWallpaper();
                    break;
                case "3":
                    ShowDownloadHistory();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
    }

    private static async Task SelectOperatingSystem()
    {
        Console.WriteLine("\nSelect your operating system:");
        Console.WriteLine("1. Windows");
        Console.WriteLine("2. Linux");
        Console.Write("Enter option (1/2): ");
        
        string osChoice = Console.ReadLine() ?? "1";
        isWindows = (osChoice == "1");
    }

    private static async Task SearchWallpapers()
    {
        Console.WriteLine("\nSearch Options:");
        Console.WriteLine("1. By Category");
        Console.WriteLine("2. By Tags");
        Console.WriteLine("3. By Color");
        Console.WriteLine("4. Random Wallpaper");
        Console.WriteLine("5. Popular This Month");
        Console.Write("Choose search method (1-5): ");

        string choice = Console.ReadLine() ?? "1";

        string query = "";
        string categories = "111"; 
        string purity = GetPurity();
        string sorting = "date_added";
        string order = "desc";
        string topRange = "1M";
        string colors = "";
        string ratios = "16x9";

        switch (choice)
        {
            case "1":
                categories = GetCategories();
                sorting = "toplist";
                break;
            case "2":
                Console.Write("Enter tags (separate with spaces, max 6 tags): ");
                query = Console.ReadLine() ?? "";
                break;
            case "3":
                Console.Write("Enter color code (e.g., 660000, 0066cc, ffffff): ");
                colors = Console.ReadLine() ?? "";
                break;
            case "4":
                sorting = "random";
                currentSeed = GenerateSeed();
                break;
            case "5":
                sorting = "toplist";
                topRange = "1M";
                break;
        }

        await SearchAndSetWallpaper(query, categories, purity, sorting, order, topRange, colors, ratios);
    }

    private static string GetCategories()
    {
        Console.WriteLine("\nSelect categories:");
        Console.WriteLine("1. General");
        Console.WriteLine("2. Anime");
        Console.WriteLine("3. People");
        Console.WriteLine("4. All Categories");
        Console.Write("Choose option (1-4): ");

        string choice = Console.ReadLine() ?? "4";

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
        Console.WriteLine("1. SFW ");
        Console.WriteLine("2. Sketchy");
        Console.WriteLine("3. NSFW ");
        Console.Write("Choose option (1-3): ");

        string choice = Console.ReadLine() ?? "1";

        return choice switch
        {
            "1" => "100",
            "2" => "010",
            "3" => "001",
            _ => "100"
        };
    }

    private static async Task SearchAndSetWallpaper(string query, string categories, string purity, 
        string sorting, string order, string topRange, string colors, string ratios)
    {
        try
        {
            var apiUrl = new StringBuilder($"{BaseApiUrl}search?apikey={ApiKey}");
            
            apiUrl.Append($"&categories={categories}");
            apiUrl.Append($"&purity={purity}");
            apiUrl.Append($"&sorting={sorting}");
            apiUrl.Append($"&order={order}");
            
            if (sorting == "toplist")
                apiUrl.Append($"&topRange={topRange}");
            
            if (!string.IsNullOrEmpty(query))
                apiUrl.Append($"&q={Uri.EscapeDataString(query)}");
            
            if (!string.IsNullOrEmpty(colors))
                apiUrl.Append($"&colors={colors}");
            
            if (!string.IsNullOrEmpty(ratios))
                apiUrl.Append($"&ratios={ratios}");
            
            if (!string.IsNullOrEmpty(currentSeed))
                apiUrl.Append($"&seed={currentSeed}");
            
            apiUrl.Append($"&page={currentPage}");
            apiUrl.Append("&atleast=1920x1080");

            Console.WriteLine($"\nFetching wallpapers from: {apiUrl.ToString().Replace(ApiKey, "***")}");

            var response = await client.GetAsync(apiUrl.ToString());
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            if (data["data"] == null || !data["data"].Any())
            {
                Console.WriteLine("No wallpapers found. Try different search parameters.");
                return;
            }

            var wallpapers = data["data"].ToObject<List<Wallpaper>>();
            DisplayWallpaperList(wallpapers);

            Console.Write("\nEnter wallpaper number to set (or 'n' for next page, 'p' for previous page): ");
            var selection = Console.ReadLine();

            if (selection?.ToLower() == "n")
            {
                currentPage++;
                await SearchAndSetWallpaper(query, categories, purity, sorting, order, topRange, colors, ratios);
                return;
            }
            else if (selection?.ToLower() == "p" && currentPage > 1)
            {
                currentPage--;
                await SearchAndSetWallpaper(query, categories, purity, sorting, order, topRange, colors, ratios);
                return;
            }

            if (int.TryParse(selection, out int index) && index > 0 && index <= wallpapers.Count)
            {
                var selectedWallpaper = wallpapers[index - 1];
                await DownloadAndSetWallpaper(selectedWallpaper);
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void DisplayWallpaperList(List<Wallpaper> wallpapers)
    {
        Console.WriteLine("\nSearch Results:");
        for (int i = 0; i < wallpapers.Count; i++)
        {
            var wp = wallpapers[i];
            Console.WriteLine($"{i + 1}. {wp.Id} - {wp.Resolution} - {wp.Category}/{wp.Purity}");
            Console.WriteLine($"   Tags: {string.Join(", ", wp.Tags?.Select(t => t.Name) ?? new List<string>())}");
            Console.WriteLine($"   Colors: {string.Join(", ", wp.Colors ?? new List<string>())}");
        }
    }

    private static async Task DownloadAndSetWallpaper(Wallpaper wallpaper)
    {
        try
        {
            string imageUrl = wallpaper.Path;
            if (string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("No valid image URL found.");
                return;
            }

            if (downloadedWallpapers.Contains(imageUrl))
            {
                Console.WriteLine("This wallpaper has already been used.");
                return;
            }

            Console.WriteLine($"\nDownloading wallpaper: {wallpaper.Id}");
            Console.WriteLine($"Resolution: {wallpaper.Resolution}");
            Console.WriteLine($"URL: {imageUrl}");

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"wallhaven-{wallpaper.Id}.jpg");
            
            using (var response = await client.GetAsync(imageUrl))
            {
                response.EnsureSuccessStatusCode();
                
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
            
            SetWallpaper(tempFilePath);
            downloadedWallpapers.Add(imageUrl);
            Console.WriteLine("Wallpaper set successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading wallpaper: {ex.Message}");
        }
    }

    private static async Task SetDefaultWallpaper()
    {
        try
        {
            string defaultUrl = isWindows ? DefaultWindowsWallpaperUrl : DefaultLinuxWallpaperUrl;
            Console.WriteLine($"\nDownloading default wallpaper from: {defaultUrl}");

            string tempFilePath = Path.Combine(Path.GetTempPath(), isWindows ? "windows_default.jpg" : "linux_default.jpg");
            
            using (var response = await client.GetAsync(defaultUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                    
                    SetWallpaper(tempFilePath);
                    Console.WriteLine("Default wallpaper set successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to download default wallpaper.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void ShowDownloadHistory()
    {
        Console.WriteLine("\nDownload History:");
        if (downloadedWallpapers.Count == 0)
        {
            Console.WriteLine("No wallpapers downloaded yet.");
            return;
        }

        for (int i = 0; i < downloadedWallpapers.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {downloadedWallpapers[i]}");
        }
    }

    private static void SetWallpaper(string path)
    {
        if (isWindows)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
        else
        {
            try
            {
                string desktopSession = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.ToLower() ?? "";

                if (desktopSession.Contains("gnome") || desktopSession.Contains("ubuntu") || desktopSession.Contains("unity"))
                {
                    ExecuteCommand($"gsettings set org.gnome.desktop.background picture-uri \"file://{path}\"");
                    ExecuteCommand($"gsettings set org.gnome.desktop.background picture-uri-dark \"file://{path}\"");
                }
                else if (desktopSession.Contains("xfce"))
                {
                    ExecuteCommand($"xfconf-query -c xfce4-desktop -p /backdrop/screen0/monitor0/workspace0/last-image -s \"{path}\"");
                }
                else if (desktopSession.Contains("kde"))
                {
                    ExecuteCommand($"dbus-send --session --dest=org.kde.plasmashell --type=method_call /PlasmaShell org.kde.PlasmaShell.evaluateScript " +
                                  $"\"string:var Desktops = desktops();" +
                                  $"for (i=0;i<Desktops.length;i++) {{" +
                                  $"d = Desktops[i];" +
                                  $"d.wallpaperPlugin = \\\"org.kde.image\\\";" +
                                  $"d.currentConfigGroup = Array(\\\"Wallpaper\\\", \\\"org.kde.image\\\", \\\"General\\\");" +
                                  $"d.writeConfig(\\\"Image\\\", \\\"file:{path}\\\")}}\"");
                }
                else
                {
                    ExecuteCommand($"feh --bg-fill \"{path}\"");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting wallpaper: {ex.Message}");
            }
        }
    }

    private static void ExecuteCommand(string command)
    {
        try
        {
            var parts = command.Split(new[] { ' ' }, 2);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = parts.Length > 1 ? parts[1] : "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Command failed: {process.StandardError.ReadToEnd()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
        }
    }

    private static string GenerateSeed()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

class Wallpaper
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string ShortUrl { get; set; }
    public string Purity { get; set; }
    public string Category { get; set; }
    public string Resolution { get; set; }
    public List<string> Colors { get; set; }
    public string Path { get; set; }
    public List<Tag> Tags { get; set; }
}

class Tag
{
    public string Name { get; set; }
}
