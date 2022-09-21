namespace Tables;

[AttributeUsage(AttributeTargets.Struct)]
public class CollectionNameAttribute : System.Attribute
{
    public string collectionName;

    public CollectionNameAttribute(string collectionName)
    {
        this.collectionName = collectionName;
    }
}