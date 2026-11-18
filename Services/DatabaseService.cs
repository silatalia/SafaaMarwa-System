using SQLite;
using SafaaAndMarwaSystem.Models;

namespace SafaaAndMarwaSystem.Services;

public sealed class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public string DatabasePath { get; }

    public DatabaseService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SafaaAndMarwaSystem");
        Directory.CreateDirectory(folder);
        DatabasePath = Path.Combine(folder, "safaa_data.db3");
        _db = new SQLiteAsyncConnection(DatabasePath);
    }

    public async Task InitializeAsync()
    {
        await _db.CreateTableAsync<TruckRecord>();
    }

    public Task<List<TruckRecord>> GetRecordsAsync()
        => _db.Table<TruckRecord>()
              .Where(x => !x.IsDeleted)
              .OrderByDescending(x => x.RecordDate)
              .ToListAsync();

    public async Task<int> SaveRecordAsync(TruckRecord record)
    {
        record.Recalculate();
        if (record.Id == 0)
            return await _db.InsertAsync(record);

        return await _db.UpdateAsync(record);
    }

    public async Task SoftDeleteAsync(int id, string deviceName)
    {
        var record = await _db.FindAsync<TruckRecord>(id);
        if (record is null) return;

        record.IsDeleted = true;
        record.DeletedAt = DateTime.Now;
        record.DeletedDevice = deviceName;
        await _db.UpdateAsync(record);
    }

    public async Task<string> CreateBackupAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SafaaAndMarwaBackups");
            Directory.CreateDirectory(backupFolder);

            var fileName = $"SafaaMarwa_{DateTime.Now:yyyyMMdd_HHmmss}.db3";
            var destination = Path.Combine(backupFolder, fileName);
            var escaped = destination.Replace("'", "''");

            // VACUUM INTO creates a consistent standalone SQLite snapshot.
            await _db.ExecuteAsync($"VACUUM INTO '{escaped}'");
            return destination;
        }
        finally
        {
            Gate.Release();
        }
    }
}
