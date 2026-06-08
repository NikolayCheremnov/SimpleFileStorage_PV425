using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleFileStorage.Model;
using SimpleFileStorage.ObjectStorage;
using SimpleFileStorage.Postgres;
using SimpleFileStorage.Stub;

var builder = WebApplication.CreateBuilder(args);

// ВАРИАНТ КОНФИГУРАЦИИ СЕРВИСА 1: С ЗАГЛУШКАМИ
//builder.Services.AddSingleton<IFileMetadataRepository, RepositoriesStub>();
//builder.Services.AddSingleton<IFileDataRepository, RepositoriesStub>();
//builder.Services.AddSingleton<FileService>();

// ВАРИАНТ КОНФИГУРАЦИИ СЕРВИСА 2: С БД Postgres
//builder.Services.AddDbContextFactory<ApplicationDbContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder opts) => {
//    // для определения используемой строки подключения задействуется значения env-переменной DB_CONNECTION_PROFILE
//    // TODO: прочитать про env vars (environment variables - переменные окружения) - их использование и реализация в .NET
//    IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();
//    string dbConnectionProfile = Environment.GetEnvironmentVariable("DB_CONNECTION_PROFILE") ?? "default";
//    string? connectionString = config.GetConnectionString(dbConnectionProfile);
//    opts.UseNpgsql(connectionString);
//});
//builder.Services.AddSingleton<IFileMetadataRepository, FileStorage>();
//builder.Services.AddSingleton<IFileDataRepository, FileStorage>();
//builder.Services.AddSingleton<FileService>();

// ВАРИАНТ КОНФИГУРАЦИИ СЕРВИСА 3: С БД Postgres + S3
builder.Services.AddDbContextFactory<ApplicationDbContext>((IServiceProvider serviceProvider, DbContextOptionsBuilder opts) => {
    // для определения используемой строки подключения задействуется значения env-переменной DB_CONNECTION_PROFILE
    // TODO: прочитать про env vars (environment variables - переменные окружения) - их использование и реализация в .NET
    IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();
    string dbConnectionProfile = Environment.GetEnvironmentVariable("DB_CONNECTION_PROFILE") ?? "default";
    string? connectionString = config.GetConnectionString(dbConnectionProfile);
    opts.UseNpgsql(connectionString);
});
builder.Services.AddSingleton<IFileMetadataRepository, FileStorage>();
builder.Services.AddSingleton<S3ServicesFactory>();
builder.Services.AddSingleton<IAmazonS3>(opts => opts.GetRequiredService<S3ServicesFactory>().CreateClient());
builder.Services.AddSingleton<IFileDataRepository>(opts => opts.GetRequiredService<S3ServicesFactory>().CreateStorage());
builder.Services.AddSingleton<FileService>();


// ДОБАВЛЕНИЕ КОНТРОЛЛЕРОВ В КОНТЕЙНЕР ЗАВИСИМОСТЕЙ (IoC-контейнер)
builder.Services.AddControllers();

var app = builder.Build();

// ВКЛЮЧЕНИЕ КОНТРОЛЛЕРОВ (МАППИНГ)
app.MapControllers();

// обеспечим существование S3-bucket
await AutoEnsureS3BucketExistsWithBackoff();

// применим БД-миграции
await AutoApplyMigrationsWithBackoff();

app.Run();

// AutoEnsureS3BucketExistsWithBackoff - автоматическое обеспечение существования бакета с backoff-ми
// TODO: прочитать что такое backoff
async Task AutoEnsureS3BucketExistsWithBackoff()
{
    using var scope = app.Services.CreateScope();
    Console.WriteLine("Starting S3 bucket existence processing...");

    IAmazonS3 s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();

    string s3ConnectionProfile = Environment.GetEnvironmentVariable("S3_OPTIONS_PROFILE") ?? "default";
    IConfigurationSection s3Options = builder.Configuration.GetSection("S3Options").GetSection(s3ConnectionProfile);
    string bucketName = s3Options["BucketName"] ?? "default";

    var maxRetryCount = 5;
    var initialDelay = 1000; // 1 секунда
    var maxDelay = 30000; // 30 секунд

    for (int i = 0; i < maxRetryCount; i++)
    {
        try
        {
            Console.WriteLine($"Attempt {i + 1} to connect to s3 ...");
            // попытка проверить существование бакета или создать его
            await s3Client.EnsureBucketExistsAsync(bucketName);
            Console.WriteLine("S3 bucket existence processed");
            break;
        }
        catch (Exception ex) when (i < maxRetryCount - 1)
        {
            var delay = Math.Min(initialDelay * (int)Math.Pow(2, i), maxDelay);
            Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
            Console.WriteLine($"Waiting {delay}ms before next attempt...");
            // асинхронное ожидание
            await Task.Delay(delay);
        }
    }
}

// AutoApplyMigrationsWithBackoff - автомиграция для postgres с backoff-ми
// TODO: прочитать что такое backoff
async Task AutoApplyMigrationsWithBackoff()
{
    using var scope = app.Services.CreateScope();
    Console.WriteLine("Starting migrations processing...");

    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // примитивно заданные параметры backoff-а
    var maxRetryCount = 5;
    var initialDelayMs = 1000; // 1 секунда
    var maxDelayMs = 30000; // 30 секунд

    for (int i = 0; i < maxRetryCount; i++)
    {
        try
        {
            Console.WriteLine($"Attempt {i + 1} to connect to database...");
            // программный вызов применения миграций
            await db.Database.MigrateAsync();
            Console.WriteLine("Migrations were applied successfully if it exists");
            break;
        }
        catch (Exception ex) when (i < maxRetryCount - 1)
        {
            int delay = Math.Min(initialDelayMs * (int)Math.Pow(2, i), maxDelayMs);
            Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
            Console.WriteLine($"Waiting {delay}ms before next attempt...");
            // асинхронное ожидание
            await Task.Delay(delay);
        }
    }
}