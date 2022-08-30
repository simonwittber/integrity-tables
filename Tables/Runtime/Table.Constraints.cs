using System.Reflection;

namespace Tables;

public partial class Table<T>
{
    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((triggerType, constraintName, constraintFn));
    }

    public void AddRelationshipConstraint(string fieldName, ITable foreignTable, CascadeOperation cascadeOperation, bool isNullable)
    {
        var fi = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (fi.FieldType != typeof(int?))
            throw new Exception("Foreign key fields must be of type 'int?'");
        var getSet = Database.getSetCompiler.Create<T, int?>(fieldName);
        AddRelationshipConstraint(getSet.Get, getSet.Set, foreignTable, cascadeOperation, isNullable);
    }

    public void AddRelationshipConstraint(ForeignKeyGetterDelegate<T> getForeignKeyFn, ForeignKeySetterDelegate<T> setForeignKeyFn, ITable foreignTable, CascadeOperation cascadeOperation, bool isNullable)
    {
        var constraintName = $"fk:{typeof(T).Name}=>{foreignTable.Name}";
        AddConstraint(TriggerType.OnUpdate, constraintName, row =>
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
                foreignTable.BeforeDelete += fk => { Delete(i => getForeignKeyFn(i) == fk); };
                break;
            case CascadeOperation.SetNull:
                foreignTable.BeforeDelete += fk => { Update(i => setForeignKeyFn(i, null), i => getForeignKeyFn(i) == fk); };
                break;
            case CascadeOperation.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cascadeOperation), cascadeOperation, null);
        }

        foreignTable.BeforeDelete += fk =>
        {
            if (IsTrue(row => getForeignKeyFn(row) == fk))
                throw new ConstraintException($"{typeof(T)}_fk");
        };
    }

    private void CheckConstraintsForItem(TriggerType triggerType, T item)
    {
        for (var i = 0; i < _constraints.Count; i++)
        {
            var (type, constraintName, constraintFn) = _constraints[i];
            if (type == triggerType && !constraintFn.Invoke(item))
                throw new ConstraintException(constraintName);
        }

        var pk = _primaryKeyGetterFn(item);
        foreach(var (fieldIndex, index) in _uniqueFields)
        {
            var fieldValue = indexer.Get(item, fieldIndex);
            if (index.TryGetValue(fieldValue, out var existingIndex) && existingIndex != pk)
            {
                var names = indexer.Names();
                
                throw new ConstraintException($"Unique Index Failed({names[fieldIndex]})");
            }
        }
    }

    public void AddUniqueConstraint(string fieldName)
    {
        var names = indexer.Names();
        var fieldIndex = System.Array.IndexOf(names, fieldName);
        _uniqueFields.Add(fieldIndex, new Dictionary<object, int>());
    }
}