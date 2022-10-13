namespace IntegrityTables;

[Serializable]
public class TableMetadata
{
    public string name;
    public List<ColumnMetadata> columns = new List<ColumnMetadata>();
}