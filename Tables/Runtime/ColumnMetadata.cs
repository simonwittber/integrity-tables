namespace IntegrityTables;

[Serializable]
public class ColumnMetadata
{
    public string name;
    public string relationshipName, inverseRelationshipName;
    public string type;
    public string foreignTable;
    public bool isUnique;
}