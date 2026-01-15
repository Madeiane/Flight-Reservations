namespace Flight_Reservations
{
    public class BusinessTicket : BaseTicket
    {
        public override decimal CalculateFinalPrice(decimal flightBasePrice)
        {
            // Overriding the base calculation to apply a 50% business class surcharge
            return flightBasePrice * 1.5m;
        }

        public override void PrintTicketDetails()
        {
            base.PrintTicketDetails();
            Console.WriteLine("Priority boarding and extra luggage included.");
        }
    }
}