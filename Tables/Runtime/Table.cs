using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Reflection;
using GetSetGenerator;

namespace Tables
{
    public partial class Table<T> : ITable, IEnumerable<T> where T:struct
    {
        
        private readonly Dictionary<int, T> _deletedRows = new();
        private readonly List<int> _newRows = new();
        private readonly Dictionary<int, T> _modifiedRows = new();
        private readonly Dictionary<int, int> _pkIndex = new();

        internal readonly PrimaryKeySetterDelegate<T> SetPrimaryKey;
        internal readonly PrimaryKeyGetterDelegate<T> GetPrimaryKey;
        private IFieldIndexer _fieldIndexer;
        
        private readonly List<Row<T>> _rows = new();

        private readonly Index<T> _index;
        private readonly ConstraintCollection<T> _constraints;

        private int _idCount = 0;

        internal Table(IFieldIndexer fieldIndexer, PrimaryKeyGetterDelegate<T> getPrimaryKey, PrimaryKeySetterDelegate<T> setPrimaryKey = null)
        {
            Name = typeof(T).Name;
            _fieldIndexer = fieldIndexer;
            GetPrimaryKey = getPrimaryKey;
            SetPrimaryKey = setPrimaryKey;
            _index = new Index<T>(this);
            _constraints = new ConstraintCollection<T>(this);
        }

    }
}