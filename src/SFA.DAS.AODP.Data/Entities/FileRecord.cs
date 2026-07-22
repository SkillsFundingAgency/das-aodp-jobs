using SFA.DAS.AODP.Common.Enum;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Data.Entities.Files
{
    [ExcludeFromCodeCoverage]
    public partial class FileRecord
    {
        public Guid Id { get; set; }


        /// <summary>
        /// Identifies the application that this file is associated with.
        /// 
        /// This value is set for all application-scoped files, including message
        /// attachments, question uploads, and application-level artefacts.
        /// 
        /// It is null for system-scoped files such as imports or background job outputs.
        /// </summary>
        public Guid? ApplicationId { get; set; }

        /// <summary>
        /// Identifies the application message that this file is attached to.
        /// 
        /// This value is set only for files whose category represents a message
        /// attachment. It allows files to be displayed alongside message timelines
        /// without relying on storage path conventions.
        /// 
        /// For non-message files, this value is null.
        /// </summary>
        public Guid? MessageId { get; set; }

        /// <summary>
        /// Identifies the form question that this file was uploaded in response to.
        /// 
        /// This value is set only for files uploaded as part of a question response,
        /// such as evidence or supporting documents.
        /// 
        /// For files not related to form questions, this value is null.
        /// </summary>
        public Guid? QuestionId { get; set; }

        /// <summary>
        /// Container name used to correlate storage events and downloads.
        /// </summary>
        public string BlobContainer { get; set; } = null!;

        /// <summary>
        /// Full blob path used to correlate storage events and downloads.
        /// </summary>
        public string BlobPath { get; set; } = null!;

        /// <summary>
        /// The original name of the file as provided by the user at upload time.
        /// 
        /// This value is used for display purposes in the UI and for download responses.
        /// It is independent of the blob storage path and must not be modified or derived
        /// from storage identifiers.
        /// 
        /// The file name is not required to be unique and should be treated as metadata
        /// only; the file is uniquely identified by its Id.
        /// </summary>

        public string FileName { get; set; } = null!;

        /// <summary>
        /// MIME content type of the file, used when serving the file for download.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Defines what the file is used for (for example, message attachment,
        /// question upload,  or system import), independent of where the file
        /// is stored or which entity it is associated with.
        /// </summary>
        public FileCategory FileCategory { get; set; }

        /// <summary>
        /// UTC timestamp when the file was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Display name of the user who uploaded the file.
        /// </summary>
        public string UploadedByDisplayName { get; set; } = null!;


        /// <summary>
        /// Malware scan outcome for the file.
        /// 
        /// This value is set by the malware scanning process once a scan completes.
        /// The default value is <see cref="MalwareScanStatus.NotScanned"/>, which indicates
        /// that the file has not yet been scanned or that no scan result is available.
        /// </summary>

        public MalwareScanStatus ScanResult { get; set; } = MalwareScanStatus.NotScanned;

        /// <summary>
        /// UTC timestamp of the last malware scan, if performed.
        /// </summary>
        public DateTime? LastScanAt { get; set; }

    }
}
