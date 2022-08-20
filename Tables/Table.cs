using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Tables
{

    public class Table<T> : ITable
    {
        public OnDeleteDelegate<T> OnDelete = null;
        public OnInsertDelegate<T> OnInsert = null;
        public OnUpdateDelegate<T> OnUpdate = null;

        private readonly List<(TriggerType, string, ConstraintDelegate<T>)> _constraints = new();
        private readonly Dictionary<int, T> _deletedRows = new();
        private readonly List<int> _newRows = new();
        private readonly Dictionary<int, T> _modifiedRows = new();
        private readonly Dictionary<int, int> _index = new();
        private readonly PrimaryKeyGetterDelegate<T> _primaryKeyGetterFn;
        private readonly PrimaryKeySetterDelegate<T> _primaryKeySetterFn;
        private readonly List<Row<T>> _rows = new();

        public int RowCount => _rows.Count;

        public Table(PrimaryKeyGetterDelegate<T> primaryKeyGetterFn, PrimaryKeySetterDelegate<T> primaryKeySetterFn = null)
        {
            _primaryKeyGetterFn = primaryKeyGetterFn;
            _primaryKeySetterFn = primaryKeySetterFn;
        }

        public T this[int id]
        {
            get => GetRow(_index[id]).data;
            set
            {
                var newData = value;
                var row = GetRow(_index[id]);
                if (OnUpdate != null)
                    newData = OnUpdate.Invoke(row.data, newData);
                CheckConstraintsForItem(TriggerType.OnUpdate, newData);
                row.data = newData;
                SetRow(_index[id], row);
            }
        }

        Row<T> GetRow(int index)
        {
            var row = _rows[index];
            if (row.deleted)
                throw new IndexOutOfRangeException("Item was deleted.");
            return row;
        }
        
        void SetRow(int index, Row<T> row)
        {
            var r = _rows[index];
            if (r.deleted)
                throw new IndexOutOfRangeException("Item was deleted.");
            _rows[index] = row;
        }

        public bool IsDirty
        {
            get
            {
                for (int i = 0, len = _rows.Count; i < len; i++)
                {
                    var row = _rows[i];
                    if (!row.committed) return true;
                }
                return false;
            }
        }

        public void Begin()
        {
            if (IsDirty)
            {
                throw new Exception("Cannot start a transaction on a dirty table.");
            }
        }

        public bool ContainsKey(int key) => _index.ContainsKey(key);

        public T Get(int id) => GetRow(_index[id]).data;

        public void Delete(T data)
        {
            OnDelete?.Invoke(data);
            CheckConstraintsForItem(TriggerType.OnDelete, data);
            var pk = _primaryKeyGetterFn(data);
            var index = _index[pk];
            _deletedRows.TryAdd(pk, data);
            var row = _rows[index];
            row.deleted = true;
            _rows[index] = row;
        }

        public T Add(T data)
        {
            if (_primaryKeySetterFn != null)
                data = _primaryKeySetterFn(data);
            if (OnInsert != null)
                data = OnInsert(data);
            CheckConstraintsForItem(TriggerType.OnCreate, data);
            CheckConstraintsForItem(TriggerType.OnUpdate, data);
            var pk = InternalAdd(data);
            _newRows.Add(pk);
            return data;
        }

        private int InternalAdd(T data)
        {
            var index = _rows.Count;
            var pk = _primaryKeyGetterFn(data);
            _rows.Add(new Row<T>() {data = data, committed = false, deleted = false});
            _index.Add(pk, index);
            return pk;
        }

        public int Delete(Predicate<T> predicateFn) => Apply(Delete, predicateFn);

        public void Delete(int id) => Delete(Get(id));

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
            
            foreach(var (pk, item) in _deletedRows)
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

        public T Update(T newData)
        {
            var pk = _primaryKeyGetterFn(newData);
            var index = _index[pk];
            var currentRow = GetRow(index);
            var oldData = currentRow.data;
            if (OnUpdate != null)
                newData = OnUpdate(oldData, newData);
            CheckConstraintsForItem(TriggerType.OnUpdate, newData);
            currentRow.data = newData;
            currentRow.committed = false;
            SetRow(index, currentRow);
            //Try and add the old data in case of rollback. Consecutive updates will be ignored.
            _modifiedRows.TryAdd(pk, oldData);
            return newData;
        }


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

        public IEnumerable<T> Select(Predicate<T> predicateFn)
        {
            for (int i = 0, len = _rows.Count; i < len; i++)
            {
                var row = _rows[i];
                if (row.deleted || !predicateFn(row.data)) continue;
                yield return row.data;
            }
        }

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


        public bool IsTrue(Predicate<T> predicateFn)
        {
            for (int i = 0, len = _rows.Count; i < len; i++)
            {
                var row = _rows[i];
                if(row.deleted) continue;
                if (predicateFn(row.data)) return true;
            }

            return false;
        }


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

        public void Apply(Action<T> applyFn)
        {
            for (int i = 0, len = _rows.Count; i < len; i++)
            {
                var row = _rows[i];
                if(row.deleted) continue;
                applyFn(row.data);
            }
        }

        public void Update(ModifyDelegate<T> modifyFn)
        {
            for (int i = 0, len = _rows.Count; i < len; i++)
            {
                var row = _rows[i];
                if (row.deleted) continue;
                Update(modifyFn(row.data));
            }
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

        public void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn)
        {
            _constraints.Add((triggerType, constraintName, constraintFn));
        }

        public void AddRelationshipConstraint<TR>(string constraintName, ForeignKeyGetterDelegate<T> getForeignKeyFn, Table<TR> otherTable, bool cascadeDelete = false)
        {
            AddConstraint(TriggerType.OnUpdate, constraintName, row =>
            {
                var fk = getForeignKeyFn(row);
                if (!fk.HasValue) return true;
                return otherTable.ContainsKey(fk.Value);
            });

            if (cascadeDelete)
            {
                otherTable.OnDelete += item =>
                {
                    var fk = otherTable._primaryKeyGetterFn(item);
                    Delete(i => getForeignKeyFn(i) == fk);
                };
            }

            otherTable.AddConstraint(TriggerType.OnDelete, constraintName, otherRow =>
            {
                var fk = otherTable._primaryKeyGetterFn(otherRow);
                return !IsTrue(row => getForeignKeyFn(row) == fk);
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
    }
}