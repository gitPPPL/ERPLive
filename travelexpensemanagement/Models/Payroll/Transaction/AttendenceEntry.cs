public class AttendenceEntry
{
    public string? DOC_ID { get; set; }
    public string? SHIFT { get; set; }
    public string? STATUS { get; set; }
    public string? OFFDAY { get; set; }
    public string? REMARK { get; set; }
    public string? FLG { get; set; }
    public int? YEAR_CODE { get; set; }
    public int? BRANCH_CODE { get; set; }
    public int? COMP_CODE { get; set; }
    public int? EMP_CODE { get; set; }
    public int? SNO { get; set; }

    public string? Dept { get; set; }
    public string? emp { get; set; }
    public string? Design { get; set; }


    // 🔽 Add these extra fields
    public int? V_NO { get; set; }
    public string? V_TYPE { get; set; }
    public DateTime? V_DATE { get; set; }
}
