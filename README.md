#README.md

#Tracker Telemetry system
- **Tracker.Publisher**
    Generates sensor readings on a timely interval, queues a set amount in memory and if not consumed in a certain time they are spilled into database for later retrieval.
- **Tracker.SensorReadingProcessor**  
    Retrieves sensor readings via REST calls from the **Tracker.Publisher** system and performs comples analysis.

## Overview
This document serves as an instruction manual to get the solutions running on your system.

---

## Prerequisites (atleast what i used)

- [Microsoft Visual Studio Community 2022]  -   Recommended
- [SqlServer local server]                  -   Required
- [Sql Server Management Studio]            -   Recommended
- [.NET 8 SDK]                              -   Required

---

## Database Setup

1. Create a new SQL Server Database on local Called **tracker_sensor_main**
2. Create a connection string in both projects to point to this database.
3. EF Core migrations are provided to create all relevant tables and stored procedures that will be used.
    - This can be done via terminal or by opening the projects in vidual studio and using
        -   Tools -> NuGet Package manager -> Package manager console
    and using the following commands within terminal/NuGet Package manager console:
    -> Package Manager console:    
        Update-Database
    -> Terminal:   
        dotnet ef database update

## Running locally
1. Preferrably run **Tracker.Publisher** first to start generating SensorReadings
2. Start **Tracker.SensorReadingProcessor** to start consuming readings

-- Console windows will appear for each logging the progress as they run
-- You can use a database tool to connect to the created database to view the records that are persisted as well.
