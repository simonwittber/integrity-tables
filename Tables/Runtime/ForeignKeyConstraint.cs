using System.Reflection;
namespace Tables;
using static Tables.Database;

public class ForeignKeyConstraint<Source, Foreign> where Source : struct where Foreign : struct
{

    public ForeignKeyConstraint(string foreignKeyFieldName, CascadeOperation cascadeOperation)
    {
        var sourceField = typeof(Source).GetField(foreignKeyFieldName, BindingFlags.Instance | BindingFlags.Public);
        var foreignField = typeof(Foreign).GetField("id", BindingFlags.Instance | BindingFlags.Public);

        var sourceTable = GetTable<Source>();
        var foreignTable = GetTable<Foreign>();
        sourceTable.AddRelationshipConstraint(
            getForeignKeyFn: source => (int?)sourceField.GetValue(source),
            setForeignKeyFn: (source, fkValue) =>
            {
                sourceField.SetValue(source, fkValue);
                return source;
            }, 
            foreignTable:foreignTable,
            cascadeOperation);
    }
}