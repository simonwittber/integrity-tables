namespace Tables;

public class CheckConstraintException : Exception
{
    public CheckConstraintException(string name) : base($"Constraint Exception: {name}")
    {
    }
}