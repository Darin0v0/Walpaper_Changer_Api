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
            Console.WriteLine("Wybierz typ tapety:");
            Console.WriteLine("1. Anime");
            Console.WriteLine("2. Las(Forest)");
            Console.WriteLine("3. Miasto(City)");
            Console.WriteLine("4. Wybierz(select) Tag");
            Console.WriteLine("5. Kolor(color)");
            Console.Write("Wprowadź opcję(select) (1/2/3/4/5): ");

            string? choice = Console.ReadLine();

            string? category = "";
            string? tag = "";
            string? excludeTag = "";
            string? color = "";
            switch (choice)
            {
                case "1":
                    category = "010";
                    tag = "Anime";
                 // excludeTag = "";
                    break;
                case "2":
                    category = "100";
                    tag = "forest";
                    excludeTag = "Anime";
                    break;
                case "3":
                    category = "111";
                    tag = "city";
                    break;
                case "4":
                    Console.WriteLine("Wprowadź tagi (jeśli używasz więcej niż jednego, odziej je ' ' spacją. max:4 tagi): ");
                    tag = Console.ReadLine();
                    category = "100";
                    break;
                case "5":
                    Console.WriteLine("Wprowadź kolor (np:cc0000, 0066cc, 336600, ffff00, 000000, ffffff)");
                    color = Console.ReadLine();
                    category = "100";
                    break;
                default:
                    Console.WriteLine("Niepoprawny wybór. Wybierz ponownie.");
                    continue;
            }

            int purity = GetPurity();
            await SetRandomWallpaper(category, tag, excludeTag, color, purity);

            Console.WriteLine("Tapeta została ustawiona. Naciśnij Enter, aby kontynuować...");
            Console.ReadLine();
        }
    }

    private static int GetPurity()
    {
        Console.WriteLine("Czystość: 1 - bezpieczna dla pracy 2-sketchy(średnie) , 3 - NSFW (niebezpieczna)");
        int purity = int.Parse(Console.ReadLine());
        if (purity == 3)
        {
            Console.WriteLine("NSFW! Wprowadź 1, aby wyjść, lub 6, aby kontynuować: ");
            int choice = int.Parse(Console.ReadLine());
            if (choice == 6)
            {
                return 001;
            }
            else
            {
                return 100;
            }
        }
        else
        {
           if(purity == 1)
            {
                return 100;
            }
            else
            {
                return 010;
            }
        }
    }

    private static async Task SetRandomWallpaper(string category, string tag, string excludeTag, string color, int purity)
    {
        string apiUrl = $"https://wallhaven.cc/api/v1/search?apikey={ApiKey}&seed={GenerateSeed()}";

        if (!string.IsNullOrEmpty(category))
        {
            apiUrl += $"&categories={category}&purity={purity}&sorting=toplist&order=desc&atlest=1980x1080%topRange=1y&ratios=16x9";
        }

        if (!string.IsNullOrEmpty(tag))
        {
            if(!string.IsNullOrEmpty(excludeTag)) 
            {
                apiUrl += $"&q=+{tag}-{excludeTag}";
            }
            else
            {
            apiUrl += $"&q=+{tag}";
            }
            
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
                    Console.WriteLine("Nie znaleziono odpowiednich tapet dla wybranego typu i tagu.");
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
                    Console.WriteLine("Nie udało się znaleźć unikalnej tapety po wielu próbach.");
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
                    Console.WriteLine("Wystąpił błąd podczas pobierania obrazu.");
                }
            }
            else
            {
                Console.WriteLine("Nie udało się pobrać danych z API.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd: {ex.Message}");
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
