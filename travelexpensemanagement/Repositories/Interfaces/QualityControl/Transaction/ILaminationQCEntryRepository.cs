using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface ILaminationQCEntryRepository
    {
        Task<RepositoryResponse> UpdateLaminationAsync(LaminationUpdateModel model);
        RepositoryResponseData<int> ProcessTenacityDataAsync(TenacityRequest request);
        public class TenacityRequest
        {
            public string StrName { get; set; }
            public int WarpWay { get; set; }
            public int WeftWay { get; set; }
        }
    }
}
