namespace IntegrityTables;

public class UncommittedException : Exception
{
    public UncommittedException(string msg) : base(msg)
    {
    }
}