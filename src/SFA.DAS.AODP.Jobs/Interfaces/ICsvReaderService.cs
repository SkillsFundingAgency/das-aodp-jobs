using CsvHelper.Configuration;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ICsvReaderService
    {
        List<T> ReadCSVFromFilePath<T, TMap>(string filePath) where TMap : ClassMap<T>;
        Task<List<T>> ReadCsvFileFromUrlAsync<T, TMap>(string urlFilePath) where TMap : ClassMap<T>;
        Task<List<T>> ReadApprovedAndArchivedFromUrlAsync<T, TMap>(string approvedUrlFilePath, string archivedUrlFilePath) where TMap : ClassMap<T>;


    }
}