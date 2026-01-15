# Flight-Reservations

A flight reservation system built with .NET 8 and SQLite.

## Prerequisites

- .NET 8 SDK

## Build and Run

To restore dependencies:
```bash
dotnet restore
```

To build the project:
```bash
dotnet build
```

To run the application:
```bash
dotnet run
```

## Project Structure

- **Aircraft Classes**: Aircraft.cs, Commercial_Aircraft.cs, Cargo_Aircraft.cs, Military_Aircraft.cs
- **Flight Management**: Flight.cs, FlightType.cs, IFlight.cs
- **Ticket System**: BaseTicket.cs, BusinessTicket.cs, EconomyTicket.cs, ITicket.cs, TicketingService.cs
- **Personnel**: Person.cs, Pilot.cs, Copilot.cs, Stewardesa.cs, Passenger.cs
- **Location**: Location.cs, City.cs, Airport.cs, Gate.cs
- **Database**: Data/DatabaseManager.cs, Data/Databases/flights.db
- **Utilities**: ConsoleWrapper.cs, IConsoleWrapper.cs, FileLogger.cs, ILogger.cs, JsonDataWrapper.cs
- **Main Program**: Program.cs

## Database

The application uses SQLite for data persistence. The database file is located at `Data/Databases/flights.db` and will be created automatically on first run with sample data.

