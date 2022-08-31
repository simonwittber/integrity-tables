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
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
        }

        _newRows.Clear();

        foreach (var pk in _modifiedRows.Keys)
        {
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.committed = true;
            _rows[index] = row;
        }

        _modifiedRows.Clear();

        foreach (var (pk, item) in _deletedRows)
        {
            var index = _pkIndex[pk];
            var lastIndex = _rows.Count - 1;
            var lastRow = _rows[lastIndex];
            _rows[index] = lastRow;
            _pkIndex[_primaryKeyGetterFn(lastRow.data)] = index;
            _rows.RemoveAt(lastIndex);
            _pkIndex.Remove(pk);
        }

        _deletedRows.Clear();
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
            _pkIndex[_primaryKeyGetterFn(lastRow.data)] = index;
            _pkIndex.Remove(pk);
            _rows.RemoveAt(lastIndex);
        }

        _newRows.Clear();

        foreach (var (pk, data) in _modifiedRows)
        {
            var index = _pkIndex[pk];
            var row = _rows[index];
            row.data = data;
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
    }

    
}