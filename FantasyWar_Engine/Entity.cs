namespace FantasyWar_Engine;

public class Entity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Visual { get; set;  }
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
    
    public bool IsSolid { get; set; } = true;
    public bool ShouldDestroy { get; set; } = false;
    
    public Vector Position { get; set; } = new Vector(1, 1);
    
    public Entity()
    {
        Id = -1;
        Name = "Entity";
        //SetActorLocation(World.Instance.GetRandomEmptyLocation());
        Visual = "?";
    }
    public Entity(int id)
    {
        Id = id;
        Name = "Entity";
        //SetActorLocation(World.Instance.GetRandomEmptyLocation());
        Visual = "?";
    }
    public Entity(int id, string name, Vector position, string visual = "?")
    {
        Id = id;
        Name = name;
        SetActorLocation(position);
        Visual = visual;
    }

    public Vector GetActorLocation()
    {
        return Position;
    }
    
    public bool SetActorLocation(Vector location)
    {
        if (World.Instance == null) return false;
        
        if (PhysicsSystem.IsWalkable(location))
        {
            
            World.Instance.SetGridValue(location, Position, Id, IsSolid);
            Position = location;
            return true;
        }
        return false;
    }

    public bool AddActorLocation(Vector movementVector)
    {
        Vector newLocation = Position + movementVector;
        return SetActorLocation(newLocation);
    }
    
    
    public virtual void OnCollide(Entity other) { }
}