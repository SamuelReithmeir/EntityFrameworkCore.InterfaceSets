namespace EntityFrameworkCore.InterfaceSets.Tests.Model;

public class Product : IArchivable, ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // IAuditable implementation
    public int? CreatedByUserId { get; set; }
    public AuditUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ModifiedByUserId { get; set; }
    public AuditUser? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

