namespace IntegrityTables
{
    public class IntegrityException : Exception
    {
        public IntegrityException(string name) : base($"Integrity Exception: {name}")
        {
        }
    }
}