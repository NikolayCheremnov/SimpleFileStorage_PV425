using SimpleFileStorage.Model;

namespace SimpleFileStorageTests.Model;

[TestClass]
public class FileServiceTest
{
    private InMemoryMetadataRepository _metadataRepository = null!;
    private InMemoryFileDataRepository _fileDataRepository = null!;
    private FileService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _metadataRepository = new InMemoryMetadataRepository();
        _fileDataRepository = new InMemoryFileDataRepository();
        _sut = new FileService(_metadataRepository, _fileDataRepository);
    }

    [TestMethod]
    public async Task Upload_ReturnsNonEmptyGuid()
    {
        var fileId = await _sut.Upload(CreateUploadParam());

        Assert.AreNotEqual(Guid.Empty, fileId);
    }

    [TestMethod]
    public async Task Upload_PersistsMetadataWithCorrectFields()
    {
        var param = CreateUploadParam(
            fileName: "report.pdf",
            contentType: "application/pdf",
            data: [0x01, 0x02, 0x03]);

        var beforeUpload = DateTime.UtcNow;
        var fileId = await _sut.Upload(param);
        var afterUpload = DateTime.UtcNow;

        var metadata = await _metadataRepository.Get(fileId);

        Assert.IsNotNull(metadata);
        Assert.AreEqual(fileId, metadata.FileID);
        Assert.AreEqual("report.pdf", metadata.FileName);
        Assert.AreEqual("application/pdf", metadata.ContentType);
        Assert.AreEqual(3, metadata.SizeBytes);
        Assert.IsTrue(metadata.UploadedAt >= beforeUpload);
        Assert.IsTrue(metadata.UploadedAt <= afterUpload);
    }

    [TestMethod]
    public async Task Upload_PersistsFileDataWithSameIdAndContent()
    {
        var content = "hello"u8.ToArray();
        var param = CreateUploadParam(data: content);

        var fileId = await _sut.Upload(param);

        var fileData = await _fileDataRepository.Get(fileId);

        Assert.IsNotNull(fileData);
        Assert.AreEqual(fileId, fileData.FileID);
        CollectionAssert.AreEqual(content, fileData.Data);
    }

    [TestMethod]
    public async Task GetFileMetadata_WhenFileExists_ReturnsMetadata()
    {
        var fileId = Guid.NewGuid();
        var expected = new FileMetadata
        {
            FileID = fileId,
            FileName = "photo.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            UploadedAt = DateTime.UtcNow,
        };
        await _metadataRepository.Insert(expected);

        var actual = await _sut.GetFileMetadata(fileId);

        Assert.AreEqual(expected.FileID, actual.FileID);
        Assert.AreEqual(expected.FileName, actual.FileName);
        Assert.AreEqual(expected.ContentType, actual.ContentType);
        Assert.AreEqual(expected.SizeBytes, actual.SizeBytes);
        Assert.AreEqual(expected.UploadedAt, actual.UploadedAt);
    }

    [TestMethod]
    public async Task GetFileMetadata_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _sut.GetFileMetadata(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task GetFileData_WhenFileExists_ReturnsData()
    {
        var fileId = Guid.NewGuid();
        var expected = new FileData
        {
            FileID = fileId,
            Data = [0xAA, 0xBB],
        };
        await _fileDataRepository.Insert(expected);

        var actual = await _sut.GetFileData(fileId);

        Assert.AreEqual(expected.FileID, actual.FileID);
        CollectionAssert.AreEqual(expected.Data, actual.Data);
    }

    [TestMethod]
    public async Task GetFileData_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _sut.GetFileData(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task Upload_ThenGetFileMetadata_ReturnsUploadedMetadata()
    {
        var param = CreateUploadParam(
            fileName: "notes.txt",
            contentType: "text/plain",
            data: "test content"u8.ToArray());

        var fileId = await _sut.Upload(param);

        var metadata = await _sut.GetFileMetadata(fileId);

        Assert.AreEqual(fileId, metadata.FileID);
        Assert.AreEqual(param.FileName, metadata.FileName);
        Assert.AreEqual(param.ContentType, metadata.ContentType);
        Assert.AreEqual(param.Data.Length, metadata.SizeBytes);
    }

    [TestMethod]
    public async Task Upload_ThenGetFileData_ReturnsUploadedContent()
    {
        var param = CreateUploadParam(data: "binary payload"u8.ToArray());

        var fileId = await _sut.Upload(param);

        var fileData = await _sut.GetFileData(fileId);

        Assert.AreEqual(fileId, fileData.FileID);
        CollectionAssert.AreEqual(param.Data, fileData.Data);
    }

    private static UploadFileParam CreateUploadParam(
        string fileName = "file.bin",
        string contentType = "application/octet-stream",
        byte[]? data = null)
    {
        return new UploadFileParam
        {
            FileName = fileName,
            ContentType = contentType,
            Data = data ?? [0x00],
        };
    }

    private sealed class InMemoryMetadataRepository : IFileMetadataRepository
    {
        private readonly Dictionary<Guid, FileMetadata> _store = new();

        public Task Insert(FileMetadata metadata)
        {
            _store[metadata.FileID] = metadata;
            return Task.CompletedTask;
        }

        public Task<FileMetadata?> Get(Guid fileID)
        {
            _store.TryGetValue(fileID, out var metadata);
            return Task.FromResult(metadata);
        }
    }

    private sealed class InMemoryFileDataRepository : IFileDataRepository
    {
        private readonly Dictionary<Guid, FileData> _store = new();

        public Task Insert(FileData data)
        {
            _store[data.FileID] = data;
            return Task.CompletedTask;
        }

        public Task<FileData?> Get(Guid fileID)
        {
            _store.TryGetValue(fileID, out var data);
            return Task.FromResult(data);
        }
    }
}
