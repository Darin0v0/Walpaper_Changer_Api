using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    private const string ApiKey = "aOAqwTWJ3bqA6D7JEqPTrgaSiwB97J9g"; // Twój klucz API

    private static readonly HttpClient client = new HttpClient();
    private static readonly List<string> downloadedWallpapers = new List<string>();

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Select One:");
            Console.WriteLine("1. Anime");
            Console.WriteLine("2. Forest");
            Console.WriteLine("3. City i Peoples");
            Console.WriteLine("4. Set Tag:");
            Console.WriteLine("5. Color");
            Console.Write("Input?: (1/2/3/4/5): ");

            string? choice = Console.ReadLine();

            string? category = "";
            string? tag = "";
            string? excludeTag = "";
            string? color = "";
            switch (choice)
            {
                case "1":
                    category = "010";
                    tag = "anime";
                    break;
                case "2":
                    category = "111";
                    tag = "forest";
                    excludeTag = "anime";
                    break;
                case "3":
                    category = "111";
                    tag = "city";
                    excludeTag = "anime";
                    break;
                case "4":
                    Console.WriteLine("Set tag (if you use mor than one use " " spaceber));
                    tag = Console.ReadLine(); 
                    category = "100";
                    break;
                case "5":
                    Console.WriteLine("Input?: Color (np. 0066cc, cc0000, 336600): ");
                    color = Console.ReadLine();
                    category = "100";
                    break;
                default:
                    Console.WriteLine("Try Again(E    rr     oo     r)");
                    continue;
            }

            int purity = GetPurity();
            await SetRandomWallpaper(category, tag, excludeTag, color, purity);

            Console.WriteLine("Tapet Set...");
            Console.ReadLine();
        }
    }

    private static int GetPurity()
    {
        Console.WriteLine("Purity: 1 - SFW, 2 - NSFW (Achtung)");
        int purity = int.Parse(Console.ReadLine());
        if (purity == 2)
        {
            Console.WriteLine("NSFW! input 1, to leve or 6 to go: ");
            int choice = int.Parse(Console.ReadLine());
            if (choice == 6)
            {
                return 111;
            }
            else
            {
                return 100;
            }
        }
        else
        {
            return 100;
        }
    }

    private static async Task SetRandomWallpaper(string category, string tag, string excludeTag, string color, int purity)
    {
        string apiUrl = $"https://wallhaven.cc/api/v1/search?apikey={ApiKey}&seed={GenerateSeed()}";

        if (!string.IsNullOrEmpty(category))
        {
            apiUrl += $"&categories={category}&purity={purity}&sorting=toplist&order=desc&topRange=6M&atleast=1920x1080";
        }

        if (!string.IsNullOrEmpty(tag))
        {
            apiUrl += $"&q={tag}";
        }

        if (!string.IsNullOrEmpty(excludeTag))
        {
            apiUrl += $"&-q={excludeTag}";
        }

        if (!string.IsNullOrEmpty(color))
        {
            apiUrl += $"&colors={color}";
        }

        try
        {
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();

                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                if (data.data.Count == 0)
                {
                    Console.WriteLine("Error.Tag Dont exisit ");
                    return;
                }

                Random random = new Random();

                string imageUrl = "";
                int attemptCount = 0;

                do
                {
                    int index = random.Next(0, Math.Min(data.data.Count, 150));
                    imageUrl = data.data[index].path;

                    attemptCount++;
                } while (downloadedWallpapers.Contains(imageUrl) && attemptCount < 5); // Ograniczenie liczby prób, aby uniknąć nieskończonej pętli

                if (attemptCount >= 5)
                {
                    Console.WriteLine("Tapet dont exist.");
                    return;
                }

                downloadedWallpapers.Add(imageUrl);

                HttpResponseMessage imageResponse = await client.GetAsync(imageUrl);

                if (imageResponse.IsSuccessStatusCode)
                {
                    using (var stream = await imageResponse.Content.ReadAsStreamAsync())
                    {
                        string tempFilePath = Path.GetTempFileName();
                        using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await stream.CopyToAsync(tempFileStream);
                        }

                        SetWallpaper(tempFilePath);
                    }
                }
                else
                {
                    Console.WriteLine("error in photos set");
                }
            }
            else
            {
                Console.WriteLine("error API.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error : {ex.Message}");
        }
    }

    private static void SetWallpaper(string path)
    {
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }

    private static string GenerateSeed()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
