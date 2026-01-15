namespace Flight_Reservations
{
    public class EconomyTicket : BaseTicket
    {
        public override decimal CalculateFinalPrice(decimal flightBasePrice)
        {
            // Economy costs exactly the base price of the flight
            return flightBasePrice;
        }
    }
}