namespace SimpleFileStorage.Model
{
    // FileService - сервис работы с файлами
    public class FileService
    {
        // stateful-объекты - объекты с состоянием
        // stateless-объекты - объекты без состояния
        private readonly IFileMetadataRepository _metadatas;
        private readonly IFileDataRepository _files;

        public FileService(IFileMetadataRepository metadatas, IFileDataRepository files)
        {
            _metadatas = metadatas;
            _files = files;
        }

        // Upload - загрузить файл в систему
        // вход: 
        // выход: внутрисистемный идентификатор сохраненного файла
        public async Task<Guid> Upload(UploadFileParam param)
        {
            // формируем объекты для добавления в хранилище файлов: метаданные и сам файл
            FileMetadata metadata = new FileMetadata()
            {
                FileID = param.FileID,
                FileName = param.FileName,
                ContentType = param.ContentType,
                SizeBytes = param.Data.Length,
                UploadedAt = DateTime.UtcNow,
            };
            FileData data = new FileData()
            {
                FileID = param.FileID,
                Data = param.Data,
            };
            // сохранить метаданные и файл в хранилища
            // TODO: отсутствует транзакционность - при ошибке вставки файла сохраняется ненужные лишние метаданные
            // TODO: проблема решается через добавления логической транзакционность, можно почитать о паттерне SAGA
            await _metadatas.Insert(metadata);
            await _files.Insert(data);
            // возвращаем идентификатор файла для возможности получения метаданных и скачивания файла в дальнейшем
            return param.FileID;
        }

        // GetFileMetadata - получить метаданные файла
        // вход: внутрисистемный идентификатор файла
        // выход: объект метаданных файла
        // исключения: FileNotFoundException если файл с таким id не найден
        public async Task<FileMetadata> GetFileMetadata(Guid fileID)
        {
            FileMetadata? metadata = await _metadatas.Get(fileID);
            if (metadata == null)
            {
                throw new FileNotFoundException();
            }
            return metadata;
        }

        // GetFileData - получить файл
        // вход: внутрисистемный идентификатор файла
        // выход: объект данных файла
        // исключения: FileNotFoundException если файл с таким id не найден
        public async Task<FileData> GetFileData(Guid fileID)
        {
            FileData? data = await _files.Get(fileID);
            if (data == null)
            {
                throw new FileNotFoundException();
            }
            return data;
        }
    }
}
