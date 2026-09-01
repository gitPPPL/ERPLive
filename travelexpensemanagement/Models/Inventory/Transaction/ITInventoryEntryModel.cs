namespace travelexpensemanagement.Models.Inventory.Transaction
{
    public class ITInventoryEntryModel
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public string? ASSET_CODE { get; set; }
        public string? UNIT_NAME { get; set; }
        public int? EMP_CODE { get; set; }
        public string? DEPT { get; set; }
        public string? DESG { get; set; }
        public string? LOCATION { get; set; }
        public string? IPADDRESS { get; set; }
        public string? USED_BY { get; set; }
        public string? DEVICE_TYPE { get; set; }
        public string? DEVICE_MODEL { get; set; }
        public string? DEVICE_NAME { get; set; }
        public int? QTY { get; set; }
        public string? MAC_ADDRESS { get; set; }
        public DateTime? PURCHASE_DATE { get; set; }
        public string? PURCHASE_FROM { get; set; }
        public string? LOGIN_NAME { get; set; }
        public string? LOGIN_PWD { get; set; }
        public string? REMARKS { get; set; }
        public string? SERIAL_NO { get; set; }
        public DateTime? ISSUE_DATE { get; set; }
        public string? EMAIL { get; set; }
        public string? ASSET_TYPE { get; set; }
        public string? DS_USERNAME { get; set; }
        public string? DS_PASSWORD { get; set; }
        public string? SERVER_IP { get; set; }
        public string? SERVER_USERNAME { get; set; }
        public string? CLOUD_USERNAME { get; set; }
        public string? CLOUD_PASSWORD { get; set; }
        public DateTime? RETURN_DATE { get; set; }
        public string? PURPOSE { get; set; }
        public string? ASSET_CAT { get; set; }
        public string? VPN_USERNAME { get; set; }
        public string? VPN_PASSWORD { get; set; }
        public string? EMAIL_PASS { get; set; }
        public string? MOB_NO { get; set; }
        public string? INTERCOM { get; set; }
        public string? WARRANTY_STATUS { get; set; }
        public string? DEVICE_STATUS { get; set; }
        public string? REASON { get; set; }
        public int? ASSET_SRNO { get; set; }
        public int? EMP_STATUS { get; set; }
        public string? EMAILLIC_TYPE { get; set; }
        public string? WINLIC_KEY { get; set; }
        public string? WINDOWLIC_TYPE { get; set; }
        public string? OFFICELIC_TYPE { get; set; }

        public List<ITInventoryEntryModel> Devices { get; set; } = new();

    }
  
}
