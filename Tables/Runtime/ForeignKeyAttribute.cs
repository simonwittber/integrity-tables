using System.Reflection;
namespace Tables;
using static Tables.Database;

[AttributeUsage(AttributeTargets.Field)]
public class ForeignKeyAttribute : System.Attribute
{
    public readonly Type relatedType;
    public readonly CascadeOperation cascadeOperation;
    public readonly bool isNullable;

    public ForeignKeyAttribute(Type relatedType, CascadeOperation cascadeOperation, bool isNullable=false)
    {
        this.relatedType = relatedType;
        this.cascadeOperation = cascadeOperation;
        this.isNullable = isNullable;
    }
}