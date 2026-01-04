# EntityFrameworkCore.InterfaceSets

A library for Entity Framework Core that enables querying entities through shared interfaces, allowing you to work with entities from different class hierarchies that implement a common interface.

## Features

- **Interface-based querying**: Query multiple entity types through a shared interface
- **Automatic discovery**: Automatically finds all entity types implementing an interface
- **LINQ support**: Full LINQ query support with `Where`, `OrderBy`, `Select`, etc.
- **Async enumeration**: Full support for async operations
- **Type-safe**: Compile-time type safety with generic constraints

## Usage

### Define your interfaces and entities

```csharp
public interface IArchivable
{
    bool IsArchived { get; set; }
    DateTime? ArchivedAt { get; set; }
}

public class Document : IArchivable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class Product : IArchivable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
```

### Query across multiple entity types

```csharp
// Get all archived items across all entity types
var archivedItems = context.InterfaceSet<IArchivable>()
    .Where(x => x.IsArchived)
    .OrderBy(x => x.ArchivedAt)
    .ToList();

// Use async enumeration
await foreach (var item in context.InterfaceSet<IArchivable>())
{
    Console.WriteLine($"Archived: {item.IsArchived}");
}

// Access specific DbSet if needed
var interfaceSet = context.InterfaceSet<IArchivable>();
var documentDbSet = interfaceSet.GetDbSet<Document>();
```

## Limitations

`InterfaceSet<TInterface>` is designed as a read-only query interface. The following operations are **not supported**:

- `Add()`, `Remove()`, `Update()` - these would be ambiguous across multiple entity types
- Change tracking operations
- Some navigation property operations

To modify entities, use the specific `DbSet<TEntity>` or call `GetDbSet<TEntity>()` on the InterfaceSet.

## Entity Hierarchies

The library correctly handles entity inheritance hierarchies. When you have a base type implementing an interface and derived types, the library automatically:

- **Only queries the root type** in the hierarchy to avoid duplicates
- **Returns all derived types** automatically (EF Core's normal behavior)

```csharp
public class BaseDocument : IArchivable { }
public class Invoice : BaseDocument { }  // Inherits IArchivable
public class Contract : BaseDocument { } // Inherits IArchivable

// DbContext
public DbSet<BaseDocument> BaseDocuments { get; set; }
public DbSet<Invoice> Invoices { get; set; }
public DbSet<Contract> Contracts { get; set; }

// InterfaceSet will only query BaseDocument (not Invoice/Contract separately)
// This prevents duplicates while still returning all Invoice and Contract instances
var allDocs = context.InterfaceSet<IArchivable>().ToList();
```

## How it works

Under the hood, the library:
1. Scans the `DbContext.Model` to find all entity types implementing the interface
2. **Filters to only root types** in entity hierarchies to prevent duplicates
3. Creates a combined enumerable that iterates over each DbSet
4. Wraps the result as `IQueryable<TInterface>` for LINQ support
5. Provides async enumeration support through `IAsyncEnumerable<TInterface>`

Note: Since EF Core doesn't support SQL UNION across different entity types, queries are executed separately for each entity type and combined in memory.
