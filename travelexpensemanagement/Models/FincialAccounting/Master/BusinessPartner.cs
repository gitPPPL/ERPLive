using System.Text.Json.Serialization;
namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class BusinessPartnerWrapper
    {
       public GeneralDetailsModel General { get; set; }
       public List<ContactDetailsModel> Contacts { get; set; }
       public List<AddressModel> Addresses { get; set; }
       public List<BankDetailModel> Banks { get; set; }
       public List<OtherDetailModel> Others { get; set; }

    }
    public class GeneralDetailsModel
    {
       public string ? ACCODE { get; set; }
       public string ? NAME { get; set; }
       public string ? NATURE { get; set; }
       public string ? SHORTNAME { get; set; }
       public string ? ALIAS { get; set; }
       public string ? LEGALNAME { get; set; }
       public string ? GROUPNAME { get; set; }
       public string ? PHONE { get; set; }
       public string ? FAX { get; set; }
       public string ? MOBILE { get; set; }
       public string ? SMS { get; set; }
       public string ? WEBSITE { get; set; }
       public string ? EMAILID { get; set; }
       public string ? LANGUAGE { get; set; }
       public string ? CURRENCY { get; set; }
       public string ? PARTYTYPE { get; set; }
       public string ? GSTTYPE { get; set; }
       public string ? MSMEYN { get; set; }
       public string ? MSMENO { get; set; }
       public string ? MSMETYPE { get; set; }
       public string ? TCSAPPLICABLE { get; set; }
       public string ? TDSAPPLICABLE { get; set; }
       public string ? CONTACTPERSON { get; set; }
       public string ? MAINAC { get; set; }
       public string ? AGENTNAME { get; set; }
       public string ? OSGROUP { get; set; }
       public string ? DISCOUNT { get; set; }
       public string ? DISTRICTGROUP { get; set; }
       public string ? PAYMENTTERM { get; set; }
       public string ? CRDAYS { get; set; }
       public string ? CRLIMIT { get; set; }
       public string ? LASTCRLIMIT { get; set; }
       public string ? BANKNAME { get; set; }
       public string ? BANKCOUNTRY { get; set; }
       public string ? BANKBRANCH { get; set; }
       public string ? IFSCCODE { get; set; }
       public string ? PAYTYPE { get; set; }
       public string ? PRINTTYPE { get; set; }
       public string ? ACNO { get; set; }
       public string ? REMARKS { get; set; }
       public string ? AADHAR { get; set; }
       public string ? CPCBNo { get; set; }
       public string ? ENTITYTYPE { get; set; }
       public string ? BANK_CODE { get; set; }
    }
    public class ContactDetailsModel
    {
       public string? Title { get; set; }
       public string? ContactPerson { get; set; }
       public string? Designation { get; set; }
       public string? Phone { get; set; }
       public string? Mobile { get; set; }
       public string? SMS { get; set; }
       public string? Email { get; set; }
       public string? Fax { get; set; }
       public DateTime? DOB { get; set; }
       public DateTime? DOM { get; set; }
       public string? Gender { get; set; }
       public string? PortalID { get; set; }
       public string? PortalPassword { get; set; }

    }
    public class AddressModel
    {
       public string? address1 { get; set; }
       public string? address2 { get; set; }
       public string? address3 { get; set; }
       public string? city { get; set; }
        public int? CityValue { get; set; }
        public string? state { get; set; }
        public int? StateValue { get; set; }
        public string? country { get; set; }
        public int? CountryValue { get; set; }
        public string? pincode { get; set; }
       public string? gstNo { get; set; }
       public string? panNo { get; set; }
       public string? tanNo { get; set; }
       public string? gstCertDt { get; set; }
       public string? declNo { get; set; }
       public string? declDate { get; set; }
       public string? distance { get; set; }
       public string? billType { get; set; }
       public string? leadDays { get; set; }
       public string? legalName { get; set; }
       public int? IsChecked { get; set; }
    }
    public class OtherDetailModel
    {
       public string? PAN { get; set; }
       public string? GST { get; set; }
       public string? Cheque { get; set; }
       public string? MSME { get; set; }
       public string? Aadhar { get; set; }
       public string? CPCB { get; set; }
       public string? Other { get; set; }
       public string? FileName { get; set; }
       public string? FilePath { get; set; }
       public string? AttachDate { get; set; }
    }
    public class BankDetailModel
    {
       public string? BankCode { get; set; }
       public string? BankName { get; set; }
       public string? CountryCode { get; set; }
       public string? Country { get; set; }
       public string? IFSC { get; set; }
       public string? acNo { get; set; }
       public string? Branch { get; set; }
       public string? pay { get; set; }
       public string? FileName { get; set; }
       public string? attachDate { get; set; }
       public string? BD_Name { get; set; }
    }
    
    public class BusinessPartnerWrapperUpdate
    {
        public GeneralDetailsModelUpdate General { get; set; }
        public List<ContactDetailsModelUpdate> Contacts { get; set; }
        public List<AddressModelUpdate> Addresses { get; set; }
        public List<BankDetailModelUpdate> Banks { get; set; }
        public List<OtherDetailModelUpdate> Others { get; set; }
    }
    public class GeneralDetailsModelUpdate
    {
        public string? ACCODE { get; set; }
        public string? NAME { get; set; }
        public string? NATURE { get; set; }
        public string? SHORTNAME { get; set; }
        public string? ALIAS { get; set; }
        public string? LEGALNAME { get; set; }
        public string? GROUPNAME { get; set; }
        public string? PHONE { get; set; }
        public string? FAX { get; set; }
        public string? MOBILE { get; set; }
        public string? SMS { get; set; }
        public string? WEBSITE { get; set; }
        public string? EMAILID { get; set; }
        public string? LANGUAGE { get; set; }
        public string? CURRENCY { get; set; }
        public string? PARTYTYPE { get; set; }
        public string? GSTTYPE { get; set; }
        public string? MSMEYN { get; set; }
        public string? MSMENO { get; set; }
        public string? MSMETYPE { get; set; }
        public string? TCSAPPLICABLE { get; set; }
        public string? TDSAPPLICABLE { get; set; }
        public string? CONTACTPERSON { get; set; }
        public string? MAINAC { get; set; }
        public string? AGENTNAME { get; set; }
        public string? OSGROUP { get; set; }
        public string? DISCOUNT { get; set; }
        public string? DISTRICTGROUP { get; set; }
        public string? PAYMENTTERM { get; set; }
        public string? CRDAYS { get; set; }
        public string? CRLIMIT { get; set; }
        public string? LASTCRLIMIT { get; set; }


        public string? BANKNAME { get; set; }
        public string? BANKCOUNTRY { get; set; }
        public string? BANKBRANCH { get; set; }
        public string? IFSCCODE { get; set; }
        public string? PAYTYPE { get; set; }
        public string? PRINTTYPE { get; set; }
        public string? ACNO { get; set; }
        public string? REMARKS { get; set; }
        public string? AADHAR { get; set; }
        public string? CPCBNo { get; set; }
        public string? ENTITYTYPE { get; set; }
        public string? BANK_CODE { get; set; }
    }
    public class ContactDetailsModelUpdate
    {
        public string? Title { get; set; }
        public string? ContactPerson { get; set; }
        public string? Designation { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? SMS { get; set; }
        public string? Email { get; set; }
        public string? Fax { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime? DOM { get; set; }
        public string? Gender { get; set; }
        public string? PortalID { get; set; }
        public string? PortalPassword { get; set; }
    }
    public class AddressModelUpdate
    {
        public string? address1 { get; set; }
        public string? address2 { get; set; }
        public string? address3 { get; set; }
        public string? city { get; set; }
        public int? CityValue { get; set; }
        public string? state { get; set; }
        public int? StateValue { get; set; }
        public string? country { get; set; }
        public int? CountryValue { get; set; }
        public string? pincode { get; set; }
        public string? gstNo { get; set; }
        public string? panNo { get; set; }
        public string? tanNo { get; set; }
        public string? gstCertDt { get; set; }
        public string? declNo { get; set; }
        public string? declDate { get; set; }
        public string? distance { get; set; }
        public string? billType { get; set; }
        public string? leadDays { get; set; }
        public string? legalName { get; set; }
        public int? IsChecked { get; set; }
    }
    public class OtherDetailModelUpdate
    {
        public string? PAN { get; set; }
        public string? GST { get; set; }
        public string? Cheque { get; set; }
        public string? MSME { get; set; }
        public string? Aadhar { get; set; }
        public string? CPCB { get; set; }
        public string? Other { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? AttachDate { get; set; }

    }
    public class BankDetailModelUpdate
    {
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? CountryCode { get; set; }
        public string? Country { get; set; }
        public string? IFSC { get; set; }
        public string? acNo { get; set; }
        public string? Branch { get; set; }
        public string? pay { get; set; }
        public string? FileName { get; set; }
        public string? attachDate { get; set; }
        public string? BD_Name { get; set; }
    }
    public class GeneralDetailsModelList
    {
        public string ACCODE { get; set; }
        public string NAME { get; set; }
        public string GROUPNAME { get; set; }
        public string NATURE { get; set; }
        public string MOBILE { get; set; }
        public string EMAILID { get; set; }
        public string PARTYTYPE { get; set; }
        public string BANKNAME { get; set; }
        public string IFSCCODE { get; set; }
        public string ACNO { get; set; }
        public string BANKBRANCH { get; set; }
        public string CURRENCY { get; set; }
    }
    public class CityDetailsModel
    {
        public string StateCode { get; set; }
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }
    }

}
