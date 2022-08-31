using System.Collections;

namespace Tables;

public partial class Table<T>
{
    public void Delete(T data)
    {
        _Delete(data);
    }

    public int Delete(Predicate<T> predicateFn) => Apply(Delete, predicateFn);
    
    public void Delete(int id) => Delete(Get(id));

    public T Get(int id) => GetRow(_pkIndex[id]).data;
    
    public T Get(int? id) => GetRow(_pkIndex[id.Value]).data;

    public T Add(T data)
    {
        return _Add(data);
    }
    
    public object GetField(object row, int index) => _fieldIndexer.Get(row, index);

    public T Update(T newData)
    {
        return _Update(newData);
    }

    public int Update(ModifyDelegate<T> modifyFn, Predicate<T> predicateFn)
    {
        var modifiedCount = 0;
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            if (predicateFn(row.data))
            {
                Update(modifyFn(row.data));
                modifiedCount++;
            }
        }

        return modifiedCount;
    }

    public bool TryGet(Predicate<T> predicateFn, out T data)
    {
        var count = 0;
        data = default;
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted || !predicateFn(row.data)) continue;
            data = row.data;
            count++;
        }

        return count == 1;
    }

    public void Update(ModifyDelegate<T> modifyFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            Update(modifyFn(row.data));
        }
    }

    public bool ContainsKey(int key) => _pkIndex.ContainsKey(key);

    public int Apply(RowAction<T> applyFn, Predicate<T> predicateFn)
    {
        var count = 0;
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted || !predicateFn(row.data)) continue;
            applyFn(row.data);
            count++;
        }

        return count;
    }

    public IEnumerable<T> Select(Predicate<T> predicateFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted || !predicateFn(row.data)) continue;
            yield return row.data;
        }
    }

    public bool IsTrue(Predicate<T> predicateFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            if (predicateFn(row.data)) return true;
        }

        return false;
    }

    public int Count(Predicate<T> predicateFn)
    {
        var count = 0;
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted || !predicateFn(row.data)) continue;
            count++;
        }

        return count;
    }

    public void Apply(Action<T> applyFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            applyFn(row.data);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (!row.deleted) yield return row.data;
        }
    }
    
    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add(triggerType, constraintName, constraintFn);
    }

    public void AddUniqueConstraint(string fieldName)
    {
        _index.AddConstraint(fieldName);
    }

    public void AddRelationshipConstraint(string foreignKeyFieldName, ITable table, CascadeOperation cascadeOperation, bool isNullable)
    {
        _constraints.AddRelationship(foreignKeyFieldName, table, cascadeOperation, isNullable);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}