namespace SFA.DAS.AODP.Jobs.Services
{
    public sealed record FileProcessingResult(
    bool IsReady,
    bool IsTimedOut,
    Stream? Stream);
    public interface IFileProcessingService
    {
        Task<FileProcessingResult> GetReadyFileAsync(
            FileCategory category,
            string username,
            Guid jobId,
            Guid jobRunId,
            DateTime startTime,
            CancellationToken cancellationToken);
    }
    public class FileProcessingService : IFileProcessingService
    {
        private readonly IFileRecordRepository _fileRepo;
        private readonly IBlobStorageFileService _blob;
        private readonly IJobConfigurationService _jobService;
        private readonly ISystemClockService _clock;

        public FileProcessingService(
            IFileRecordRepository fileRepo,
            IBlobStorageFileService blob,
            IJobConfigurationService jobService,
            ISystemClockService clock)
        {
            _fileRepo = fileRepo;
            _blob = blob;
            _jobService = jobService;
            _clock = clock;
        }

        public async Task<FileProcessingResult> GetReadyFileAsync(
            FileCategory category,
            string username,
            Guid jobId,
            Guid jobRunId,
            DateTime startTime,
            CancellationToken cancellationToken)
        {
            var file = await _fileRepo.GetByCategoryAsync(category);

            if (file == null || file.ScanResult != MalwareScanStatus.Clean)
            {
                if (startTime < _clock.UtcNow.AddHours(-1))
                {
                    await _jobService.UpdateJobRun(username, jobId, jobRunId, 0, JobStatus.Error);
                    return new FileProcessingResult(false, true, null);
                }

                await _jobService.UpdateJobRun(username, jobId, jobRunId, 0, JobStatus.Requested);
                return new FileProcessingResult(false, false, null);
            }

            var stream = await _blob.DownloadFileAsync(
                file.BlobContainer,
                file.BlobPath,
                cancellationToken);

            return new FileProcessingResult(true, false, stream);
        }
    }
}
