namespace Flight_Reservations;

public class Military_Aircraft:Aircraft
{
    public bool IsArmed { get; }

    public Military_Aircraft(
        string model,
        string manufacturer,
        int maxRangeKm,
        bool isArmed)
        : base(model, manufacturer, maxRangeKm)
    {
        IsArmed = isArmed;
    }

    public override bool CanCarryPassengers() => false;
}