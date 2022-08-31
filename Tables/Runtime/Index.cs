namespace Tables;

public class Index<T> where T:struct
{
    private readonly Dictionary<int, Dictionary<object, int>> _fieldIndexes = new();
    private readonly Table<T> _table;

    public Index(Table<T> table)
    {
        this._table = table;
    }

    public void AddConstraint(string fieldName)
    {
        var fieldNames = _table.Names;
        var fieldIndex = System.Array.IndexOf(fieldNames, fieldName);
        if (fieldIndex < 0) throw new KeyNotFoundException($"Unknown field name: {fieldName}");
        _fieldIndexes[fieldIndex] = new Dictionary<object, int>(); 
        _table.BeforeAdd += CheckFieldValueIsUnique(fieldIndex);
        _table.AfterAdd += SaveUniqueFieldValue(fieldIndex);
        _table.BeforeUpdate += CheckFieldValueIsStillUnique(fieldIndex);
        _table.AfterUpdate += UpdateUniqueFieldValue(fieldIndex);
        _table.AfterDelete += RemoveUniqueFieldValue(fieldIndex);
    }

    private BeforeUpdateDelegate<T> CheckFieldValueIsStillUnique(int fieldIndex)
    {
        return (item, newItem) =>
        {
            var fieldValue = _table.GetField(newItem, fieldIndex);
            if (_fieldIndexes[fieldIndex].ContainsKey(fieldValue))
            {
                throw new ConstraintException($"Unique Index violation on {_table.Names[fieldIndex]}");
            }
            return newItem;
        };
    }

    private AfterDeleteDelegate<T> RemoveUniqueFieldValue(int fieldIndex)
    {
        return item =>
        {
            var fieldValue = _table.GetField(item, fieldIndex);
            _fieldIndexes[fieldIndex].Remove(fieldValue);
        };
    }

    private AfterUpdateDelegate<T> UpdateUniqueFieldValue(int fieldIndex)
    {
        return (item, newItem) =>
        {
            var idx = _fieldIndexes[fieldIndex];
            var oldFieldValue = _table.GetField(item, fieldIndex);
            var newFieldValue = _table.GetField(item, fieldIndex);
            var pk = _table.GetPrimaryKey(newItem);
            idx.Remove(oldFieldValue);
            idx.Add(newFieldValue, pk);
        };
    }

    private AfterAddDelegate<T> SaveUniqueFieldValue(int fieldIndex) 
    {
        return item =>
        {
            var fieldValue = _table.GetField(item, fieldIndex);
            var pk = _table.GetPrimaryKey(item);
            _fieldIndexes[fieldIndex].Add(fieldValue, pk);
        };
    }

    private BeforeAddDelegate<T> CheckFieldValueIsUnique(int fieldIndex) 
    {
        return item =>
        {
            var fieldvalue = _table.GetField(item, fieldIndex);
            if (_fieldIndexes[fieldIndex].ContainsKey(fieldvalue))
            {
                throw new ConstraintException($"Unique Index violation on {_table.Names[fieldIndex]}");
            }
            return item;
        };
    }
}