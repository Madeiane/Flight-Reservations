
namespace Flight_Reservations
{
    public abstract class BaseTicket : ITicket
    {
        public Guid TicketId { get; } = Guid.NewGuid();
        public string FlightNumber { get; set; }
        public string PassengerId { get; set; }
        public string SeatNumber { get; set; }

        // This function calculates the price based on the ticket type
        public abstract decimal CalculateFinalPrice(decimal flightBasePrice);
        
        public virtual void PrintTicketDetails()//create a function that can also be used for business ticket
        {
            Console.WriteLine($"[Ticket {TicketId}] Flight: {FlightNumber}, Seat: {SeatNumber}");
        }
    }
}