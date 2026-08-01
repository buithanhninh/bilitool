using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class ClinicalAuditGovernanceTests
{
    [Fact]
    public async Task ClinicalAudit_CannotBeUpdatedOrDeletedThroughChangeTracker()
    {
        var options = new DbContextOptionsBuilder<BiliToolDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new BiliToolDbContext(options);
        var audit = new ClinicalAuditLog { RequestJson = "{}", ResponseJson = "{}", TraceJson = "{}" };
        db.Add(audit);
        await db.SaveChangesAsync();

        audit.EngineVersion = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        db.Entry(audit).State = EntityState.Unchanged;
        db.Remove(audit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
