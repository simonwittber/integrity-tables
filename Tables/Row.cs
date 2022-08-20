namespace Tables
{

    internal struct Row<T>
    {
        public bool committed;
        public bool deleted;
        public T data;
    }
}