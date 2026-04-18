using System.ComponentModel.DataAnnotations;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Models
{
     
    public class UpdateEmpBankDataModel
    {

        public int? CODE { get; set; }

        public int? EMP_CODE { get; set; }

        public String? EMP_Name { get; set; }
          
        public int? BANK_CODE { get; set; }

        public string? BANK_NAME { get; set; }

        public string? BRANCH { get; set; }

        public string? AC_NO { get; set; }

        public string? IFSC_CODE { get; set; }

        public string? AC_TYPE { get; set; }

        public string? BANK_VERIFY { get; set; }

        public String? action { get; set; }
        public String? FileName { get; set; }
        public String? Filepath { get; set; }

    }
}
