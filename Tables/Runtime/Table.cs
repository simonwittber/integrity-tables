using GetSetGenerator;

namespace IntegrityTables
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

        private readonly UniqueIndex<T> _uniqueIndex;
        private readonly ConstraintCollection<T> _constraints;

        private int _idCount = 0;

        internal Table(IFieldIndexer fieldIndexer, PrimaryKeyGetterDelegate<T> getPrimaryKey, PrimaryKeySetterDelegate<T> setPrimaryKey = null)
        {
            Name = typeof(T).Name;
            _fieldIndexer = fieldIndexer;
            GetPrimaryKey = getPrimaryKey;
            SetPrimaryKey = setPrimaryKey;
            _uniqueIndex = new UniqueIndex<T>(this);
            _constraints = new ConstraintCollection<T>(this);
        }

    }
}