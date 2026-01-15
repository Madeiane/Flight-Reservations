using Flight_Reservations.Data;
using System.Collections.Generic;
using System.Linq;

namespace Flight_Reservations
{
    class Program
    {
        static DatabaseManager db;
        static IConsoleWrapper console;
        static TicketingService ticketingService;
        static ILogger logger;

        static void Main(string[] args)
        {
            console = new ConsoleWrapper();
            logger = new FileLogger();
            db = new DatabaseManager(logger);
            ticketingService = new TicketingService();

            logger.LogInfo("Application Started");
            console.WriteLine("Initializing System...");
            db.InitializeDatabase();

            SeedDataIfEmpty();

            bool running = true;
            while (running)
            {
                console.Clear();
                ShowHeader();
                console.WriteLine("1. BOOK A TICKET (Customer)");
                console.WriteLine("2. ADMINISTRATIVE PANEL");
                console.WriteLine("3. VIEW ALL FLIGHTS");
                console.WriteLine("4. VIEW ALL BOOKINGS");
                console.WriteLine("0. EXIT");
                console.WriteLine("---------------------------------------------");
                console.Write("Select option: ");

                string choice = console.ReadLine();

                switch (choice)
                {
                    case "1":
                        HandleBookingFlow();
                        break;
                    case "2":
                        HandleAdminMenu();
                        break;
                    case "3":
                        ListAllFlights();
                        PressKeyToContinue();
                        break;
                    case "4":
                        db.ListBookings();
                        PressKeyToContinue();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        console.WriteError("Invalid option!");
                        PressKeyToContinue();
                        break;
                }
            }
        }

        static void HandleBookingFlow()
        {
            console.Clear();
            console.WriteLine("=== NEW FLIGHT RESERVATION ===\n");

            // STEP 1: Select Departure Airport
            var airports = db.GetAllAirports();
            if (airports.Count < 2)
            {
                console.WriteWarning("Not enough airports in database. Contact administrator.");
                PressKeyToContinue();
                return;
            }

            console.WriteLine("--- STEP 1/5: Select Departure Airport ---");
            for (int i = 0; i < airports.Count; i++)
            {
                console.WriteLine($"{i + 1}. {airports[i].Code} - {airports[i].Name}");
            }

            console.Write("\nDeparture Airport: ");
            if (!int.TryParse(console.ReadLine(), out int depIndex) || depIndex < 1 || depIndex > airports.Count)
            {
                console.WriteError("Invalid selection.");
                PressKeyToContinue();
                return;
            }

            Airport departureAirport = airports[depIndex - 1];

          
            var destinationAirports = airports.Where(a => a.Code != departureAirport.Code).ToList();

            console.WriteLine("\n--- STEP 2/5: Select Arrival Airport ---");
            for (int i = 0; i < destinationAirports.Count; i++)
            {
                console.WriteLine($"{i + 1}. {destinationAirports[i].Code} - {destinationAirports[i].Name}");
            }

            console.Write("\nArrival Airport: ");
            if (!int.TryParse(console.ReadLine(), out int arrIndex) || arrIndex < 1 || arrIndex > destinationAirports.Count)
            {
                console.WriteError("Invalid selection.");
                PressKeyToContinue();
                return;
            }

            Airport arrivalAirport = destinationAirports[arrIndex - 1];

           
            console.WriteLine($"\n--- STEP 3/5: Available Flights {departureAirport.Code} → {arrivalAirport.Code} ---");

            var availableFlights = db.GetFlightsByRoute(departureAirport.Code, arrivalAirport.Code);

            if (availableFlights.Count == 0)
            {
                console.WriteWarning($"No flights available for this route.");
                console.WriteLine("Please contact administrator to add flights.");
                PressKeyToContinue();
                return;
            }

            for (int i = 0; i < availableFlights.Count; i++)
            {
                var f = availableFlights[i];
                console.WriteLine($"{i + 1}. Flight {f.FlightNumber} | {f.DepartureTime:dd/MM/yyyy HH:mm} | {f.BasePrice} EUR | Available Seats: {f.AvailableSeats}/{f.TotalSeats}");
            }

            console.Write("\nSelect Flight: ");
            if (!int.TryParse(console.ReadLine(), out int flightIndex) || flightIndex < 1 || flightIndex > availableFlights.Count)
            {
                console.WriteError("Invalid selection.");
                PressKeyToContinue();
                return;
            }

            Flight selectedFlight = availableFlights[flightIndex - 1];

            if (selectedFlight.IsFullyBooked())
            {
                console.WriteError("Sorry, this flight is fully booked!");
                PressKeyToContinue();
                return;
            }

           
            console.WriteLine("\n--- STEP 4/5: Passenger Information ---");
            console.Write("Full Name: ");
            string pName = console.ReadLine();
            console.Write("Passport Number : ");
            string pPassport = console.ReadLine();

            int passengerId = db.AddPassenger(pName, pPassport);
            if (passengerId == -1)
            {
                console.WriteError("Failed to register passenger.");
                PressKeyToContinue();
                return;
            }

           
            console.WriteLine("\n--- STEP 5/5: Select Ticket Class ---");
            console.WriteLine("1. Economy Class (Standard Price)");
            console.WriteLine("2. Business Class (+50% Price, Priority Boarding)");
            console.Write("Your choice: ");
            string classChoice = console.ReadLine();

            ITicket ticket;
            string ticketClassName;
            if (classChoice == "2")
            {
                ticket = new BusinessTicket();
                ticketClassName = "Business";
            }
            else
            {
                ticket = new EconomyTicket();
                ticketClassName = "Economy";
            }

           
            ticket.FlightNumber = selectedFlight.FlightNumber;
            ticket.PassengerId = pName;
            ticket.SeatNumber = GenerateSeatNumber(selectedFlight);

           
            try
            {
                selectedFlight.AddTicket(ticket);

                db.UpdateFlightAvailableSeats(selectedFlight.FlightNumber, selectedFlight.AvailableSeats);
            }
            catch (Exception ex)
            {
                console.WriteError($"Booking failed: {ex.Message}");
                logger.LogError($"Booking failed due to exception", ex);
                PressKeyToContinue();
                return;
            }

           
            decimal finalPrice = ticket.CalculateFinalPrice(selectedFlight.BasePrice);
            db.AddBooking(selectedFlight.FlightNumber, passengerId, ticket.SeatNumber, ticketClassName, finalPrice);

           
            console.Clear();
            console.WriteSuccess("\n✓✓✓ BOOKING CONFIRMED ✓✓✓\n");
            logger.LogInfo($"Booking Confirmed. Flight: {selectedFlight.FlightNumber}, Passenger: {pName}, Seat: {ticket.SeatNumber}, Price: {finalPrice} EUR");
            console.WriteLine($"Route: {departureAirport.Code} ({departureAirport.Name}) → {arrivalAirport.Code} ({arrivalAirport.Name})");
            console.WriteLine($"Flight: {selectedFlight.FlightNumber}");
            console.WriteLine($"Departure: {selectedFlight.DepartureTime:dddd, dd MMMM yyyy 'at' HH:mm}");
            console.WriteLine($"Passenger: {pName}");
            console.WriteLine($"Seat: {ticket.SeatNumber}");
            console.WriteLine($"Class: {ticketClassName}");
            console.WriteLine($"Final Price: {finalPrice} EUR");
            console.WriteLine($"Remaining Seats: {selectedFlight.AvailableSeats}");

            console.WriteLine("\n--- Generating Boarding Pass ---");
            ticketingService.GeneratePrintableTicket(selectedFlight, ticket);

            PressKeyToContinue();
        }

        static string GenerateSeatNumber(Flight flight)
        {
            int bookedCount = flight.BookedTickets.Count + 1;
            int row = (bookedCount / 6) + 1;
            char seat = (char)('A' + (bookedCount % 6));
            return $"{row}{seat}";
        }

       
        static void HandleAdminMenu()
        {
            bool inAdmin = true;
            while (inAdmin)
            {
                console.Clear();
                console.WriteLine("╔════════════════════════════════════╗");
                console.WriteLine("║     ADMINISTRATIVE PANEL           ║");
                console.WriteLine("╚════════════════════════════════════╝");
                console.WriteLine("1. Manage Locations (Cities/Airports/Gates)");
                console.WriteLine("2. Manage Flights");
                console.WriteLine("3. Manage Staff (Pilots/Crew)");
                console.WriteLine("4. Manage Passengers");
                console.WriteLine("5. View All Bookings");
                console.WriteLine("0. Back to Main Menu");
                console.WriteLine("─────────────────────────────────────");
                console.Write("Select: ");

                switch (console.ReadLine())
                {
                    case "1":
                        HandleLocationMenu();
                        break;
                    case "2":
                        HandleFlightManagement();
                        break;
                    case "3":
                        HandleStaffManagement();
                        break;
                    case "4":
                        HandlePassengerManagement();
                        break;
                    case "5":
                        db.ListBookings();
                        PressKeyToContinue();
                        break;
                    case "0":
                        inAdmin = false;
                        break;
                    default:
                        console.WriteError("Invalid option!");
                        PressKeyToContinue();
                        break;
                }
            }
        }

        static void HandlePassengerManagement()
        {
            console.Clear();
            console.WriteLine("=== PASSENGER MANAGEMENT ===\n");
            console.WriteLine("1. View All Passengers");
            console.WriteLine("2. Delete Passenger");
            console.WriteLine("0. Back");
            console.Write("\nSelect: ");

            switch (console.ReadLine())
            {
                case "1":
                    var list = db.GetAllPassengers();
                    console.WriteLine("\n--- REGISTERED PASSENGERS ---");
                    foreach (var p in list) console.WriteLine($"[{p.Id}] {p.Name} | Passport: {p.Passport}");
                    PressKeyToContinue();
                    break;
                case "2":
                    DeletePassenger();
                    break;
            }
        }

        static void DeletePassenger()
        {
            var list = db.GetAllPassengers();
            console.WriteLine("\n--- Delete Passenger ---");
            if (list.Count == 0) { console.WriteWarning("No passengers found."); PressKeyToContinue(); return; }

            for (int i = 0; i < list.Count; i++)
            {
                console.WriteLine($"{i + 1}. {list[i].Name} (Passport: {list[i].Passport})");
            }

            console.Write("Select passenger to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= list.Count)
            {
                if (!db.DeletePassenger(list[idx - 1].Id)) console.WriteError("Failed to delete passenger.");
                else console.WriteSuccess("Passenger deleted.");
            }
            else
            {
                console.WriteError("Invalid selection.");
            }
            PressKeyToContinue();
        }

        static void HandleLocationMenu()
        {
            console.Clear();
            console.WriteLine("=== LOCATION MANAGEMENT ===\n");
            console.WriteLine("1. Add City & Airport");
            console.WriteLine("2. Add Gate to Airport");
            console.WriteLine("3. Delete City");
            console.WriteLine("4. Delete Airport");
            console.WriteLine("5. Delete Gate");
            console.WriteLine("0. Back");
            console.Write("\nSelect: ");

            switch (console.ReadLine())
            {
                case "1":
                    AddCityAndAirport();
                    break;
                case "2":
                    AddGateToAirport();
                    break;
                case "3":
                    DeleteCityUI();
                    break;
                case "4":
                    DeleteAirportUI();
                    break;
                case "5":
                    DeleteGateUI();
                    break;
            }
        }

        static void DeleteCityUI()
        {
            var list = db.GetAllCities();
            console.WriteLine("\n--- Delete City ---");
            if (list.Count == 0) { console.WriteWarning("No cities found."); PressKeyToContinue(); return; }

            for (int i = 0; i < list.Count; i++)
                console.WriteLine($"{i + 1}. {list[i].Name} ({list[i].Country})");

            console.Write("\nSelect city to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= list.Count)
            {
                if (db.DeleteCity(list[idx - 1].Id)) console.WriteSuccess("City deleted.");
            }
            else console.WriteError("Invalid selection.");
            PressKeyToContinue();
        }

        static void DeleteAirportUI()
        {
            var list = db.GetAllAirports();
            console.WriteLine("\n--- Delete Airport ---");
            if (list.Count == 0) { console.WriteWarning("No airports found."); PressKeyToContinue(); return; }

            for (int i = 0; i < list.Count; i++)
                console.WriteLine($"{i + 1}. {list[i].Code} - {list[i].Name}");

            console.Write("\nSelect airport to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= list.Count)
            {
                if (db.DeleteAirport(list[idx - 1].Id)) console.WriteSuccess("Airport deleted.");
            }
            else console.WriteError("Invalid selection.");
            PressKeyToContinue();
        }

        static void DeleteGateUI()
        {
            var list = db.GetAllGatesWithInfo();
            console.WriteLine("\n--- Delete Gate ---");
            if (list.Count == 0) { console.WriteWarning("No gates found."); PressKeyToContinue(); return; }

            for (int i = 0; i < list.Count; i++)
                console.WriteLine($"{i + 1}. {list[i].Name} ({list[i].AirportCode})");

            console.Write("\nSelect gate to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= list.Count)
            {
                if (db.DeleteGate(list[idx - 1].Id)) console.WriteSuccess("Gate deleted.");
            }
            else console.WriteError("Invalid selection.");
            PressKeyToContinue();
        }

        static void AddCityAndAirport()
        {
            console.WriteLine("\n--- Add New City & Airport ---");
            console.Write("City Name: ");
            string cName = console.ReadLine();
            console.Write("Country: ");
            string cCountry = console.ReadLine();

            var city = new City(cName, cCountry);
            int cityId = db.AddCity(city);

            console.Write("\nAirport Name: ");
            string aName = console.ReadLine();
            console.Write("Airport Code (3 letters, e.g., OTP): ");
            string aCode = console.ReadLine();

            if (aCode.Length == 3)
            {
                db.AddAirport(new Airport(aName, aCode, cityId));
            }
            else
            {
                console.WriteError("Airport code must be exactly 3 characters!");
            }

            PressKeyToContinue();
        }

        static void AddGateToAirport()
        {
            var airports = db.GetAllAirports();
            if (airports.Count == 0)
            {
                console.WriteWarning("No airports available. Add an airport first.");
                PressKeyToContinue();
                return;
            }

            console.WriteLine("\n--- Available Airports ---");
            for (int i = 0; i < airports.Count; i++)
            {
                console.WriteLine($"{i + 1}. {airports[i].Code} - {airports[i].Name}");
            }

            console.Write("\nSelect Airport: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= airports.Count)
            {
                console.Write("Gate Name (e.g., A1, B12): ");
                string gateName = console.ReadLine();
                db.AddGate(new Gate(gateName, airports[idx - 1].Id));
            }

            PressKeyToContinue();
        }

        static void HandleFlightManagement()
        {
            console.Clear();
            console.WriteLine("=== FLIGHT MANAGEMENT ===\n");
            console.WriteLine("1. Add New Flight");
            console.WriteLine("2. View All Flights");
            console.WriteLine("0. Back");
            console.Write("\nSelect: ");

            switch (console.ReadLine())
            {
                case "1":
                    AddNewFlight();
                    break;
                case "2":
                    ListAllFlights();
                    PressKeyToContinue();
                    break;
                case "3":
                    DeleteFlight();
                    break;
            }
        }

        static void DeleteFlight()
        {
            var flights = db.GetFlights(); 
            console.WriteLine("\n--- Delete/Cancel Flight ---");
            if (flights.Count == 0) { console.WriteWarning("No flights found."); PressKeyToContinue(); return; }

            for (int i = 0; i < flights.Count; i++)
            {
                var f = flights[i];
                console.WriteLine($"{i + 1}. Flight {f.FlightNumber} | {f.DepartureAirportId}->{f.ArrivalAirportId} | {f.DepartureTime:dd/MM HH:mm}");
            }

            console.Write("Select flight to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= flights.Count)
            {
                if (db.DeleteFlight(flights[idx - 1].FlightNumber))
                {
                    console.WriteSuccess("Flight deleted.");
                }
                else
                {
                    console.WriteError("Failed to delete flight.");
                }
            }
            else
            {
                console.WriteError("Invalid selection.");
            }
            PressKeyToContinue();
        }

        static void AddNewFlight()
        {
            console.WriteLine("\n--- Add New Flight ---");
            Flight f = new Flight();

            console.Write("Flight Number (e.g., 321): ");
            if (!decimal.TryParse(console.ReadLine(), out decimal fNum))
            {
                console.WriteError("Invalid flight number!");
                PressKeyToContinue();
                return;
            }
            f.FlightNumber = fNum;

            console.Write("Departure Airport Code (e.g., OTP): ");
            f.DepartureAirportId = console.ReadLine().ToUpper().Trim();

            console.Write("Arrival Airport Code (e.g., LHR): ");
            f.ArrivalAirportId = console.ReadLine().ToUpper().Trim();

            console.Write("Departure Date & Time (dd/MM/yyyy HH:mm): ");
            if (DateTime.TryParseExact(console.ReadLine(), "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime depTime))
            {
                f.DepartureTime = depTime;
            }
            else
            {
                f.DepartureTime = DateTime.Now.AddDays(1);
                console.WriteWarning("Invalid format. Using tomorrow as default.");
            }



            console.WriteLine("Select Flight Type:");
            console.WriteLine("1. Commercial (Passengers)");
            console.WriteLine("2. Cargo (Merchandise, No Passengers)");
            console.WriteLine("3. Military (No Passengers)");
            console.Write("Choice: ");
            string typeChoice = console.ReadLine();

            switch (typeChoice)
            {
                case "2": f.Type = FlightType.Cargo; break;
                case "3": f.Type = FlightType.Military; break;
                default: f.Type = FlightType.Commercial; break;
            }

            if (f.Type == FlightType.Military)
            {
                f.BasePrice = 0;
                console.WriteLine("Price set to 0 for Military flight.");
            }
            else
            {
                string pricePrompt = f.Type == FlightType.Cargo ? "Price per Kg (EUR): " : "Base Price (EUR): ";
                console.Write(pricePrompt);
                decimal.TryParse(console.ReadLine(), out decimal price);
                f.BasePrice = price > 0 ? price : 100;
            }

            if (f.Type == FlightType.Commercial)
            {
                console.Write("Total Seats: ");
                int.TryParse(console.ReadLine(), out int seats);
                f.TotalSeats = seats > 0 ? seats : 150;
            }
            else
            {
                f.TotalSeats = 0;
                console.WriteLine($"Total Seats set to 0 for {f.Type} flight.");
            }

            // Assign Pilot
            var pilots = db.GetStaffByRole("Pilot");
            int? pilotId = null;
            if (pilots.Count > 0)
            {
                console.WriteLine("\n--- Available Pilots ---");
                for (int i = 0; i < pilots.Count; i++)
                {
                    console.WriteLine($"{i + 1}. {pilots[i].Name} ({pilots[i].FlightHours} hours)");
                }
                console.Write("Select Pilot (or 0 to skip): ");
                if (int.TryParse(console.ReadLine(), out int pIdx) && pIdx > 0 && pIdx <= pilots.Count)
                {
                    pilotId = pilots[pIdx - 1].Id;
                }
            }

            // Assign Copilot
            var copilots = db.GetStaffByRole("Copilot");
            int? copilotId = null;
            if (copilots.Count > 0)
            {
                console.WriteLine("\n--- Available Copilots ---");
                for (int i = 0; i < copilots.Count; i++)
                {
                    console.WriteLine($"{i + 1}. {copilots[i].Name} ({copilots[i].FlightHours} hours)");
                }
                console.Write("Select Copilot (or 0 to skip): ");
                if (int.TryParse(console.ReadLine(), out int cIdx) && cIdx > 0 && cIdx <= copilots.Count)
                {
                    copilotId = copilots[cIdx - 1].Id;
                }
            }

            if (!db.AddFlight(f, pilotId, copilotId))
            {
                
                return;
            }

         
            if (f.Type == FlightType.Commercial)
            {
                var stewardesses = db.GetStaffByRole("Stewardess");
                List<int> selectedCrew = new List<int>();

                if (stewardesses.Count >= 2)
                {
                    console.WriteLine("\n--- Select 2 Flight Attendants ---");

                    while (selectedCrew.Count < 2)
                    {
                        console.WriteLine($"\nSelect Flight Attendant #{selectedCrew.Count + 1}:");
                        for (int i = 0; i < stewardesses.Count; i++)
                        {
                            if (!selectedCrew.Contains(stewardesses[i].Id))
                            {
                                console.WriteLine($"{i + 1}. {stewardesses[i].Name} ({stewardesses[i].FlightHours} hours)");
                            }
                        }

                        console.Write("Select (number): ");
                        if (int.TryParse(console.ReadLine(), out int sIdx) && sIdx > 0 && sIdx <= stewardesses.Count)
                        {
                            int selectedId = stewardesses[sIdx - 1].Id;
                            if (!selectedCrew.Contains(selectedId))
                            {
                                selectedCrew.Add(selectedId);
                            }
                            else
                            {
                                console.WriteError("Already selected!");
                            }
                        }
                        else
                        {
                            console.WriteError("Invalid selection.");
                        }
                    }

                    db.AddFlightCrew(f.FlightNumber, selectedCrew);
                }
                else
                {
                    console.WriteWarning("Not enough stewardesses in database to assign to this flight (need 2).");
                }
            }
            else
            {
                console.WriteLine($"\nSkipping flight attendants for {f.Type} flight.");
            }

            console.WriteSuccess("Flight added successfully!");
            logger.LogInfo($"Flight Added: {f.FlightNumber} ({f.Type})");
            PressKeyToContinue();
        }

        static void HandleStaffManagement()
        {
            console.Clear();
            console.WriteLine("=== STAFF MANAGEMENT ===\n");
            console.WriteLine("1. Add Pilot");
            console.WriteLine("2. Add Copilot");
            console.WriteLine("3. Add Stewardess");
            console.WriteLine("4. View All Staff");
            console.WriteLine("0. Back");
            console.Write("\nSelect: ");

            string choice = console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddStaffMember("Pilot");
                    break;
                case "2":
                    AddStaffMember("Copilot");
                    break;
                case "3":
                    AddStaffMember("Stewardess");
                    break;
                case "4":
                    var staffList = db.GetAllStaff();
                    console.WriteLine("\n--- ALL STAFF MEMBERS ---");
                    foreach (var s in staffList)
                    {
                        var extra = s.Role == "Pilot" || s.Role == "Copilot" ? $"Flight Hours: {s.FlightHours}" : "";
                        console.WriteLine($"[{s.Id}] {s.Name} - {s.Role} {extra}");
                    }
                    PressKeyToContinue();
                    break;
                case "5":
                    DeleteStaffMember();
                    break;
            }
        }

        static void DeleteStaffMember()
        {
            var list = db.GetAllStaff();
            console.WriteLine("\n--- Delete Staff Member ---");
            if (list.Count == 0) { console.WriteWarning("No staff found."); PressKeyToContinue(); return; }

            for (int i = 0; i < list.Count; i++)
                console.WriteLine($"{i + 1}. {list[i].Name} - {list[i].Role}");

            console.Write("Select staff to delete: ");
            if (int.TryParse(console.ReadLine(), out int idx) && idx > 0 && idx <= list.Count)
            {
                if (!db.DeleteStaff(list[idx - 1].Id)) console.WriteError("Failed to delete staff member.");
                else console.WriteSuccess("Staff member deleted.");
            }
            else
            {
                console.WriteError("Invalid selection.");
            }
            PressKeyToContinue();
        }

        static void AddStaffMember(string role)
        {
            console.WriteLine($"\n--- Add {role} ---");
            console.Write("Name: ");
            string name = console.ReadLine();

            console.Write("Age: ");
            if (!int.TryParse(console.ReadLine(), out int age) || age < 18)
            {
                console.WriteError("Invalid age! Must be at least 18.");
                PressKeyToContinue();
                return;
            }

            int flightHours = 0;
            bool hasAdvancedCert = false;
            string languages = "";

            if (role == "Pilot")
            {
                console.Write("Flight Hours: ");
                int.TryParse(console.ReadLine(), out flightHours);
                if (flightHours < 1000)
                {
                    console.WriteError("Pilot must have at least 1000 flight hours!");
                    PressKeyToContinue();
                    return;
                }
            }
            else if (role == "Copilot")
            {
                console.Write("Flight Hours: ");
                int.TryParse(console.ReadLine(), out flightHours);

                console.Write("Has Advanced Certification? (y/n): ");
                hasAdvancedCert = console.ReadLine().ToLower() == "y";

                if (!hasAdvancedCert)
                {
                    console.WriteError("Copilot must have advanced certification!");
                    PressKeyToContinue();
                    return;
                }
            }
            else if (role == "Stewardess")
            {
                console.Write("Languages Spoken (comma separated): ");
                languages = console.ReadLine();
            }

            db.AddStaff(name, age, role, flightHours, hasAdvancedCert, languages);
            PressKeyToContinue();
        }

        static void ListAllFlights()
        {
            var flights = db.GetFlights();
            console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
            console.WriteLine("║             ALL SCHEDULED FLIGHTS                      ║");
            console.WriteLine("╚════════════════════════════════════════════════════════╝");

            if (flights.Count == 0)
            {
                console.WriteWarning("No flights in database.");
                return;
            }

            foreach (var f in flights)
            {
                string priceDisplay = f.Type == FlightType.Military ? "N/A" :
                                      f.Type == FlightType.Cargo ? $"{f.BasePrice} EUR/kg" :
                                      $"{f.BasePrice} EUR";

                string seatsDisplay = f.Type == FlightType.Military ? "N/A" : $"{f.AvailableSeats}/{f.TotalSeats}";

                console.WriteLine($"[{f.FlightNumber}] {f.DepartureAirportId} → {f.ArrivalAirportId} | {f.DepartureTime:dd/MM/yyyy HH:mm} | Type: {f.Type} | Price: {priceDisplay} | Seats: {seatsDisplay}");
            }
        }

        static void SeedDataIfEmpty()
        {
            if (db.GetFlights().Count == 0)
            {
                // Cities & Airports
                int cId1 = db.AddCity(new City("Bucharest", "Romania"));
                db.AddAirport(new Airport("Henri Coanda International Airport", "OTP", cId1));

                int cId2 = db.AddCity(new City("London", "UK"));
                db.AddAirport(new Airport("Heathrow", "LHR", cId2));

                int cId3 = db.AddCity(new City("Paris", "France"));
                db.AddAirport(new Airport("Charles de Gaulle", "CDG", cId3));

                // Staff
                int pilot1 = db.AddStaff("Captain John Smith", 45, "Pilot", 5000);
                int copilot1 = db.AddStaff("Sarah Johnson", 32, "Copilot", 2000, true);
                db.AddStaff("Maria Garcia", 28, "Stewardess", 0, false, "English, Spanish, French");
                db.AddStaff("Emma Wilson", 26, "Stewardess", 0, false, "English, German");

                // Flights with crew
                db.AddFlight(new Flight
                {
                    FlightNumber = 321,
                    DepartureAirportId = "OTP",
                    ArrivalAirportId = "LHR",
                    DepartureTime = DateTime.Now.AddDays(2).AddHours(10),
                    BasePrice = 150m,
                    TotalSeats = 180
                }, pilot1, copilot1);

                db.AddFlight(new Flight
                {
                    FlightNumber = 322,
                    DepartureAirportId = "OTP",
                    ArrivalAirportId = "LHR",
                    DepartureTime = DateTime.Now.AddDays(2).AddHours(18),
                    BasePrice = 175m,
                    TotalSeats = 180
                }, pilot1, copilot1);

                db.AddFlight(new Flight
                {
                    FlightNumber = 401,
                    DepartureAirportId = "OTP",
                    ArrivalAirportId = "CDG",
                    DepartureTime = DateTime.Now.AddDays(3).AddHours(8),
                    BasePrice = 120m,
                    TotalSeats = 150
                }, pilot1, copilot1);

                db.AddFlight(new Flight
                {
                    FlightNumber = 501,
                    DepartureAirportId = "LHR",
                    ArrivalAirportId = "CDG",
                    DepartureTime = DateTime.Now.AddDays(3).AddHours(14),
                    BasePrice = 95m,
                    TotalSeats = 120
                }, pilot1, copilot1);

               
                var stewardessIds = new List<int> { db.GetStaffByRole("Stewardess").First(s => s.Name == "Maria Garcia").Id, db.GetStaffByRole("Stewardess").First(s => s.Name == "Emma Wilson").Id };
                db.AddFlightCrew(321, stewardessIds);
                db.AddFlightCrew(322, stewardessIds);
                db.AddFlightCrew(401, stewardessIds);
                db.AddFlightCrew(501, stewardessIds);

                console.WriteSuccess("✓ Sample data loaded successfully!");
            }
        }

        static void ShowHeader()
        {
            console.WriteLine("╔═══════════════════════════════════════════╗");
            console.WriteLine("║    UPT AIRWAYS MANAGEMENT SYSTEM          ║");
            console.WriteLine("║    Professional Flight Booking Platform   ║");
            console.WriteLine("╚═══════════════════════════════════════════╝\n");
        }

        static void PressKeyToContinue()
        {
            console.WriteLine("\nPress any key to continue...");
            console.ReadKey();
        }
    }
}