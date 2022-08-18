namespace Tables;

public delegate T ForeignKeySetterDelegate<T>(ref T item, int? fk);