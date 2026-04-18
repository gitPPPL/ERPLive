namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class TRANSPORT_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string NAME { get; set; }
        public int PARTY_CODE { get; set; }
        public string OWNER_NAME { get; set; }
        public string ADDRESS { get; set; }
        public string GSTIN { get; set; }
        public string PAN { get; set; }
        public byte DECL_YN { get; set; }
        public string DECL_NO { get; set; }
        public DateTime DECL_DATE { get; set; }
        public DateTime EXPIRY_DATE { get; set; }
        public decimal TDS_PER { get; set; }
        public int ACTIVE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public string SALE_GROUP { get; set; }
        public string ACTION { get; set; }
        public string? PartyName { get; set; }

    }
}
