using System.Collections;

namespace Tables;

public delegate bool ConstraintDelegate<T>(T item);
public delegate bool QueryDelegate<T>(T item);
public delegate T ModifyDelegate<T>(T item);
public delegate T OnInsertDelegate<T>(T item);
public delegate T OnUpdateDelegate<T>(T oldItem, T newItem);
public delegate int PrimaryKeyDelegate<T>(T item);
public delegate void OnDeleteDelegate<T>(T item);

public class Table<T>
{
    
    public OnInsertDelegate<T> OnInsert = null;
    public OnUpdateDelegate<T> OnUpdate = null;
    public OnDeleteDelegate<T> OnDelete = null;

    private readonly List<T> _rows = new();
    private readonly List<int> _deletedRows = new();
    private readonly Dictionary<int, int> _index = new();
    private readonly List<(string, ConstraintDelegate<T>)> _constraints = new();
    readonly PrimaryKeyDelegate<T> _primaryKey;
    public int Count => _rows.Count;
    
    /// <summary>
    /// Create a new Table instance.
    /// </summary>
    /// <param name="primaryKeyFn">A method which returns the unique key for a row.</param>
    public Table(PrimaryKeyDelegate<T> primaryKeyFn)
    {
        this._primaryKey = primaryKeyFn;
    }

    public bool Contains(int key)
    {
        return _index.ContainsKey(key);
    }
    
    /// <summary>
    /// Insert a row into the table. Before inserting, OnInsert is called, and any constraints are checked.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>The row, which may have been modified.</returns>
    public T Insert(T item)
    {
        if (OnInsert != null) 
            item = OnInsert(item);
        var index = _rows.Count;
        CheckConstraintsForItem(item);
        _rows.Add(item);
        _index.Add(_primaryKey(item), index);
        return item;
    }
    
    /// <summary>
    /// Returns the row identified by the unique id. The method for obtaining the unique id is specified in the constructor.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public T Get(int id)
    {
        var index = _index[id];
        return _rows[index];
    }

    /// <summary>
    /// Deletes all rows that match a query function.
    /// </summary>
    /// <param name="queryFn">A function that returns true if the argument should be deleted.</param>
    /// <returns>Number of rows deleted.ß</returns>
    public int DeleteAndFlush(QueryDelegate<T> queryFn)
    {
        var count = Apply(row => Delete(row), queryFn);
        Flush();
        return count;
    }

    /// <summary>
    /// Delete a row. Flush must be called to perform the delete.
    /// </summary>
    /// <param name="row"></param>
    public void Delete(T row) => Delete(_primaryKey(row));

    /// <summary>
    /// Delete a row specified by an id. Flush must be called to perform the delete.
    /// </summary>
    /// <param name="id"></param>
    public void Delete(int id)
    {
        _deletedRows.Add(id);
    }

    /// <summary>
    /// Perform any pending delete operations.
    /// </summary>
    /// <returns>Number of rows deleted.</returns>
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

    /// <summary>
    /// Update a row. Before the update occurs, OnUpdate is called which may modify the row.
    /// </summary>
    /// <param name="item"></param>
    public T Update(T item)
    {
        var index = _primaryKey(item);
        var currentRow = _rows[index];
        if (OnUpdate != null)
            item = OnUpdate(currentRow, item);
        SetItem(index, item);
        return item;
    }

    /// <summary>
    /// Modifies rows which match a query.
    /// </summary>
    /// <param name="modifyFn">A method which modifies a row.</param>
    /// <param name="queryFn">A method that returns true if a row should be modified.</param>
    /// <returns>Number of rows modified.</returns>
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
                SetItem(i, newItem);
            }
        }
        return modifiedCount;
    }
    
    /// <summary>
    /// Applies an action to all rows in the table which match a query.
    /// </summary>
    /// <param name="action">The method to call for each row.</param>
    /// <param name="queryFn">A method which returns true for all rows which should be actioned.</param>
    /// <returns>Number of rows which were actioned.</returns>
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
    
    /// <summary>
    /// Applies an action to all rows in the table.
    /// </summary>
    /// <param name="action">The method to call for each row.</param>
    /// <returns>Number of rows that were actioned.</returns>
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
    
    /// <summary>
    /// Modifies every row in the table.
    /// </summary>
    /// <param name="modifyFn">A method which takes an row as an argument and returns a modified row.</param>
    /// <returns>Number of rows modified.</returns>
    public int Update(ModifyDelegate<T> modifyFn)
    {
        var count = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            var item = _rows[i];
            var newItem = modifyFn(item);
            if (OnUpdate != null)
                newItem = OnUpdate(item, newItem);
            SetItem(i, newItem);
            count++;
        }
        return count;
    }
    
    /// <summary>
    /// Add a constraint function which is checked before a row is inserted or updated.
    /// </summary>
    /// <param name="constraintName">The name of the constraint.</param>
    /// <param name="constraintFn">A method which returns false if the row should not be updated or inserted.</param>
    public void AddConstraint(string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add((constraintName, constraintFn));
    }
    
    void SetItem(int index, T item)
    {
        CheckConstraintsForItem(item);
        _rows[index] = item;
    }

    private void CheckConstraintsForItem(T item)
    {
        for (var i = 0; i < _constraints.Count; i++)
        {
            var (constraintName, constraintFn) = _constraints[i];
            if (!constraintFn.Invoke(item))
                throw new CheckConstraintException(constraintName);
        }
    }

    /// <summary>
    /// Get and set a row using the unique id.
    /// </summary>
    /// <param name="id"></param>
    public T this[int id]
    {
        get => _rows[_index[id]];
        set => SetItem(_index[id], value);
    }

    public void AddRelation<TR>(Table<TR> otherTable, Func<T, int> func)
    {
        
    }
}

