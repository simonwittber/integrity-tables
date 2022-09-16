namespace Tables;

[AttributeUsage(AttributeTargets.Field)]
public class ForeignKeyAttribute : System.Attribute
{
    public string relationshipName;
    public string inverseRelationshipName;
    public readonly Type relatedType;
    public readonly CascadeOperation cascadeOperation;

    public ForeignKeyAttribute(string relationshipName, string inverseRelationshipName, Type relatedType, CascadeOperation cascadeOperation)
    {
        this.relationshipName = relationshipName;
        this.inverseRelationshipName = inverseRelationshipName;
        this.relatedType = relatedType;
        this.cascadeOperation = cascadeOperation;
    }
}