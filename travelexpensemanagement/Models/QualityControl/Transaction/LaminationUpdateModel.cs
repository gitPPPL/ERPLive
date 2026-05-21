namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class LaminationUpdateModel
    {
        public List<LaminationDetail> LaminationDetails { get; set; }
    }

    public class LaminationDetail
    {
        public string Docid { get; set; }
        public double? NWARPWAY_RES { get; set; }
        public double? WARPWAY_RES { get; set; }
        public double? NWEFTWAY_RES { get; set; }
        public double? WEFTWAY_RES { get; set; }

        public double? ELONG_WARP { get; set; }
        public double? ELONG_WEFT { get; set; }

        public string? QC_REMARKS { get; set; }

        public int? STATUS_CODE_A { get; set; }
        public int? TENA_CODE_A { get; set; }

        public int? LAMSUP_CODE { get; set; }
        public string? LAMSUP_NAME { get; set; }

        public int? LAMOP_CODE { get; set; }
        public string? LAMOP_NAME { get; set; }

        public string? QCUSER { get; set; }
    }

}
