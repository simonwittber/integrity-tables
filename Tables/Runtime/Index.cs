namespace Tables;

public partial class Index<T> where T : struct
{
    private readonly Dictionary<string, Dictionary<IndexKey, int>> _fieldIndexes = new();
    private readonly Table<T> _table;

    public Index(Table<T> table)
    {
        _table = table;
    }

    public void AddConstraint(string indexName, params string[] uniqueFieldNames)
    {
        var allFieldNames = _table.FieldNames;
        var fieldIndexes = new int[uniqueFieldNames.Length];
        for (var i = 0; i < uniqueFieldNames.Length; i++)
        {
            var fieldName = uniqueFieldNames[i];
            var fieldIndex = Array.IndexOf(allFieldNames, fieldName);
            if (fieldIndex < 0) throw new KeyNotFoundException($"Unknown field name: {fieldName}");
            fieldIndexes[i] = fieldIndex;
        }

        var idx = _fieldIndexes[indexName] = new Dictionary<IndexKey, int>();
        _table.BeforeAdd += CheckFieldValueIsUnique(idx, fieldIndexes);
        _table.AfterAdd += SaveUniqueFieldValue(idx, fieldIndexes);
        _table.BeforeUpdate += CheckFieldValueIsStillUnique(idx, fieldIndexes);
        _table.AfterUpdate += UpdateUniqueFieldValue(idx, fieldIndexes);
        _table.AfterDelete += RemoveUniqueFieldValue(idx, fieldIndexes);
    }

    private BeforeUpdateDelegate<T> CheckFieldValueIsStillUnique(Dictionary<IndexKey, int> index, int[] fieldIndexes)
    {
        return (item, newItem) =>
        {
            var indexKey = CreateIndexkey(fieldIndexes, newItem);

            if (index.ContainsKey(indexKey)) throw new ConstraintException("Unique Index violation");

            return newItem;
        };
    }

    private IndexKey CreateIndexkey(int[] fieldIndexes, T item)
    {
        var indexKey = new IndexKey();
        for (var i = 0; i < fieldIndexes.Length; i++)
        {
            var fieldIndex = fieldIndexes[i];
            indexKey.SetValue(fieldIndex, _table.GetField(item, fieldIndex));
        }

        return indexKey;
    }

    private AfterDeleteDelegate<T> RemoveUniqueFieldValue(Dictionary<IndexKey, int> index, int[] fieldIndexes)
    {
        return item =>
        {
            var indexKey = CreateIndexkey(fieldIndexes, item);
            index.Remove(indexKey);
        };
    }

    private AfterUpdateDelegate<T> UpdateUniqueFieldValue(Dictionary<IndexKey, int> index, int[] fieldIndexes)
    {
        return (item, newItem) =>
        {
            var pk = _table.GetPrimaryKey(newItem);

            var oldIndexKey = CreateIndexkey(fieldIndexes, item);
            index.Remove(oldIndexKey);
            var newIndexKey = CreateIndexkey(fieldIndexes, newItem);
            index.Add(newIndexKey, pk);
        };
    }

    private AfterAddDelegate<T> SaveUniqueFieldValue(Dictionary<IndexKey, int> index, int[] fieldIndexes)
    {
        return item =>
        {
            var indexKey = CreateIndexkey(fieldIndexes, item);
            var pk = _table.GetPrimaryKey(item);
            index.Add(indexKey, pk);
        };
    }

    private BeforeAddDelegate<T> CheckFieldValueIsUnique(Dictionary<IndexKey, int> index, int[] fieldIndexes)
    {
        return item =>
        {
            var indexKey = CreateIndexkey(fieldIndexes, item);
            if (index.ContainsKey(indexKey)) throw new ConstraintException("Unique Index violation");
            return item;
        };
    }
}