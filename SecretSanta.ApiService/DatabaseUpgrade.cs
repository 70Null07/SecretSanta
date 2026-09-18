using Microsoft.EntityFrameworkCore;

namespace SecretSanta.ApiService;

public static class DatabaseUpgrade
{
    public static async Task ApplyAssignmentConstraintsAsync(
        this AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Ensure that only one application instance performs this upgrade at a time. Existing
        // EnsureCreated databases do not have migration history, so this explicit upgrade keeps
        // them compatible while fresh databases still receive the indexes from the EF model.
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtext('SecretSanta.AssignmentConstraints'));",
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TEMP TABLE "GamesWithDuplicateAssignments" ON COMMIT DROP AS
            SELECT "GameId"
            FROM "SantaAssignments"
            GROUP BY "GameId", "GiverUserId"
            HAVING COUNT(*) > 1
            UNION
            SELECT "GameId"
            FROM "SantaAssignments"
            GROUP BY "GameId", "ReceiverUserId"
            HAVING COUNT(*) > 1;

            DELETE FROM "SantaAssignments"
            WHERE "GameId" IN (SELECT "GameId" FROM "GamesWithDuplicateAssignments");

            UPDATE "Games"
            SET "IsDrawn" = FALSE
            WHERE "GameId" IN (SELECT "GameId" FROM "GamesWithDuplicateAssignments");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SantaAssignments_GameId_GiverUserId"
            ON "SantaAssignments" ("GameId", "GiverUserId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SantaAssignments_GameId_ReceiverUserId"
            ON "SantaAssignments" ("GameId", "ReceiverUserId");
            """,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
