using Org.BouncyCastle.Asn1.X509;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Pages;

namespace travelexpensemanagement.Models.Purchase.Transaction
{


    public class PurchaseSauda_model
    {
        public PurchaseSauda_Header Header { get; set; }
        public List<DispatchDeliveryPlaning> DispatchDelivery { get; set; }
        public List<DocumentAttachment> Document { get; set; }

    }

    public class PurchaseSauda_Header
    {
        public string V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }  // Nullable DateTime
        public string DOC_ID { get; set; }
        public int? STATUS { get; set; }  // Nullable int
        public int? PARTY_CODE { get; set; }  // Nullable int
        public string CustomerName { get; set; }
        public string PARTY_TO { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; }
        public string ADD3 { get; set; }
        public int? CITY_CODE { get; set; }  // Nullable int
        public string CityName { get; set; }
        public string Type { get; set; }
        public string PHONE { get; set; }
        public string ITEM_TYPE { get; set; }
        public int? ITEM_CODE { get; set; }  // Nullable int
        public string ItemName { get; set; }
        public int? TRUCK_NO { get; set; }  // Nullable int
        public decimal? QTY { get; set; }  // Nullable decimal
        public decimal? DISC_PER { get; set; }  // Nullable decimal
        public string FRT_TERM { get; set; }
        public decimal? FRT_RATE { get; set; }  // Nullable decimal
        public decimal? TAX_RATE { get; set; }  // Nullable decimal
        public decimal? NET_RATE { get; set; }  // Nullable decimal
        public int? PAYTERM_CODE { get; set; }  // Nullable int
        public string DEL_TERM { get; set; }
        public string REMARK { get; set; }
        public decimal? WASTE_PER { get; set; }  // Nullable decimal
        public int? DEAL_THROUGH { get; set; }  // Nullable int
        public string SHIP_TYPE { get; set; }
        public string Delivery_From { get; set; }
        public string COUNTRY { get; set; }
        public int? COUNTRY_CODE { get; set; }  // Nullable int
        public string action { get; set; }
        public int? REF_NO { get; set; }  // Nullable int
        public int? SHIP_CODE { get; set; }  // Nullable int
        public decimal? EXRATE { get; set; }  // Nullable decimal
        public decimal? RATE { get; set; }  // Nullable decimal
        public string CURRENCY { get; set; }
        public int? TAX_CODE { get; set; }  // Nullable int
        public int? ONLY_NATURAL { get; set; }  // Nullable int

        public int? REF_REQNO { get; set; }  // Nullable int
        public string HOLD_PAY { get; set; }
        public string PINO { get; set; }
        public DateTime? PIDATE { get; set; }  // Nullable DateTime
        public string OFFERNO { get; set; }
        public string GRADE { get; set; }
        public int? BROKER { get; set; }  // Nullable int
        public decimal? BROKER_RATE { get; set; }  // Nullable decimal
        public int? DISPATCH_FROM { get; set; }  // Nullable int
        public string SHIP_FROM { get; set; }
        public string PACK_TYPE { get; set; }
        public DateTime? SBLC_DUEDATE { get; set; } 
        public DateTime? LC_DUEDATE { get; set; }  
        public string ITEM_REMARKS { get; set; }

        public string PAYMENT_STATUS { get; set; }


        public string REF_TYPE { get; set; }


        public string TAX_TERM { get; set; }    




    }








    public class DispatchDeliveryPlaning
    {

        public string ItemName { get; set; }
        public int ItemCode { get; set; }
        public DateTime DeliveryDate { get; set; } // DateTime to match the formatted date from JS
        public decimal Qty { get; set; } // Nullable decimal to match optional quantity
        public string Remarks { get; set; }

        public int v_no { get; set; }

        public DateTime? V_DATE { get; set; }


    }


    public class DocumentAttachment
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }

    }


}
