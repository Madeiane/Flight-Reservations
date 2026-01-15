namespace Flight_Reservations;

public class Cargo_Aircraft:Aircraft
{
    public int MaxCargoKg { get; }

    public Cargo_Aircraft(
        string model,
        string manufacturer,
        int maxRangeKm,
        int maxCargoKg)
        : base(model, manufacturer, maxRangeKm)
    {
        if (maxCargoKg <= 0)
            throw new ArgumentException("Cargo capacity must be greater than 0.");

        MaxCargoKg = maxCargoKg;
    }

    public override bool CanCarryPassengers() => false;
}