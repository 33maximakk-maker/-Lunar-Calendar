# lunar_calendar.py
import sys
import os
import json
import argparse
import math
from datetime import datetime, timedelta
import time

CONFIG_FILE = "lunar_config.json"
DEFAULT_LAT = 0.0
DEFAULT_LON = 0.0

class LunarCalculator:
    def __init__(self, lat=DEFAULT_LAT, lon=DEFAULT_LON):
        self.lat = lat
        self.lon = lon
        self.config = self.load_config()

    def load_config(self):
        if os.path.exists(CONFIG_FILE):
            with open(CONFIG_FILE, "r") as f:
                return json.load(f)
        return {"locations": {}}

    def save_config(self):
        with open(CONFIG_FILE, "w") as f:
            json.dump(self.config, f, indent=2)

    def save_location(self, name, lat, lon):
        self.config["locations"][name] = {"lat": lat, "lon": lon}
        self.save_config()

    def julian_day(self, dt):
        """Calculate Julian Day Number."""
        year = dt.year
        month = dt.month
        day = dt.day + dt.hour/24.0 + dt.minute/1440.0 + dt.second/86400.0
        if month <= 2:
            year -= 1
            month += 12
        A = int(year / 100)
        B = 2 - A + int(A / 4)
        return int(365.25 * (year + 4716)) + int(30.6001 * (month + 1)) + day + B - 1524.5

    def moon_position(self, jd):
        """Calculate moon's ecliptic longitude and latitude."""
        # Simplified lunar theory (Meeus)
        T = (jd - 2451545.0) / 36525.0
        L_prime = 218.3165 + 481267.8813 * T
        D = 297.8502 + 445267.1114 * T
        M = 357.5291 + 35999.0503 * T
        M_prime = 134.9634 + 477198.8676 * T
        F = 93.2720 + 483202.0175 * T

        L_prime = math.radians(L_prime % 360)
        D = math.radians(D % 360)
        M = math.radians(M % 360)
        M_prime = math.radians(M_prime % 360)
        F = math.radians(F % 360)

        lon = L_prime + math.radians(6.289 * math.sin(M_prime) +
                                      1.274 * math.sin(2*D - M_prime) +
                                      0.658 * math.sin(2*D) +
                                      0.214 * math.sin(2*M_prime) -
                                      0.186 * math.sin(M) -
                                      0.114 * math.sin(2*F))
        lat = math.radians(5.128 * math.sin(F) +
                           0.280 * math.sin(M_prime + F) +
                           0.278 * math.sin(M_prime - F) +
                           0.173 * math.sin(2*D - F))
        return lon, lat

    def sun_position(self, jd):
        """Calculate sun's ecliptic longitude."""
        T = (jd - 2451545.0) / 36525.0
        M = 357.5291 + 35999.0503 * T
        M = math.radians(M % 360)
        C = (1.9146 * math.sin(M) + 0.0200 * math.sin(2*M) + 0.0003 * math.sin(3*M))
        lon = math.radians((280.4665 + 36000.7698 * T + C) % 360)
        return lon

    def moon_phase(self, dt):
        """Calculate moon phase, illumination, age."""
        jd = self.julian_day(dt)
        lon_moon, lat_moon = self.moon_position(jd)
        lon_sun = self.sun_position(jd)
        # Elongation
        elong = lon_moon - lon_sun
        elong = math.atan2(math.sin(elong), math.cos(elong))
        # Phase angle
        phase_angle = math.atan2(math.sin(elong), math.cos(elong))
        # Illumination fraction (0 to 1)
        illumination = (1 + math.cos(phase_angle)) / 2
        # Moon age (days since New Moon)
        age = (jd - 2451550.1) / 29.53058867
        age = age % 29.53058867
        # Phase name
        if age < 1.0:
            phase = "New Moon"
        elif age < 7.38:
            phase = "Waxing Crescent"
        elif age < 8.38:
            phase = "First Quarter"
        elif age < 14.77:
            phase = "Waxing Gibbous"
        elif age < 15.77:
            phase = "Full Moon"
        elif age < 22.15:
            phase = "Waning Gibbous"
        elif age < 23.15:
            phase = "Last Quarter"
        else:
            phase = "Waning Crescent"
        return {
            "phase": phase,
            "illumination": illumination * 100,
            "age": age,
            "elongation": math.degrees(phase_angle)
        }

    def moon_rise_set(self, dt):
        """Approximate moon rise/set times (simplified)."""
        # Placeholder for simplicity; real calculation requires more parameters
        # Returns approximate times relative to noon
        jd = self.julian_day(dt)
        lon_moon, _ = self.moon_position(jd)
        # Simplified: moon rises ~6 hours after noon, sets ~18 hours after noon
        # This is a placeholder; real implementations use horizon calculations
        noon = dt.replace(hour=12, minute=0, second=0, microsecond=0)
        return {
            "rise": noon + timedelta(hours=6),
            "set": noon + timedelta(hours=18)
        }

    def zodiac_sign(self, lon):
        """Convert ecliptic longitude to zodiac sign."""
        signs = [
            "Aries", "Taurus", "Gemini", "Cancer",
            "Leo", "Virgo", "Libra", "Scorpio",
            "Sagittarius", "Capricorn", "Aquarius", "Pisces"
        ]
        lon_deg = math.degrees(lon) % 360
        idx = int(lon_deg / 30)
        return signs[idx]

    def ascii_phase(self, illumination):
        """Draw ASCII moon phase."""
        if illumination < 1:
            return "🌑 (New Moon)"
        if illumination < 20:
            return "🌒 (Waxing Crescent)"
        if illumination < 40:
            return "🌓 (First Quarter)"
        if illumination < 60:
            return "🌔 (Waxing Gibbous)"
        if illumination < 80:
            return "🌕 (Full Moon)"
        if illumination < 90:
            return "🌖 (Waning Gibbous)"
        if illumination < 98:
            return "🌗 (Last Quarter)"
        return "🌘 (Waning Crescent)"

def main():
    parser = argparse.ArgumentParser(description="Lunar Calendar")
    parser.add_argument("--date", help="YYYY-MM-DD")
    parser.add_argument("--lat", type=float, help="Latitude (positive North)")
    parser.add_argument("--lon", type=float, help="Longitude (positive East)")
    parser.add_argument("--phase-only", action="store_true", help="Output only phase name")
    parser.add_argument("--all", action="store_true", help="Show all details")
    parser.add_argument("--save-location", help="Save current location")
    args = parser.parse_args()

    # Load config for saved location
    config_file = "lunar_config.json"
    config = {}
    if os.path.exists(config_file):
        with open(config_file, "r") as f:
            config = json.load(f)

    lat = args.lat if args.lat is not None else DEFAULT_LAT
    lon = args.lon if args.lon is not None else DEFAULT_LON

    if args.save_location:
        config["saved_location"] = {"name": args.save_location, "lat": lat, "lon": lon}
        with open(config_file, "w") as f:
            json.dump(config, f, indent=2)
        print(f"✅ Location '{args.save_location}' saved.")

    dt = datetime.now()
    if args.date:
        dt = datetime.strptime(args.date, "%Y-%m-%d")

    calc = LunarCalculator(lat, lon)
    result = calc.moon_phase(dt)

    if args.phase_only:
        print(result["phase"])
        return

    print(f"\n🌙 Lunar Calendar")
    print(f"Date: {dt.strftime('%Y-%m-%d %H:%M')}")
    print(f"Location: {lat:.2f}°, {lon:.2f}°")

    print(f"\n🌓 Phase: {result['phase']} ({result['illumination']:.1f}% illuminated)")
    print(f"📅 Moon age: {result['age']:.1f} days")

    # Zodiac sign
    jd = calc.julian_day(dt)
    lon_moon, _ = calc.moon_position(jd)
    zodiac = calc.zodiac_sign(lon_moon)
    print(f"♑ Zodiac: {zodiac}")

    # ASCII phase
    print(f"\nVisual:")
    print(calc.ascii_phase(result['illumination']))

if __name__ == "__main__":
    main()
