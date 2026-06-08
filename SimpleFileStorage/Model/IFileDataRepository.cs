namespace SimpleFileStorage.Model
{
    // IFileDataRepository - хранилище самих файлов
    public interface IFileDataRepository
    {
        Task Insert(FileData data);
        Task<FileData?> Get(Guid fileID);
    }
}
