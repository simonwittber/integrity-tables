namespace Tables;

public partial class Table<T>
{
    public BeforeDeleteDelegate BeforeDelete { get; set; } = null;
    public AfterDeleteDelegate<T> AfterDelete = null;
    public BeforeAddDelegate<T> BeforeAdd = null;
    public AfterAddDelegate<T> AfterAdd = null;
    public BeforeUpdateDelegate<T> BeforeUpdate = null;
    public AfterUpdateDelegate<T> AfterUpdate = null;
}