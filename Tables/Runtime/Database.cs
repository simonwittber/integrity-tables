using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Tables
{
    public static class Database
    {
        private static Dictionary<Type, ITable> tables = new();

        public static Table<T> CreateTable<T>(PrimaryKeyGetterDelegate<T> primaryKeyGetterFn, PrimaryKeySetterDelegate<T> primaryKeySetterFn = null) where T:struct
        {
            var table = new Table<T>(primaryKeyGetterFn, primaryKeySetterFn);
            tables.Add(typeof(T), table);
            return table;
        }
        
        public static Table<T> GetTable<T>() where T:struct => (Table<T>)tables[typeof(T)];
        public static ITable GetTable(Type rowType) => tables[rowType];

        public static void DropTables()
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

    }
}