using System;
using System.Collections.Generic;

namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class InventoryTransferRequest_Model
    {
        public InventoryTransferRequest_Header Header { get; set; }
        public List<InventoryTransferRequest_Details> Details { get; set; } 
    }

    public class InventoryTransferRequest_Header
    {
        public string?    V_TYPE { get; set; } 
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public string? PORD_TYPE { get; set; } 
        public int? PLACE_CODE { get; set; }
        public int? EMP_CODE { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? REMARKS { get; set; }   
        public int? STATUS { get; set; }
        public string? action { get; set; }
        public string? statusName { get; set; }

    }

    public class InventoryTransferRequest_Details
    {
        public int? SNO { get; set; }
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
        public decimal? RATE { get; set; }
        public decimal? AMOUNT { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public int? MACH_CODE { get; set; }
        public string? REMARKS { get; set; }
         
    }
}