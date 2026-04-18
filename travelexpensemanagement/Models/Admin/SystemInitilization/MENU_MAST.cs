using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Admin.SystemInitilization
{
    public class MENU_MAST
    {
        [Key]
        public int CODE { get; set; } // Assuming CODE is the primary key and always required

        public int? MODULE_CODE { get; set; }
        public int? MAINMENU_CODE { get; set; }
        public int? MENU_OPTION { get; set; }

        public string? NAME { get; set; }
        public string? DISPLAY_NAME { get; set; }
        public string? FORM_NAME { get; set; }
        public string? TAG_NAME { get; set; }
        public string? MENU_TYPE { get; set; }

        public int? SECURITY_TYPE { get; set; }
        public string? APPROVAL { get; set; }
        public int? ACTIVE { get; set; }

        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }

        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }

        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }

        public int? SRNO { get; set; }
        public int? LOCK_EDIT { get; set; }

        public string? ACTION { get; set; }
        public string? WebFORM_NAME { get; set; }
    }
    public class MENU_MASTEport
    {
        public int? CODE { get; set; }
        public string MENU_OPTION { get; set; }
        public string NAME { get; set; }
        public string DISPLAY_NAME { get; set; }
        public string FORM_NAME { get; set; }
        public string MENU_TYPE { get; set; }
        public string ACTIVE { get; set; } 
    }
   
}
