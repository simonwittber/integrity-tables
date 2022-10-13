using System.Reflection;

namespace IntegrityTables;

internal class ConstraintCollection<T> where T : struct
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
                throw new IntegrityException(constraintName);
        }
    }

    
    public void AddWeakRelationship(WeakKeyGetterDelegate<T> getWeakKeyFn, WeakKeySetterDelegate<T> setWeakKeyFn, ITable foreignTable, CascadeOperation cascadeOperation)
    {
        var constraintName = $"fk:{typeof(T).Name}=>{foreignTable.Name}";
        Add(TriggerType.OnUpdate, constraintName, row =>
        {
            var fk = getWeakKeyFn(row);
            if (!fk.HasValue) return true;
            return foreignTable.ContainsKey(fk.Value);
        });

        switch (cascadeOperation)
        {
            case CascadeOperation.Delete:
                foreignTable.BeforeDelete += fk => { table.Delete(i => getWeakKeyFn(i) == fk); };
                break;
            case CascadeOperation.SetNull:
                foreignTable.BeforeDelete += fk => { table.Update(i => setWeakKeyFn(i, null), i => getWeakKeyFn(i) == fk); };
                break;
            case CascadeOperation.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cascadeOperation), cascadeOperation, null);
        }

        foreignTable.BeforeDelete += fk =>
        {
            if (table.IsTrue(row => getWeakKeyFn(row) == fk))
                throw new IntegrityException($"{typeof(T)}_fk");
        };
    }
    
    public void AddStrongRelationship(StrongKeyGetterDelegate<T> getForeignKeyFn, StrongKeySetterDelegate<T> setForeignKeyFn, ITable foreignTable, CascadeOperation cascadeOperation)
    {
        var constraintName = $"fk:{typeof(T).Name}=>{foreignTable.Name}";
        Add(TriggerType.OnUpdate, constraintName, row =>
        {
            var fk = getForeignKeyFn(row);
            return foreignTable.ContainsKey(fk);
        });

        switch (cascadeOperation)
        {
            case CascadeOperation.Delete:
                foreignTable.BeforeDelete += fk => { table.Delete(i => getForeignKeyFn(i) == fk); };
                break;
            case CascadeOperation.SetNull:
                throw new InvalidOperationException("Cannot set a strong relationship to null.");
                break;
            case CascadeOperation.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cascadeOperation), cascadeOperation, null);
        }

        foreignTable.BeforeDelete += fk =>
        {
            if (table.IsTrue(row => getForeignKeyFn(row) == fk))
                throw new IntegrityException($"{typeof(T)}_fk");
        };
    }

    public void AddRelationship(string fieldName, ITable foreignTable, CascadeOperation cascadeOperation)
    {
        var fi = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (fi.FieldType == typeof(int?))
        {
            var getSet = Database.GetSetCompiler.Create<T, int?>(fieldName);
            AddWeakRelationship(getSet.Get, getSet.Set, foreignTable, cascadeOperation);
        }
        else if (fi.FieldType == typeof(int))
        {
            var getSet = Database.GetSetCompiler.Create<T, int>(fieldName);
            AddStrongRelationship(getSet.Get, getSet.Set, foreignTable, cascadeOperation);
        }
    }
}

