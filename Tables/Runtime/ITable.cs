namespace Tables
{

    public interface ITable
    {
        string Name { get; }
        int RowCount { get; }
        bool IsDirty { get; }
        void Commit();
        void Rollback();
        void Begin();
    }
}