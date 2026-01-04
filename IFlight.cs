namespace Flight_Reservations;
using System;
using System.Collections.Generic;

//initial interface for a flight
public interface IFlight
{
    string FlightNumber { get; set; }
    DateTime DepartureTime { get; set; }
    DateTime ArrivalTime { get; set; }
    string DepartureAirportId { get; set; }
    string ArrivalAirportId { get; set; }
    string AircraftId { get; set; }
    int TotalSeats { get; }
    int AvailableSeats { get; }
    decimal BasePrice { get; set; }

    bool IsFullyBooked(); //checks if the flight is fully booked
}