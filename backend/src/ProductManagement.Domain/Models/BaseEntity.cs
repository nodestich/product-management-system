namespace ProductManagement.Domain.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }
}