namespace Tables
{

    public class ConstraintException : Exception
    {
        public ConstraintException(string name) : base($"Constraint Exception: {name}")
        {
        }
    }
}