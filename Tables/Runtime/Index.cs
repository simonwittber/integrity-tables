using System.Xml.Schema;

namespace Tables;

public partial class Index<T> where T : struct
{
    private readonly Dictionary<string, DoubleMap<IndexKey, int>> _fieldMaps = new();
    
    private readonly Table<T> _table;
    

    public void Begin()
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            doubleMap.Begin();
        }
    }
    
    public void Commit()
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            doubleMap.Commit();
        }
    }
    
    public void Rollback()
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            doubleMap.Rollback();
        }
    }

    public Index(Table<T> table)
    {
        _table = table;
        _table.BeforeAdd += BeforeAdd;
        _table.BeforeUpdate += BeforeUpdate;
        _table.BeforeDelete += BeforeDelete;
    }

    private void BeforeDelete(int pk)
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            var item = _table.Get(pk); 
            var indexKey = CreateIndexkey(doubleMap.fieldIndexes, item);
            doubleMap.Remove(indexKey);
        }
    }

    private T BeforeUpdate(T oldItem, T newItem)
    {
        foreach(var doubleMap in _fieldMaps.Values)
        {
            var indexKey = CreateIndexkey(doubleMap.fieldIndexes, newItem);
            if (doubleMap.ContainsKey(indexKey)) 
                throw new ConstraintException("Unique Index violation");
            var pk = _table.GetPrimaryKey(newItem);
            var oldIndexKey = CreateIndexkey(doubleMap.fieldIndexes, oldItem);
            doubleMap.Remove(oldIndexKey);
            doubleMap.Add(indexKey, pk);
        }
        return newItem;
    }
    
    private T BeforeAdd(T item)
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            var indexKey = CreateIndexkey(doubleMap.fieldIndexes, item);
            if (doubleMap.ContainsKey(indexKey)) 
                throw new ConstraintException("Unique Index violation");
            var pk = _table.GetPrimaryKey(item);
            doubleMap.Add(indexKey, pk);
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

        _fieldMaps[indexName] = new DoubleMap<IndexKey, int>(fieldIndexes);
        // _table.BeforeAdd += BeforeAdd;//CheckFieldValueIsUnique(idx, fieldIndexes);
        // _table.BeforeUpdate += CheckFieldValueIsStillUnique(idx, fieldIndexes);
        // _table.BeforeDelete += RemoveUniqueFieldValue(idx, fieldIndexes);
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


    internal void Remove(int pk)
    {
        foreach (var doubleMap in _fieldMaps.Values)
        {
            doubleMap.Remove(pk);
        }
        
    }
}