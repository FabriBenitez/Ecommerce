namespace pruebaPagoMp.Security;

public interface IBackupRestoreService
{
    Task<BackupPayload> ExportAsync();
    Task ImportAsync(BackupPayload payload);
}
