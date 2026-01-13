namespace Flight_Reservations
{
    public interface IConsoleWrapper
    {
        void WriteLine(string message);
        
        void WriteError(string message);
        
        void WriteSuccess(string message);
        
        void WriteWarning(string message);
        
        string ReadLine();
        
        void ReadKey();
        
        void Clear();
    }
}