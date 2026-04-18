using System;
using System.ComponentModel.DataAnnotations;

namespace TravelExpenseManagement.Models.Purchase.Transaction
{

    public class PurchaseReq1Model
    {
        public int? V_NO { get; set; }

        public string V_TYPE { get; set; }

        public DateTime V_DATE { get; set; }

        public int PLACE_CODE { get; set; }

        public string PlaceName { get; set; }

        public string? DOC_ID { get; set; }
          
        public int OWNER_CODE { get; set; }

        public string OWNER_NAME { get; set; }

        public int DEPT_CODE { get; set; }

        public string DEPT_NAME { get; set; }

        public int STATUS { get; set; }

        public DateTime? VALID_DATE { get; set; }

        public DateTime? TARGET_DATE { get; set; }

        public string REASON { get; set; }

        public string REMARKS { get; set; }
    
        public string action { get; set; }

        public int URGENT_REQUEST { get; set; }

        public int PLAN_NO { get; set; }

        public string PLAN_TYPE { get; set; }

        public String Unit { get; set; }





    }



}
