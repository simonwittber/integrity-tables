using System.Collections;

namespace Tables;

public partial class Table<T>
{
    /// <summary>
    /// Delete a specific row.
    /// </summary>
    /// <param name="data">The row to delete.</param>
    public void Delete(T data) => _Delete(data);

    /// <summary>
    /// Delete all rows that match a predicate.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <returns>Number of rows deleted.</returns>
    public int Delete(Predicate<T> predicateFn) => Apply(Delete, predicateFn);
    
    /// <summary>
    /// Delete a row specified by id.
    /// </summary>
    /// <param name="id"></param>
    public void Delete(int id) => Delete(Get(id));

    /// <summary>
    /// Get a row specified by id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>The row.</returns>
    public T Get(int id) => GetRow(_pkIndex[id]).data;
    
    /// <summary>
    /// Get a row specified by id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>The row.</returns>
    public T Get(int? id) => GetRow(_pkIndex[id.Value]).data;

    /// <summary>
    /// Add a new row.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>The new row.</returns>
    public T Add(T data) => _Add(data);
    
    /// <summary>
    /// Get a field from a row, specified by index.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="index"></param>
    /// <returns>An object, which is the value of the field.</returns>
    public object GetField(object row, int index) => _fieldIndexer.Get(row, index);

    /// <summary>
    /// Update a row with new data.
    /// </summary>
    /// <param name="newData"></param>
    /// <returns>The modified row.</returns>
    public T Update(T newData) =>_Update(newData);

    /// <summary>
    /// Modify any rows that match a predicate.
    /// </summary>
    /// <param name="modifyFn"></param>
    /// <param name="predicateFn"></param>
    /// <returns>Number of rows modified.</returns>
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

    /// <summary>
    /// Try and fetch a single row that matches a predicate.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <param name="data"></param>
    /// <returns>True if a single row was found.</returns>
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

    /// <summary>
    /// Modify every row.
    /// </summary>
    /// <param name="modifyFn"></param>
    public void Update(ModifyDelegate<T> modifyFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            Update(modifyFn(row.data));
        }
    }

    /// <summary>
    /// Check if a key exists in the table.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if key exists.</returns>
    public bool ContainsKey(int key) => _pkIndex.ContainsKey(key);

    /// <summary>
    /// Run an action for all rows that match a predicate.
    /// </summary>
    /// <param name="applyFn"></param>
    /// <param name="predicateFn"></param>
    /// <returns>Number of rows that matched the predicate.</returns>
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

    /// <summary>
    /// Enumerate over all rows that match a predicate.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <returns></returns>
    public IEnumerable<T> Select(Predicate<T> predicateFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted || !predicateFn(row.data)) continue;
            yield return row.data;
        }
    }

    /// <summary>
    /// Returns true if a predicate matches any row.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Count the number of rows that match a predicate.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <returns>Number of matched rows,</returns>
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

    /// <summary>
    /// Run an action for each row.
    /// </summary>
    /// <param name="applyFn"></param>
    public void Apply(Action<T> applyFn)
    {
        for (int i = 0, len = _rows.Count; i < len; i++)
        {
            var row = _rows[i];
            if (row.deleted) continue;
            applyFn(row.data);
        }
    }
    
    /// <summary>
    /// When a predicate becomes true, trigger an action. Will not retrigger until the predicate becomes false, then true again.
    /// </summary>
    /// <param name="predicateFn"></param>
    /// <param name="actionFn"></param>
    public void When(Predicate<T> predicateFn, RowAction<T> actionFn)
    {
        AfterAdd += item =>
        {
            if (predicateFn(item)) actionFn(item);
        };
        AfterUpdate += (oldItem, newItem) =>
        {
            if (!predicateFn(oldItem) && predicateFn(newItem)) actionFn(newItem);
        };
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (!row.deleted) yield return row.data;
        }
    }
    
    public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
    {
        _constraints.Add(triggerType, constraintName, constraintFn);
    }

    public void AddUniqueConstraint(string indexName, params string[] fieldNames)
    {
        _uniqueIndex.AddConstraint(indexName, fieldNames);
    }

    public void AddRelationshipConstraint(string foreignKeyFieldName, ITable table, CascadeOperation cascadeOperation)
    {
        _constraints.AddRelationship(foreignKeyFieldName, table, cascadeOperation);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}