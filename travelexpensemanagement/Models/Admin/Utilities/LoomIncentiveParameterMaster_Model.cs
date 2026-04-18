using System;
using DocumentFormat.OpenXml.Wordprocessing;

namespace travelexpensemanagement.Models.Admin.Utilities
{
    public class LoomIncentiveParameterMaster_Model
    {
        public string? V_Type { get; set; }

        public int? Code { get; set; }
        public int? Active { get; set; }

        public string? Name { get; set; }

        public string? LoomType { get; set; }

        public int? ConvCode { get; set; }

        public string? ConvName { get; set; }
        public string? action { get; set; }

        public decimal? Per { get; set; }

        public decimal? FixAmt { get; set; }



    }
}
