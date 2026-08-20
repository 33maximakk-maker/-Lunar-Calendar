// LunarCalendar.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

class Config
{
    [JsonPropertyName("saved_location")]
    public SavedLocation SavedLocation { get; set; } = new SavedLocation();
}

class SavedLocation
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("lat")] public double Lat { get; set; } = 0.0;
    [JsonPropertyName("lon")] public double Lon { get; set; } = 0.0;
}

class LunarResult
{
    public string Phase { get; set; }
    public double Illumination { get; set; }
    public double Age { get; set; }
    public string Zodiac { get; set; }
}

class LunarCalendar
{
    private const double DEFAULT_LAT = 0.0;
    private const double DEFAULT_LON = 0.0;
    private const string CONFIG_FILE = "lunar_config.json";
    private static readonly JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

    static double JulianDay(DateTime dt)
    {
        int year = dt.Year;
        int month = dt.Month;
        double day = dt.Day + dt.Hour/24.0 + dt.Minute/1440.0 + dt.Second/86400.0;
        if (month <= 2) { year--; month += 12; }
        int A = year / 100;
        int B = 2 - A + A / 4;
        return (int)(365.25 * (year + 4716)) + (int)(30.6001 * (month + 1)) + day + B - 1524.5;
    }

    static (double lon, double lat) MoonPosition(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double L_prime = 218.3165 + 481267.8813 * T;
        double D = 297.8502 + 445267.1114 * T;
        double M = 357.5291 + 35999.0503 * T;
        double M_prime = 134.9634 + 477198.8676 * T;
        double F = 93.2720 + 483202.0175 * T;

        L_prime = ((L_prime % 360) + 360) % 360 * Math.PI / 180;
        D = ((D % 360) + 360) % 360 * Math.PI / 180;
        M = ((M % 360) + 360) % 360 * Math.PI / 180;
        M_prime = ((M_prime % 360) + 360) % 360 * Math.PI / 180;
        F = ((F % 360) + 360) % 360 * Math.PI / 180;

        double lon = L_prime + (6.289 * Math.Sin(M_prime) + 1.274 * Math.Sin(2*D - M_prime) + 0.658 * Math.Sin(2*D) + 0.214 * Math.Sin(2*M_prime) - 0.186 * Math.Sin(M) - 0.114 * Math.Sin(2*F)) * Math.PI / 180;
        double lat = (5.128 * Math.Sin(F) + 0.280 * Math.Sin(M_prime + F) + 0.278 * Math.Sin(M_prime - F) + 0.173 * Math.Sin(2*D - F)) * Math.PI / 180;
        return (lon, lat);
    }

    static double SunPosition(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;
        double M = ((357.5291 + 35999.0503 * T) % 360) * Math.PI / 180;
        double C = 1.9146 * Math.Sin(M) + 0.0200 * Math.Sin(2*M) + 0.0003 * Math.Sin(3*M);
        double lon = (280.4665 + 36000.7698 * T + C) % 360;
        return lon * Math.PI / 180;
    }

    static LunarResult MoonPhase(DateTime dt)
    {
        double jd = JulianDay(dt);
        var (lonMoon, _) = MoonPosition(jd);
        double lonSun = SunPosition(jd);

        double elong = lonMoon - lonSun;
        elong = Math.Atan2(Math.Sin(elong), Math.Cos(elong));
        double phaseAngle = Math.Atan2(Math.Sin(elong), Math.Cos(elong));
        double illumination = (1 + Math.Cos(phaseAngle)) / 2;

        double age = (jd - 2451550.1) / 29.53058867;
        age = ((age % 29.53058867) + 29.53058867) % 29.53058867;

        string phase;
        if (age < 1.0) phase = "New Moon";
        else if (age < 7.38) phase = "Waxing Crescent";
        else if (age < 8.38) phase = "First Quarter";
        else if (age < 14.77) phase = "Waxing Gibbous";
        else if (age < 15.77) phase = "Full Moon";
        else if (age < 22.15) phase = "Waning Gibbous";
        else if (age < 23.15) phase = "Last Quarter";
        else phase = "Waning Crescent";

        string[] signs = {"Aries","Taurus","Gemini","Cancer","Leo","Virgo","Libra","Scorpio","Sagittarius","Capricorn","Aquarius","Pisces"};
        double lonDeg = ((lonMoon * 180 / Math.PI) % 360 + 360) % 360;
        int idx = (int)(lonDeg / 30);
        string zodiac = signs[idx];

        return new LunarResult { Phase = phase, Illumination = illumination * 100, Age = age, Zodiac = zodiac };
    }

    static string AsciiPhase(double illumination)
    {
        if (illumination < 1) return "🌑 (New Moon)";
        if (illumination < 20) return "🌒 (Waxing Crescent)";
        if (illumination < 40) return "🌓 (First Quarter)";
        if (illumination < 60) return "🌔 (Waxing Gibbous)";
        if (illumination < 80) return "🌕 (Full Moon)";
        if (illumination < 90) return "🌖 (Waning Gibbous)";
        if (illumination < 98) return "🌗 (Last Quarter)";
        return "🌘 (Waning Crescent)";
    }

    static void Main(string[] args)
    {
        var parsed = ParseArgs(args);

        // Load config
        Config config = new Config();
        if (File.Exists(CONFIG_FILE))
        {
            string json = File.ReadAllText(CONFIG_FILE);
            config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }

        if (parsed.ContainsKey("save-location"))
        {
            config.SavedLocation.Name = parsed["save-location"];
            config.SavedLocation.Lat = double.Parse(parsed.GetValueOrDefault("lat", DEFAULT_LAT.ToString()));
            config.SavedLocation.Lon = double.Parse(parsed.GetValueOrDefault("lon", DEFAULT_LON.ToString()));
            File.WriteAllText(CONFIG_FILE, JsonSerializer.Serialize(config, options));
            Console.WriteLine($"✅ Location '{config.SavedLocation.Name}' saved.");
        }

        double lat = parsed.ContainsKey("lat") ? double.Parse(parsed["lat"]) : config.SavedLocation.Lat;
        double lon = parsed.ContainsKey("lon") ? double.Parse(parsed["lon"]) : config.SavedLocation.Lon;

        DateTime dt = DateTime.Now;
        if (parsed.ContainsKey("date"))
        {
            dt = DateTime.Parse(parsed["date"]);
        }

        var result = MoonPhase(dt);

        if (parsed.ContainsKey("phase-only"))
        {
            Console.WriteLine(result.Phase);
            return;
        }

        Console.WriteLine($"\n🌙 Lunar Calendar");
        Console.WriteLine($"Date: {dt:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Location: {lat:F2}°, {lon:F2}°");
        Console.WriteLine($"\n🌓 Phase: {result.Phase} ({result.Illumination:F1}% illuminated)");
        Console.WriteLine($"📅 Moon age: {result.Age:F1} days");
        Console.WriteLine($"♑ Zodiac: {result.Zodiac}");
        Console.WriteLine($"\nVisual:\n{AsciiPhase(result.Illumination)}");
    }

    static Dictionary<string, string> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string>();
        for (int i=0; i<args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                string key = args[i].Substring(2);
                if (i+1 < args.Length && !args[i+1].StartsWith("--"))
                    dict[key] = args[++i];
                else
                    dict[key] = "";
            }
        }
        return dict;
    }
}
