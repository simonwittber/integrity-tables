
using System;
using IntegrityTables;
using NUnit.Framework;

namespace TestTables;

public class DocumentationTests
{

    public struct TypeOfThing
    {
        public int id;
        public string name;
        
    }
    
    public struct InstanceOfThing
    {
        public int id;
        [ForeignKey("Type", "Things", typeof(TypeOfThing), CascadeOperation.None)]
        public int typeId;

        public int counter;
    }
    
    
    private Table<TypeOfThing> types;
    private Table<InstanceOfThing> instances;
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        db.DropDatabase();
        types = db.CreateTable<TypeOfThing>();
        instances = db.CreateTable<InstanceOfThing>();
    }

    [Test]
    public void TestThings()
    {
        var newType = types.Add(new TypeOfThing() {name = "A Type"});
        Assert.AreEqual(1, newType.id);
        var anotherNewType = types.Add(new TypeOfThing() {name = "Another Type"});
        Assert.AreEqual(2, anotherNewType.id);
        
        var instance = instances.Add(new InstanceOfThing() { typeId = newType.id });

        // iterate over a selection of rows.
        foreach (var i in instances.Select(q => q.typeId == newType.id))
        {
        }

        // call a function for each row.
        instances.Apply(i => Console.WriteLine(i));

        // update each row that matches a predicate.
        instances.Update(i =>
        {
            i.counter++;
            return i;
        }, q => q.typeId == 2);
        
        // attach a trigger which will run when the predicate is true.
        instances.When(q => q.counter >= 2, i => Console.WriteLine(i));

        // try and delete a type... this will fail because a row in the instances table references this row.
        types.Delete(newType);
        
        // delete a row.
        instances.Delete(instance);

        // check if a condition is true for any row in the table.
        if (instances.IsTrue(i => instance.counter > 10))
        {
        }
        
        // start a transaction
        db.Begin();
        instances.Add(new InstanceOfThing() {typeId = anotherNewType.id, counter = 99});
        // at this point, the instances table contains the above row.
        db.Rollback();
        // but after a rollback, it is gone.
        

    }

    
}