using System.Reflection;
namespace Tables;
using static Tables.Database;

[AttributeUsage(AttributeTargets.Field)]
public class ForeignKeyAttribute : System.Attribute
{
    public readonly Type relatedType;
    public readonly CascadeOperation cascadeOperation;

    public ForeignKeyAttribute(Type relatedType, CascadeOperation cascadeOperation)
    {
        this.relatedType = relatedType;
        this.cascadeOperation = cascadeOperation;
    }
}