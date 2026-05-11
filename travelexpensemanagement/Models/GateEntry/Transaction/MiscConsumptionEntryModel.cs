namespace travelexpensemanagement.Models
{
    public class MiscConsumptionEntryModel
    {
        public MiscConsumptionEntry_Header Header { get; set; }

        public List<Details> Deatils { get; set; }

    }

    public class MiscConsumptionEntry_Header
    {
        public string? action { get; set; }
        public string? Add1 { get; set; }
        public string? Add2 { get; set; }
        public string? Add3 { get; set; }

        public string? BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public string? PARTY_GST { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? PARTY_PINCODE { get; set; }
        public int? PARTY_ADDRESSID { get; set; }
        public int? PARTY_CITY { get; set; }
        public int? PARTY_CODE { get; set; }
        public string? REMARKS { get; set; }
        public string? TRUCK_NO { get; set; }
        public string? V_TIME { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public string? WAYBILL_NO { get; set; }

        public string? REF_TYPE { get; set; }

        public int? REF_NO { get; set; }

        public DateTime? RETURN_DATE { get; set; }

        public string? RESPONSIBLE_PERSONB { get; set; }
        public string? ITEM_TYPE { get; set; }

        public string? City { get; set; }

        public int? STATE_CODE { get; set; }

        public string? state { get; set; }
        public string? PartyAddress { get; set; }
        public string? VtypeCode { get; set; }





    
    }

    public class Details

    {

        public int? ITEM_CODE { get; set; }

        public string? ITEM_NAME { get; set; }

        public int? DEPT_CODE { get; set; }
        public int? NOS { get; set; }

        public decimal? QTY { get; set; }

        public int? UOM_CODE { get; set; }

        public string? UOM_NAME { get; set; }

        public string? REMARKS { get; set; }

        public int? REF_NO { get; set; }
        public string? REF_TYPE { get; set; }






    }


}
