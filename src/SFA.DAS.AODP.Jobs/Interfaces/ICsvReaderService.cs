using CsvHelper.Configuration;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface ICsvReaderService
    {
        List<T> ReadCSVFromFilePath<T, TMap>(string filePath, params object[] additionalParameters) where TMap : ClassMap<T>;

        Task<List<T>> ReadCsvFileFromStreamAsync<T, TMap>(Stream stream, params object[] additionalParameters) where TMap : ClassMap<T>;
    }
}