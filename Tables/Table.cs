namespace Tables;


public delegate bool ConstraintDelegate<T>(T item);
public delegate bool PredicateDelegate<T>(T item);
public delegate T ModifyDelegate<T>(T item);
public delegate T OnInsertDelegate<T>(T item);
public delegate T OnUpdateDelegate<T>(T oldItem, T newItem);
public delegate int PrimaryKeyGetterDelegate<T>(T item);
public delegate void OnDeleteDelegate<T>(T item);
public delegate int ForeignKeyGetterDelegate<T>(T item);


public class Table<T>
{
    public OnDeleteDelegate<T> OnDelete = null;
    public OnInsertDelegate<T> OnInsert = null;
    public OnUpdateDelegate<T> OnUpdate = null;

    private readonly List<(TriggerType, string, ConstraintDelegate<T>)> _constraints = new();
    private readonly List<T> _deletedRows = new();
    private readonly List<T> _newRows = new();
    private readonly List<T> _modifiedRows = new();
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
        set
        {
            var row = value;
            if (OnUpdate != null)
                row = OnUpdate.Invoke(_rows[_index[id]], row);
            CheckConstraintsForItem(TriggerType.OnUpdate, row);
            _rows[_index[id]] = row;
        }
    }

    public bool ContainsKey(int key) => _index.ContainsKey(key);
    
    public T Get(int id) => _rows[_index[id]];

    public void Delete(T row)
    {
        OnDelete?.Invoke(row);
        CheckConstraintsForItem(TriggerType.OnDelete, row);
        _deletedRows.Add(row);
    }

    public T Add(T item)
    {
        if (OnInsert != null)
            item = OnInsert(item);
        CheckConstraintsForItem(TriggerType.OnCreate, item);
        CheckConstraintsForItem(TriggerType.OnUpdate, item);
        _newRows.Add(item);
        return item;
    }

    public int Delete(PredicateDelegate<T> predicateFn) => Apply(Delete, predicateFn);

    public void Delete(int id) => Delete(Get(id));

    public void Commit()
    {
        for (var i = 0; i < _newRows.Count; i++)
        {
            var item = _newRows[i];
            var index = _rows.Count;
            _rows.Add(item);
            _index.Add(_primaryKeyGetterFn(item), index);
        }
        _newRows.Clear();
        
        for (var i = 0; i < _modifiedRows.Count; i++)
        {
            var item = _modifiedRows[i];
            var index = _index[_primaryKeyGetterFn(item)];
            _rows[index] = item;
        }
        _modifiedRows.Clear();
        
        for (var i = 0; i < _deletedRows.Count; i++)
        {
            InternalDelete(_primaryKeyGetterFn(_deletedRows[i]));
        }
        _deletedRows.Clear();
    }


    public T Update(T item)
    {
        var index = _primaryKeyGetterFn(item);
        var currentRow = _rows[index];
        if (OnUpdate != null)
            item = OnUpdate(currentRow, item);
        CheckConstraintsForItem(TriggerType.OnUpdate, item);
        _modifiedRows.Add(item);
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
                CheckConstraintsForItem(TriggerType.OnUpdate, newItem);
                _modifiedRows.Add(newItem);
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

    public void Apply(Action<T> applyFn)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            applyFn(item);
        }
    }

    public void Update(ModifyDelegate<T> modifyFn)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var oldItem = _rows[i];
            var newItem = modifyFn(oldItem);
            if (OnUpdate != null)
                newItem = OnUpdate(oldItem, newItem);
            CheckConstraintsForItem(TriggerType.OnUpdate, newItem);
            _modifiedRows.Add(newItem);
        }
    }

    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((triggerType, constraintName, constraintFn));
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