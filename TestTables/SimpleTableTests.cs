using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using NUnit.Framework;
using Tables;
using static Tables.Database;

namespace TestTables;

public struct TestRow
{
    public int id;
    public string name;

}

public static class TestRowExtensions {
    public static IEnumerable Fields(this TestRow r)
    {
        yield return r.id;
        yield return r.name;
    }
}

public class SimpleTableTests
{
    [Test]
    public void TestInsertAndGet()
    {
        var table = CreateTable<TestRow>("id");
        table.AddConstraint(TriggerType.OnUpdate,"Name Not Null", i => i.name != null);
        var row = table.Add(new TestRow() { name = "srw" });
        table.Commit();
        Assert.AreEqual(0, (int)row.id);
        var anotherRow = table.Get(0);
        Assert.AreEqual("srw", anotherRow.name);

                
    }
    
    

}