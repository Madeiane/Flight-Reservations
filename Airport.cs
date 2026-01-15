namespace Flight_Reservations
{
    public class Airport : Location
    {
        public string Code { get; set; }
        public int CityId { get; set; }
        public List<Gate> Gates { get; set; } = new List<Gate>();

        public Airport(string name, string code, int cityId) : base(name)
        {
            Code = code;
            CityId = cityId;
        }        public void AddGate(Gate gate)
        {
            Gates.Add(gate);
        }
    }
}