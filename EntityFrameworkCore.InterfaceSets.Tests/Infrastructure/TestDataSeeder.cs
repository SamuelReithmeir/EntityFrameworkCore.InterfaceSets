using EntityFrameworkCore.InterfaceSets.Tests.Model;

namespace EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;

public static class TestDataSeeder
{
    public static void SeedData(TestDbContext context)
    {
        // Seed audit users
        var user1 = new AuditUser { Id = 1, Username = "admin", Email = "admin@test.com" };
        var user2 = new AuditUser { Id = 2, Username = "user1", Email = "user1@test.com" };
        context.AuditUsers.AddRange(user1, user2);
        context.SaveChanges();

        var now = DateTime.UtcNow;

        context.Products.AddRange(
            new Product
            {
                Id = 1, Name = "Product 1", Price = 10.00m, IsArchived = false, IsDeleted = false,
                CreatedByUserId = 1, CreatedAt = now.AddDays(-30), ModifiedByUserId = null, ModifiedAt = null
            },
            new Product
            {
                Id = 2, Name = "Product 2", Price = 20.00m, IsArchived = true, ArchivedAt = now.AddDays(-10), IsDeleted = false,
                CreatedByUserId = 1, CreatedAt = now.AddDays(-25), ModifiedByUserId = 2, ModifiedAt = now.AddDays(-10)
            },
            new Product
            {
                Id = 3, Name = "Product 3", Price = 30.00m, IsArchived = false, IsDeleted = true, DeletedAt = now.AddDays(-5),
                CreatedByUserId = 2, CreatedAt = now.AddDays(-20), ModifiedByUserId = 1, ModifiedAt = now.AddDays(-5)
            },
            new Product
            {
                Id = 4, Name = "Product 4", Price = 40.00m, IsArchived = true, ArchivedAt = now.AddDays(-3), IsDeleted = true, DeletedAt = now.AddDays(-2),
                CreatedByUserId = 2, CreatedAt = now.AddDays(-15), ModifiedByUserId = 2, ModifiedAt = now.AddDays(-2)
            }
        );

        context.Orders.AddRange(
            new Order
            {
                Id = 1, OrderNumber = "ORD-001", OrderDate = now.AddDays(-20), TotalAmount = 100.00m, IsArchived = false, IsDeleted = false,
                CreatedByUserId = 1, CreatedAt = now.AddDays(-20), ModifiedByUserId = null, ModifiedAt = null
            },
            new Order
            {
                Id = 2, OrderNumber = "ORD-002", OrderDate = now.AddDays(-15), TotalAmount = 200.00m, IsArchived = true, ArchivedAt = now.AddDays(-5), IsDeleted = false,
                CreatedByUserId = 2, CreatedAt = now.AddDays(-15), ModifiedByUserId = 1, ModifiedAt = now.AddDays(-5)
            },
            new Order
            {
                Id = 3, OrderNumber = "ORD-003", OrderDate = now.AddDays(-10), TotalAmount = 300.00m, IsArchived = false, IsDeleted = true, DeletedAt = now.AddDays(-3),
                CreatedByUserId = 1, CreatedAt = now.AddDays(-10), ModifiedByUserId = 2, ModifiedAt = now.AddDays(-3)
            }
        );

        context.Customers.AddRange(
            new Customer { Id = 1, Name = "Customer 1", Email = "customer1@test.com", IsDeleted = false },
            new Customer { Id = 2, Name = "Customer 2", Email = "customer2@test.com", IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-7) },
            new Customer { Id = 3, Name = "Customer 3", Email = "customer3@test.com", IsDeleted = false }
        );

        context.Invoices.AddRange(
            new Invoice { Id = 1, InvoiceNumber = "INV-001", InvoiceDate = DateTime.UtcNow.AddDays(-30), Amount = 500.00m, IsArchived = false },
            new Invoice { Id = 2, InvoiceNumber = "INV-002", InvoiceDate = DateTime.UtcNow.AddDays(-25), Amount = 600.00m, IsArchived = true, ArchivedAt = DateTime.UtcNow.AddDays(-10) },
            new Invoice { Id = 3, InvoiceNumber = "INV-003", InvoiceDate = DateTime.UtcNow.AddDays(-20), Amount = 700.00m, IsArchived = false }
        );

        context.SaveChanges();
    }

    public static class ExpectedCounts
    {
        public const int TotalArchivable = 10;
        public const int ArchivedArchivable = 4;
        public const int NonArchivedArchivable = 6;

        public const int TotalSoftDeletable = 10;
        public const int DeletedSoftDeletable = 4;
        public const int NonDeletedSoftDeletable = 6;

        public const int TotalProducts = 4;
        public const int ArchivedProducts = 2;
        public const int DeletedProducts = 2;

        public const int TotalOrders = 3;
        public const int ArchivedOrders = 1;
        public const int DeletedOrders = 1;

        public const int TotalCustomers = 3;
        public const int DeletedCustomers = 1;

        public const int TotalInvoices = 3;
        public const int ArchivedInvoices = 1;

        public const int TotalAuditable = 7; // 4 Products + 3 Orders
        public const int AuditableCreatedByUser1 = 4; // 2 Products + 2 Orders
        public const int AuditableCreatedByUser2 = 3; // 2 Products + 1 Order
    }
}

