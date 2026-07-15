namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportPaymentEntry
    {
        public class PartyDetailsModel
        {
            public string? EcbLenderCode { get; set; }
            public string? EcbAddress { get; set; }
            public string? BeneficiaryCode { get; set; }
            public string? BeneficiaryName { get; set; }
            public string? BeneficiaryActNo { get; set; }
            public string? BeneficiaryBankAddress { get; set; }
            public string? ImportCategory { get; set; }
            public string? ImportRemit { get; set; }
            public string? PayType { get; set; }
            public string? ForeignBankCharge { get; set; }
            public string? InterestApplicable { get; set; }
            public string? Roi { get; set; }
            public string? RoiPeriod { get; set; }
            public string? BeneficiaryBankCode { get; set; }
            public string? BeneficiarySwift { get; set; }
            public string? BeneficiaryAccount { get; set; }
            public string? CorrBankCode { get; set; }
            public string? CorrSwift { get; set; }
            public string? CorrAccount { get; set; }
        }

    }
}
