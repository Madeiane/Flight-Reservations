namespace Flight_Reservations;

public abstract class Location
{
    public int Id { get; set; }
    public string Name { get; set; }

    protected Location(string name)
    {
        Name = name;
    }
}