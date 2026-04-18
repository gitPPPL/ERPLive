using System;
using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Payroll.Transaction
{
    public class GatePassEntry
    {
        public int V_NO { get; set; }


        public DateTime? V_DATE { get; set; }


        public string DOC_ID { get; set; }

        public int? EMP_CODE { get; set; }


        public string EMP_NAME { get; set; }
        public string V_TYPE { get; set; }

        public int SNO { get; set; }

        public int? DEPT_CODE { get; set; }


        public string DEPT_NAME { get; set; }


        public string WORKPLACE_NAME { get; set; }

        public int? WORKPLACE_CODE { get; set; }

        public decimal? AHRS { get; set; }

        public decimal? BHRS { get; set; }


        public string REMARK { get; set; }


        public string DUTY_TIME { get; set; }


        public string IN_TIME { get; set; }


        public string OUT_TIME { get; set; }


        public string SYS_TIME { get; set; }


        public string REASON { get; set; }

        public int? REASON_CODE { get; set; }


        public string AUTH_BY { get; set; }

  
        public string GP_NO { get; set; }

        public int? GATE_NO { get; set; }

        public int? HOD_CODE { get; set; }

        public int? DUR { get; set; }


        public string REF_TYPE { get; set; }

        public int? REF_NO { get; set; }


        public string RETUN { get; set; }

        public int? COND { get; set; }

        public string APROV_STATUS { get; set; }

        public string APROV_REMARKS { get; set; }

        public string FAPROV_STATUS { get; set; }

        public string FAPROV_REMARKS { get; set; }
        
        public string MAC_IN { get; set; }

        public string MAC_OUT { get; set; }

        public int? DESG_CODE { get; set; }

        public string DESG_NAME { get; set; }

        public int? REQ_NOS { get; set; }

        public int? PRESENT_NOS { get; set; }
    }
}
