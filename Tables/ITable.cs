namespace Tables;

public interface ITable
{
    int RowCount { get; }
    bool IsDirty { get; }
    void Commit();
    void Rollback();
    void Begin();
}