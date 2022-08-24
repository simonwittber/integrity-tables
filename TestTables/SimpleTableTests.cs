using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tables;
using static Tables.Database;

namespace TestTables;

public struct TestRow
{
    public int id;
    public string name;

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