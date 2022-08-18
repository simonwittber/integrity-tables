using System;
using System.Diagnostics;
using NUnit.Framework;
using Tables;

namespace TestTables;

public class MoreComplexTests
{
    struct Employee
    {
        public int id;
        public string name;
        public int? department_id;
        public int? manager_id;
        public int? version;
    }

    struct Department
    {
        public int id;
        public string name;
    }

    private Table<Employee> emp;
    private Table<Department> dept;
    private Database db;

    [SetUp]
    public void Setup()
    {
        emp = new Table<Employee>(i => i.id);
        dept = new Table<Department>(i => i.id);
        emp.AddRelationshipConstraint("dept_fk", i=> i.department_id, dept);
        emp.AddRelationshipConstraint("manager_fk", i=>i.manager_id, emp);
        db = new Database(emp, dept);
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
            db.Begin();
        });
        db.Rollback();
        db.Begin();
        dept1 = dept.Add(new Department() {id = 1, name = "Sales"});
        emp1 = emp.Add(new Employee() {id = 0, name = "Simon", department_id = 1});
        db.Commit();
        Assert.AreEqual(1, emp.RowCount);
        Assert.AreEqual(1, dept.RowCount);
    }

    [Test]
    public void TriggerTest()
    {
        emp.OnUpdate += (oldItem, newItem) =>
        {
            if (oldItem.version.HasValue)
            {
                Console.WriteLine($"Has Value {oldItem.name} {oldItem.version}");
                newItem.version = oldItem.version + 1;
            }
            else
            {
                Console.WriteLine($"No Value {oldItem.name} {oldItem.version}");
                newItem.version = 1;
            }
            Console.WriteLine($"{oldItem.version} = {newItem.version}");
            return newItem;
        };
        db.Begin();
        emp.Add(new Employee() {id = 32, name = "Simon"});
        db.Commit();
        var item = emp.Get(32);
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
}