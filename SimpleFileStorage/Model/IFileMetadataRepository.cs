namespace SimpleFileStorage.Model
{
    // IFileMetadataRepository - хранилище метаданных файла
    public interface IFileMetadataRepository
    {
        Task Insert(FileMetadata metadata);
        Task<FileMetadata?> Get(Guid fileID);
    }
}
