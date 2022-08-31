namespace Tables;

public partial class Table<T>
{
    public string Name { get; private set; }

    public int RowCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < _rows.Count; i++)
                if (!_rows[i].deleted)
                    count++;
            return count;
        }
    }

    public bool IsDirty
    {
        get
        {
            for (int i = 0, len = _rows.Count; i < len; i++)
            {
                var row = _rows[i];
                if (row.committed && !row.deleted) continue;
                return true;
            }

            return false;
        }
    }

    public T this[int id]
    {
        get => GetRow(_pkIndex[id]).data;
        set
        {
            var newData = value;
            var pk = _primaryKeyGetterFn(newData);
            if (pk != id) throw new ConstraintException("Cannot change a primary key.");
            Update(newData);
        }
    }

    private Row<T> GetRow(int index)
    {
        var row = _rows[index];
        if (row.deleted) throw new KeyNotFoundException("Item was deleted.");
        return row;
    }

    private void SetRow(int index, Row<T> row)
    {
        var r = _rows[index];
        if (r.deleted) throw new KeyNotFoundException("Item was deleted.");
        _rows[index] = row;
    }
}