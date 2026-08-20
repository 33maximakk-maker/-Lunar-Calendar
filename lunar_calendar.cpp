// lunar_calendar.cpp
#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <cmath>
#include <ctime>
#include <iomanip>
#include <sstream>
#include <nlohmann/json.hpp>
#include <getopt.h>

using namespace std;
using json = nlohmann::json;

const double DEFAULT_LAT = 0.0;
const double DEFAULT_LON = 0.0;
const string CONFIG_FILE = "lunar_config.json";

double julianDay(const tm& dt) {
    int year = dt.tm_year + 1900;
    int month = dt.tm_mon + 1;
    double day = dt.tm_mday + dt.tm_hour/24.0 + dt.tm_min/1440.0 + dt.tm_sec/86400.0;
    if (month <= 2) { year--; month += 12; }
    int A = year / 100;
    int B = 2 - A + A / 4;
    return (int)(365.25 * (year + 4716)) + (int)(30.6001 * (month + 1)) + day + B - 1524.5;
}

pair<double, double> moonPosition(double jd) {
    double T = (jd - 2451545.0) / 36525.0;
    double L_prime = 218.3165 + 481267.8813 * T;
    double D = 297.8502 + 445267.1114 * T;
    double M = 357.5291 + 35999.0503 * T;
    double M_prime = 134.9634 + 477198.8676 * T;
    double F = 93.2720 + 483202.0175 * T;

    L_prime = fmod(L_prime, 360) * M_PI / 180;
    D = fmod(D, 360) * M_PI / 180;
    M = fmod(M, 360) * M_PI / 180;
    M_prime = fmod(M_prime, 360) * M_PI / 180;
    F = fmod(F, 360) * M_PI / 180;

    double lon = L_prime + (6.289 * sin(M_prime) + 1.274 * sin(2*D - M_prime) + 0.658 * sin(2*D) + 0.214 * sin(2*M_prime) - 0.186 * sin(M) - 0.114 * sin(2*F)) * M_PI / 180;
    double lat = (5.128 * sin(F) + 0.280 * sin(M_prime + F) + 0.278 * sin(M_prime - F) + 0.173 * sin(2*D - F)) * M_PI / 180;
    return {lon, lat};
}

double sunPosition(double jd) {
    double T = (jd - 2451545.0) / 36525.0;
    double M = fmod(357.5291 + 35999.0503 * T, 360) * M_PI / 180;
    double C = 1.9146 * sin(M) + 0.0200 * sin(2*M) + 0.0003 * sin(3*M);
    double lon = fmod(280.4665 + 36000.7698 * T + C, 360);
    return lon * M_PI / 180;
}

struct LunarResult {
    string phase;
    double illumination;
    double age;
    string zodiac;
};

LunarResult moonPhase(const tm& dt) {
    double jd = julianDay(dt);
    auto [lonMoon, _] = moonPosition(jd);
    double lonSun = sunPosition(jd);

    double elong = lonMoon - lonSun;
    elong = atan2(sin(elong), cos(elong));
    double phaseAngle = atan2(sin(elong), cos(elong));
    double illumination = (1 + cos(phaseAngle)) / 2;

    double age = (jd - 2451550.1) / 29.53058867;
    age = fmod(age, 29.53058867);
    if (age < 0) age += 29.53058867;

    string phase;
    if (age < 1.0) phase = "New Moon";
    else if (age < 7.38) phase = "Waxing Crescent";
    else if (age < 8.38) phase = "First Quarter";
    else if (age < 14.77) phase = "Waxing Gibbous";
    else if (age < 15.77) phase = "Full Moon";
    else if (age < 22.15) phase = "Waning Gibbous";
    else if (age < 23.15) phase = "Last Quarter";
    else phase = "Waning Crescent";

    vector<string> signs = {"Aries","Taurus","Gemini","Cancer","Leo","Virgo","Libra","Scorpio","Sagittarius","Capricorn","Aquarius","Pisces"};
    double lonDeg = fmod(lonMoon * 180 / M_PI, 360);
    if (lonDeg < 0) lonDeg += 360;
    int idx = (int)(lonDeg / 30);
    string zodiac = signs[idx];

    return {phase, illumination * 100, age, zodiac};
}

string asciiPhase(double illumination) {
    if (illumination < 1) return "🌑 (New Moon)";
    if (illumination < 20) return "🌒 (Waxing Crescent)";
    if (illumination < 40) return "🌓 (First Quarter)";
    if (illumination < 60) return "🌔 (Waxing Gibbous)";
    if (illumination < 80) return "🌕 (Full Moon)";
    if (illumination < 90) return "🌖 (Waning Gibbous)";
    if (illumination < 98) return "🌗 (Last Quarter)";
    return "🌘 (Waning Crescent)";
}

int main(int argc, char* argv[]) {
    static struct option long_options[] = {
        {"date", required_argument, 0, 'd'},
        {"lat", required_argument, 0, 'a'},
        {"lon", required_argument, 0, 'o'},
        {"phase-only", no_argument, 0, 'p'},
        {"all", no_argument, 0, 'A'},
        {"save-location", required_argument, 0, 's'},
        {0,0,0,0}
    };
    int opt;
    string dateStr, saveLocation;
    double lat = DEFAULT_LAT, lon = DEFAULT_LON;
    bool phaseOnly = false, all = false;

    while ((opt = getopt_long(argc, argv, "d:a:o:pAs:", long_options, nullptr)) != -1) {
        switch (opt) {
            case 'd': dateStr = optarg; break;
            case 'a': lat = stod(optarg); break;
            case 'o': lon = stod(optarg); break;
            case 'p': phaseOnly = true; break;
            case 'A': all = true; break;
            case 's': saveLocation = optarg; break;
            default:
                cerr << "Usage: lunar_calendar --date YYYY-MM-DD --lat LAT --lon LON --phase-only --all --save-location NAME\n";
                return 1;
        }
    }

    // Load config
    json config;
    ifstream f(CONFIG_FILE);
    if (f.is_open()) {
        f >> config;
    }

    if (!saveLocation.empty()) {
        config["saved_location"] = {{"name", saveLocation}, {"lat", lat}, {"lon", lon}};
        ofstream out(CONFIG_FILE);
        out << setw(2) << config << endl;
        cout << "✅ Location '" << saveLocation << "' saved.\n";
    }

    if (!config.is_null() && config.contains("saved_location")) {
        if (lat == DEFAULT_LAT && lon == DEFAULT_LON) {
            lat = config["saved_location"]["lat"];
            lon = config["saved_location"]["lon"];
        }
    }

    time_t now = time(nullptr);
    tm dt = *localtime(&now);
    if (!dateStr.empty()) {
        strptime(dateStr.c_str(), "%Y-%m-%d", &dt);
    }

    LunarResult result = moonPhase(dt);

    if (phaseOnly) {
        cout << result.phase << "\n";
        return 0;
    }

    char dateBuf[20];
    strftime(dateBuf, sizeof(dateBuf), "%Y-%m-%d %H:%M", &dt);

    cout << "\n🌙 Lunar Calendar\n";
    cout << "Date: " << dateBuf << "\n";
    cout << "Location: " << fixed << setprecision(2) << lat << "°, " << lon << "°\n";
    cout << "\n🌓 Phase: " << result.phase << " (" << result.illumination << "% illuminated)\n";
    cout << "📅 Moon age: " << result.age << " days\n";
    cout << "♑ Zodiac: " << result.zodiac << "\n";
    cout << "\nVisual:\n" << asciiPhase(result.illumination) << "\n";

    return 0;
}
