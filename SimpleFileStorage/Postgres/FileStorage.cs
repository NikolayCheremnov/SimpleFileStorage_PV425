using Microsoft.EntityFrameworkCore;
using SimpleFileStorage.Model;

namespace SimpleFileStorage.Postgres
{
    // FileStorage - postgres-хранилище файлов и метаданных файлов
    public class FileStorage : IFileDataRepository, IFileMetadataRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory; // пул соединений с БД

        public FileStorage(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<FileMetadata?> Get(Guid fileID)
        {
            using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Metadatas.FirstOrDefaultAsync(md => md.FileID == fileID);
        }

        public async Task Insert(FileMetadata metadata)
        {
            using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
            await db.Metadatas.AddAsync(metadata);
            await db.SaveChangesAsync();
        }

        public async Task Insert(FileData data)
        {
            using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
            await db.Files.AddAsync(data);
            await db.SaveChangesAsync();
        }

        async Task<FileData?> IFileDataRepository.Get(Guid fileID)
        {
            using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
            return await db.Files.FirstOrDefaultAsync(f => f.FileID == fileID);
        }
    }
}
