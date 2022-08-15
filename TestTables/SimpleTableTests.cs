using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NUnit.Framework;
using Tables;

namespace TestTables;

public class SimpleTableTests
{
    public struct TestRow
    {
        public int id;
        public string name;
    }
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestInsertAndGet()
    {
        var table = new Table<TestRow>(i=>i.id);
        table.AddConstraint(TriggerType.OnUpdate,"Name Not Null", i => i.name != null);
        var row = table.Insert(new TestRow() { id=23, name = "srw" });
        Assert.AreEqual(23, row.id);
        var anotherRow = table.Get(23);
        Assert.AreEqual("srw", anotherRow.name);
    }
    
    [Test]
    public void TestInsertTrigger()
    {
        var table = new Table<TestRow>(i=>i.id);
        table.OnInsert = item =>
        {
            item.name = "haha";
            return item;
        };
        
        var row = table.Insert(new TestRow() { id=23, name = "srw" });
        Assert.AreEqual("haha", row.name);
    }
    
    [Test]
    public void TestInsertTriggerFailsConstraint()
    {
        var table = new Table<TestRow>(i=>i.id);
        table.AddConstraint(TriggerType.OnUpdate,"NotNullName", row => row.name != null);
        table.OnInsert = item =>
        {
            item.name = null;
            return item;
        };
        Assert.Throws<ConstraintException>(() =>
        {
            table.Insert(new TestRow() { id=23, name = "srw" });
        });

    }
    
    [Test]
    public void TestDelete()
    {
        var table = new Table<TestRow>(i=>i.id);
        table.Insert(new TestRow() { id=23, name = "srw" });
        table.Insert(new TestRow() { id=22, name = "srw" });
        table.Delete(23);
        table.Flush();
        Assert.Throws<KeyNotFoundException>(() => table.Get(23));
    }
    
    [Test]
    public void TestConstraints()
    {
        var table = new Table<TestRow>(i=>i.id);
        table.AddConstraint(TriggerType.OnUpdate,"Name Not Null", i => i.name != null);
        Assert.Throws<ConstraintException>(() =>
        {
            var row = table.Insert(new TestRow() {id = 23, name = null});
        });
    }
    
    struct Person
    {
        public int id;
        public string name;
        public int location_id;
    }

    struct Location
    {
        public int id;
        public string name;
    }

    [Test]
    public void TestRelations()
    {
        var persons = new Table<Person>(item => item.id);
        var locations = new Table<Location>(item => item.id);
        //persons.AddConstraint("location exists", person => locations.Contains(person.location_id));
        persons.AddRelation("person_fk", i=>i.location_id, locations);
        locations.Insert(new Location() {id = 1, name = "Here"});
        Assert.Throws<ConstraintException>(() =>
        {
            persons.Insert(new Person() {id = 0, location_id = 0, name = "Simon"});
        });
        locations.Insert(new Location() {id = 0, name = "There"});
        persons.Insert(new Person() {id = 0, location_id = 0, name = "Simon"});
        Assert.Throws<ConstraintException>(() =>
        {
            locations.Delete(0);
        });
    }
}