using System.Collections;

namespace Tables;

public partial class Table<T>
{
    public void Begin()
    {
        if (IsDirty)
        {
            throw new Exception("Cannot start a transaction on a dirty table.");
        }
    }

    public void Commit()
    {
        for (var i = 0; i < _newRows.Count; i++)
        {
            var pk = _newRows[i];
            var index = _index[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
        }

        _newRows.Clear();

        foreach (var pk in _modifiedRows.Keys)
        {
            var index = _index[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
        }

        _modifiedRows.Clear();

        foreach (var (pk, item) in _deletedRows)
        {
            var index = _index[pk];
            var lastIndex = _rows.Count - 1;
            var lastRow = _rows[lastIndex];
            _rows[index] = lastRow;
            _index[_primaryKeyGetterFn(lastRow.data)] = index;
            _rows.RemoveAt(lastIndex);
            _index.Remove(pk);
        }

        _deletedRows.Clear();
    }

    public void Rollback()
    {
        for (int i = 0, len = _newRows.Count; i < len; i++)
        {
            var pk = _newRows[i];
            var index = _index[pk];
            var lastIndex = _rows.Count - 1;
            var lastRow = _rows[lastIndex];
            _rows[index] = lastRow;
            _index[_primaryKeyGetterFn(lastRow.data)] = index;
            _index.Remove(pk);
            _rows.RemoveAt(lastIndex);
        }

        _newRows.Clear();

        foreach (var (pk, data) in _modifiedRows)
        {
            var index = _index[pk];
            var row = _rows[index];
            row.data = data;
            _rows[index] = row;
        }

        _modifiedRows.Clear();

        foreach (var (pk, data) in _deletedRows)
        {
            var index = _index[pk];
            var row = _rows[index];
            row.deleted = false;
            row.data = data;
            _rows[index] = row;
            _index[pk] = index;
        }

        _deletedRows.Clear();
    }

    
}