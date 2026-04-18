using System;

namespace travelexpensemanagement.Models.PayRoll
{
    public class HolidayModel
    {

        public int? Code { get; set; }
        public string? Name { get; set; }
        public DateTime? HolidayDate { get; set; }
        public DateTime? BeforeDate { get; set; }
        public DateTime? AfterDate { get; set; }
        public int? NationalHoliday { get; set; }
        public int? Active { get; set; }
        public string? action { get; set; }
    }
}
