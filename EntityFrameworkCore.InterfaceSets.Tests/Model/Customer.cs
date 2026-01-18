namespace EntityFrameworkCore.InterfaceSets.Tests.Model;

public class Customer : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

