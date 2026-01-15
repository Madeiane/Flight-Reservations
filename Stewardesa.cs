namespace Flight_Reservations;

public class Stewardesa : Person
{
    public string LimbiVorbite { get; }

    public Stewardesa(string nume, int varsta, string limbiVorbite)
        : base(nume, varsta)
    {
        LimbiVorbite = limbiVorbite;
    }
}