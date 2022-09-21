using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tables;
using static Tables.Database;

namespace TestTables;

public class RelationalDataTests
{
    
    public struct Emp
    {
        public int id;
        public string name;
        
        public int? department_id;
        
        public int? manager_id;
        public float salary;

        public override string ToString()
        {
            return $"{nameof(id)}: {id}, {nameof(name)}: {name}, {nameof(department_id)}: {department_id}, {nameof(manager_id)}: {manager_id}, {nameof(salary)}: {salary}";
        }
    }

    public struct Dept
    {
        public int id;
        public string name;
        public string location;
    }

    
    private Table<Emp> emp;
    private Table<Dept> dept;
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        db.DropDatabase();
        dept = db.CreateTable<Dept>();

        emp = db.CreateTable<Emp>();
        emp.AddRelationshipConstraint("manager_id", emp, CascadeOperation.SetNull);
        emp.AddRelationshipConstraint("department_id", dept, CascadeOperation.Delete);
    }

    [Test]
    public void TestPKFn()
    {
        db.Begin();
        var e1 = emp.Add(new() {name = "Boris", salary = 1000});
        var e2 = emp.Add(new() {name = "Vlad", salary = 2000});
        var e3 = emp.Add(new() {name = "Simon", salary = 1000, manager_id = e1.id});
        Assert.Throws<ConstraintException>(() =>
        {
            var e4 = emp.Add(new() {name = "Slobodan", salary = 1000, manager_id = e1.id, department_id = 322});
        });
        
        
        
        Assert.AreEqual(0, e1.id);
        Assert.AreEqual(1, e2.id);
        Assert.AreEqual(2, e3.id);
        Assert.IsTrue(emp.TryGet(i => i.manager_id == e1.id, out var result));
        Assert.AreEqual("Simon", result.name);
        Assert.AreEqual(2, emp.Count(i=>i.salary<2000));
        Assert.IsTrue(emp.IsDirty);
        db.Commit();
        Assert.IsFalse(emp.IsDirty);
        var deletedCount = emp.Delete(i => i.salary < 2000);
        Assert.IsTrue(emp.IsDirty);
        Assert.AreEqual(2, deletedCount);
        Assert.Throws<KeyNotFoundException>(() =>
        {
            emp.Get(0);
        });
        var r = emp.Get(1);
        Assert.AreEqual(1, r.id);
        Assert.IsTrue(emp.IsDirty);
        emp.Apply(i => Console.WriteLine(i));
        Assert.AreEqual(0, emp.Count(i=>i.salary<2000));
        db.Commit();
        Assert.AreEqual(0, emp.Count(i=>i.salary<2000));
        
    }

    
}