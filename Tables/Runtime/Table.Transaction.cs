namespace IntegrityTables;

public partial class Table<T>
{
    public void Begin()
    {
        if (IsDirty)
        {
            throw new Exception("Cannot start a transaction on a dirty table.");
        }
        _uniqueIndex.Begin();
    }

    private List<T> pendingAdd = new();
    private List<(T data, T oldData)> pendingUpdate = new();
    private List<T> pendingDelete = new();
    
    public void Commit()
    {
        pendingAdd.Clear();
        for (var i = 0; i < _newRows.Count; i++)
        {
            var pk = _newRows[i];
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
            pendingAdd.Add(row.data);
        }
        _newRows.Clear();

        pendingUpdate.Clear();
        foreach (var (pk,oldData) in _modifiedRows)
        {
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
            pendingUpdate.Add((row.data, oldData));
        }
        _modifiedRows.Clear();

        pendingDelete.Clear();
        foreach (var (pk, item) in _deletedRows)
        {
            var index = _pkIndex[pk];
            var lastIndex = _rows.Count - 1;
            var lastRow = _rows[lastIndex];
            _rows[index] = lastRow;
            _pkIndex[GetPrimaryKey(lastRow.data)] = index;
            _rows.RemoveAt(lastIndex);
            _pkIndex.Remove(pk);
            pendingDelete.Add(item);
        }
        _deletedRows.Clear();
        
        _uniqueIndex.Commit();
        
        foreach(var row in pendingAdd)
            AfterAdd?.Invoke(row);
        foreach(var (newRow, oldRow) in pendingUpdate)
            AfterUpdate?.Invoke(oldRow, newRow);
        foreach(var deadRow in pendingDelete)
            AfterDelete?.Invoke(deadRow);
    }

    public void Rollback()
    {
        for (int i = 0, len = _newRows.Count; i < len; i++)
        {
            var pk = _newRows[i];
            var index = _pkIndex[pk];
            var lastIndex = _rows.Count - 1;
            var lastRow = _rows[lastIndex];
            _rows[index] = lastRow;
            _pkIndex[GetPrimaryKey(lastRow.data)] = index;
            _pkIndex.Remove(pk);
            _rows.RemoveAt(lastIndex);
        }

        _newRows.Clear();

        foreach (var (pk, oldData) in _modifiedRows)
        {
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.data = oldData;
            _rows[index] = row;
        }

        _modifiedRows.Clear();

        foreach (var (pk, data) in _deletedRows)
        {
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.deleted = false;
            row.data = data;
            _rows[index] = row;
            _pkIndex[pk] = index;
        }

        _deletedRows.Clear();
        
        _uniqueIndex.Rollback();
    }

    
}