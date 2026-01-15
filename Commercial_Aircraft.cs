namespace Flight_Reservations;

public class Commercial_Aircraft:Aircraft
{
    public int PassengerCapacity { get; }

    public Commercial_Aircraft(
        string model,
        string manufacturer,
        int maxRangeKm,
        int passengerCapacity)
        : base(model, manufacturer, maxRangeKm)
    {
        if (passengerCapacity <= 0)
            throw new ArgumentException("Passenger capacity must be greater than 0.");

        PassengerCapacity = passengerCapacity;
    }

    public override bool CanCarryPassengers() => true;
}