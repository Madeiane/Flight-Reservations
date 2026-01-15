namespace Flight_Reservations
{
    public class Gate : Location
    {
        public int AirportId { get; set; }

        public Gate(string name, int airportId) : base(name)
        {
            AirportId = airportId;
        }
    }
}