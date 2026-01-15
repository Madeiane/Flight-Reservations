namespace Flight_Reservations;

public class  Passenger : Person
{
    public string PassportNumber { get; }

    public Passenger (string nume, int varsta, string passportNumber)
        : base(nume, varsta)
    {
        PassportNumber = passportNumber;
    }
}
