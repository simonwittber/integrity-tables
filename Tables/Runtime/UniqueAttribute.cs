namespace Tables;

[AttributeUsage(AttributeTargets.Field)]
public class UniqueAttribute : Attribute
{
    public readonly string indexName;

    public UniqueAttribute(string indexName = null)
    {
        this.indexName = indexName;
    }
}