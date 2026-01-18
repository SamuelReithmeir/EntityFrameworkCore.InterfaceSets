# EntityFrameworkCore.InterfaceSets

A library for Entity Framework Core that enables querying entities through shared interfaces, allowing you to work with
entities from different class hierarchies that implement a common interface.

## Overview

EntityFrameworkCore.InterfaceSets extends EF Core with the ability to query across multiple entity types that share a
common interface. This is particularly useful for implementing cross-cutting concerns like soft deletion, archiving,
auditing, or multi-tenancy.

Instead of writing repetitive queries for each entity type, you can query all entities that implement a specific
interface as if they were a single collection.

## Installation

```bash
dotnet add package EntityFrameworkCore.InterfaceSets
```

## Quick Start

### 1. Define your interface

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
```

### 2. Implement the interface on your entities

```csharp
public class Order : ISoftDeletable
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class Product : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### 3. Configure your DbContext

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlServer(connectionString)
        .UseInventoryServices(); // Enable InterfaceSets
}
```

### 4. Query by interface

```csharp
// Count all soft-deleted entities across all entity types
var deletedCount = context.InterfaceSet<ISoftDeletable>()
    .Where(x => x.IsDeleted)
    .Count();

// Get all archived entities
var archivedItems = context.InterfaceSet<IArchivable>()
    .Where(x => x.IsArchived)
    .ToList();

// Restore all soft-deleted items older than 30 days
var cutoffDate = DateTime.UtcNow.AddDays(-30);
var oldDeletedItems = context.InterfaceSet<ISoftDeletable>()
    .Where(x => x.IsDeleted && x.DeletedAt < cutoffDate)
    .ToList();

foreach (var item in oldDeletedItems)
{
    item.IsDeleted = false;
    item.DeletedAt = null;
}
await context.SaveChangesAsync();
```

## Supported Operations

### Synchronous Operations

- `Count()` / `LongCount()`
- `First()` / `FirstOrDefault()`
- `Single()` / `SingleOrDefault()`
- `Any()`
- `ToList()`
- `Where()` with LINQ expressions
- `OfType<T>()` for filtering by specific entity types

### Asynchronous Operations

- `CountAsync()` / `LongCountAsync()`
- `FirstAsync()` / `FirstOrDefaultAsync()`
- `SingleAsync()` / `SingleOrDefaultAsync()`
- `AnyAsync()`
- `ToListAsync()`

All operations support method chaining and standard LINQ query syntax.

## How It Works

When you call `InterfaceSet<TInterface>()`, the library:

1. **Discovers** all entity types in your DbContext that implement the interface
2. **Rewrites** your LINQ expressions to target the concrete entity types
3. **Executes** separate queries against each entity type's DbSet
4. **Aggregates** the results based on the operation (e.g., sums counts, concatenates lists)

For example, `InterfaceSet<ISoftDeletable>().Count()` internally executes:

```csharp
context.Orders.Where(rewrittenExpression).Count() +
context.Products.Where(rewrittenExpression).Count() +
// ... for all ISoftDeletable entity types
```

## Limitations

### 1. No Direct Modifications

You cannot use `InterfaceSet` for direct updates or deletes:

```csharp
// This will NOT work
context.InterfaceSet<ISoftDeletable>().ExecuteDelete(); // Not supported
```

Instead, materialize the entities first:

```csharp
var items = context.InterfaceSet<ISoftDeletable>().ToList();
foreach (var item in items)
{
    item.IsDeleted = true;
}
context.SaveChanges();
```

### 2. Limited JOIN Support

Joins across interface sets are not supported. You can only query and filter within a single interface set.

### 3. Navigation Properties

Navigation properties defined in interfaces are supported with some limitations:

**What Works:**

- Filtering by navigation property foreign keys
- Filtering by navigation property object properties (e.g., `x.CreatedBy.Username == "admin"`)

```csharp
public interface IAuditable
{
    int? CreatedByUserId { get; set; }
    AuditUser? CreatedBy { get; set; }
    DateTime CreatedAt { get; set; }
}

// Filter by foreign key - works perfectly
var itemsCreatedByUser = context.InterfaceSet<IAuditable>()
    .Where(x => x.CreatedByUserId == userId)
    .ToList();

// Filter by navigation property - also works!
var itemsByAdmin = context.InterfaceSet<IAuditable>()
    .Where(x => x.CreatedBy.Username == "admin")
    .ToList();
```

**What Doesn't Work:**

- `.Include()` - Navigation properties will NOT be loaded even if you use `.Include(x => x.CreatedBy)`
- Navigation properties in returned entities will always be `null` unless you manually load them afterward

The navigation properties must be properly configured in EF Core's `OnModelCreating` for each entity type.

### 4. Performance Considerations

- Each query generates N separate SQL queries (one per entity type implementing the interface)

### 5. Sorting Limitations

Sorting with `OrderBy()` does not work as expected, as it only orders per dbSet, resulting in an unordered total
sequence.  
To implement proper ordering, the OrderBy expression would need to be extracted and evaluated inMemory, i am happy to
accept PRs for this feature.

## Advanced Usage

### Filtering by Specific Entity Types

Use `OfType<T>()` to query only specific entity types:

```csharp
// Only query Orders, not all ISoftDeletable entities
var deletedOrders = context.InterfaceSet<ISoftDeletable>()
    .OfType<Order>()
    .Where(x => x.IsDeleted)
    .ToList();
```

### Complex Filters

```csharp
var recentlyArchived = context.InterfaceSet<IArchivable>()
    .Where(x => x.IsArchived && x.ArchivedAt > DateTime.UtcNow.AddDays(-7))
    .ToList();
```

### Async Operations

```csharp
var count = await context.InterfaceSet<ISoftDeletable>()
    .Where(x => !x.IsDeleted)
    .CountAsync();

var items = await context.InterfaceSet<IArchivable>()
    .ToListAsync();
```

## Extensibility

The library uses an auto-discovery pattern for operation handlers. To add support for custom operations, create a
handler implementing `IOperationHandler<TResult>`:

```csharp
public class CustomOperationHandler<T> : BaseOperationHandler<T>
{
    public override bool CanHandle(string operationName, Expression expression)
    {
        return operationName == "MyCustomOperation";
    }

    public override T Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        // Your implementation
    }

    public override Task<T> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken)
    {
        // Your async implementation
    }
}
```

Handlers are automatically discovered and registered at runtime.

