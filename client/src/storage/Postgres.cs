using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public static class Postgres {
    public static async Task Persist(
        List<Hashtag> hashtags,
        Stopwatch Stopwatch,
        int TweetCounter
    ) {
        Debug.WriteLine(String.Format(
                "{0}: {1} {2} seconds processed {3} tweets.",
                "Starting persist operation", "current stream running for",
                Stopwatch.Elapsed.TotalSeconds.ToString("F0"), TweetCounter
        ));
        using (var db = new PostgresContext()) {
            hashtags.ForEach((Hashtag hashtag) => {
                if (hashtag.persisted == false) {
                    hashtag.persisted = true;
                    var hashtagSaved = db.Hashtags.FindAsync(
                        hashtag.name, hashtag.datehour
                    ).Result;
                    if (hashtagSaved != null) {
                        hashtag.happiness += hashtagSaved.happiness;
                        db.Entry(hashtagSaved).CurrentValues
                            .SetValues(hashtag);
                    } else { db.Hashtags.Add(hashtag); };
                };
            });
            await db.SaveChangesAsync().ContinueWith(
                (Task task) => Program.ClearPersistedHashtags()
            );
        }
    }
    private class PostgresContext : DbContext {
        public PostgresContext() : base() { }
        public DbSet<Hashtag> Hashtags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(Program.ConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableDetailedErrors(true);

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Hashtag>().HasKey(c => new { c.name, c.datehour });
        }
    }
}
