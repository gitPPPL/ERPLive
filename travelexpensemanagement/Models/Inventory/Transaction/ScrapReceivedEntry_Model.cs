using System;
using System.Collections.Generic;

namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class ScrapReceivedEntry_Model
    {
        public ScrapReceivedEntry_Header Header { get; set; }
        public List<ScrapReceivedEntry_Details> Details { get; set; }
    }

    public class ScrapReceivedEntry_Header
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public int? PARTY { get; set; }
        public int? PLACE_CODE { get; set; }
        public string? REMARK { get; set; }
        public string? ACName { get; set; }
        public string? PlaceName { get; set; }
        public string? action { get; set; }

    }

    public class ScrapReceivedEntry_Details
    {
        public int? SNO { get; set; }

        public int? DEPT_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public decimal? QTY { get; set; }
        public decimal? WEIGHT { get; set; }
        public string? REMARK { get; set; }
        public decimal? ADJ_QTY { get; set; }
        public int? SCRAP_CODE { get; set; }
        public string? SCRAP_NAME { get; set; }
    }
}