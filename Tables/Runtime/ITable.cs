using System.Collections;

namespace Tables
{

    public interface ITable : IEnumerable
    {
        string Name { get; }
        int RowCount { get; }
        bool IsDirty { get; }
        void Commit();
        void Rollback();
        void Begin();
        bool ContainsKey(int fk);
        
        public BeforeDeleteDelegate BeforeDelete { get; set; }

        public string[] Names { get; }
        public Type[] Types { get; }
        public object Column(object row, int index);
    }
}