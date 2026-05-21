namespace travelexpensemanagement.Models.Production.LaminationProcess
{
    public class RollLaminationEntryModel
    {
        public int? vNo {get; set;}
        public int? jobWork {get; set;}
        public string? shiftBefore {get; set;}
        public DateTime? dateBefore {get; set;}
        public int? itemBefore {get; set;}
        public string? itemNameBefore { get; set;}
        public string? rollNoBefore {get; set;}
        public int? meterBefore {get; set;}
        public decimal? grossBefore {get; set;}
        public decimal? tareBefore {get; set;}
        public decimal? netBefore {get; set;}
        public decimal? avgBefore {get; set;}
        public decimal? gramBefore {get; set;}
        public decimal? sizeBefore {get; set;}
        public int? loomNo {get; set;}
        public string? remarksBefore {get; set;}
        public int? placeCode {get; set;}
        public int? pordNo {get; set;}
        public string? shiftAfter {get; set;}
        public DateTime? dateAfter {get; set;}
        public int? itemAfter {get; set;}
        public string? itemNameAfter { get; set;}
        public string? rollNoAfter {get; set;}
        public int? meterAfter { get; set;}
        public string? batchNo {get; set;}
        public decimal? grossAfter {get; set;}
        public decimal? tareAfter {get; set;}
        public decimal? netAfter {get; set;}
        public decimal? avgAfter {get; set;}
        public decimal? gramAfter {get; set;}
        public decimal? sizeAfter {get; set;}
        public int? machineNo {get; set;}
        public int? status {get; set;}
        public string? remarksAfter { get; set; }
    }
    public class RollLaminationPendingRecordModel
    {
        public string? itemName { get; set; }
        public string? rollNo { get; set; }
        public decimal? meter { get; set; }
        public decimal? grossWt { get; set; }
        public decimal? tareWt { get; set; }
        public decimal? netWt { get; set; }
        public int? loomNo { get; set; }
        public string? loomType { get; set; }
    }
    public class RollRecordBeforeLaminationModel
    {
        public decimal? meter { get; set; }
        public decimal? grossWt { get; set; }
        public decimal? tareWt { get; set; }
        public decimal? netWt { get; set; }
        public decimal? avgWt { get; set; }
        public decimal? gram { get; set; }
        public int? loomNo { get; set; }
    }
    public class RollLaminationListModel
    {
        public int? vNo {get; set;}
        public string? vType {get; set;}
        public DateTime? vDate {get; set;}
        public string? itemName {get; set;}
        public string? rollNo {get; set;}
        public int? meter {get; set;}
        public decimal? grossWeight {get; set;}
        public decimal? netWeight {get; set;}
        public decimal? averageWeight {get; set;}
        public decimal? gram {get; set;}
        public string? rollNoLam {get; set;}
        public int? meterLam {get; set;}
        public decimal? netWeightLam {get; set;}
        public decimal? averageWeightLam {get; set;}
        public decimal? gramLam {get; set;}
        public decimal? size {get; set;}
        public decimal? sizeLam {get; set;}
        public string? tenacity {get; set;}
        public string? place { get; set; }
    }
}
