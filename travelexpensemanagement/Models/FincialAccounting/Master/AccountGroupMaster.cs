namespace travelexpensemanagement.Models.FincialAccounting.Master
{
   
    public class AccountGroupMaster
    {
        public int CODE { get; set; } 
        public int COMP_CODE { get; set; }
        public string GROUP_NAME { get; set; }
        public string SHORT_NAME { get; set; }
        public int? MAIN_GROUP_NAME { get; set; }
        public string NATURE { get; set; }
        public string SCHEDULE_GROUPING { get; set; }
        public int? SUB_SCHEDULE_NAME { get; set; }  
        public int? MAIN_SCHEDULE_NAME { get; set; } 
        public bool GROUPING_ON_TRAIL { get; set; }
        public bool ACTIVE { get; set; }
        public string? TYPE { get; set; }
    
    }


    public class AccountGroupMasterList
    {
        public int CODE { get; set; } // primary key, usually auto-generated
        public int COMP_CODE { get; set; }
        public string GROUP_NAME { get; set; }
        public string SHORT_NAME { get; set; }
        public string MAIN_GROUP_NAME { get; set; } // Changed to int? because GR_CODE is int?
        public string NATURE { get; set; }
        public string SCHEDULE_GROUPING { get; set; }
        public string SUB_SCHEDULE_NAME { get; set; }  // int? because SCH_CODE is int?
        public string MAIN_SCHEDULE_NAME { get; set; } // int? because MSCH_CODE is int?
        public bool GROUPING_ON_TRAIL { get; set; }
        public bool ACTIVE { get; set; }
        public string TYPE { get; set; }

    }


}
