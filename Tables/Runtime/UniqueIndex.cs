using System.Xml.Schema;

namespace IntegrityTables;

internal partial class UniqueIndex<T> where T : struct
{
    private readonly Dictionary<string, BiMap<MultiFieldKey, int>> _biMaps = new();
    private readonly Table<T> _table;
    

    public void Begin()
    {
        foreach (var biMap in _biMaps.Values)
        {
            biMap.Begin();
        }
    }
    
    public void Commit()
    {
        foreach (var biMap in _biMaps.Values)
        {
            biMap.Commit();
        }
    }
    
    public void Rollback()
    {
        foreach (var biMap in _biMaps.Values)
        {
            biMap.Rollback();
        }
    }

    public UniqueIndex(Table<T> table)
    {
        _table = table;
        _table.BeforeAdd += BeforeAdd;
        _table.BeforeUpdate += BeforeUpdate;
        _table.BeforeDelete += BeforeDelete;
    }

    private void BeforeDelete(int pk)
    {
        foreach (var biMap in _biMaps.Values)
        {
            var item = _table.Get(pk); 
            var indexKey = CreateIndexKeyFromFields(biMap.fieldIndexes, item);
            biMap.Remove(indexKey);
        }
    }

    private T BeforeUpdate(T oldItem, T newItem)
    {
        foreach(var biMap in _biMaps.Values)
        {
            var pk = _table.GetPrimaryKey(newItem);
            var indexKey = CreateIndexKeyFromFields(biMap.fieldIndexes, newItem);
            if (biMap.TryGet(indexKey, out int _pk) && pk != _pk) 
                throw new IntegrityException("Unique Index violation");
            var oldIndexKey = CreateIndexKeyFromFields(biMap.fieldIndexes, oldItem);
            biMap.Remove(oldIndexKey);
            biMap.Add(indexKey, pk);
        }
        return newItem;
    }
    
    private T BeforeAdd(T item)
    {
        foreach (var biMap in _biMaps.Values)
        {
            var indexKey = CreateIndexKeyFromFields(biMap.fieldIndexes, item);
            if (biMap.ContainsKey(indexKey)) 
                throw new IntegrityException("Unique Index violation");
            var pk = _table.GetPrimaryKey(item);
            biMap.Add(indexKey, pk);
        }
        return item;
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

        _biMaps[indexName] = new BiMap<MultiFieldKey, int>(fieldIndexes);
    }
    
    private MultiFieldKey CreateIndexKeyFromFields(int[] fieldIndexes, T item)
    {
        var indexKey = new MultiFieldKey();
        for (var i = 0; i < fieldIndexes.Length; i++)
        {
            var fieldIndex = fieldIndexes[i];
            indexKey.SetValue(fieldIndex, _table.GetField(item, fieldIndex));
        }

        return indexKey;
    }
}