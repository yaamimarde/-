using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pharmaceutical.Core;
using System.Text.Json;

namespace Pharmaceutical.Infrastructure.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context == null) return;
        if (_httpContextAccessor.HttpContext == null) return;

        var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
        var now = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLogEntity)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                oldValues = TrySerialize(entry.OriginalValues.ToObject());

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                newValues = TrySerialize(entry.CurrentValues.ToObject());

            context.Set<AuditLogEntity>().Add(new AuditLogEntity
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserName = userName,
                OldValues = oldValues,
                NewValues = newValues,
                CreatedAt = now
            });
        }
    }

    private static string GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null) return "unknown";
        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join("-", values);
    }

    private static string? TrySerialize(object? value)
    {
        if (value == null) return null;
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString();
        }
    }
}
