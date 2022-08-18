namespace Tables;

internal struct Row<T>
{
    public bool committed;
    public T data;
}