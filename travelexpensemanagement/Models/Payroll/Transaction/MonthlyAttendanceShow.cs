using System;
using System.Collections.Generic;

namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class MonthlyAttendanceShow
    {
        public string? DOC_ID { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? EMP_CODE { get; set; }
        public string? SHIFT { get; set; }
        public string? STATUS { get; set; }
        public int? OFFDAY { get; set; }
        public string? REMARK { get; set; }
        public int? SNO { get; set; }
    }

    // Wrapper class to match the client payload
    public class AttendanceSaveRequest
    {
        public string? Action { get; set; }
        public List<MonthlyAttendanceShow>? Data { get; set; }
    }
}
