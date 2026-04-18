using DocumentFormat.OpenXml.Office.CoverPageProps;
using DocumentFormat.OpenXml.Wordprocessing;

namespace travelexpensemanagement.Models.Sale
{
    public class Sale_TransportRateQuatation_Model
    {

        public class Sale_TransportRateQuatation_Request
        {
            public sale_TransportRate_Header? Header { get; set; }
            public List<sale_TransportRate_Detail>? Detail { get; set; }
        }

        public class sale_TransportRate_Header
        {
            public String? DOC_ID { get; set; }
            public int? V_NO { get; set; }
            public DateTime V_DATE { get; set; }
            public String? DO_TYPE { get; set; }
            public int? DO_NO { get; set; }
            public int? BILL_CODE { get; set; }  
            public String? BILL_NAME { get; set; }
            public String? SHIP_ADD1 { get; set; }
            public String? SHIP_ADD2 { get; set; }
            public String? SHIP_ADD3 { get; set; }
            public int? SHIP_CITY { get; set; }
            public String? REMARKS { get; set; }
            public String? FAPROV_STATUS { get; set; }
            public String? FAPROV_REMARKS { get; set; }
            public decimal? QTY { get; set; }
            public string? action { get; set; }

            public decimal? Rate { get; set;  }
           
            public String? TransportName { get; set; }
            }

        public class sale_TransportRate_Detail
        {
            public int? TRANSPORT_CODE { get; set; }
            public String? TRANSPORT_NAME { get; set; }
            public decimal? RATE { get; set; }
            public decimal? OUR_RATE { get; set; }
            public String? TRUCK_NO { get; set; }
            public String? GRNO { get; set; }
            public DateTime? GRDATE { get; set; }
            
        }

    }
} 
