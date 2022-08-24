using System;
using NUnit.Framework;
using Tables;
using static Tables.Database;
using ConstraintException = Tables.ConstraintException;

namespace TestTables;

public struct Employee
{
    public int id;
    public string name;
    public int? department_id;
    public int? manager_id;
    public int? version;
}

public struct Department
{
    public int id;
    public string name;
}

public class MoreComplexTests
{
    

    private Table<Employee> emp;
    private Table<Department> dept;

    [SetUp]
    public void Setup()
    {
        DropDatabase();        
        emp = CreateTable<Employee>(i => i.id);
        dept = CreateTable<Department>(i => i.id);
        emp.AddRelationshipConstraint(nameof(Employee.department_id), dept, CascadeOperation.Delete);
        emp.AddRelationshipConstraint(nameof(Employee.manager_id), emp, CascadeOperation.SetNull);

    }

    [Test]
    public void SetterTest()
    {
        var e = new Employee() {id = 1979};
        
        var setter = Compiler.CreateGetSet<Employee, int>("id");
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
        Assert.Throws<ConstraintException>(() =>
        {
            emp.Add(new Employee() {id = 1, manager_id = 2});
        });
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
        emp.Add(new Employee() {id = 0});
        emp.Commit();
        emp.Add(new Employee() {id = 1, manager_id = 0});
        //table should be dirty with pending adds.
        Assert.IsTrue(emp.IsDirty);
        emp.Rollback();
        Assert.AreEqual(1, emp.RowCount);
        emp.Add(new Employee() {id = 1, manager_id = 0});
        emp.Commit();
        Assert.AreEqual(2, emp.RowCount);
        emp.Delete(1);
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
        emp.Add(new Employee() {id = 32, name = "Simon"});
        Commit();
        var item = emp.Get(32);
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
}