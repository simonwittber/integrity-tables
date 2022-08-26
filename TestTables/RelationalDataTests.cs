using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tables;
using static Tables.Database;

namespace TestTables;

public static class EmployeeExtensionMethods
{
    public static Department Deparment(this Employee e)
    {
        return Database.GetTable<Department>()[(int) e.department_id];
    }
}

public class RelationalDataTests
{
    
    public struct Employee
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

    public struct Department
    {
        public int id;
        public string name;
        public string location;
    }

    

    private Table<Employee> emp;
    private Table<Department> dept;



    [SetUp]
    public void Setup()
    {
        DropDatabase();
        dept = CreateTable<Department>();

        emp = CreateTable<Employee>();


        emp.AddRelationshipConstraint(
            i => i.manager_id,
            (i, fk) =>
            {
                i.manager_id = fk;
                return i;
            },
            emp,
            CascadeOperation.SetNull, isNullable:true);
        emp.AddRelationshipConstraint(
            e => e.department_id,
            (e, fk) =>
            {
                e.department_id = fk;
                return e;
            },
            dept,
            CascadeOperation.Delete, isNullable:true);

    }

    

    [Test]
    public void TestPKFn()
    {
        Begin();
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
        Commit();
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
        Commit();
        Assert.AreEqual(0, emp.Count(i=>i.salary<2000));
        
    }

    
}