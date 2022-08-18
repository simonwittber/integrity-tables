using System;
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
    }

    struct Department
    {
        public int id;
        public string name;
    }

    private Table<Employee> emp;
    private Table<Department> dept;

    [SetUp]
    public void Setup()
    {
        emp = new Table<Employee>(i => i.id);
        dept = new Table<Department>(i => i.id);
        emp.AddRelationshipConstraint("dept_fk", i=> i.department_id, dept);
        emp.AddRelationshipConstraint("manager_fk", i=>i.manager_id, emp);
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
}