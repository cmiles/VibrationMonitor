## C# Vibration Monitor for the Raspberry Pi and SW-420 Vibration Sensor

This C# project runs on a Raspberry Pi, records vibration periods from the SW-420 and has a web api to retrieve the data.

The SW-420 Vibration Sensor is an inexpensive package (search SW-420 on Amazon - as of late 2024 prices are under $10 for a pack of 5) that can be powered from a Raspberry Pi (this has been tested on the Pi Zero 2 W, 3 A+ and 3 B+) and outputs its state on a GPIO pin. The sensor has a potentiometer to adjust the sensitivity of the sensor.

This program uses several settings to record 'Vibration Periods' to a SQLite database and serve that data via a web api:
 - Description: A short description to associate with the Vibration entries in the database.
 - GpioPin: The gpio pin the SW420 Sensor is connected to.
 - MinimumPeriod: The minimum duration in milliseconds to record as a Vibration Period.
 - PollingFrequency: The number of milliseconds between polling the sensor state.

This is NOT designed to record each individual vibration that the SW-420 records - and is also not designed to record single very short vibrations - rather it is designed to record something like a motor running where the SW-420 is may switch between vibrating/not-vibrating multiple times over the motor's run. Once the program receives a vibration (how often it checks is set with the Polling Frequency) it will consider the vibration ongoing as long as it receives another vibration within the Minimum Period (even if every check of the sensor does not register as vibrating).

Vibration Periods are written to the database and can be retrieved via the web api.

### Command Line Arguments

-d, --description         (Default: Vibration Detected) A short description to associate with the Vibration entries in the database.
-p, --gpiopin             (Default: 17) The gpio pin the SW420 Sensor is connected to.
-m, --minimumperiod       (Default: 2000) The minimum duration in milliseconds to record as a Vibration Entry.
-f, --pollingfrequency    (Default: 500) The number of milliseconds between polling the sensor state
--help                    Display this help screen.

### Pi Setup

There is a PublishAll.ps1 script in the root directory that will publish both the VibrationMonitor and VibrationMonitorApi projects for the Raspberry Pi to M:\VibrationMonitor using the FolderProfile.pubxml files in the VibrationMonitor\Properties\PublishProfiles and VibrationMonitorApi\Properties\PublishProfiles. You can either map a directory to M: or edit the script and the FolderProfile.pubxml files to point to another directory.

Setting up and securing a Raspberry Pi is beyond the scope of this readme - suggested below are some basic steps to get the program up and running on a Raspberry Pi.

 - Run updates
	```
	sudo apt update
	sudo apt upgrade
	```

 - Setup auto-updates: This is not a good idea for all situations, listed here because this program lends itself to being setup on a Pi that you may access except via the web api...
	```
	sudo apt-get install unattended-upgrades
	sudo dpkg-reconfigure --priority=low unattended-upgrades
	```

 - Run the Vibration Monitor and web API as a service: 
	- Edit the vibrationmonitor.service replacing [Your Directory Here] and optionally adding program arguments, copy it to /etc/systemd/system/, start and follow the service to check for any errors:
	```
	chmod +x ./VibrationMonitor/VibrationMonitor/VibrationMonitor
	nano VibrationMonitor/VibrationMonitor/vibrationmonitor.service
	sudo cp VibrationMonitor/VibrationMonitor/vibrationmonitor.service /etc/systemd/system/
	sudo systemctl daemon-reload
 	sudo systemctl enable vibrationmonitor --now
	journalctl -u vibrationmonitor -f
	```

	- Edit the vibrationmonitorapi.service replacing [Your Directory Here] and optionally specifying the port for the service (the default is 7171), copy it to /etc/systemd/system/, start and follow the service to check for any errors:
	```
	chmod +x ./VibrationMonitor/VibrationMonitorApi/VibrationMonitorApi
	nano VibrationMonitor/VibrationMonitorApi/vibrationmonitorapi.service
	sudo cp VibrationMonitor/VibrationMonitorApi/vibrationmonitorapi.service /etc/systemd/system/
	sudo systemctl daemon-reload
 	sudo systemctl enable vibrationmonitorapi --now
	journalctl -u vibrationmonitorapi -f
	```

### Web API

** This program assumes that the Web API will be used on an internal network and that there is ZERO need to secure Vibration API data!! **

The easiest way to get started with the api is to visit http://[Your Pi's IP Address/name]:[7171 or the port you specified]/ - this will show the Swagger UI for the API.

API Endpoints:
 - GET lastvibrationperiod - returns the last vibration period - nice for writing alerts against the API since an ongoing vibration will have a 'null' EndedOn allowing you to quickly check if the sensor is vibrating and calculate how long it has been vibrating for.
 - GET lastvibrationperiods?count={int} - to get the last {int} vibration periods.
 - GET vibrationperiodsbystarttime?startTime={datetime}&endTime={datetime} - vibration periods where StartedOn is between startTime and endTime.
 - GET lasterror - returns the last logged error
 - GET lasterrors?count={int} - returns the last {int} logged errors


### Background

Our house has a an alternative septic system - I had never even heard of an 'alternative' septic system until a few years ago!The system we have includes control panel with a timer for a pump in the holding tank. Overall this is a durable and elegant system - but it doesn't log information, and if you have a problem or a question (and aren't an expert on the system) this can be frustrating. I wrote this system to monitor the pump in our holding tank. There are two important reasons I used a vibration sensor: attaching the vibration sensor to the output pipe from the tank means the monitor is more likely to measure 'water flowing out of the tank' which is ultimately what matters (vs for example monitoring current to the motor, which would probably be cleaner, but really I only care about water leaving the tank...) and it allows monitoring without any changes to, or extra equipment in, the control panel - a critical detail for a system under warranty!

### Packages Used

Even this fairly simple program owes an incredible debt to the amazing open source projects available for .NET and all of the people who have contributed to them!
 - [dotnet/core](https://github.com/dotnet/core) - .NET and a number of packages from Microsoft make this project possible!
 - [dotnet/efcore: EF Core is a modern object-database mapper for .NET. It supports LINQ queries, change tracking, updates, and schema migrations.](https://github.com/dotnet/efcore)
 - [SQLite](https://www.sqlite.org/index.html) - An absolutely brilliant project - having a Public Domain option for such a high quality data store that can be used locally and cross platform is amazing! Public Domain.
 - [commandlineparser/commandline: The best C# command line parser that brings standardized \*nix getopt style, for .NET. Includes F# support](https://github.com/commandlineparser/commandline)
 - [devlooped/GitInfo: Git and SemVer Info from MSBuild, C# and VB](https://github.com/devlooped/GitInfo)
 - [thomasgalliker/ObjectDumper: ObjectDumper is a utility which aims to serialize C# objects to string for debugging and logging purposes.](https://github.com/thomasgalliker/ObjectDumper)
 - [NUnit.org](https://nunit.org/) - NUnit is a unit-testing framework for all .Net languages. Initially ported from JUnit, the current production release, version 3, has been completely rewritten with many new features and support for a wide range of .NET platforms.
 - [serilog/serilog: Simple .NET logging with fully-structured events](https://github.com/serilog/serilog). Easy full featured logging. Apache-2.0 License.
   - [RehanSaeed/Serilog.Exceptions: Log exception details and custom properties that are not output in Exception.ToString().](https://github.com/RehanSaeed/Serilog.Exceptions) MIT License.
   - [serilog/serilog-formatting-compact: Compact JSON event format for Serilog](https://github.com/serilog/serilog-formatting-compact). Apache-2.0 License.
   - [serilog/serilog-sinks-console: Write log events to System.Console as text or JSON, with ANSI theme support](https://github.com/serilog/serilog-sinks-console). Apache-2.0 License.
   - [serilog/serilog-extensions-hosting: Serilog logging for Microsoft.Extensions.Hosting](https://github.com/serilog/serilog-extensions-hosting)
   - [serilog/serilog-sinks-file: Write Serilog events to files in text and JSON formats, optionally rolling on time or size](https://github.com/serilog/serilog-sinks-file)
   - [Yu-Core/Serilog-Sinks-SQLite-Maui: A Serilog sink that writes to SQLite](https://github.com/Yu-Core/Serilog-Sinks-SQLite-Maui) - forked from [saleem-mirza/serilog-sinks-sqlite: A Serilog sink that writes to SQLite](https://github.com/saleem-mirza/serilog-sinks-sqlite) with the changes needed for running on a Pi.

**Tools:**
 - [Visual Studio IDE](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
 - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
 - [GitHub Copilot · Your AI pair programmer · GitHub](https://github.com/features/copilot)
 - [PowerShell](https://github.com/PowerShell/PowerShell)
 - [AutoHotkey](https://www.autohotkey.com/)
 - [Compact-Log-Format-Viewer: A cross platform tool to read & query JSON aka CLEF log files created by Serilog](https://github.com/warrenbuckley/Compact-Log-Format-Viewer)
 - [DB Browser for SQLite](https://sqlitebrowser.org/)
 - [Fork - a fast and friendly git client for Mac and Windows](https://git-fork.com/)
 - [grepWin: A powerful and fast search tool using regular expressions](https://github.com/stefankueng/grepWin)
 - [Notepad++](https://notepad-plus-plus.org/)
