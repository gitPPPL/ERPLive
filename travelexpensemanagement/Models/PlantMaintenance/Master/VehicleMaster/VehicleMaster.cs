namespace travelexpensemanagement.Models.PlantMaintenance.Master.VehicleMaster
{
    public class VehicleMaster
    {
        public int? CODE { get; set; }
        public string? VEHICLE_NAME {  get; set; }
        public string? SHORTNAME { get; set; }
        public string? VEHICLE_CATEGORY {  get; set; }
        public string? VEHICLE_REGNO {  get; set; }
        public int? COLOR_CODE {  get; set; }
        public int? MAKE_CODE {  get; set; }
        public string? MODEL {  get; set; }
        public string? CHASSIS_NO {  get; set; }
        public string? ENGINE_NO {  get; set; }
        public string? FUEL_TYPE {  get; set; }
        public int? PLACE_CODE {  get; set; }
        public int? ACTIVE {  get; set; }
        public int? COUNTRY_CODE {  get; set; }
        public DateTime? ROADTAX_DATE {  get; set; }
        public DateTime? ROADTAX_DUEDATE {  get; set; }
        public string? ROADTAX_RECNO { get; set; }
        public DateTime? NEXT_SERVICE_DATE {  get; set; }
        public string? FC { get; set; }
        public DateTime? FC_DATE {  get; set; }
        public DateTime? FC_DUEDATE { get; set; }
        public string? FC_RECNO { get; set; }
        public string? FC_REMARKS { get; set; }
        public string? POLLUTION_NO { get; set; }
        public DateTime? POLLUTION_DATE { get; set; }

    }
}
