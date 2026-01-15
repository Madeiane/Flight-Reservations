using System;
using System.Collections.Generic;
using System.Linq;

namespace Flight_Reservations
{
    public class Flight : IFlight
    {
        // Encapsulation: Private fields protect the internal state 
        // from being set to invalid values directly.
        private int _totalSeats;
        private decimal _basePrice;
        public string FlightNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string DepartureAirportId { get; set; }
        public string ArrivalAirportId { get; set; }
        public string AircraftId { get; set; }
        public FlightType Type { get; set; } = FlightType.Commercial;

        // List for tickets 
        public List<ITicket> BookedTickets { get; private set; } = new List<ITicket>();

        public int TotalSeats
        {
            get => _totalSeats;
            set
            {
                if (value < 0) throw new ArgumentException("Total seats can not be negative.");
                _totalSeats = value;
            }
        }

        public decimal BasePrice
        {
            get => _basePrice;
            set
            {
                if (value < 0) throw new ArgumentException("Price can not be negative.");
                _basePrice = value;
            }
        }

        // LINQ: Using the Count property to calculate availability dynamically.
        public int AvailableSeats => TotalSeats - BookedTickets.Count;

        public bool IsFullyBooked() => AvailableSeats <= 0;

        public void AddTicket(ITicket ticket)// Adds a ticket to the flight after verifying capacity.
        {
            if (IsFullyBooked())
                throw new InvalidOperationException("No more seats available on this flight.");

            BookedTickets.Add(ticket);
        }
    }
}