using System.ComponentModel.DataAnnotations;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class PurchaseReq2
    {
        public int? ITEM_CODE { get; set; }
        public string ItemName { get; set; }
        public int MAKE_CODE { get; set; }

        public String Make { get; set; }

        public string TECH_DESC { get; set; }
        public int UOM_CODE { get; set; }
        public decimal STD_REQ { get; set; }
        public decimal CUR_STK { get; set; }
        public decimal?AVG_CONS { get; set; }
        public decimal RESERVE_QTY { get; set; }
        public decimal OPEN_POQTY { get; set; }
        public decimal OPEN_RQQTY { get; set; }
        public decimal USER_QTY { get; set; }
        public decimal REQ_QTY { get; set; }
        public string REQ_REASON { get; set; }
        public string REMARKS { get; set; }
        public decimal APROX_RATE { get; set; }
        public string PRIORITY_TYPE { get; set; }
        public string SCRAP_TYPE { get; set; }
        public string PLACE_USE { get; set; }
        public string WORK_TYPE { get; set; }
        public string APROV_STATUS { get; set; }
        public string APROV_REMARKS { get; set; }
        public int STATUS { get; set; }




    }

}


