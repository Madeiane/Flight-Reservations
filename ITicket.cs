namespace Flight_Reservations;
using System;

//initial interface for a ticket
public interface ITicket
{
    // Generates a Globally Unique Identifier to ensure every ticket has a unique
    // reference number across the system.
    Guid TicketId { get; }
    string FlightNumber { get; set; }
    string PassengerId { get; set; }
    string SeatNumber { get; set; }
    
    decimal CalculateFinalPrice(decimal flightBasePrice);//calculate the price for Economy/Business ticket
    
    void PrintTicketDetails();
}