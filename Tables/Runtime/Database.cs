using System.Reflection;
using GetSetGenerator;

namespace Tables
{
    public class Database
    {
        private Dictionary<Type, ITable> tables = new();
        
        public static readonly GetSetCompiler GetSetCompiler = new GetSetCompiler();
        public static readonly ExtensionCompiler IndexerCompiler = new ExtensionCompiler();

        public DatabaseMetadata GetMetadata()
        {
            string TypeName(Type t)
            {
                if (t.IsGenericType && ! t.IsConstructedGenericType)
                {
                    var name = t.Name;
                    return name.Substring(0, name.LastIndexOf('`'));
                }
                if (t.IsConstructedGenericType)
                {
                    var genericArguments = t.GetGenericArguments();
                    var argumentString = string.Join(",", from i in genericArguments select TypeName(i));
                    return $"{TypeName(t.GetGenericTypeDefinition())}<{argumentString}>";
                }

                return t.Name;
            }
            
            var metadata = new DatabaseMetadata();
            foreach (var (tableType, table) in tables)
            {
                var tableMetaData = new TableMetadata();
                tableMetaData.name = TypeName(tableType);
                metadata.tables.Add(tableMetaData);
                var names = table.FieldNames;
                var columnTypes = table.Types;
                for (var i = 0; i < names.Length; i++)
                {
                    var columnMetadata = new ColumnMetadata();
                    columnMetadata.name = names[i];
                    columnMetadata.type = TypeName(columnTypes[i]);
                    var fi = tableType.GetField(columnMetadata.name, BindingFlags.Public | BindingFlags.Instance);
                    var fka = fi.GetCustomAttribute<ForeignKeyAttribute>();
                    if (fka != null)
                    {
                        columnMetadata.relationshipName = fka.relationshipName;
                        if (string.IsNullOrEmpty(fka.inverseRelationshipName))
                        {
                            var cna = tableType.GetCustomAttribute<CollectionNameAttribute>();
                            if (cna == null)
                                columnMetadata.inverseRelationshipName = $"{tableType.Name}Collection";
                            else
                                columnMetadata.inverseRelationshipName = cna.collectionName;
                        }
                        else
                        {
                            columnMetadata.inverseRelationshipName = fka.inverseRelationshipName;
                        }
                        columnMetadata.foreignTable = TypeName(fka.relatedType);
                    }

                    var u = fi.GetCustomAttribute<UniqueAttribute>();
                    if(u != null)
                    {
                        columnMetadata.isUnique = true;
                    }
                    tableMetaData.columns.Add(columnMetadata);
                }
                
            }

            return metadata;
        }

        public Table<T> CreateTable<T>(string primaryKeyFieldName = "id") where T:struct
        {
            if(tables.TryGetValue(typeof(T), out var existingTable))
                return existingTable as Table<T>;
            var getSet = GetSetCompiler.Create<T, int>(primaryKeyFieldName);
            var indexer = IndexerCompiler.Create<T>();
            var table = new Table<T>(indexer, getSet.Get, getSet.Set);
            tables.Add(typeof(T), table);
            SetupTableConstraints(table);
        
            return table;
        }

        public IEnumerable<Type> GetTypes() 
        {
            foreach (var i in tables.Keys) yield return i;
        }

        public Table<T> GetTable<T>() where T:struct => (Table<T>)tables[typeof(T)];
        
        public ITable GetTable(Type rowType) => tables[rowType];
        

        public void DropDatabase()
        {
            tables.Clear();
        }

        public void Begin()
        {
            foreach (var table in tables.Values) table.Begin();
        }

        public void Commit()
        {
            foreach (var table in tables.Values) table.Commit();
        }

        public void Rollback()
        {
            foreach (var table in tables.Values) table.Rollback();
        }

        public ITable[] Tables => tables.Values.ToArray();
        
        
        public T Q<T>(int? id) where T : struct
        {
            return GetTable<T>().Get(id);
        }

        public Table<T> Q<T>() where T : struct
        {
            return GetTable<T>();
        }
        
        public IEnumerable<T> Q<T>(Predicate<T> predicate) where T : struct
        {
            return Q<T>().Select(predicate);
        }

        private void SetupTableConstraints<T>(Table<T> table) where T : struct
        {
            var uniqueConstraints = new Dictionary<string, List<string>>();
            foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fka = fi.GetCustomAttribute<ForeignKeyAttribute>();
                if (fka != null)
                {
                    var foreignKeyFieldName = fi.Name;
                    var foreignTableType = fka.relatedType;
                    var cascadeOperation = fka.cascadeOperation;
                    table.AddRelationshipConstraint(foreignKeyFieldName, GetTable(foreignTableType), cascadeOperation);
                }
                var ua = fi.GetCustomAttribute<UniqueAttribute>();
                if (ua != null)
                {
                    var indexName = ua.indexName ?? fi.Name;
                    if (!uniqueConstraints.TryGetValue(indexName, out var fieldNames))
                        fieldNames = uniqueConstraints[indexName] = new List<string>();
                    fieldNames.Add(fi.Name);
                }
            }

            foreach (var (constraintName, fieldNames) in uniqueConstraints)
            {
                table.AddUniqueConstraint(constraintName, fieldNames.ToArray());
            }
        }

    }
}