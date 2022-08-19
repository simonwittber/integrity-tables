using System;
using NUnit.Framework;
using Tables;

namespace TestTables;

public class RelationalDataTests
{
    struct Employee
    {
        public static int last_id = 0;
        public int id;
        public string name;
        public int? department_id;
        public int? manager_id;
        public float salary;
    }

    struct Department
    {
        public int id;
        public string name;
        public string location;
    }

    private Table<Employee> emp;
    private Table<Department> dept;
    private Database db;

    [SetUp]
    public void Setup()
    {
        emp = new Table<Employee>(i => i.id, i =>
        {
            i.id = Employee.last_id++;
            return i;
        });
        dept = new Table<Department>(i => i.id);
        emp.AddRelationshipConstraint("dept_fk", i=> i.department_id, dept);
        emp.AddRelationshipConstraint("manager_fk", i=>i.manager_id, emp);
        
        db = new Database(emp, dept);

        
    }

    [Test]
    public void TestPKFn()
    {
        var e1 = emp.Add(new() {name = "Boris", salary = 1000});
        var e2 = emp.Add(new() {name = "Vlad", salary = 2000});
        var e3 = emp.Add(new() {name = "Simon", salary = 1000, manager_id = e1.id});
        Assert.AreEqual(0, e1.id);
        Assert.AreEqual(1, e2.id);
        Assert.AreEqual(2, e3.id);

        Assert.IsTrue(emp.TryGet(i => i.manager_id == e1.id, out var result));
        Assert.AreEqual("Simon", result.name);
        Assert.AreEqual(2, emp.Count(i=>i.salary<2000));
    }

    
}