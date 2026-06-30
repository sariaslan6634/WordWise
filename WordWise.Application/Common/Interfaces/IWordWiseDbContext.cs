using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Entities;

namespace WordWise.Application.Common.Interfaces
{
    public interface IWordWiseDbContext
    {
        public DbSet<User> Users { get; }
        public DbSet<Word> Words { get;}
        public DbSet<UserWord> UserWords { get;}
        public DbSet<Video> Videos { get; }
        public DbSet<QuizQuestion> QuizQuestions { get; }
        public DbSet<UserQuizAnswer> UserQuizAnswers { get;  }
        public DbSet<UserXpHistory> UserXpHistories { get; }
        public DbSet<VideoCandidate> VideoCandidates { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
