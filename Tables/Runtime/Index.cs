namespace Tables;

public class Index<T> where T:struct
{
    private Dictionary<int, Dictionary<object, int>> _fieldIndexes = new();
    private readonly Table<T> table;

    public Index(Table<T> table)
    {
        this.table = table;
    }

    public void AddConstraint(string fieldName)
    {
        var fieldNames = table.Names;
        var fieldIndex = System.Array.IndexOf(fieldNames, fieldName);
        if (fieldIndex < 0) throw new KeyNotFoundException($"Unknown field name: {fieldName}");
        _fieldIndexes[fieldIndex] = new Dictionary<object, int>(); 
        table.BeforeAdd += CheckFieldValueIsUnique(fieldIndex);
        table.AfterAdd += SaveUniqueFieldValue(fieldIndex);
        table.BeforeUpdate += CheckFieldValueIsStillUnique(fieldIndex);
        table.AfterUpdate += UpdateUniqueFieldValue(fieldIndex);
        table.AfterDelete += RemoveUniqueFieldValue(fieldIndex);
    }

    private BeforeUpdateDelegate<T> CheckFieldValueIsStillUnique(int fieldIndex)
    {
        return (item, newItem) =>
        {
            var fieldValue = table.indexer.Get(newItem, fieldIndex);
            if (_fieldIndexes[fieldIndex].ContainsKey(fieldValue))
            {
                throw new ConstraintException($"Unique Index violation on {table.Names[fieldIndex]}");
            }
            return newItem;
        };
    }

    private AfterDeleteDelegate<T> RemoveUniqueFieldValue(int fieldIndex)
    {
        return item =>
        {
            var fieldValue = table.indexer.Get(item, fieldIndex);
            _fieldIndexes[fieldIndex].Remove(fieldValue);
        };
    }

    private AfterUpdateDelegate<T> UpdateUniqueFieldValue(int fieldIndex)
    {
        return (item, newItem) =>
        {
            var idx = _fieldIndexes[fieldIndex];
            var oldFieldValue = table.indexer.Get(item, fieldIndex);
            var newFieldValue = table.indexer.Get(item, fieldIndex);
            var pk = table._primaryKeyGetterFn(newItem);
            idx.Remove(oldFieldValue);
            idx.Add(newFieldValue, pk);
        };
    }

    private AfterAddDelegate<T> SaveUniqueFieldValue(int fieldIndex) 
    {
        return item =>
        {
            var fieldValue = table.indexer.Get(item, fieldIndex);
            var pk = table._primaryKeyGetterFn(item);
            _fieldIndexes[fieldIndex].Add(fieldValue, pk);
        };
    }

    private BeforeAddDelegate<T> CheckFieldValueIsUnique(int fieldIndex) 
    {
        return item =>
        {
            var fieldvalue = table.indexer.Get(item, fieldIndex);
            if (_fieldIndexes[fieldIndex].ContainsKey(fieldvalue))
            {
                throw new ConstraintException($"Unique Index violation on {table.Names[fieldIndex]}");
            }
            return item;
        };
    }
}