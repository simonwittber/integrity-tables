using System.Buffers;
using NUnit.Framework;
using Tables;
using static Tables.Database;

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
    [SetUp]
    public void Setup()
    {
        DropDatabase();
        CreateTable<Hero>();
        CreateTable<Statistics>();
        CreateTable<Friends>();

    }
    
    [Test]
    public void TestInsertAndGet()
    {
        var heroes = GetTable<Hero>();
        var stats = GetTable<Statistics>();
        var friends = GetTable<Friends>();
        
        Begin();
        var a = heroes.Add(new Hero() {name = "A Type of Thing"});
        var b = stats.Add(new Statistics() {hero_id = a.id});
        var c = heroes.Add(new Hero() {name = "Another Type of Thing"});
        Assert.Throws<ConstraintException>(() =>
        {
            heroes.Add(new Hero() {name = "A Type of Thing"});
        });
        Assert.Throws<ConstraintException>(() =>
        {
            stats.Add(new Statistics() {hero_id = a.id});
        });
        friends.Add(new Friends() {hero_id = a.id, other_hero_id = c.id});
        Assert.Throws<ConstraintException>(() =>
        {
            friends.Add(new Friends() {hero_id = a.id, other_hero_id = c.id});
        });
        Assert.DoesNotThrow(() =>
        {
            friends.Add(new Friends() {other_hero_id = a.id, hero_id = c.id});    
        });
        
        Commit();
    }
}