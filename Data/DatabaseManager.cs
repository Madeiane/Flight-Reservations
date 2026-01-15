using Microsoft.Data.Sqlite;
using Flight_Reservations;
using System;
using System.Collections.Generic;
using System.IO;

namespace Flight_Reservations.Data
{
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private readonly IConsoleWrapper _console;
        private readonly ILogger _logger;

        public DatabaseManager(string connectionString, IConsoleWrapper console, ILogger logger = null)
        {
            _connectionString = connectionString;
            _console = console;
            _logger = logger;
        }

        public DatabaseManager(ILogger logger = null) : this(
            GetDefaultConnectionString(),
            new ConsoleWrapper(),
            logger)
        {
        }

        private static string GetDefaultConnectionString()
        {
            // Get the directory where the application is running
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Navigate to the project root (during development, bin folder is nested)
            string projectRoot = baseDir;
            while (!string.IsNullOrEmpty(projectRoot) && !File.Exists(Path.Combine(projectRoot, "Flight_Reservations.csproj")))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName;
            }
            
            // If we can't find the project root, use current directory
            if (string.IsNullOrEmpty(projectRoot))
            {
                projectRoot = Directory.GetCurrentDirectory();
            }
            
            // Create OS-agnostic path to database
            string dataDir = Path.Combine(projectRoot, "Data", "Databases");
            string dbPath = Path.Combine(dataDir, "flights.db");
            
            // Ensure the directory exists
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
                Console.WriteLine($"Created database directory: {dataDir}");
            }
            
            Console.WriteLine($"Database path: {dbPath}");
            
            return $"Data Source={dbPath}";
        }

        public void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    -- TABELE LOCAȚII
                    CREATE TABLE IF NOT EXISTS Cities (
                        CityId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Country TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Airports (
                        AirportId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Code TEXT NOT NULL UNIQUE,
                        CityId INTEGER NOT NULL,
                        FOREIGN KEY (CityId) REFERENCES Cities(CityId) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Gates (
                        GateId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        AirportId INTEGER NOT NULL,
                        FOREIGN KEY (AirportId) REFERENCES Airports(AirportId) ON DELETE CASCADE,
                        UNIQUE(Name, AirportId)
                    );

                    -- TABEL ZBORURI
                    CREATE TABLE IF NOT EXISTS Flights (
                        FlightId INTEGER PRIMARY KEY AUTOINCREMENT,
                        FlightNumber TEXT NOT NULL UNIQUE,
                        DepartureAirportId TEXT NOT NULL,
                        ArrivalAirportId TEXT NOT NULL,
                        DepartureTime TEXT NOT NULL,
                        BasePrice DECIMAL(10,2) NOT NULL,
                        TotalSeats INTEGER NOT NULL,
                        AvailableSeats INTEGER NOT NULL,
                        PilotId INTEGER,
                        CopilotId INTEGER,
                        FOREIGN KEY (PilotId) REFERENCES Staff(StaffId),
                        FOREIGN KEY (CopilotId) REFERENCES Staff(StaffId)
                    );

                    -- TABEL PERSONAL
                    CREATE TABLE IF NOT EXISTS Staff (
                        StaffId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Age INTEGER NOT NULL,
                        Role TEXT NOT NULL CHECK(Role IN ('Pilot', 'Copilot', 'Stewardess')),
                        FlightHours INTEGER DEFAULT 0,
                        HasAdvancedCert BOOLEAN DEFAULT 0,
                        Languages TEXT
                    );

                    -- TABEL PASAGERI
                    CREATE TABLE IF NOT EXISTS Passengers (
                        PassengerId INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullName TEXT NOT NULL,
                        PassportNumber TEXT
                    );

                    -- TABEL REZERVĂRI (legătură între zboruri și pasageri)
                    CREATE TABLE IF NOT EXISTS Bookings (
                        BookingId INTEGER PRIMARY KEY AUTOINCREMENT,
                        FlightNumber TEXT NOT NULL,
                        PassengerId INTEGER NOT NULL,
                        SeatNumber TEXT NOT NULL,
                        TicketClass TEXT NOT NULL,
                        FinalPrice DECIMAL(10,2) NOT NULL,
                        BookingDate TEXT NOT NULL,
                        FOREIGN KEY (FlightNumber) REFERENCES Flights(FlightNumber),
                        FOREIGN KEY (PassengerId) REFERENCES Passengers(PassengerId)
                    );

                    -- TABEL ASIGNARE STEWARDESE
                    CREATE TABLE IF NOT EXISTS FlightCrew (
                        CrewId INTEGER PRIMARY KEY AUTOINCREMENT,
                        FlightNumber TEXT NOT NULL,
                        StaffId INTEGER NOT NULL,
                        FOREIGN KEY (FlightNumber) REFERENCES Flights(FlightNumber),
                        FOREIGN KEY (StaffId) REFERENCES Staff(StaffId)
                    );

                    -- Indexuri pentru performanță
                    CREATE INDEX IF NOT EXISTS idx_airports_code ON Airports(Code);
                    CREATE INDEX IF NOT EXISTS idx_airports_cityid ON Airports(CityId);
                    CREATE INDEX IF NOT EXISTS idx_gates_airportid ON Gates(AirportId);
                    CREATE INDEX IF NOT EXISTS idx_flights_route ON Flights(DepartureAirportId, ArrivalAirportId);
                    CREATE INDEX IF NOT EXISTS idx_bookings_flight ON Bookings(FlightNumber);
                ";
                cmd.ExecuteNonQuery();


                using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Flights') WHERE name='AvailableSeats'";
                    var colExists = (long)checkCmd.ExecuteScalar() > 0;

                    if (!colExists)
                    {
                        _console.WriteWarning("Detected missing column 'AvailableSeats'. Performing migration...");
                        using (var alterCmd = connection.CreateCommand())
                        {
                            alterCmd.CommandText = "ALTER TABLE Flights ADD COLUMN AvailableSeats INTEGER NOT NULL DEFAULT 0";
                            alterCmd.ExecuteNonQuery();
                        }

                        using (var updateCmd = connection.CreateCommand())
                        {
                            updateCmd.CommandText = "UPDATE Flights SET AvailableSeats = TotalSeats";
                            updateCmd.ExecuteNonQuery();
                        }
                        _console.WriteSuccess("Migration: Added 'AvailableSeats' column and updated existing records.");
                    }
                }

                using (var checkTypeCmd = connection.CreateCommand())
                {
                    checkTypeCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Flights') WHERE name='Type'";
                    var typeColExists = (long)checkTypeCmd.ExecuteScalar() > 0;

                    if (!typeColExists)
                    {
                        _console.WriteWarning("Detected missing column 'Type'. Performing migration...");
                        using (var alterTypeCmd = connection.CreateCommand())
                        {
                            alterTypeCmd.CommandText = "ALTER TABLE Flights ADD COLUMN Type INTEGER NOT NULL DEFAULT 0";
                            alterTypeCmd.ExecuteNonQuery();
                        }
                        _console.WriteSuccess("Migration: Added 'Type' column defaulted to Commercial (0).");
                        _logger?.LogInfo("Database migrated: Added 'Type' column to Flights.");
                    }
                }

                _console.WriteSuccess("Database initialized successfully!");
                _logger?.LogInfo("Database initialized successfully.");
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Database initialization failed: {ex.Message}");
                _logger?.LogError("Database initialization failed", ex);
                throw;
            }
        }


        public int AddCity(City city)
        {
            if (string.IsNullOrWhiteSpace(city.Name))
                throw new ArgumentException("City name cannot be empty");

            if (string.IsNullOrWhiteSpace(city.Country))
                throw new ArgumentException("Country cannot be empty");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Cities (Name, Country) 
                    VALUES (@name, @country); 
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@name", city.Name.Trim());
                cmd.Parameters.AddWithValue("@country", city.Country.Trim());

                var id = (long)cmd.ExecuteScalar();
                city.Id = (int)id;

                _console.WriteSuccess($"City '{city.Name}' added with ID {city.Id}");
                return city.Id;
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                _console.WriteWarning($"City '{city.Name}' already exists in database");
                return GetCityByName(city.Name)?.Id ?? -1;
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to add city: {ex.Message}");
                throw;
            }
        }

        public int AddAirport(Airport airport)
        {
            if (string.IsNullOrWhiteSpace(airport.Name))
                throw new ArgumentException("Airport name cannot be empty");

            if (string.IsNullOrWhiteSpace(airport.Code) || airport.Code.Length != 3)
                throw new ArgumentException("Airport code must be exactly 3 characters");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var checkCity = connection.CreateCommand();
                checkCity.CommandText = "SELECT COUNT(*) FROM Cities WHERE CityId = @cityId";
                checkCity.Parameters.AddWithValue("@cityId", airport.CityId);
                var cityExists = (long)checkCity.ExecuteScalar() > 0;

                if (!cityExists)
                    throw new InvalidOperationException($"City with ID {airport.CityId} does not exist");

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Airports (Name, Code, CityId) 
                    VALUES (@name, @code, @cityId); 
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@name", airport.Name.Trim());
                cmd.Parameters.AddWithValue("@code", airport.Code.ToUpper().Trim());
                cmd.Parameters.AddWithValue("@cityId", airport.CityId);

                var id = (long)cmd.ExecuteScalar();
                airport.Id = (int)id;
                airport.Code = airport.Code.ToUpper();

                _console.WriteSuccess($"Airport '{airport.Name}' ({airport.Code}) added with ID {airport.Id}");
                return airport.Id;
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                _console.WriteWarning($"Airport with code '{airport.Code}' already exists");
                return GetAirportByCode(airport.Code)?.Id ?? -1;
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to add airport: {ex.Message}");
                throw;
            }
        }

        public int AddGate(Gate gate)
        {
            if (string.IsNullOrWhiteSpace(gate.Name))
                throw new ArgumentException("Gate name cannot be empty");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var checkAirport = connection.CreateCommand();
                checkAirport.CommandText = "SELECT COUNT(*) FROM Airports WHERE AirportId = @airportId";
                checkAirport.Parameters.AddWithValue("@airportId", gate.AirportId);
                var airportExists = (long)checkAirport.ExecuteScalar() > 0;

                if (!airportExists)
                    throw new InvalidOperationException($"Airport with ID {gate.AirportId} does not exist");

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Gates (Name, AirportId) 
                    VALUES (@name, @airportId); 
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@name", gate.Name.Trim());
                cmd.Parameters.AddWithValue("@airportId", gate.AirportId);

                var id = (long)cmd.ExecuteScalar();
                gate.Id = (int)id;

                _console.WriteSuccess($"Gate '{gate.Name}' added with ID {gate.Id}");
                return gate.Id;
            }
            catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                _console.WriteWarning($"Gate '{gate.Name}' already exists at this airport");
                return -1;
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to add gate: {ex.Message}");
                throw;
            }
        }

        public List<Gate> GetAllGates()
        {
            var list = new List<Gate>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT g.GateId, g.Name, g.AirportId, a.Code FROM Gates g JOIN Airports a ON g.AirportId = a.AirportId ORDER BY a.Code, g.Name";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var g = new Gate(reader.GetString(1), reader.GetInt32(2)) { Id = reader.GetInt32(0) };
                    list.Add(g);
                }
            }
            catch { }
            return list;
        }

        public List<(int Id, string Name, string AirportCode)> GetAllGatesWithInfo()
        {
            var list = new List<(int, string, string)>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT g.GateId, g.Name, a.Code FROM Gates g JOIN Airports a ON g.AirportId = a.AirportId ORDER BY a.Code, g.Name";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
                }
            }
            catch (Exception ex) { _console.WriteError($"Error loading gates: {ex.Message}"); }
            return list;
        }

        public bool DeleteCity(int id)
        {
            var airports = GetAirportsByCityId(id);
            foreach (var airport in airports)
            {
                DeleteAirport(airport.Id);
            }
            return DeleteRecord("Cities", "CityId", id);
        }

        public bool DeleteAirport(int id)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Gates WHERE AirportId = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error clearing gates for airport: {ex.Message}");
            }

            return DeleteRecord("Airports", "AirportId", id);
        }

        public bool DeleteGate(int id)
        {
            return DeleteRecord("Gates", "GateId", id);
        }

        private bool DeleteRecord(string tableName, string idColumn, object idValue)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var pragma = connection.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM {tableName} WHERE {idColumn} = @id";
                cmd.Parameters.AddWithValue("@id", idValue);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    _console.WriteSuccess($"Deleted from {tableName} successfully.");
                    return true;
                }
                else
                {
                    _console.WriteWarning($"Record not found in {tableName}.");
                    return false;
                }
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Could not delete from {tableName}: {ex.Message}");
                return false;
            }
        }

        public List<City> GetAllCities()
        {
            var list = new List<City>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT CityId, Name, Country FROM Cities ORDER BY Name";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new City(reader.GetString(1), reader.GetString(2)) { Id = reader.GetInt32(0) });
                }
            }
            catch (Exception ex) { _console.WriteError($"Error loading cities: {ex.Message}"); }
            return list;
        }

        private List<Airport> GetAirportsByCityId(int cityId)
        {
            var list = new List<Airport>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT AirportId, Name, Code, CityId FROM Airports WHERE CityId = @cid";
                cmd.Parameters.AddWithValue("@cid", cityId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Airport(reader.GetString(1), reader.GetString(2), reader.GetInt32(3)) { Id = reader.GetInt32(0) });
                }
            }
            catch { }
            return list;
        }

        public City? GetCityByName(string name)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT CityId, Name, Country FROM Cities WHERE Name = @name";
                cmd.Parameters.AddWithValue("@name", name.Trim());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new City(reader.GetString(1), reader.GetString(2))
                    {
                        Id = reader.GetInt32(0)
                    };
                }
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to retrieve city: {ex.Message}");
            }
            return null;
        }

        public Airport? GetAirportByCode(string code)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT AirportId, Name, Code, CityId FROM Airports WHERE Code = @code";
                cmd.Parameters.AddWithValue("@code", code.ToUpper().Trim());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Airport(reader.GetString(1), reader.GetString(2), reader.GetInt32(3))
                    {
                        Id = reader.GetInt32(0)
                    };
                }
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to retrieve airport: {ex.Message}");
            }
            return null;
        }

        public List<Airport> GetAllAirports()
        {
            var airports = new List<Airport>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT AirportId, Name, Code, CityId FROM Airports ORDER BY Code";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    airports.Add(new Airport(reader.GetString(1), reader.GetString(2), reader.GetInt32(3))
                    {
                        Id = reader.GetInt32(0)
                    });
                }
            }
            catch (SqliteException ex)
            {
                _console.WriteError($"Failed to retrieve airports: {ex.Message}");
            }
            return airports;
        }


        public bool AddFlight(Flight flight, int? pilotId = null, int? copilotId = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Flights (FlightNumber, DepartureAirportId, ArrivalAirportId, 
                                        DepartureTime, BasePrice, TotalSeats, AvailableSeats, PilotId, CopilotId, Type)
                    VALUES (@fNum, @dep, @arr, @time, @price, @seats, @available, @pilot, @copilot, @type)";

                cmd.Parameters.AddWithValue("@fNum", flight.FlightNumber);
                cmd.Parameters.AddWithValue("@dep", flight.DepartureAirportId);
                cmd.Parameters.AddWithValue("@arr", flight.ArrivalAirportId);
                cmd.Parameters.AddWithValue("@time", flight.DepartureTime.ToString("o"));
                cmd.Parameters.AddWithValue("@price", flight.BasePrice);
                cmd.Parameters.AddWithValue("@seats", flight.TotalSeats);
                cmd.Parameters.AddWithValue("@available", flight.TotalSeats);
                cmd.Parameters.AddWithValue("@pilot", pilotId.HasValue ? pilotId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@copilot", copilotId.HasValue ? copilotId.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@type", (int)flight.Type);

                cmd.ExecuteNonQuery();
                _console.WriteSuccess($"Flight {flight.FlightNumber} added to database.");
                return true;
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error adding flight: {ex.Message}");
                return false;
            }
        }

        public List<Flight> GetFlights()
        {
            var list = new List<Flight>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT FlightNumber, DepartureAirportId, ArrivalAirportId, DepartureTime, BasePrice, TotalSeats, AvailableSeats, Type FROM Flights";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var flight = new Flight
                    {
                        FlightNumber = reader.GetString(0),
                        DepartureAirportId = reader.GetString(1),
                        ArrivalAirportId = reader.GetString(2),
                        DepartureTime = DateTime.Parse(reader.GetString(3)),
                        BasePrice = reader.GetDecimal(4),
                        TotalSeats = reader.GetInt32(5),
                        Type = (FlightType)reader.GetInt32(7)
                    };

                    int availableSeats = reader.GetInt32(6);
                    int bookedSeats = flight.TotalSeats - availableSeats;

                    for (int i = 0; i < bookedSeats; i++)
                    {
                        flight.BookedTickets.Add(new EconomyTicket());
                    }

                    list.Add(flight);
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error loading flights: {ex.Message}");
            }
            return list;
        }

        public List<Flight> GetFlightsByRoute(string departureCode, string arrivalCode)
        {
            var list = new List<Flight>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT FlightNumber, DepartureAirportId, ArrivalAirportId, DepartureTime, BasePrice, TotalSeats, AvailableSeats, Type
                    FROM Flights 
                    WHERE DepartureAirportId = @dep AND ArrivalAirportId = @arr
                    ORDER BY DepartureTime";

                cmd.Parameters.AddWithValue("@dep", departureCode.ToUpper().Trim());
                cmd.Parameters.AddWithValue("@arr", arrivalCode.ToUpper().Trim());

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var flight = new Flight
                    {
                        FlightNumber = reader.GetString(0),
                        DepartureAirportId = reader.GetString(1),
                        ArrivalAirportId = reader.GetString(2),
                        DepartureTime = DateTime.Parse(reader.GetString(3)),
                        BasePrice = reader.GetDecimal(4),
                        TotalSeats = reader.GetInt32(5),
                        Type = (FlightType)reader.GetInt32(7)
                    };

                    int availableSeats = reader.GetInt32(6);
                    int bookedSeats = flight.TotalSeats - availableSeats;
                    for (int i = 0; i < bookedSeats; i++)
                    {
                        flight.BookedTickets.Add(new EconomyTicket());
                    }

                    list.Add(flight);
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error loading flights by route: {ex.Message}");
            }
            return list;
        }

        public bool UpdateFlightAvailableSeats(string flightNumber, int newAvailableSeats)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Flights SET AvailableSeats = @seats WHERE FlightNumber = @fNum";
                cmd.Parameters.AddWithValue("@seats", newAvailableSeats);
                cmd.Parameters.AddWithValue("@fNum", flightNumber);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error updating flight seats: {ex.Message}");
                return false;
            }
        }

        public bool DeleteFlight(string flightNumber)
        {
            return DeleteRecord("Flights", "FlightNumber", flightNumber);
        }


        public int AddStaff(string name, int age, string role, int flightHours = 0, bool hasAdvancedCert = false, string languages = "")
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Staff (Name, Age, Role, FlightHours, HasAdvancedCert, Languages)
                    VALUES (@name, @age, @role, @hours, @cert, @lang);
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@age", age);
                cmd.Parameters.AddWithValue("@role", role);
                cmd.Parameters.AddWithValue("@hours", flightHours);
                cmd.Parameters.AddWithValue("@cert", hasAdvancedCert ? 1 : 0);
                cmd.Parameters.AddWithValue("@lang", languages ?? "");

                var id = (long)cmd.ExecuteScalar();
                _console.WriteSuccess($"{role} '{name}' added with ID {id}");
                return (int)id;
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error adding staff: {ex.Message}");
                return -1;
            }
        }

        public List<(int Id, string Name, string Role, int FlightHours)> GetStaffByRole(string role)
        {
            var list = new List<(int, string, string, int)>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT StaffId, Name, Role, FlightHours FROM Staff WHERE Role = @role ORDER BY Name";
                cmd.Parameters.AddWithValue("@role", role);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error loading staff: {ex.Message}");
            }
            return list;
        }

        public List<(int Id, string Name, string Role, int FlightHours)> GetAllStaff()
        {
            var list = new List<(int, string, string, int)>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT StaffId, Name, Role, FlightHours FROM Staff ORDER BY Role, Name";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error loading staff: {ex.Message}");
            }
            return list;
        }

        public bool DeleteStaff(int id)
        {
            return DeleteRecord("Staff", "StaffId", id);
        }


        public int AddPassenger(string name, string passportNumber = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Passengers (FullName, PassportNumber) 
                    VALUES (@name, @passport);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@passport", passportNumber ?? (object)DBNull.Value);

                var id = (long)cmd.ExecuteScalar();
                _console.WriteSuccess($"Passenger {name} added with ID {id}");
                return (int)id;
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error adding passenger: {ex.Message}");
                return -1;
            }
        }

        public bool DeletePassenger(int id)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Bookings WHERE PassengerId = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error clearing bookings for passenger: {ex.Message}");
            }

            return DeleteRecord("Passengers", "PassengerId", id);
        }

        public void AddBooking(string flightNumber, int passengerId, string seatNumber, string ticketClass, decimal finalPrice)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Bookings (FlightNumber, PassengerId, SeatNumber, TicketClass, FinalPrice, BookingDate)
                    VALUES (@fNum, @pId, @seat, @class, @price, @date)";

                cmd.Parameters.AddWithValue("@fNum", flightNumber);
                cmd.Parameters.AddWithValue("@pId", passengerId);
                cmd.Parameters.AddWithValue("@seat", seatNumber);
                cmd.Parameters.AddWithValue("@class", ticketClass);
                cmd.Parameters.AddWithValue("@price", finalPrice);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("o"));

                cmd.ExecuteNonQuery();
                _console.WriteSuccess("Booking saved to database.");
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error saving booking: {ex.Message}");
            }
        }

        public List<(int Id, string Name, string Passport)> GetAllPassengers()
        {
            var list = new List<(int, string, string)>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT PassengerId, FullName, PassportNumber FROM Passengers ORDER BY FullName";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string passport = reader.IsDBNull(2) ? "-" : reader.GetString(2);
                    list.Add((reader.GetInt32(0), reader.GetString(1), passport));
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error list passengers: {ex.Message}");
            }
            return list;
        }

        public void ListBookings()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT b.BookingId, b.FlightNumber, p.FullName, b.SeatNumber, b.TicketClass, b.FinalPrice, b.BookingDate
                    FROM Bookings b
                    JOIN Passengers p ON b.PassengerId = p.PassengerId
                    ORDER BY b.BookingDate DESC";

                using var reader = cmd.ExecuteReader();
                _console.WriteLine("\n--- ALL BOOKINGS ---");
                while (reader.Read())
                {
                    _console.WriteLine($"[{reader.GetInt32(0)}] Flight {reader.GetString(1)} | {reader.GetString(2)} | Seat {reader.GetString(3)} | {reader.GetString(4)} | {reader.GetDecimal(5)} EUR | {DateTime.Parse(reader.GetString(6)):dd/MM/yyyy HH:mm}");
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error listing bookings: {ex.Message}");
            }
        }


        public void AddFlightCrew(string flightNumber, List<int> staffIds)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var staffId in staffIds)
                    {
                        var cmd = connection.CreateCommand();
                        cmd.Transaction = transaction;
                        cmd.CommandText = "INSERT INTO FlightCrew (FlightNumber, StaffId) VALUES (@fNum, @sId)";
                        cmd.Parameters.AddWithValue("@fNum", flightNumber);
                        cmd.Parameters.AddWithValue("@sId", staffId);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    _console.WriteSuccess($"Added {staffIds.Count} crew members to flight {flightNumber}.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Error adding flight crew: {ex.Message}");
            }
        }
    }

}
