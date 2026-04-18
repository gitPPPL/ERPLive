namespace travelexpensemanagement.Models.Admin.Setup
{
    public class ItemDeptViewModel
    {
        public int? CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string TRAN_TYPE { get; set; }
        public string PLACE_TYPE { get; set; }
        public string REPORT_TYPE { get; set; }
        public int? SORT_ON { get; set; }
        public string UNIT_TYPE { get; set; }
        public int? PLACE_CODE { get; set; }
        public decimal? MPURCH_BUDGET { get; set; }
        public decimal? MCONSUMP_BUDGET { get; set; }
        public int ACTIVE { get; set; }
        public int Cost { get; set; }
    }

    public class ItemDeptViewModelList
    {
        public int? CODE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string TRAN_TYPE { get; set; }
        public string PLACE_TYPE { get; set; }
        public string REPORT_TYPE { get; set; }
        public int? SORT_ON { get; set; }
        public string UNIT_TYPE { get; set; }
        public string PLACE_CODE { get; set; }
        public decimal? MPURCH_BUDGET { get; set; }
        public decimal? MCONSUMP_BUDGET { get; set; }
        public int ACTIVE { get; set; }
    }

    public class ItemDeptExportDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string TranType { get; set; }
        public string PlaceType { get; set; }
        public string ReportType { get; set; }
        public string UnitType { get; set; }
        public string PlaceCode { get; set; }
        public bool Active { get; set; }
        public string Status { get; set; }
    }

    public class ItemDeptDetailDto
    {
        public string CODE { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }



}
