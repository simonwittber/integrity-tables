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
        internal readonly PrimaryKeyGetterDelegate<T> _primaryKeyGetterFn;
        private readonly PrimaryKeySetterDelegate<T> _primaryKeySetterFn;
        private readonly List<Row<T>> _rows = new();

        private readonly Index<T> _index;
        private readonly ConstraintCollection<T> _constraints;

        private int id_count = 0;

        internal IFieldIndexer indexer;

        internal Table(PrimaryKeyGetterDelegate<T> primaryKeyGetterFn, PrimaryKeySetterDelegate<T> primaryKeySetterFn = null)
        {
            Name = typeof(T).Name;
            _primaryKeyGetterFn = primaryKeyGetterFn;
            _primaryKeySetterFn = primaryKeySetterFn;
            _index = new Index<T>(this);
            _constraints = new ConstraintCollection<T>(this);
        }

        public string[] Names => indexer.Names();
        
        public Type[] Types => indexer.Types();
        public object Column(object row, int index) => indexer.Get(row, index);
        
    }
}