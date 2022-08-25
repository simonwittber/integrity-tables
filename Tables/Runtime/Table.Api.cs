using System.Collections;

namespace Tables;

public partial class Table<T>
{
    public void Delete(T data)
    {
        BeforeDelete?.Invoke(_primaryKeyGetterFn(data));
        CheckConstraintsForItem(TriggerType.OnDelete, data);
        var pk = _primaryKeyGetterFn(data);
        var index = _index[pk];
        _deletedRows.TryAdd(pk, data);
        var row = _rows[index];
        row.deleted = true;
        _rows[index] = row;
        AfterDelete?.Invoke(data);
    }

    public int Delete(Predicate<T> predicateFn) => Apply(Delete, predicateFn);
    
    public void Delete(int id) => Delete(Get(id));

    public T Get(int id) => GetRow(_index[id]).data;

    public T Add(T data)
    {
        if (_primaryKeySetterFn != null) data = _primaryKeySetterFn(data, id_count++);
        if (BeforeAdd != null) data = BeforeAdd(data);
        CheckConstraintsForItem(TriggerType.OnCreate, data);
        CheckConstraintsForItem(TriggerType.OnUpdate, data);
        var pk = InternalAdd(data);
        _newRows.Add(pk);
        if (AfterAdd != null) AfterAdd(data);
        return data;
    }

    private int InternalAdd(T data)
    {
        var index = _rows.Count;
        var pk = _primaryKeyGetterFn(data);
        _index.Add(pk, index);
        _rows.Add(new Row<T>() {data = data, committed = false, deleted = false});
        return pk;
    }

    public T Update(T newData)
    {
        var pk = _primaryKeyGetterFn(newData);
        var index = _index[pk];
        var currentRow = GetRow(index);
        var oldData = currentRow.data;
        if (BeforeUpdate != null) newData = BeforeUpdate(oldData, newData);
        CheckConstraintsForItem(TriggerType.OnUpdate, newData);
        currentRow.data = newData;
        currentRow.committed = false;
        SetRow(index, currentRow);
        //Try and add the old data in case of rollback. Consecutive updates will be ignored.
        _modifiedRows.TryAdd(pk, oldData);
        if (AfterUpdate != null) AfterUpdate(newData);
        return newData;
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

    public bool ContainsKey(int key) => _index.ContainsKey(key);

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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}