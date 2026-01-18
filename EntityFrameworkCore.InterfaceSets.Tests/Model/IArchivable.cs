namespace EntityFrameworkCore.InterfaceSets.Tests.Model;
/// <summary>
/// Interface for entities that can be archived.
/// </summary>
public interface IArchivable
{
    DateTime? ArchivedAt { get; set; }
    bool IsArchived { get; set; }
}


