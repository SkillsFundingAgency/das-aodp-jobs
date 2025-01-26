using CsvHelper.Configuration;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ICsvReaderService
    {
        List<T> ReadCSVFromFilePath<T, TMap>(string filePath) where TMap : ClassMap<T>;
        Task<List<T>> ReadCsvFileFromUrlAsync<T, TMap>(string urlFilePath) where TMap : ClassMap<T>;
    }
}