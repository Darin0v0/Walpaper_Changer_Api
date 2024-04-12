using System;
using System.Collections.Generic;
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

    private const string ApiKey = "k2NvVteKsYaaWAWmVla6hKQ6cA76595v"; // Your API key here

    private static readonly HttpClient client = new HttpClient();
    private static readonly List<string> downloadedWallpapers = new List<string>();

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Choose wallpaper type:");
            Console.WriteLine("1. Anime");
            Console.WriteLine("2. Forest");
            Console.WriteLine("3. City and People");
            Console.WriteLine("4. Select");
            Console.Write("Enter option (1/2/3/4): ");

            string choice = Console.ReadLine();

            string category = "";
            string tag = "";
            string excludeTag = "";
            switch (choice)
            {
                case "1":
                    category = "101";
                    tag = "anime";
                    break;
                case "2":
                    category = "111";
                    tag = "Forest";
                    excludeTag = "anime";
                    break;
                case "3":
                    category = "111";
                    tag = "city";
                    excludeTag = "anime";
                    break;
                case "4":
                    Console.WriteLine("Tag if you use more than 1 use ","");
                    var input = Console.ReadLine(); 
                    category = "100";
                    tag = input;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please choose again.");
                    continue;
            }

            await SetRandomWallpaper(category, tag, excludeTag);

            Console.WriteLine("Wallpaper has been set. Press Enter to continue...");
            Console.ReadLine();
        }
    }

    private static async Task SetRandomWallpaper(string category, string tag, string excludeTag)
    {
        string apiUrl = $"https://wallhaven.cc/api/v1/search?apikey={ApiKey}&seed={GenerateSeed()}";

        if (!string.IsNullOrEmpty(category))
        {
            apiUrl += $"&categories={category}&purity=100&sorting=toplist&order=desc&topRange=1y&atleast=1920x1080";
        }

        if (!string.IsNullOrEmpty(tag))
        {
            apiUrl += $"&q={tag}";
        }

        if (!string.IsNullOrEmpty(excludeTag))
        {
            apiUrl += $"&-q={excludeTag}";
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
                    Console.WriteLine("No suitable wallpapers found for the selected type and tag.");
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
                } while (downloadedWallpapers.Contains(imageUrl) && attemptCount < 5); // Limit attempts to avoid infinite loop

                if (attemptCount >= 5)
                {
                    Console.WriteLine("Failed to find a unique wallpaper after multiple attempts.");
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
                    Console.WriteLine("An error occurred while downloading the image.");
                }
            }
            else
            {
                Console.WriteLine("Failed to fetch data from the API.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
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
