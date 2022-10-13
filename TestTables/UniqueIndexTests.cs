using System.Buffers;
using NUnit.Framework;
using IntegrityTables;
using static IntegrityTables.Database;

namespace TestTables;

public struct Hero
{
    public int id;
    [Unique]
    public string name;
}

public struct Statistics
{
    public int id;

    [Unique][ForeignKey("Hero", "Stats", typeof(Hero), CascadeOperation.Delete)]
    public int? hero_id;

    public float hitPoints;
    public float shields;
    public float armour;

}

public struct Friends
{
    public int id;
    
    [Unique("hero_pair")][ForeignKey("Hero", "Friends", typeof(Hero), CascadeOperation.Delete)]
    public int? hero_id;

    [Unique("hero_pair"), ForeignKey("Hero", "Friends", typeof(Hero), CascadeOperation.Delete)]
    public int? other_hero_id;
}

public struct SomeInstance
{
    public int id;
}


public class UniqueIndexTests
{
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        db.DropDatabase();
        db.CreateTable<Hero>();
        db.CreateTable<Statistics>();
        db.CreateTable<Friends>();

    }

    [Test]
    public void TestModifyToUnUnique()
    {
        var heroes = db.GetTable<Hero>();
        db.Begin();
        var a = heroes.Add(new Hero() {name = "X"});
        var b = heroes.Add(new Hero() {name = "Y"});
        db.Commit();
        Assert.Throws<IntegrityException>(() =>
        {
            b.name = "X";
            heroes.Update(b);
        });
        db.Commit();
    }

    [Test]
    public void TestInsertAndGet()
    {
        var heroes = db.GetTable<Hero>();
        var stats = db.GetTable<Statistics>();
        var friends = db.GetTable<Friends>();
        
        db.Begin();
        var a = heroes.Add(new Hero() {name = "A Type of Thing"});
        var b = stats.Add(new Statistics() {hero_id = a.id});
        var c = heroes.Add(new Hero() {name = "Another Type of Thing"});
        Assert.Throws<IntegrityException>(() =>
        {
            heroes.Add(new Hero() {name = "A Type of Thing"});
        });
        Assert.Throws<IntegrityException>(() =>
        {
            stats.Add(new Statistics() {hero_id = a.id});
        });
        friends.Add(new Friends() {hero_id = a.id, other_hero_id = c.id});
        Assert.Throws<IntegrityException>(() =>
        {
            friends.Add(new Friends() {hero_id = a.id, other_hero_id = c.id});
        });
        Assert.DoesNotThrow(() =>
        {
            friends.Add(new Friends() {other_hero_id = a.id, hero_id = c.id});    
        });
        
        db.Commit();
    }
}