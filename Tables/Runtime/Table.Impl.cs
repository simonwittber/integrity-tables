namespace Tables;

public partial class Table<T>
{
    private T _Add(T data)
    {
        if (SetPrimaryKey != null) data = SetPrimaryKey(data, _idCount++);
        if (BeforeAdd != null) data = BeforeAdd(data);
        _constraints.CheckConstraintsForItem(TriggerType.OnCreate, data);
        _constraints.CheckConstraintsForItem(TriggerType.OnUpdate, data);
        var index = _rows.Count;
        var pk = GetPrimaryKey(data);
        _pkIndex.Add(pk, index);
        _rows.Add(new Row<T>() {data = data, committed = false, deleted = false});
        _newRows.Add(pk);
        if (AfterAdd != null) AfterAdd(data);
        return data;
    }

    private T _Update(T newData)
    {
        var pk = GetPrimaryKey(newData);
        var index = _pkIndex[pk];
        var currentRow = GetRow(index);
        var rollbackData = currentRow.data;
        if (BeforeUpdate != null) newData = BeforeUpdate(rollbackData, newData);
        _constraints.CheckConstraintsForItem(TriggerType.OnUpdate, newData);
        currentRow.data = newData;
        currentRow.committed = false;
        SetRow(index, currentRow);
        //Add the old data in case of rollback. Consecutive updates will be ignored.
        _modifiedRows.TryAdd(pk, rollbackData);
        if (AfterUpdate != null) AfterUpdate(rollbackData, newData);
        return newData;
    }

    private void _Delete(T data)
    {
        BeforeDelete?.Invoke(GetPrimaryKey(data));
        _constraints.CheckConstraintsForItem(TriggerType.OnDelete, data);
        var pk = GetPrimaryKey(data);
        var index = _pkIndex[pk];
        _deletedRows.TryAdd(pk, data);
        var row = _rows[index];
        row.deleted = true;
        _rows[index] = row;
        AfterDelete?.Invoke(data);
    }
}