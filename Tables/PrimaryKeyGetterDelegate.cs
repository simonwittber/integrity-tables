namespace Tables
{

    public delegate int PrimaryKeyGetterDelegate<T>(T item);

    public delegate T PrimaryKeySetterDelegate<T>(T item);
}