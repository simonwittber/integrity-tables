using System;
using GetSetGenerator;
using NUnit.Framework;

namespace TestTables;

public class FieldIndexerTests
{
    public struct SomeStruct
    {
        public int a, b;
        public string c, d;
        public float e, f;
        public int? xyz;
    }

    [Test]
    public void TestIndexer()
    {
        var compiler = new ExtensionCompiler();
        var indexer = compiler.Create<SomeStruct>();
        var row = new SomeStruct() {a = 1, b = 2, c = "3", d = "4", e = 5f, f = 6f};
        
        Assert.NotNull(indexer);
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            indexer.Get(row, 13);
        });
        
        var fieldValue1 = (int)indexer.Get(row, 0);
        Assert.AreEqual(row.a, fieldValue1);
        var fieldValue2 = (string)indexer.Get(row, 2);
        Assert.AreEqual(row.c, fieldValue2);

        var types = indexer.Types();
        Assert.AreEqual(7, types.Length);
        Assert.AreEqual(typeof(float), types[4]);
        Assert.AreEqual(typeof(int?), types[6]);

        var names = indexer.Names();
        Assert.AreEqual("xyz", names[6]);
    }
    
}