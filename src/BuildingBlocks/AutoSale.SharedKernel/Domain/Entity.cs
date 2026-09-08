namespace AutoSale.SharedKernel.Domain;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; private set; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity()
    {
    }

    public bool Equals(Entity? other)
    {
        return other is not null && GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
