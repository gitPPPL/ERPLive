using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sale.Transaction
{
    public interface ISalesProformaInvoice
    {

        Task<(string Status, string Message)> SubmitRequest(SalesProformaInvoice_Header header, List<SalesProformaInvoice_Detail> details, string action);

    }
}
