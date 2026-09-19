namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class SaleSaudaEntryModel
    {
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }

        public int? STATUS { get; set; }
        public int? PARTY_CODE { get; set; }

        public string? PARTY_TO { get; set; }
        public string? ADD1 { get; set; }
        public string? ADD2 { get; set; }
        public string? ADD3 { get; set; }

        public int? CITY_CODE { get; set; }
        public string? PHONE { get; set; }

        public string? ITEM_TYPE { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public int? ITEM_CODE { get; set; }

        public int? TENACITY_GRPCODE { get; set; }
        public string? TENACITY_GRP { get; set; }

        public int? TRUCK_NO { get; set; }

        public decimal? QTY { get; set; }
        public decimal? RATE { get; set; }

        public string? CURRENCY { get; set; }

        public decimal? DISC_PER { get; set; }
        public decimal? CDISC_PER { get; set; }
        public string? DISC_TYPE { get; set; }

        public string? FRT_TERM { get; set; }
        public string? TAX_TERM { get; set; }

        public decimal? FRT_RATE { get; set; }
        public decimal? TAX_RATE { get; set; }
        public decimal? NET_RATE { get; set; }

        public int? PAYTERM_CODE { get; set; }
        public int? CD_DAYS { get; set; }

        public int? DEFECTIVE_GOODS { get; set; }

        public string? ITEM_REMARKS { get; set; }
        public string? DEL_TERM { get; set; }

        public int? DELIVERY_DAYS { get; set; }

        public string? REMARK { get; set; }

        public int? DEAL_THROUGH { get; set; }

        public string? PINO { get; set; }
        public string? OFFERNO { get; set; }

        public decimal? BROKER_RATE { get; set; }

        public string? FLAKES_SIZE { get; set; }
        public string? FLAKES_SIZEMAX { get; set; }
        public string? FLAKES_PVCPPM { get; set; }
        public string? FLAKES_PPMALL { get; set; }
        public string? GRADE { get; set; }

        public decimal? WASTE_PER { get; set; }

        public string? FLAKES_USETYPE { get; set; }

        public int? DEL_STATION { get; set; }

        public string? REF_ASTYPE { get; set; }

        public string? DEL_PORT { get; set; }
        public string? SIZE { get; set; }
        public string? INCOTERM { get; set; }
        public string? ATTACHMENT_PATH { get; set; }

        public int? REF_ASNO { get; set; }

        public decimal? STUFFING_WT { get; set; }

        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }

        public string? SHIP_TYPE { get; set; }

        public int? NOS { get; set; }


        public byte[]? IMG_FILE { get; set; }
        public string? FILE_NAME { get; set; }
        public string? FILE_TYPE { get; set; }
        public string? FILE_DESC { get; set; }
        public string? FILE_Path { get; set; }
        public int? SRNO { get; set; }

        public IFormFile? Attachment { get; set; }

    }
}
