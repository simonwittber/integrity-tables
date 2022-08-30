using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GetSetGenerator;

namespace Tables
{
    public static class Database
    {
        private static Dictionary<Type, ITable> tables = new();
        
        public static readonly GetSetCompiler Compiler = new GetSetCompiler();

        public static Table<T> CreateTable<T>(string primaryKeyFieldName = "id") where T:struct
        {
            if(tables.TryGetValue(typeof(T), out var existingTable))
                return existingTable as Table<T>;
            var getSet = Compiler.Create<T, int>(primaryKeyFieldName);
            var table = new Table<T>(getSet.Get, getSet.Set);
            tables.Add(typeof(T), table);
            InspectTable(table);
        
            return table;
        }

        private static void InspectTable<T>(Table<T> table) where T : struct
        {
            foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fka = fi.GetCustomAttribute<ForeignKeyAttribute>();
                if (fka != null)
                {
                    var foreignKeyFieldName = fi.Name;
                    var foreignTableType = fka.relatedType;
                    var cascadeOperation = fka.cascadeOperation;
                    table.AddRelationshipConstraint(foreignKeyFieldName, GetTable(foreignTableType), cascadeOperation, fka.isNullable);
                }
            }
        }

        public static Table<T> GetTable<T>() where T:struct => (Table<T>)tables[typeof(T)];
        public static ITable GetTable(Type rowType) => tables[rowType];
        

        public static void DropDatabase()
        {
            tables.Clear();
        }

        public static void Begin()
        {
            foreach (var table in tables.Values) table.Begin();
        }

        public static void Commit()
        {
            foreach (var table in tables.Values) table.Commit();
        }

        public static void Rollback()
        {
            foreach (var table in tables.Values) table.Rollback();
        }

        public static ITable[] Tables => tables.Values.ToArray();
        
        
        public static T Q<T>(int? id) where T : struct
        {
            return GetTable<T>().Get(id);
        }

        public static Table<T> Q<T>() where T : struct
        {
            return GetTable<T>();
        }
        
        public static IEnumerable<T> Q<T>(Predicate<T> predicate) where T : struct
        {
            return Q<T>().Select(predicate);
        }

    }
}