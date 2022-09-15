namespace Tables;

[Serializable]
public class ColumnMetadata
{
    public string name;
    public string type;
    public string foreignTable;
    public bool isUnique;
}