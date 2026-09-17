using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmaceutical.Core;
using Pharmaceutical.Infrastructure;
using MySqlConnector;

namespace Pharmaceutical.WebAPI;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PharmaceuticalDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (MySqlException ex) when (ex.Message.Contains("already exists"))
        {
            Console.WriteLine($"迁移提示：{ex.Message}，继续执行...");
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = ["Admin", "Operator", "Viewer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new AppUser { UserName = "admin", DisplayName = "系统管理员" };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        var seedDemo = configuration.GetValue<bool>("SEED_DEMO_DATA")
            || string.Equals(Environment.GetEnvironmentVariable("SEED_DEMO_DATA"), "true", StringComparison.OrdinalIgnoreCase);

        if (seedDemo)
        {
            await SeedDemoUsersAsync(userManager);
            if (!await context.Drugs.AnyAsync())
                await SeedDemoDataAsync(context);
            else
                await SeedSupplementaryDemoDataAsync(context);
        }
    }

    private static async Task SeedDemoUsersAsync(UserManager<AppUser> userManager)
    {
        await EnsureUserAsync(userManager, "operator1", "Operator@123", "演示操作员", "Operator");
        await EnsureUserAsync(userManager, "viewer1", "Viewer@123", "演示只读", "Viewer");
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> userManager, string username, string password, string displayName, string role)
    {
        if (await userManager.FindByNameAsync(username) != null) return;
        var user = new AppUser { UserName = username, DisplayName = displayName };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedDemoDataAsync(PharmaceuticalDbContext context)
    {
        if (await context.Drugs.AnyAsync())
        {
            Console.WriteLine("演示数据：库中已有药品，跳过种子数据。");
            return;
        }

        Console.WriteLine("演示数据：正在写入供应商、药品、批次、流水与采购单...");

        var supplier1 = new SupplierEntity
        {
            Name = "华北医药供应有限公司",
            ContactPerson = "张经理",
            Phone = "010-88886666",
            Address = "北京市朝阳区医药产业园"
        };
        var supplier2 = new SupplierEntity
        {
            Name = "华南康宁药业",
            ContactPerson = "李主管",
            Phone = "020-66668888",
            Address = "广州市白云区物流园"
        };
        var supplier3 = new SupplierEntity
        {
            Name = "华东仁济医药",
            ContactPerson = "王主任",
            Phone = "021-55557777",
            Address = "上海市浦东新区医药港"
        };
        context.Suppliers.AddRange(supplier1, supplier2, supplier3);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var drugs = new[]
        {
            new DrugCatalogEntity { DrugId = "D001", DrugName = "阿莫西林胶囊", TradeName = "阿莫仙", Specification = "0.25g*24粒", DosageForm = "胶囊剂", ApprovalNum = "国药准字H12345678", StorageCond = "密封阴凉干燥", PurchasePrice = 12.50m, RetailPrice = 18.00m, StockQuantity = 45, SupplierId = supplier1.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D002", DrugName = "布洛芬缓释胶囊", TradeName = "芬必得", Specification = "0.3g*20粒", DosageForm = "胶囊剂", ApprovalNum = "国药准字H23456789", StorageCond = "密封保存", PurchasePrice = 15.00m, RetailPrice = 22.00m, StockQuantity = 180, SupplierId = supplier1.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D003", DrugName = "维生素C片", TradeName = "", Specification = "100mg*100片", DosageForm = "片剂", ApprovalNum = "国药准字H34567890", StorageCond = "遮光密封", PurchasePrice = 8.00m, RetailPrice = 12.00m, StockQuantity = 320, SupplierId = supplier2.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D004", DrugName = "感冒灵颗粒", TradeName = "999感冒灵", Specification = "10g*9袋", DosageForm = "颗粒剂", ApprovalNum = "国药准字Z45678901", StorageCond = "密封", PurchasePrice = 10.50m, RetailPrice = 15.80m, StockQuantity = 25, SupplierId = supplier2.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D005", DrugName = "盐酸氨溴索口服液", TradeName = "沐舒坦", Specification = "100ml", DosageForm = "口服液", ApprovalNum = "国药准字H56789012", StorageCond = "遮光密封", PurchasePrice = 22.00m, RetailPrice = 32.00m, StockQuantity = 90, SupplierId = supplier1.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D006", DrugName = "头孢克肟分散片", TradeName = "世福素", Specification = "100mg*6片", DosageForm = "片剂", ApprovalNum = "国药准字H67890123", StorageCond = "密封", PurchasePrice = 28.00m, RetailPrice = 38.00m, StockQuantity = 60, SupplierId = supplier1.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D007", DrugName = "蒙脱石散", TradeName = "思密达", Specification = "3g*10袋", DosageForm = "散剂", ApprovalNum = "国药准字H78901234", StorageCond = "密封", PurchasePrice = 18.00m, RetailPrice = 26.00m, StockQuantity = 150, SupplierId = supplier2.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D008", DrugName = "复方甘草片", TradeName = "", Specification = "100片", DosageForm = "片剂", ApprovalNum = "国药准字Z89012345", StorageCond = "密封遮光", PurchasePrice = 6.50m, RetailPrice = 10.00m, StockQuantity = 35, SupplierId = supplier2.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D009", DrugName = "氯雷他定片", TradeName = "开瑞坦", Specification = "10mg*6片", DosageForm = "片剂", ApprovalNum = "国药准字H90123456", StorageCond = "密封", PurchasePrice = 20.00m, RetailPrice = 30.00m, StockQuantity = 110, SupplierId = supplier3.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D010", DrugName = "奥美拉唑肠溶胶囊", TradeName = "洛赛克", Specification = "20mg*14粒", DosageForm = "胶囊剂", ApprovalNum = "国药准字H01234567", StorageCond = "密封遮光", PurchasePrice = 35.00m, RetailPrice = 48.00m, StockQuantity = 75, SupplierId = supplier3.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D011", DrugName = "葡萄糖注射液", TradeName = "", Specification = "250ml", DosageForm = "注射剂", ApprovalNum = "国药准字H11234567", StorageCond = "常温", PurchasePrice = 5.00m, RetailPrice = 8.00m, StockQuantity = 200, SupplierId = supplier3.SupplierId, IsActive = true },
            new DrugCatalogEntity { DrugId = "D012", DrugName = "碘伏消毒液", TradeName = "", Specification = "100ml", DosageForm = "外用液体", ApprovalNum = "国药准字H21234567", StorageCond = "密封避光", PurchasePrice = 9.00m, RetailPrice = 14.00m, StockQuantity = 18, SupplierId = supplier1.SupplierId, IsActive = true }
        };
        context.Drugs.AddRange(drugs);
        await context.SaveChangesAsync();

        context.DrugBatches.AddRange(
            new DrugBatchEntity { DrugId = "D001", BatchNumber = "AMX202601", ManufactureDate = now.AddMonths(-6), ExpiryDate = now.AddDays(20), Quantity = 30 },
            new DrugBatchEntity { DrugId = "D001", BatchNumber = "AMX202602", ManufactureDate = now.AddMonths(-3), ExpiryDate = now.AddDays(120), Quantity = 15 },
            new DrugBatchEntity { DrugId = "D002", BatchNumber = "IBU202512", ManufactureDate = now.AddMonths(-4), ExpiryDate = now.AddDays(200), Quantity = 180 },
            new DrugBatchEntity { DrugId = "D003", BatchNumber = "VC202511", ManufactureDate = now.AddMonths(-5), ExpiryDate = now.AddDays(55), Quantity = 200 },
            new DrugBatchEntity { DrugId = "D003", BatchNumber = "VC202603", ManufactureDate = now.AddMonths(-2), ExpiryDate = now.AddDays(180), Quantity = 120 },
            new DrugBatchEntity { DrugId = "D004", BatchNumber = "GM202508", ManufactureDate = now.AddMonths(-8), ExpiryDate = now.AddDays(25), Quantity = 25 },
            new DrugBatchEntity { DrugId = "D005", BatchNumber = "AMB202603", ManufactureDate = now.AddMonths(-2), ExpiryDate = now.AddDays(60), Quantity = 90 },
            new DrugBatchEntity { DrugId = "D006", BatchNumber = "CEF202602", ManufactureDate = now.AddMonths(-3), ExpiryDate = now.AddDays(150), Quantity = 60 },
            new DrugBatchEntity { DrugId = "D007", BatchNumber = "MTS202601", ManufactureDate = now.AddMonths(-4), ExpiryDate = now.AddDays(80), Quantity = 150 },
            new DrugBatchEntity { DrugId = "D008", BatchNumber = "GC202507", ManufactureDate = now.AddMonths(-7), ExpiryDate = now.AddDays(15), Quantity = 35 },
            new DrugBatchEntity { DrugId = "D009", BatchNumber = "LR202512", ManufactureDate = now.AddMonths(-3), ExpiryDate = now.AddDays(220), Quantity = 110 },
            new DrugBatchEntity { DrugId = "D010", BatchNumber = "OMZ202604", ManufactureDate = now.AddMonths(-1), ExpiryDate = now.AddDays(45), Quantity = 75 },
            new DrugBatchEntity { DrugId = "D011", BatchNumber = "GLU202605", ManufactureDate = now.AddMonths(-1), ExpiryDate = now.AddDays(300), Quantity = 200 },
            new DrugBatchEntity { DrugId = "D012", BatchNumber = "IOD202509", ManufactureDate = now.AddMonths(-6), ExpiryDate = now.AddDays(10), Quantity = 18 });

        var transactions = new List<StockTransactionEntity>();
        for (var i = 0; i < 20; i++)
        {
            transactions.Add(new StockTransactionEntity
            {
                DrugId = "D002",
                TransactionType = StockTransactionTypes.Out,
                Quantity = 3 + i % 5,
                Operator = "admin",
                CreatedAt = now.AddDays(-(i + 1)),
                Remark = "演示出库"
            });
        }
        for (var i = 0; i < 12; i++)
        {
            transactions.Add(new StockTransactionEntity
            {
                DrugId = "D001",
                TransactionType = StockTransactionTypes.Out,
                Quantity = 2,
                Operator = "admin",
                CreatedAt = now.AddDays(-(i + 2)),
                Remark = "演示出库"
            });
        }
        for (var i = 0; i < 8; i++)
        {
            transactions.Add(new StockTransactionEntity
            {
                DrugId = "D007",
                TransactionType = StockTransactionTypes.Out,
                Quantity = 4,
                Operator = "operator1",
                CreatedAt = now.AddDays(-(i + 3)),
                Remark = "演示出库"
            });
        }
        transactions.Add(new StockTransactionEntity { DrugId = "D003", TransactionType = StockTransactionTypes.In, Quantity = 100, Operator = "admin", CreatedAt = now.AddDays(-10), Remark = "演示入库" });
        transactions.Add(new StockTransactionEntity { DrugId = "D011", TransactionType = StockTransactionTypes.In, Quantity = 50, Operator = "operator1", CreatedAt = now.AddDays(-8), Remark = "采购收货入库" });
        transactions.Add(new StockTransactionEntity { DrugId = "D005", TransactionType = StockTransactionTypes.Adjust, Quantity = 5, Operator = "admin", CreatedAt = now.AddDays(-6), Remark = "演示盘盈" });
        transactions.Add(new StockTransactionEntity { DrugId = "D008", TransactionType = StockTransactionTypes.Adjust, Quantity = -3, Operator = "admin", CreatedAt = now.AddDays(-5), Remark = "演示盘亏" });
        transactions.Add(new StockTransactionEntity { DrugId = "D009", TransactionType = StockTransactionTypes.Return, Quantity = 10, Operator = "operator1", CreatedAt = now.AddDays(-4), Remark = "客户退货入库" });
        transactions.Add(new StockTransactionEntity { DrugId = "D006", TransactionType = StockTransactionTypes.Damage, Quantity = 2, Operator = "admin", CreatedAt = now.AddDays(-3), Remark = "破损报损" });
        transactions.Add(new StockTransactionEntity { DrugId = "D012", TransactionType = StockTransactionTypes.Damage, Quantity = 1, Operator = "operator1", CreatedAt = now.AddDays(-2), Remark = "过期报损" });
        context.StockTransactions.AddRange(transactions);
        await context.SaveChangesAsync();

        var draftOrder = new PurchaseOrderEntity
        {
            SupplierId = supplier1.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            CreatedBy = "admin",
            CreatedAt = now.AddDays(-2),
            Lines = new List<PurchaseOrderLineEntity>
            {
                new() { DrugId = "D001", Quantity = 155, ReceivedQuantity = 0 },
                new() { DrugId = "D012", Quantity = 182, ReceivedQuantity = 0 }
            }
        };
        var pendingOrder = new PurchaseOrderEntity
        {
            SupplierId = supplier2.SupplierId,
            Status = PurchaseOrderStatus.Pending,
            CreatedBy = "operator1",
            CreatedAt = now.AddDays(-5),
            Lines = new List<PurchaseOrderLineEntity>
            {
                new() { DrugId = "D004", Quantity = 175, ReceivedQuantity = 0 },
                new() { DrugId = "D008", Quantity = 165, ReceivedQuantity = 0 }
            }
        };
        var approvedOrder = new PurchaseOrderEntity
        {
            SupplierId = supplier1.SupplierId,
            Status = PurchaseOrderStatus.Approved,
            CreatedBy = "admin",
            CreatedAt = now.AddDays(-7),
            ApprovedAt = now.AddDays(-6),
            Lines = new List<PurchaseOrderLineEntity>
            {
                new() { DrugId = "D005", Quantity = 110, ReceivedQuantity = 0 },
                new() { DrugId = "D006", Quantity = 140, ReceivedQuantity = 0 }
            }
        };
        var receivedOrder = new PurchaseOrderEntity
        {
            SupplierId = supplier3.SupplierId,
            Status = PurchaseOrderStatus.Received,
            CreatedBy = "admin",
            CreatedAt = now.AddDays(-15),
            ApprovedAt = now.AddDays(-14),
            Lines = new List<PurchaseOrderLineEntity>
            {
                new() { DrugId = "D009", Quantity = 90, ReceivedQuantity = 90 },
                new() { DrugId = "D010", Quantity = 50, ReceivedQuantity = 50 }
            }
        };
        var cancelledOrder = new PurchaseOrderEntity
        {
            SupplierId = supplier2.SupplierId,
            Status = PurchaseOrderStatus.Cancelled,
            CreatedBy = "operator1",
            CreatedAt = now.AddDays(-3),
            Lines = new List<PurchaseOrderLineEntity>
            {
                new() { DrugId = "D003", Quantity = 50, ReceivedQuantity = 0 }
            }
        };
        context.PurchaseOrders.AddRange(draftOrder, pendingOrder, approvedOrder, receivedOrder, cancelledOrder);
        await context.SaveChangesAsync();

        await SeedAuditLogsAsync(context, now);

        Console.WriteLine("演示数据：写入完成（12 种药品、批次、全类型流水、5 张采购单、审计日志）。");
    }

    private static async Task SeedSupplementaryDemoDataAsync(PharmaceuticalDbContext context)
    {
        var now = DateTime.UtcNow;
        var added = false;

        if (!await context.AuditLogs.AnyAsync())
        {
            await SeedAuditLogsAsync(context, now);
            added = true;
            Console.WriteLine("演示数据：已补充审计日志。");
        }

        if (!await context.PurchaseOrders.AnyAsync(o => o.Status == PurchaseOrderStatus.Cancelled))
        {
            var supplier = await context.Suppliers.OrderBy(s => s.SupplierId).FirstOrDefaultAsync();
            if (supplier != null && await context.Drugs.AnyAsync(d => d.DrugId == "D003"))
            {
                context.PurchaseOrders.Add(new PurchaseOrderEntity
                {
                    SupplierId = supplier.SupplierId,
                    Status = PurchaseOrderStatus.Cancelled,
                    CreatedBy = "operator1",
                    CreatedAt = now.AddDays(-3),
                    Lines = new List<PurchaseOrderLineEntity>
                    {
                        new() { DrugId = "D003", Quantity = 50, ReceivedQuantity = 0 }
                    }
                });
                await context.SaveChangesAsync();
                added = true;
                Console.WriteLine("演示数据：已补充已取消采购单。");
            }
        }

        if (!added)
            Console.WriteLine("演示数据：库中已有完整演示数据，无需补充。");
    }

    private static async Task SeedAuditLogsAsync(PharmaceuticalDbContext context, DateTime now)
    {
        var logs = new List<AuditLogEntity>
        {
            new() { EntityType = "Drug", EntityId = "D001", Action = "Create", UserName = "admin", NewValues = "{\"drugName\":\"阿莫西林胶囊\"}", CreatedAt = now.AddDays(-20) },
            new() { EntityType = "Drug", EntityId = "D002", Action = "Update", UserName = "admin", OldValues = "{\"retailPrice\":20.00}", NewValues = "{\"retailPrice\":22.00}", CreatedAt = now.AddDays(-18) },
            new() { EntityType = "Stock", EntityId = "D001", Action = "StockIn", UserName = "admin", NewValues = "{\"quantity\":100,\"type\":\"IN\"}", CreatedAt = now.AddDays(-10) },
            new() { EntityType = "Stock", EntityId = "D002", Action = "StockOut", UserName = "operator1", NewValues = "{\"quantity\":5,\"type\":\"OUT\"}", CreatedAt = now.AddDays(-9) },
            new() { EntityType = "Stock", EntityId = "D005", Action = "Adjust", UserName = "admin", OldValues = "{\"quantity\":85}", NewValues = "{\"quantity\":90}", CreatedAt = now.AddDays(-6) },
            new() { EntityType = "Stock", EntityId = "D009", Action = "Return", UserName = "operator1", NewValues = "{\"quantity\":10,\"type\":\"RETURN\"}", CreatedAt = now.AddDays(-4) },
            new() { EntityType = "Stock", EntityId = "D006", Action = "Damage", UserName = "admin", NewValues = "{\"quantity\":2,\"type\":\"DAMAGE\"}", CreatedAt = now.AddDays(-3) },
            new() { EntityType = "PurchaseOrder", EntityId = "2", Action = "Submit", UserName = "operator1", NewValues = "{\"status\":\"Pending\"}", CreatedAt = now.AddDays(-5) },
            new() { EntityType = "PurchaseOrder", EntityId = "3", Action = "Approve", UserName = "admin", NewValues = "{\"status\":\"Approved\"}", CreatedAt = now.AddDays(-6) },
            new() { EntityType = "PurchaseOrder", EntityId = "4", Action = "Receive", UserName = "admin", NewValues = "{\"status\":\"Received\"}", CreatedAt = now.AddDays(-14) },
            new() { EntityType = "Supplier", EntityId = "1", Action = "Update", UserName = "admin", OldValues = "{\"phone\":\"010-88880000\"}", NewValues = "{\"phone\":\"010-88886666\"}", CreatedAt = now.AddDays(-12) },
            new() { EntityType = "User", EntityId = "operator1", Action = "RoleChange", UserName = "admin", NewValues = "{\"role\":\"Operator\"}", CreatedAt = now.AddDays(-30) }
        };
        context.AuditLogs.AddRange(logs);
        await context.SaveChangesAsync();
    }
}
