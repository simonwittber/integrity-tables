using System.Collections;
using System.Reflection.Metadata;

namespace Tables;

public delegate bool ConstraintDelegate<T>(T item);
public delegate bool QueryDelegate<T>(T item);
public delegate T ModifyDelegate<T>(T item);
public delegate T OnInsertDelegate<T>(T item);
public delegate T OnUpdateDelegate<T>(T oldItem, T newItem);
public delegate int PrimaryKeyDelegate<T>(T item);
public delegate void OnDeleteDelegate<T>(T item);

public enum TriggerType
{
    OnCreate,
    OnUpdate,
    OnDelete
}

public class Table<T>
{
    
    public OnInsertDelegate<T> OnInsert = null;
    public OnUpdateDelegate<T> OnUpdate = null;
    public OnDeleteDelegate<T> OnDelete = null;

    private readonly List<T> _rows = new();
    private readonly List<int> _deletedRows = new();
    private readonly Dictionary<int, int> _index = new();
    private readonly List<(TriggerType, string, ConstraintDelegate<T>)> _constraints = new();
    readonly PrimaryKeyDelegate<T> _primaryKey;
    public int RowCount => _rows.Count;
    
    public Table(PrimaryKeyDelegate<T> primaryKeyFn)
    {
        this._primaryKey = primaryKeyFn;
    }

    public bool Contains(int key)
    {
        return _index.ContainsKey(key);
    }
    
    public T Insert(T item)
    {
        if (OnInsert != null) 
            item = OnInsert(item);
        var index = _rows.Count;
        CheckConstraintsForItem(TriggerType.OnCreate, item);
        CheckConstraintsForItem(TriggerType.OnUpdate, item);
        _rows.Add(item);
        _index.Add(_primaryKey(item), index);
        return item;
    }
    
    public T Get(int id)
    {
        var index = _index[id];
        return _rows[index];
    }

    public int DeleteAndFlush(QueryDelegate<T> queryFn)
    {
        var count = Apply(row => Delete(row), queryFn);
        Flush();
        return count;
    }

    public void Delete(T row) => Delete(_primaryKey(row));

    public void Delete(int id)
    {
        var item = Get(id);
        CheckConstraintsForItem(TriggerType.OnDelete, item);
        _deletedRows.Add(id);
    }

    public int Flush()
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
    
    void InternalDelete(int id)
    {
        var index = _index[id];
        var deletedRow = _rows[index];
        var lastIndex = _rows.Count - 1;
        var lastRow = _rows[lastIndex];
        _rows[index] = lastRow;
        _index[_primaryKey(lastRow)] = index;
        _index.Remove(id);
        _rows.RemoveAt(lastIndex);
        OnDelete?.Invoke(deletedRow);
    }

    public T Update(T item)
    {
        var index = _primaryKey(item);
        var currentRow = _rows[index];
        if (OnUpdate != null)
            item = OnUpdate(currentRow, item);
        SetItem(TriggerType.OnUpdate, index, item);
        return item;
    }

    public int Update(ModifyDelegate<T> modifyFn, QueryDelegate<T> queryFn)
    {
        var modifiedCount = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (queryFn(item))
            {
                var newItem = modifyFn(item);
                if (OnUpdate != null)
                    newItem = OnUpdate(item, newItem);
                SetItem(TriggerType.OnUpdate, i, newItem);
            }
        }
        return modifiedCount;
    }
    
    public int Apply(System.Action<T> action, QueryDelegate<T> queryFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (!queryFn(item)) continue;
            action(item);
            count++;
        }
        return count;
    }
    
    public bool Exists(QueryDelegate<T> queryFn)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (queryFn(item)) return true;
        }
        return false;
    }
    
    public int Count(QueryDelegate<T> queryFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            if (queryFn(item)) count ++;
        }
        return count;
    }
    
    public int Apply(System.Action<T> action)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            count++;
            action(item);
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
            SetItem(TriggerType.OnUpdate,i, newItem);
            count++;
        }
        return count;
    }
    
    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((triggerType, constraintName, constraintFn));
    }
    
    
    void SetItem(TriggerType triggerType, int index, T item)
    {
        CheckConstraintsForItem(triggerType, item);
        _rows[index] = item;
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

    public T this[int id]
    {
        get => _rows[_index[id]];
        set => SetItem(TriggerType.OnUpdate, _index[id], value);
    }
    
    public void AddRelation<TR>(string name, Func<T, int> getForeignKeyFn, Table<TR> otherTable)
    {
        AddConstraint(TriggerType.OnUpdate, name, row => otherTable.Contains(getForeignKeyFn(row)));
        otherTable.AddConstraint(TriggerType.OnDelete, name, otherRow =>
        {
            var fk = otherTable._primaryKey(otherRow);
            return !Exists(row => getForeignKeyFn(row) == fk);
        });
    }
}

