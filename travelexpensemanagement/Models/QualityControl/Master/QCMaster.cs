    namespace travelexpensemanagement.Models.QualityControl.Master
{
   
    public class DetailModel
    {
        public int Parameter { get; set; }
        public int ParameterValue { get; set; }
        public int Unit { get; set; }
        public decimal StdResult { get; set; }
        public string DeductQty { get; set; }
        public string DeductType { get; set; }
        public string Ppm { get; set; }
        public decimal BasePrice { get; set; }
        public string Remarks { get; set; }
        public int Code { get; set; }
        public string CurrentCode { get; set; }
    }

    public class QCMaster
    {
        public int? code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int QCGroup { get; set; }
        public decimal MaxPPM { get; set; }
        public int active { get; set; }
        public List<DetailModel> Details { get; set; }
    }

    public class DeductRateModel
    {
        public decimal From { get; set; }
        public decimal To { get; set; }
        public decimal Rate { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public string nextQcpCode { get; set; }
    }
    public class QcCodeRequest
    {
        public int Code { get; set; }
        public int nextQcpCode { get; set; }
    }

    public class QCMasterList
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string QCGroup { get; set; }
        public string MaxPPM { get; set; }
        public int ACTIVE { get; set; }

    }
    public class CheckDeductRateRequest
    {
        public string Code { get; set; }
        public string ParameterId { get; set; }
    }
    public class DeductRateModelList
    {
        public decimal? FromResult { get; set; }
        public decimal? ToResult { get; set; }
        public string DeductType { get; set; }
        public decimal? DeductRate { get; set; }
    }


}
