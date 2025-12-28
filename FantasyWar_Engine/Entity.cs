namespace FantasyWar_Engine;

public class Entity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Location Position { get; set; }
    public string Visual { get; set;  }
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
    
    public bool IsSolid { get; set; } = false;
    public bool ShouldDestroy { get; set; } = false;

    public Entity()
    {
        Id = -1;
        Name = "Entity";
        Position = new Location(0, 0);
        Visual = "?";
    }
    public Entity(int id)
    {
        Id = id;
        Name = "Entity";
        Position = new Location(0, 0);
        Visual = "?";
    }
    public virtual void OnCollide(Entity other) { }
}