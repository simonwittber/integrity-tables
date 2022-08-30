using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tables;
using static Tables.Database;
using ConstraintException = Tables.ConstraintException;

namespace TestTables;

public struct Employee
{
    public int id;
    public string name;
    [ForeignKey(typeof(Department), CascadeOperation.Delete, isNullable:true)] public int? department_id;
    public int? version;
    [ForeignKey(typeof(Employee), CascadeOperation.SetNull, isNullable:true)] public int? manager_id;
}

public struct Department
{
    public int id;
    [ForeignKey(typeof(Location), CascadeOperation.None, isNullable:true)]
    public int? location_id;
    public string name;
}

public struct Location
{
    public int id;
    [Unique]
    public string name;
}

public class MoreComplexTests
{
    

    private Table<Employee> emp;
    private Table<Department> dept;
    private Table<Location> location;

    [SetUp]
    public void Setup()
    {
        DropDatabase();
        CreateTable<Location>();
        dept = CreateTable<Department>("id");
        emp = CreateTable<Employee>("id");
        location = CreateTable<Location>();
    }

    [Test]
    public void FKAddTest()
    {
        var s = emp.Add(new Employee() {name = "S"});
        var c1 = emp.Add(new Employee() {manager_id = s.id, name = "C1"});
        
        Assert.Throws<ConstraintException>(() =>
        {
            var c2 = emp.Add(new Employee() {manager_id = 78, name = "C2"});
        });
    }
    
    [Test]
    public void FKDeleteSetNullTest()
    {
        var s = emp.Add(new Employee() {name = "S"});
        var c1 = emp.Add(new Employee() {manager_id = s.id, name = "C1"});
        Assert.IsNotNull(c1.manager_id);
        emp.Delete(s.id);
        c1 = emp.Get(c1.id);
        Assert.IsNull(c1.manager_id);
    }
    
    [Test]
    public void FKDeleteDeleteTest()
    {
        var d = dept.Add(new Department() {name = "D"});
        var s = emp.Add(new Employee() {name = "S", department_id = d.id});
        
        dept.Delete(d.id);
        
        Assert.Throws<KeyNotFoundException>(() => emp.Get(s.id));
        Commit();
        Assert.Throws<KeyNotFoundException>(() => emp.Get(s.id));
    }

    [Test]
    public void SetterTest()
    {
        var e = new Employee() {id = 1979};
        
        var setter = getSetCompiler.Create<Employee, int>("id");
        Assert.NotNull(setter);
        e = setter.Set(e, 2022);
        Assert.AreEqual(2022, e.id);
        e.id = 1968;
        var test = setter.Get(e);
        Assert.AreEqual(e.id, test);

    }

    [Test]
    public void SelfRefTest()
    {
        emp.Add(new Employee() {id = 0});
        emp.Add(new Employee() {id = 1, manager_id = 0});
        //table should be dirty with pending adds.
        Assert.IsTrue(emp.IsDirty);
        emp.Commit();
        var rowCount = emp.RowCount;
        //table should now be clean
        Assert.IsFalse(emp.IsDirty);
        
        //table should still be clean
        Assert.IsFalse(emp.IsDirty);
        //Table should have same row count
        Assert.AreEqual(rowCount, emp.RowCount);
    }
    
    [Test]
    public void IsDirtyTest()
    {
        Assert.IsFalse(emp.IsDirty);
        emp.Add(new Employee() {id = 0});
        Assert.IsTrue(emp.IsDirty);
        emp.Commit();
        Assert.IsFalse(emp.IsDirty);
    }
    
    [Test]
    public void RollbackTest()
    {
        var emp1 = emp.Add(new Employee() {});
        emp.Commit();
        var emp2 = emp.Add(new Employee() {manager_id = 0});
        //table should be dirty with pending adds.
        Assert.IsTrue(emp.IsDirty);
        emp.Rollback();
        Assert.AreEqual(1, emp.RowCount);
        var emp3 = emp.Add(new Employee() {manager_id = 0});
        emp.Commit();
        Assert.AreEqual(2, emp.RowCount);
        foreach (var i in emp)
        {
            Console.WriteLine($"{i.id} - {i.manager_id}");
        }
        emp.Delete(emp3.id);
        Assert.AreEqual(1, emp.RowCount);
        emp.Rollback();
        Assert.AreEqual(2, emp.RowCount);
    }
    
    
    [Test]
    public void MultiRollbackTest()
    {
        var emp1 = emp.Add(new Employee() {id = 0, name = "Simon"});
        var dept1 = dept.Add(new Department() {id = 1, name = "Sales"});
        Assert.Throws<Exception>(() =>
        {
            Begin();
        });
        Rollback();
        Begin();
        dept1 = dept.Add(new Department() {id = 1, name = "Sales"});
        emp1 = emp.Add(new Employee() {id = 0, name = "Simon", department_id = 1});
        Commit();
        Assert.AreEqual(1, emp.RowCount);
        Assert.AreEqual(1, dept.RowCount);
    }

    [Test]
    public void TriggerTest()
    {
        emp.BeforeUpdate += (oldItem, newItem) =>
        {
            if (oldItem.version.HasValue)
            {
                newItem.version = oldItem.version + 1;
            }
            else
            {
                newItem.version = 1;
            }
            return newItem;
        };
        Begin();
        var emp1 = emp.Add(new Employee() {id = 32, name = "Simon"});
        Commit();
        var item = emp.Get(emp1.id);
        Assert.IsFalse(item.version.HasValue);
        Begin();
        item.name = "Boris";
        var newItem = emp.Update(item);
        Assert.IsTrue(newItem.version.HasValue);
        Assert.AreEqual(1, newItem.version.Value);
        Assert.AreEqual("Boris", newItem.name);
        Commit();
        newItem.name = "Vlad";
        var anotherNewItem = emp.Update(newItem);
        Assert.AreEqual("Vlad", anotherNewItem.name);
        Assert.AreEqual(2, anotherNewItem.version.Value);
    }

    [Test]
    public void UniqueTest()
    {
        var row1 = location.Add(new Location() {name = "X"});
        location.Add(new Location() {name = "Y"});
        Assert.Throws<ConstraintException>(() =>
        {
            location.Add(new Location() {name = "X"});
        });
        location.Delete(row1.id);
        Assert.DoesNotThrow(() =>
        {
            location.Add(new Location() {name = "X"});
        });

    }
}