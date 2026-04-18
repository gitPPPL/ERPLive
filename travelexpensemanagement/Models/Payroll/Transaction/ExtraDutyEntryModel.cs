namespace travelexpensemanagement.Models.GateEntry.Transaction
{
    public class ExtraDutyEntryModel
    {
        public Header? header { get; set; }
        public Details? details { get; set; }

        public class Header
        {
            public string? DocType { get; set; }
            public string? Vno { get; set; }
            public string? DocDate { get; set; }
            public string? Department { get; set; }
            public string? Action { get; set; }
        }

        public class Details
        {
            public List<TableRow>? TableData { get; set; }
        }

        public class TableRow
        {
            public string? EmpCode { get; set; }
            public string? EmpName { get; set; }
            public string? Department { get; set; }
            public string? Designation { get; set; }
            public string? Before { get; set; }
            public string? After { get; set; }
            public string? Shift { get; set; }
            public string? Required { get; set; }
            public string? Present { get; set; }
            public string? InTime { get; set; }
            public string? OutTime { get; set; }
            public string? Reason { get; set; }
            public string? HodName { get; set; }
            public string? AuthBy { get; set; }
            public string? Approval { get; set; }
            public string? ApprovalRemarks { get; set; }
            public string? Remarks { get; set; }
            public string? Duration { get; set; }
            public string? MacTime { get; set; }
            public string? RefType { get; set; }
            public string? RefNo { get; set; }
            public string? GateNo { get; set; }
            public string? DeptCode { get; set; }
            public string? DesgCode { get; set; }

            public int Action { get; set; }
        }
    }
}
