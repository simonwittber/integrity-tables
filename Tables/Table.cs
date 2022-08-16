namespace Tables;

public class Table<T> : ITable<T>
{
    public OnDeleteDelegate<T> OnDelete = null;
    public OnInsertDelegate<T> OnInsert = null;
    public OnUpdateDelegate<T> OnUpdate = null;

    private readonly List<(TriggerType, string, ConstraintDelegate<T>)> _constraints = new();
    private readonly List<int> _deletedRows = new();
    private readonly Dictionary<int, int> _index = new();
    private readonly PrimaryKeyGetterDelegate<T> _primaryKeyGetterFn;
    private readonly List<T> _rows = new();

    public int RowCount => _rows.Count;
    
    public Table(PrimaryKeyGetterDelegate<T> primaryKeyGetterFn)
    {
        _primaryKeyGetterFn = primaryKeyGetterFn;
    }

    public T this[int id]
    {
        get => _rows[_index[id]];
        set => SetItem(TriggerType.OnUpdate, _index[id], value);
    }

    public bool ContainsKey(int key) => _index.ContainsKey(key);
    
    public T Get(int id) => _rows[_index[id]];
    
    public void Delete(T row) => Delete(_primaryKeyGetterFn(row));

    public T Add(T item)
    {
        if (OnInsert != null)
            item = OnInsert(item);
        var index = _rows.Count;
        CheckConstraintsForItem(TriggerType.OnCreate, item);
        CheckConstraintsForItem(TriggerType.OnUpdate, item);
        _rows.Add(item);
        _index.Add(_primaryKeyGetterFn(item), index);
        return item;
    }

    public int DeleteAndFlush(PredicateDelegate<T> predicateFn)
    {
        var count = Apply(row => Delete(row), predicateFn);
        FlushDeleteQueue();
        return count;
    }

    public void Delete(int id)
    {
        var item = Get(id);
        CheckConstraintsForItem(TriggerType.OnDelete, item);
        _deletedRows.Add(id);
    }

    public int FlushDeleteQueue()
    {
        var count = 0;
        for (var i = 0; i < _deletedRows.Count; i++)
        {
            InternalDelete(_deletedRows[i]);
            count++;
        }

        _deletedRows.Clear();
        return count;
    }


    public T Update(T item)
    {
        var index = _primaryKeyGetterFn(item);
        var currentRow = _rows[index];
        if (OnUpdate != null)
            item = OnUpdate(currentRow, item);
        SetItem(TriggerType.OnUpdate, index, item);
        return item;
    }

    public int Update(ModifyDelegate<T> modifyFn, PredicateDelegate<T> predicateFn)
    {
        var modifiedCount = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (predicateFn(item))
            {
                var newItem = modifyFn(item);
                if (OnUpdate != null)
                    newItem = OnUpdate(item, newItem);
                SetItem(TriggerType.OnUpdate, i, newItem);
            }
        }

        return modifiedCount;
    }

    public int Apply(Action<T> applyFn, PredicateDelegate<T> predicateFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (!predicateFn(item)) continue;
            applyFn(item);
            count++;
        }

        return count;
    }


    public bool IsTrue(PredicateDelegate<T> predicateFn)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (predicateFn(item)) return true;
        }

        return false;
    }

    
    public int Count(PredicateDelegate<T> predicateFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (predicateFn(item)) count++;
        }

        return count;
    }

    public int Apply(Action<T> applyFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            count++;
            applyFn(item);
        }

        return count;
    }

    public int Update(ModifyDelegate<T> modifyFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            var newItem = modifyFn(item);
            if (OnUpdate != null)
                newItem = OnUpdate(item, newItem);
            SetItem(TriggerType.OnUpdate, i, newItem);
            count++;
        }

        return count;
    }

    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((triggerType, constraintName, constraintFn));
    }


    private void SetItem(TriggerType triggerType, int index, T item)
    {
        CheckConstraintsForItem(triggerType, item);
        _rows[index] = item;
    }

    public void AddRelationshipConstraint<TR>(string constraintName, ForeignKeyGetterDelegate<T> foreignKeyFn, Table<TR> otherTable)
    {
        AddConstraint(TriggerType.OnUpdate, constraintName, row => otherTable.ContainsKey(foreignKeyFn(row)));
        otherTable.AddConstraint(TriggerType.OnDelete, constraintName, otherRow =>
        {
            var fk = otherTable._primaryKeyGetterFn(otherRow);
            return !IsTrue(row => foreignKeyFn(row) == fk);
        });
    }

    private void CheckConstraintsForItem(TriggerType triggerType, T item)
    {
        for (var i = 0; i < _constraints.Count; i++)
        {
            var (type, constraintName, constraintFn) = _constraints[i];
            if (type == triggerType && !constraintFn.Invoke(item))
                throw new ConstraintException(constraintName);
        }
    }

    private void InternalDelete(int id)
    {
        var index = _index[id];
        var deletedRow = _rows[index];
        var lastIndex = _rows.Count - 1;
        var lastRow = _rows[lastIndex];
        _rows[index] = lastRow;
        _index[_primaryKeyGetterFn(lastRow)] = index;
        _index.Remove(id);
        _rows.RemoveAt(lastIndex);
        OnDelete?.Invoke(deletedRow);
    }
}