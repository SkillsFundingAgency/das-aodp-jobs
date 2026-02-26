namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IOfqualRegisterService
    {
        Task<PaginatedResult<QualificationDTO>> SearchPrivateQualificationsAsync(QualificationsQueryParameters parameters);

        List<QualificationDTO> ExtractQualificationsList(PaginatedResult<QualificationDTO> paginatedResult);

        QualificationsQueryParameters ParseQueryParameters(NameValueCollection query);
    }
}
