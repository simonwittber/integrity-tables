using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using NUnit.Framework;
using Tables;
using static Tables.Database;
using ConstraintException = Tables.ConstraintException;

namespace TestTables;

public struct Employee
{
    public int id;
    public string name;
    [ForeignKey("Deparment", "Staff", typeof(Department), CascadeOperation.Delete)] public int? department_id;
    public int? version;
    [ForeignKey("Manager", "Subordinates", typeof(Employee), CascadeOperation.SetNull)] public int? manager_id;
    public float salary;
}

public struct Department
{
    public int id;
    [ForeignKey("Location", "Departments", typeof(Location), CascadeOperation.None)]
    public int location_id;
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
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        db.DropDatabase();
        db.CreateTable<Location>();
        dept = db.CreateTable<Department>("id");
        emp = db.CreateTable<Employee>("id");
        location = db.CreateTable<Location>();
    }

    [Test]
    public void WhenTest()
    {
        var fired = false;
        emp.When(employee => employee.salary > 1000, item =>
        {
            System.Console.WriteLine("When");
            fired = true;
        });
        Assert.IsFalse(fired);
        emp.Add(new Employee() {salary = 999});
        emp.Commit();
        Assert.IsFalse(fired);
        var e = emp.Add(new Employee() {salary = 9999});
        emp.Commit();
        Assert.IsTrue(fired);
        fired = false;
        e.salary = 999999;
        emp.Update(e);
        emp.Commit();
        Assert.IsFalse(fired);
        e.salary = 999;
        emp.Update(e);
        db.Commit();
        
        Assert.IsFalse(fired);
        e.salary = 999999;
        emp.Update(e);
        db.Commit();
        
        Assert.IsTrue(fired);
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
        emp.Commit();
        emp.Delete(s.id);
        c1 = emp.Get(c1.id);
        Assert.IsNull(c1.manager_id);
    }
    
    [Test]
    public void FKDeleteDeleteTest()
    {
        var location = this.location.Add(new Location() {name = "Here"});
        var d = dept.Add(new Department() {name = "D", location_id = location.id});
        var s = emp.Add(new Employee() {name = "S", department_id = d.id});
        
        dept.Delete(d.id);
        
        Assert.Throws<KeyNotFoundException>(() => emp.Get(s.id));
        db.Commit();
        Assert.Throws<KeyNotFoundException>(() => emp.Get(s.id));
    }

    [Test]
    public void SetterTest()
    {
        var e = new Employee() {id = 1979};
        
        var setter = GetSetCompiler.Create<Employee, int>("id");
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
        db.Begin();
        var location = this.location.Add(new Location() {name = "Here"});
        db.Commit();
        var emp1 = emp.Add(new Employee() {id = 0, name = "Simon"});
        var dept1 = dept.Add(new Department() {id = 1, name = "Sales", location_id = location.id});
        Assert.Throws<Exception>(() =>
        {
            db.Begin();
        });
        db.Rollback();
        db.Begin();
        dept1 = dept.Add(new Department() {id = 1, name = "Sales", location_id = location.id});
        emp1 = emp.Add(new Employee() {id = 0, name = "Simon", department_id = 1});
        db.Commit();
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
        db.Begin();
        var emp1 = emp.Add(new Employee() {id = 32, name = "Simon"});
        db.Commit();
        var item = emp.Get(emp1.id);
        Assert.IsFalse(item.version.HasValue);
        db.Begin();
        item.name = "Boris";
        var newItem = emp.Update(item);
        Assert.IsTrue(newItem.version.HasValue);
        Assert.AreEqual(1, newItem.version.Value);
        Assert.AreEqual("Boris", newItem.name);
        db.Commit();
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
    
    
    [Test]
    public void UniqueAddRollbackTest()
    {
        var row1 = location.Add(new Location() {name = "X"});
        location.Rollback();
        
        Assert.DoesNotThrow(() =>
        {
            location.Add(new Location() {name = "X"});
        });
    }
    
    [Test]
    public void UniqueUpdateRollbackTest()
    {
        var row1 = location.Add(new Location() {name = "X"});
        location.Commit();
        row1.name = "Y";
        location.Update(row1);
        
        location.Rollback();
        
        Assert.DoesNotThrow(() =>
        {
            location.Add(new Location() {name = "Y"});
        });

    }
    
    
    [Test]
    public void UniqueUpdateRollbackCommitTest()
    {
        var row1 = location.Add(new Location() {name = "X"});
        location.Commit();
        row1.name = "Y";
        location.Update(row1);
        location.Rollback();
        
        Assert.Throws<ConstraintException>(() =>
        {
            location.Add(new Location() {name = "X"});
            location.Commit();
        });

    }
}