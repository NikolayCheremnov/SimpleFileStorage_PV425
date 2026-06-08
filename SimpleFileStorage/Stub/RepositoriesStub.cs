using SimpleFileStorage.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleFileStorage.Stub
{
    // RepositoriesStub - in-memory заглушка для IFileMetadataRepository и IFileDataRepository
    public class RepositoriesStub : IFileMetadataRepository, IFileDataRepository
    {
        private static Dictionary<Guid, FileMetadata> metadatas = new();
        private static Dictionary<Guid, FileData> files = new();

        public RepositoriesStub() { }

        public async Task<FileData?> Get(Guid fileID)
        {
            if (files.ContainsKey(fileID))
            {
                return files[fileID];
            }
            return null;
        }

        public async Task Insert(FileData data)
        {
            files[data.FileID] = data;
        }

        public async Task Insert(FileMetadata metadata)
        {
            metadatas[metadata.FileID] = metadata;
        }

        async Task<FileMetadata?> IFileMetadataRepository.Get(Guid fileID)
        {
            if (metadatas.ContainsKey(fileID))
            {
                return metadatas[fileID];
            }
            return null;
        }
    }
}
