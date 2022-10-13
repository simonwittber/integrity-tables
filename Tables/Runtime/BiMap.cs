namespace IntegrityTables;

internal class BiMap<T1, T2>
{
    public int[] fieldIndexes;
    
    private Dictionary<T1, T2> mapA = new Dictionary<T1, T2>();
    private Dictionary<T2, T1> mapB = new Dictionary<T2, T1>();

    private List<(T1, T2)> _deleted = new();
    private List<(T1, T2)> _added = new();

    public BiMap(int[] fieldIndexes)
    {
        this.fieldIndexes = fieldIndexes;
    }

    public bool ContainsKey(T1 key) => mapA.ContainsKey(key);

    public void Remove(T1 keyA)
    {
        var keyB = mapA[keyA];
        mapA.Remove(keyA);
        mapB.Remove(keyB);
        _deleted.Add((keyA, keyB));
    }

    public void Add(T1 keyA, T2 keyB)
    {
        mapA.Add(keyA, keyB);
        mapB.Add(keyB, keyA);
        _added.Add((keyA, keyB));
    }

    public void Remove(T2 keyB)
    {
        var keyA = mapB[keyB];
        mapA.Remove(keyA);
        mapB.Remove(keyB);
        _deleted.Add((keyA, keyB));
    }

    public void Commit()
    {
        _added.Clear();
        _deleted.Clear();
    }

    public void Rollback()
    {
        foreach (var (keyA, keyB) in _added)
        {
            mapA.Remove(keyA);
            mapB.Remove(keyB);
        }
        _added.Clear();
        foreach (var (keyA, keyB) in _deleted)
        {
            mapA.Add(keyA, keyB);
            mapB.Add(keyB, keyA);
        }
        _deleted.Clear();
    }

    public void Begin()
    {
        if (_added.Count > 0 || _deleted.Count > 0)
            throw new System.Exception("Index has not been committed or rolled back.");
    }

    public bool TryGet(T1 keyA, out T2 keyB)
    {
        keyB = default;
        if (mapA.TryGetValue(keyA, out keyB))
            return true;
        return false;
    }
}