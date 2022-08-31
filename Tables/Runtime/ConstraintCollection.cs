using System.Reflection;

namespace Tables;

public class ConstraintCollection<T> where T : struct
{
    private readonly Table<T> table;
    private List<(TriggerType, string, ConstraintDelegate<T>)> _constraints;

    public ConstraintCollection(Table<T> table)
    {
        this.table = table;
        _constraints = new List<(TriggerType, string, ConstraintDelegate<T>)>();
    }

    public void Add(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((triggerType, constraintName, constraintFn));
    }

    internal void CheckConstraintsForItem(TriggerType triggerType, T item)
    {
        for (var i = 0; i < _constraints.Count; i++)
        {
            var (type, constraintName, constraintFn) = _constraints[i];
            if (type == triggerType && !constraintFn.Invoke(item))
                throw new ConstraintException(constraintName);
        }
    }

    public void AddRelationship(ForeignKeyGetterDelegate<T> getForeignKeyFn, ForeignKeySetterDelegate<T> setForeignKeyFn, ITable foreignTable, CascadeOperation cascadeOperation, bool isNullable)
    {
        var constraintName = $"fk:{typeof(T).Name}=>{foreignTable.Name}";
        Add(TriggerType.OnUpdate, constraintName, row =>
        {
            var fk = getForeignKeyFn(row);
            if (!isNullable && !fk.HasValue)
                return false;
            if (!fk.HasValue) return true;
            return foreignTable.ContainsKey(fk.Value);
        });

        switch (cascadeOperation)
        {
            case CascadeOperation.Delete:
                foreignTable.BeforeDelete += fk => { table.Delete(i => getForeignKeyFn(i) == fk); };
                break;
            case CascadeOperation.SetNull:
                foreignTable.BeforeDelete += fk => { table.Update(i => setForeignKeyFn(i, null), i => getForeignKeyFn(i) == fk); };
                break;
            case CascadeOperation.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cascadeOperation), cascadeOperation, null);
        }

        foreignTable.BeforeDelete += fk =>
        {
            if (table.IsTrue(row => getForeignKeyFn(row) == fk))
                throw new ConstraintException($"{typeof(T)}_fk");
        };
    }

    public void AddRelationship(string fieldName, ITable foreignTable, CascadeOperation cascadeOperation, bool isNullable)
    {
        var fi = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (fi.FieldType != typeof(int?))
            throw new Exception("Foreign key fields must be of type 'int?'");
        var getSet = Database.getSetCompiler.Create<T, int?>(fieldName);
        AddRelationship(getSet.Get, getSet.Set, foreignTable, cascadeOperation, isNullable);
    }
}

