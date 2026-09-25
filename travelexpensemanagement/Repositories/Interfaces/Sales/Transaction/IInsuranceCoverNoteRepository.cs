using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IInsuranceCoverNoteRepository
    {
        Task<object> SaveAndUpdateDataAsync(InsuranceCoverNoteModel model);

        Task<object> LoadEditDataAsync(int vNo, string vType);

    }
}
