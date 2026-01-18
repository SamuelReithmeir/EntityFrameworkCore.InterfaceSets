namespace EntityFrameworkCore.InterfaceSets.Tests.Model;

/// <summary>
/// Interface for entities that track creation and modification by users.
/// Includes navigation property to AuditUser.
/// </summary>
public interface IAuditable
{
    int? CreatedByUserId { get; set; }
    AuditUser? CreatedBy { get; set; }

    DateTime CreatedAt { get; set; }

    int? ModifiedByUserId { get; set; }
    AuditUser? ModifiedBy { get; set; }

    DateTime? ModifiedAt { get; set; }
}
