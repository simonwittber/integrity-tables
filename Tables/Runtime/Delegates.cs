namespace Tables 
{
    public delegate T BeforeAddDelegate<T>(T item);
    public delegate void AfterAddDelegate<T>(T item);
    public delegate void BeforeDeleteDelegate<T>(T item);
    public delegate void AfterDeleteDelegate<T>(T item);
    public delegate T BeforeUpdateDelegate<T>(T oldItem, T newItem);
    public delegate void AfterUpdateDelegate<T>(T item);
    public delegate bool ConstraintDelegate<T>(T item);
    public delegate int? ForeignKeyGetterDelegate<T>(T item);
    public delegate T ForeignKeySetterDelegate<T>(T item, int? fk);
    public delegate T ModifyDelegate<T>(T item);
    public delegate int PrimaryKeyGetterDelegate<T>(T item);
    public delegate T PrimaryKeySetterDelegate<T>(T item);
    public delegate void RowAction<in T>(T item);


}