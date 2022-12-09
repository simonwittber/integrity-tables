namespace IntegrityTables 
{
    public delegate T BeforeAddDelegate<T>(T item);
    public delegate void AfterAddDelegate<T>(T item);
    public delegate void BeforeDeleteDelegate(int id);
    public delegate void AfterDeleteDelegate<T>(T item);
    public delegate T BeforeUpdateDelegate<T>(T oldItem, T newItem);
    public delegate void AfterUpdateDelegate<T>(T item, T newItem);
    public delegate bool ConstraintDelegate<T>(T oldItem, T newItem);
    public delegate int? WeakKeyGetterDelegate<T>(T item);
    public delegate T WeakKeySetterDelegate<T>(T item, int? fk);
    public delegate int StrongKeyGetterDelegate<T>(T item);
    public delegate T StrongKeySetterDelegate<T>(T item, int fk);
    public delegate T ModifyDelegate<T>(T item);
    public delegate int PrimaryKeyGetterDelegate<T>(T item);
    public delegate T PrimaryKeySetterDelegate<T>(T item, int id);
    public delegate void RowAction<in T>(T item);

}