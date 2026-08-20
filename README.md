🌙 Lunar Calendar — Multi‑Language Astronomical Moon Phase Tracker
8 languages, one precise lunar calculator – track moon phases, illumination, age, rise/set times, and zodiac signs using real astronomical formulas – right from your terminal.

✨ Features
🌓 Calculate moon phase – New Moon, Waxing Crescent, First Quarter, Waxing Gibbous, Full Moon, Waning Gibbous, Last Quarter, Waning Crescent

📊 Illumination percentage – exact fraction of the moon's visible disk

📅 Moon age – days since last New Moon (0–29.53)

🌍 Custom location – latitude/longitude for accurate rise/set times

📆 Any date – compute for today or any past/future date

🎨 ASCII phase visualization – visual representation of the moon

♑ Zodiac sign – moon's current zodiac constellation

📁 Save favorite locations – persistent storage of coordinates

🚀 Common Usage
All implementations follow the same CLI pattern:

bash
# Show today's moon phase
<command>

# Show moon phase for a specific date
<command> --date 2026-08-20

# Show with custom location (for rise/set times)
<command> --lat 48.8584 --lon 2.2945

# Show just the phase name
<command> --phase-only

# Show all details (default)
<command> --all

# Save current location for future use
<command> --save-location "Paris" --lat 48.8584 --lon 2.2945
Arguments:

--date YYYY-MM-DD – date to calculate (default: today)

--lat <degrees> – latitude (positive North)

--lon <degrees> – longitude (positive East)

--phase-only – output only the phase name

--all – show full details (default)

--save-location <name> – save location for later

📸 Example Output
text
🌙 Lunar Calendar
Date: 2026-08-20 14:30 UTC
Location: Paris (48.86°N, 2.29°E)

🌓 Phase: Waxing Gibbous (85.2% illuminated)
📅 Moon age: 10.8 days
🌅 Moonrise: 16:45
🌇 Moonset: 02:30
♑ Zodiac: Sagittarius

Visual:
    ████████
  ████████████
 ██████████████
 ████████░░░░░░
 ████████░░░░░░
  ████████████
    ████████
📁 Repository Structure
text
.
├── README.md
├── python/
│   └── lunar_calendar.py
├── go/
│   └── lunar_calendar.go
├── javascript/
│   └── lunar_calendar.js
├── ruby/
│   └── lunar_calendar.rb
├── php/
│   └── lunar_calendar.php
├── java/
│   └── LunarCalendar.java
├── csharp/
│   └── LunarCalendar.cs
└── cpp/
    └── lunar_calendar.cpp
