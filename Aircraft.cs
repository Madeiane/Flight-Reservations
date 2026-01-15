namespace Flight_Reservations;

public abstract class Aircraft
{
    public string Model { get; set; }
    public string Manufacturer { get; set; }
    public int MaxRangeKm { get; set; }

    protected Aircraft(string model, string manufacturer, int maxRangeKm)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty.");

        Model = model;
        Manufacturer = manufacturer;
        MaxRangeKm = maxRangeKm;
    }

    public abstract bool CanCarryPassengers();
    

}