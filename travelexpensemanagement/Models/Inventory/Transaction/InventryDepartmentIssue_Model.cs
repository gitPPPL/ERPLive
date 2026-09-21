using System.ComponentModel.DataAnnotations;

namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class InventryDepartmentIssue_Model
    {
        public InventryDepartmentIssue_Header Header { get; set; }
        public List<InventryDepartmentIssue_Details> Details { get; set; }
    }

    public class InventryDepartmentIssue_Header
    {

 
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public int? PLACE_CODE { get; set; }
        public int? EMP_CODE { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? REMARKS { get; set; }
        public string? CONS_TYPE { get; set; }
        public int? STATUS { get; set; }
        public decimal? AMOUNT { get; set; }
        public string? PLAN_TYPE { get; set; }
        public string? StatusText { get; set; }
        public int? PLAN_NO { get; set; }
        public string? FAPROV_STATUS { get; set; }
        public string? FAPROV_REMARKS { get; set; }
        public string? action { get; set; }
    }

    public class InventryDepartmentIssue_Details
    {
        public int? SNO { get; set; }
        public string? SHIFT { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public int? MAKE_CODE { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public int? FROM_DEPT { get; set; }
        public int? TO_DEPT { get; set; }
        public int? MAC_CODE { get; set; }
        public int? NOS { get; set; }
        public decimal? QTY { get; set; }
        public decimal? ADJ_QTY { get; set; }
        public decimal? WASTE { get; set; }
        public decimal? RATE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public string? BIN_LOCATION { get; set; }
        public int? BIN_CODE { get; set; }
        public DateTime? WB_DATETIME { get; set; }
        public string? KANTA_TYPE { get; set; }
        public int? KANTA_NO { get; set; }
        public string? REQ_TYPE { get; set; }
        public int? REQ_NO { get; set; }
        public string? EMPTY_YN { get; set; }
        public int? MACH_CODE { get; set; }
        public string? REMARKS { get; set; }
        public int? SFG_ITEM_CODE { get; set; }
        public string? FINAL_LOCK { get; set; }
        public int? ACK_STATUS { get; set; }
        public string? ACK_REMARKS { get; set; }
        public DateTime? ACK_DATE { get; set; }
        public string? LOT_NO { get; set; }
        public int? COSTCAT_CODE { get; set; }
        public int? COSTSCAT_CODE { get; set; }
        public int? COSTCENTER_CODE { get; set; }
    }
}