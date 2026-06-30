using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces.Repositories;
using WordWise.Infrastructure.Persistence.Context;

namespace WordWise.Infrastructure.Persistence
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly WordWiseDbContext _context;

        // Lazy initialization — sadece kullanıldığında oluşturulur
        private IWordRepository? _words;
        private IVideoRepository? _videos;
        private IVideoCandidateRepository? _videoCandidates;

        public UnitOfWork(WordWiseDbContext context)
        {
            _context = context;
        }

        public IWordRepository Words
            => _words ??= new WordRepository(_context);

        public IVideoRepository Videos
            => _videos ??= new VideoRepository(_context);

        public IVideoCandidateRepository VideoCandidates
            => _videoCandidates ??= new VideoCandidateRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public void Dispose()
            => _context.Dispose();
    }
}
