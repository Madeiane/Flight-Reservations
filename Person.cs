namespace Flight_Reservations;

public class Person
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Nume { get; }
    public int Varsta { get; }

    protected Person(string nume, int varsta)
    {
        if (string.IsNullOrWhiteSpace(nume))
            throw new ArgumentException("Name cannot be empty.");

        if (varsta < 18)
            throw new ArgumentException("Person must be at least 18 years old.");

        Nume = nume;
        Varsta = varsta;
    }
}