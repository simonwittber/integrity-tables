namespace Tables;


public delegate bool ConstraintDelegate<T>(T item);
public delegate bool PredicateDelegate<T>(T item);
public delegate T ModifyDelegate<T>(T item);
public delegate T OnInsertDelegate<T>(T item);
public delegate T OnUpdateDelegate<T>(T oldItem, T newItem);
public delegate int PrimaryKeyGetterDelegate<T>(T item);
public delegate void OnDeleteDelegate<T>(T item);
public delegate int ForeignKeyGetterDelegate<T>(T item);


public interface ITable<T>
{
    T this[int id] { get; set; }
    int RowCount { get; }
    
    T Add(T item);
    void AddConstraint(TriggerType triggerType, string constraintName, ConstraintDelegate<T> constraintFn);
    void AddRelationshipConstraint<TR>(string constraintName, ForeignKeyGetterDelegate<T> foreignKeyGetterFn, Table<TR> otherTable);
    int Apply(Action<T> applyFn);
    int Apply(Action<T> applyFn, PredicateDelegate<T> predicateFn);
    bool ContainsKey(int key);
    int Count(PredicateDelegate<T> predicateFn);
    void Delete(int id);
    void Delete(T row);
    int DeleteAndFlush(PredicateDelegate<T> predicateFn);
    int FlushDeleteQueue();
    T Get(int id);
    bool IsTrue(PredicateDelegate<T> predicateFn);
    T Update(T item);
    int Update(ModifyDelegate<T> modifyFn);
    int Update(ModifyDelegate<T> modifyFn, PredicateDelegate<T> predicateFn);
}