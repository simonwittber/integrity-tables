namespace Tables;


public class Database
{
    internal ITable[] tables;

    public Database(params ITable[] tables)
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