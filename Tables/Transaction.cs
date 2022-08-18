namespace Tables;

public struct Transaction
{
    private ITable[] tables;

    public Transaction(params ITable[] tables)
    {
        this.tables = tables;
    }

    public void Begin()
    {
        foreach(var table in tables) table.Begin();
    }

    public void Commit()
    {
        foreach(var table in tables) table.Commit();
    }

    public void Rollback()
    {
        foreach(var table in tables) table.Rollback();
    }

}