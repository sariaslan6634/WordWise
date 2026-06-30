using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Infrastructure.Persistence.Context
{
    public class WordWiseDbContext(DbContextOptions<WordWiseDbContext> options) : DbContext(options), IWordWiseDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<UserWord> UserWords { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<UserQuizAnswer> UserQuizAnswers { get; set; }
        public DbSet<UserXpHistory> UserXpHistories { get; set; }
        public DbSet<VideoCandidate> VideoCandidates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(WordWiseDbContext).Assembly);

            builder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Word>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Video>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<VideoCandidate>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<UserWord>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<QuizQuestion>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<UserQuizAnswer>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<UserXpHistory>().HasQueryFilter(x => !x.IsDeleted);
        }
        public override int SaveChanges()
        {
            SetAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void SetAuditFields()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
