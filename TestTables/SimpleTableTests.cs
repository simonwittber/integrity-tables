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
        var table = CreateTable<TestRow>(i=>i.id);
        table.AddConstraint(TriggerType.OnUpdate,"Name Not Null", i => i.name != null);
        var row = table.Add(new TestRow() { id=23, name = "srw" });
        table.Commit();
        Assert.AreEqual(23, (int)row.id);
        var anotherRow = table.Get(23);
        Assert.AreEqual("srw", anotherRow.name);
        
        
    }
    
    

}