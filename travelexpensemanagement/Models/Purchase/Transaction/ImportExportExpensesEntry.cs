using static travelexpensemanagement.Models.Purchase.Transaction.ImportExportExpensesEntry;

namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportExportExpensesEntry
    {
        public class ItemDetailModel1
        {
            public string Code { get; set; }
            public string ItemName { get; set; }
            public string HSNCode { get; set; }
            public string Unit { get; set; }
            public decimal Nos { get; set; }
            public decimal PlusMinusQty { get; set; }
            public decimal RecQty { get; set; }
            public decimal BillQty { get; set; }
            public decimal USDRate { get; set; }
            public decimal ExRate { get; set; }
            public decimal Rate { get; set; }
            public decimal Amount { get; set; }
            public string EmptyYN { get; set; }
            public decimal WBQty { get; set; }
            public string TaxType { get; set; }
            public decimal PackPer { get; set; }
            public decimal PackAmt { get; set; }
            public decimal DiscPer { get; set; }
            public decimal DiscAmt { get; set; }
            public decimal CGSTPer { get; set; }
            public decimal CGSTAmt { get; set; }
            public decimal SGSTPer { get; set; }
            public decimal SGSTAmt { get; set; }
            public decimal IGSTPer { get; set; }
            public decimal IGSTAmt { get; set; }
            public decimal CESSPer { get; set; }
            public decimal CESSAmt { get; set; }
            public decimal VATPer { get; set; }
            public decimal VATAmt { get; set; }
            public decimal OthAmt { get; set; }
            public decimal NetAmt { get; set; }
            public string Make { get; set; }
            public string Department { get; set; }
            public string Remarks { get; set; }
            public decimal LDRate { get; set; }
            public decimal LDAmt { get; set; }
            public string BinLocation { get; set; }
            public string POType { get; set; }
            public string PONo { get; set; }
            public string KantaType { get; set; }
            public string KantaNo { get; set; }
            public string ReqType { get; set; }
            public string ReqNo { get; set; }
            public string GateType { get; set; }
            public string GateNo { get; set; }
            public string TransportName { get; set; }
            public string VehicleNo { get; set; }
            public string ContainerNo { get; set; }
            public decimal? FreightPay { get; set; }
            public decimal? FrtTax1 { get; set; }
            public decimal? FrtTax2 { get; set; }
            public string FrtPayNarr { get; set; }
            public int? GRNo { get; set; }
            public DateTime? GRDate { get; set; }
            public int? BinCode { get; set; }
            public int? UOMCode { get; set; }
            public string UOMName { get; set; }
            public int? DeptCode { get; set; }
        }

        public class ItemDetailImportExportExpensesEntryModel
        {
            public int VNo { get; set; }                           
            public string DocId { get; set; }                      
            public string VType { get; set; }                       
            public DateTime? VDate { get; set; }                   
            public int? CompCode { get; set; }                    
            public int? BranchCode { get; set; }                 
            public int? YearCode { get; set; }                     
            public int Sno { get; set; }                         
            public int? ItemCode { get; set; }                  
            public string? ItemName { get; set; }                    
            public int? MakeCode { get; set; }                     
            public string HSNCode { get; set; }                    
            public string ? RCMYN { get; set; }                       
            public string? InputYN { get; set; }                    
            public int? UOMCode { get; set; }                        
            public string? UOMName { get; set; }                      
            public int? DeptCode { get; set; }                       
            public int? Nos { get; set; }                           
            public decimal? PlusMinusQty { get; set; }              
            public decimal? WBQty { get; set; }                     
            public decimal? RecQty { get; set; }                    
            public decimal? BillQty { get; set; }                    
            public decimal? USDRate { get; set; }                    
            public decimal? ExRate { get; set; }                     
            public decimal? Rate { get; set; }                       
            public decimal? Amount { get; set; }                    
            public decimal? DiscPer { get; set; }                   
            public decimal? DiscAmt { get; set; }                 
            public decimal? PackPer { get; set; }                    
            public decimal? PackAmt { get; set; }                    
            public int? TaxCode { get; set; }                       
            public decimal? CGSTPer { get; set; }                    
            public decimal? CGSTAmt { get; set; }                    
            public decimal? SGSTPer { get; set; }                   
            public decimal? SGSTAmt { get; set; }                    
            public decimal? IGSTPer { get; set; }                   
            public decimal? IGSTAmt { get; set; }                   
            public decimal? CESSPer { get; set; }                   
            public decimal? CESSAmt { get; set; }                   
            public decimal? VATPer { get; set; }                     
            public decimal? VATAmt { get; set; }                    
            public decimal? OthAmt { get; set; }                     
            public decimal? NetAmt { get; set; }                     
            public decimal? LDRate { get; set; }                     
            public decimal? LDAmt { get; set; }                      
            public decimal? PolandRate { get; set; }               
            public decimal? PORate { get; set; }                     
            public string BinLocation { get; set; }                 
            public int? BinCode { get; set; }                        
            public string? POType { get; set; }                      
            public int? PONo { get; set; }                           
            public string SaudaType { get; set; }                    
            public int? SaudaNo { get; set; }                        
            public string? KantaType { get; set; }                   
            public int? KantaNo { get; set; }                        
            public string ReqType { get; set; }                    
            public int? ReqNo { get; set; }                         
            public string GateType { get; set; }                     
            public int? GateNo { get; set; }                        
            public string RefType { get; set; }                     
            public int? RefNo { get; set; }                          
            public string QCType { get; set; }                       
            public int? QCNo { get; set; }                           
            public string? PassType { get; set; }                    
            public int? PassNo { get; set; }                        
            public string? EmptyYN { get; set; }                     
            public int? MachCode { get; set; }                       
            public string? Remarks { get; set; }                      
            public decimal? RateMonthly { get; set; }               
            public decimal? RateQuarterly { get; set; }              
            public decimal? RateAnnualy { get; set; }                
            public decimal? RateSpecial { get; set; }                
            public string? FinalLock { get; set; }     
           
        }
        public class ImportExportExpensesEntryHeaderModel
        {
            public string? DocType { get; set; }
            public string? DocNo { get; set; }
            public string? code { get; set; }
            public string? BillNo { get; set; }
            public string? ChallanNo { get; set; }
            public string? WaybillNo { get; set; }
            public string? WaybillInvNo { get; set; }
            public string? ReturnType { get; set; }
            public string DocStatus { get; set; }
            public string? DocDate { get; set; }
            public string? GateNo { get; set; }
            public string? BillDate { get; set; }
            public string? ChallanDate { get; set; }
            public string? WaybillDate { get; set; }
            public string? WaybillExpiry { get; set; }
            public string? ExchangeRate { get; set; }
            public string? NetAmount { get; set; }
            public string? BillFrom { get; set; }
            public string? AddLine1 { get; set; }
            public string? AddLine2 { get; set; }
            public string? AddLine3 { get; set; }
            public string? City { get; set; }
            public string? Pincode { get; set; }
            public string? State { get; set; }
            public string? GST { get; set; }
            public string? Remarks { get; set; }
            public string? ShipFrom { get; set; }
            public string? ShipAddLine1 { get; set; }
            public string? ShipAddLine2 { get; set; }
            public string? ShipAddLine3 { get; set; }
            public string? ShipCity { get; set; }
            public string? ShipPincode { get; set; }
            public string? BILL_ADDRESSID { get; set; }
            public string? SHIP_ADDRESSID { get; set; }
            public string? ShipState { get; set; }
            public string? ShipGST { get; set; }
            public string? TransportName { get; set; }
            public string? VehicleNo { get; set; }
            public string? ContainerNo { get; set; }
            public string? FreightPay { get; set; }
            public string? FrtTax1 { get; set; }
            public string? FrtTax2 { get; set; }
            public string? FrtPayNarr { get; set; }
            public string? GRNo { get; set; }
            public string? GRDate { get; set; }
            public decimal? NumReceivedQty { get; set; }
            public decimal? NumBillQty { get; set; }
            public decimal? NumAmount { get; set; }
            public decimal? NumPacking { get; set; }
            public decimal? NumDiscount { get; set; }
            public decimal? NumCGST { get; set; }
            public decimal? NumSGST { get; set; }
            public decimal? NumIGST { get; set; }
            public decimal? NumCESS { get; set; }
            public decimal? NumVAT { get; set; }
            public decimal? NumOtherAmt { get; set; }
            public decimal? NumTCSPer1 { get; set; }
            public decimal? NumTCSPer2 { get; set; }
            public decimal? NumRoundOff { get; set; }
            public decimal? NumFinalNetAmt { get; set; }
            public DateTime? EWB_DATE { get; set; }     
            public DateTime? EWB_EXPDATE { get; set; }
            public string? EWB_INVNO { get; set; }
            public string? GATE_TYPE { get; set; }
            public string? TRANSIT_NO { get; set; }
            public string? TRANSPORT_CODE { get; set; }
            public string? HOLD_PAY { get; set; }
            public string? HOLD_REASON { get; set; }
            public string? HOLD_DATE { get; set; }
            public string? ACTION { get; set; }
        }

        public class ImportExportExpensesEntryAttachmentModel
        {
            public string FileName { get; set; }
            public IFormFile File { get; set; }
            public string IMG_FILE { get; set; }
            public string FILE_NAME { get; set; }
            public string FILE_TYPE { get; set; }
        }

        public class ImportExportExpensesEntry1
        {
            public string DOC_ID { get; set; }
            public int V_NO { get; set; }
            public string V_TYPE { get; set; }
            public DateTime? V_DATE { get; set; }
            public int COMP_CODE { get; set; }
            public int BRANCH_CODE { get; set; }
            public int YEAR_CODE { get; set; }
            public int? PARTY_CODE { get; set; }
            public decimal? EXCH_RATE { get; set; }
            public string BILL_ADD1 { get; set; }
            public string BILL_ADD2 { get; set; }
            public string BILL_ADD3 { get; set; }
            public string BILL_CITY { get; set; }
            public string BILL_PINCODE { get; set; }
            public string BILL_ADDRESSID { get; set; }
            public string BILL_GST { get; set; }
            public string SHIP_CODE { get; set; }
            public string SHIP_ADD1 { get; set; }
            public string SHIP_ADD2 { get; set; }
            public string SHIP_ADD3 { get; set; }
            public string SHIP_CITY { get; set; }
            public string SHIP_PINCODE { get; set; }
            public string SHIP_ADDRESSID { get; set; }
            public string SHIP_GST { get; set; }
            public string BILL_NO { get; set; }
            public DateTime? BILL_DATE { get; set; }
            public string CHALL_NO { get; set; }
            public DateTime? CHALL_DATE { get; set; }
            public string GATE_TYPE { get; set; }
            public string GATE_NO { get; set; }
            public string TRANSIT_NO { get; set; }
            public string WAYBILL_NO { get; set; }
            public string TRANSPORT_CODE { get; set; }
            public string TRANSPORT_NAME { get; set; }
            public string GR_NO { get; set; }
            public DateTime? GR_DATE { get; set; }
            public string TRUCK_NO { get; set; }
            public string CONTAINER_NO { get; set; }
            public decimal? FRTPAY_AMT { get; set; }
            public decimal? FRTPAY_TAXPER { get; set; }
            public decimal? FRTPAY_TAX { get; set; }
            public string FRTPAY_NAR { get; set; }
            public string REMARKS { get; set; }
            public string STATUS { get; set; }
            public decimal? RECD_QTY { get; set; }
            public decimal? BILL_QTY { get; set; }
            public decimal? AMOUNT { get; set; }
            public decimal? DISC_AMT { get; set; }
            public decimal? PACK_AMT { get; set; }
            public decimal? CGST_AMT { get; set; }
            public decimal? SGST_AMT { get; set; }
            public decimal? IGST_AMT { get; set; }
            public decimal? CESS_AMT { get; set; }
            public decimal? VAT_AMT { get; set; }
            public decimal? OTH_AMT { get; set; }
            public decimal? TCS_PER { get; set; }
            public decimal? TCS_AMT { get; set; }
            public decimal? ROUND_OFF { get; set; }
            public decimal? NAMOUNT { get; set; }
            public string HOLD_PAY { get; set; }
            public string HOLD_REASON { get; set; }
            public DateTime? HOLD_DATE { get; set; }
            public string RET_TYPE { get; set; }
            public string FAPROV_STATUS { get; set; }
            public string FAPROV_REMARKS { get; set; }
        }

        public class ImportExportExpensesEntry2
        {
            public int V_NO { get; set; }
            public string DOC_ID { get; set; }
            public string V_TYPE { get; set; }
            public DateTime V_DATE { get; set; }
            public int COMP_CODE { get; set; }
            public int BRANCH_CODE { get; set; }
            public int YEAR_CODE { get; set; }
            public int SNO { get; set; }
            public int ITEM_CODE { get; set; }
            public string ITEM_NAME { get; set; }
            public int? MAKE_CODE { get; set; }
            public string HSN_CODE { get; set; }
            public string RCM_YN { get; set; }
            public string INPUT_YN { get; set; }
            public int? UOM_CODE { get; set; }
            public string UOM_NAME { get; set; }
            public int? DEPT_CODE { get; set; }
            public decimal? NOS { get; set; }
            public decimal? PLUS_MINUSQTY { get; set; }
            public decimal? WB_QTY { get; set; }
            public decimal? RECD_QTY { get; set; }
            public decimal? BILL_QTY { get; set; }
            public decimal? USD_RATE { get; set; }
            public decimal? EXCH_RATE { get; set; }
            public decimal? RATE { get; set; }
            public decimal? AMOUNT { get; set; }
            public decimal? DISC_PER { get; set; }
            public decimal? DISC_AMT { get; set; }
            public decimal? PACK_PER { get; set; }
            public decimal? PACK_AMT { get; set; }
            public int? TAX_CODE { get; set; }
            public decimal? CGST_PER { get; set; }
            public decimal? CGST_AMT { get; set; }
            public decimal? SGST_PER { get; set; }
            public decimal? SGST_AMT { get; set; }
            public decimal? IGST_PER { get; set; }
            public decimal? IGST_AMT { get; set; }
            public decimal? CESS_PER { get; set; }
            public decimal? CESS_AMT { get; set; }
            public decimal? VAT_PER { get; set; }
            public decimal? VAT_AMT { get; set; }
            public decimal? OTH_AMT { get; set; }
            public decimal? NET_AMT { get; set; }
            public decimal? LAND_RATE { get; set; }
            public decimal? LAND_AMT { get; set; }
            public decimal? POLAND_RATE { get; set; }
            public decimal? PO_RATE { get; set; }
            public string BIN_LOCATION { get; set; }
            public string BIN_CODE { get; set; }
            public string PO_TYPE { get; set; }
            public string PO_NO { get; set; }
            public string SAUDA_TYPE { get; set; }
            public string SAUDA_NO { get; set; }
            public string KANTA_TYPE { get; set; }
            public string KANTA_NO { get; set; }
            public string REQ_TYPE { get; set; }
            public string REQ_NO { get; set; }
            public string GATE_TYPE { get; set; }
            public string GATE_NO { get; set; }
            public string REF_TYPE { get; set; }
            public string REF_NO { get; set; }
            public string QC_TYPE { get; set; }
            public string QC_NO { get; set; }
            public string PASS_TYPE { get; set; }
            public string PASS_NO { get; set; }
            public string EMPTY_YN { get; set; }
            public string MACH_CODE { get; set; }
            public string REMARKS { get; set; }
            public decimal? RATE_MONTHLY { get; set; }
            public decimal? RATE_QUARTERLY { get; set; }
            public decimal? RATE_ANNUALY { get; set; }
            public decimal? RATE_SPECIAL { get; set; }
            public string FINAL_LOCK { get; set; }
        }

        public class ImportExportExpensesEntry3
        {
            public string DOC_ID { get; set; }
            public int V_NO { get; set; }
            public string V_TYPE { get; set; }
            public DateTime V_DATE { get; set; }
            public byte[] IMG_FILE { get; set; }
            public string FILE_NAME { get; set; }
            public string FILE_TYPE { get; set; }
        }

        public class WeightSummary
        {
            public string KantaType { get; set; }
            public int KantaNo { get; set; }
            public int ITEM_CODE { get; set; }
            public string ITEM_NAME { get; set; }
            public decimal NetWt { get; set; }
        }

        public class ImportExportExpensesAllDetailsResponse
        {
            public List<ImportExportExpensesEntry1> Purchase1 { get; set; } = new List<ImportExportExpensesEntry1>();
            public List<ImportExportExpensesEntry2> Purchase2 { get; set; } = new List<ImportExportExpensesEntry2>();
            public List<ImportExportExpensesEntry3> Purchase3 { get; set; } = new List<ImportExportExpensesEntry3>();
            //public List<WeightSummary> WeightSummary { get; set; } = new();
        }

        public class GetImportExportExpensesAllDetailsResponseDetailsRequest
        {
            public string VNO { get; set; }
            public string vType { get; set; }
        }

        public class GatePurchaseDetailsResponse
        {
            public List<Dictionary<string, object>> Header { get; set; } = new();
            public List<Dictionary<string, object>> Items { get; set; } = new();
            public List<WeightSummary> WeightSummary { get; set; } = new();
        }


        //=============Production Batch=============
        public class ProductionBatchSaveModel
        {
            public int VNo { get; set; }

            public string VType { get; set; }

            public DateTime VDate { get; set; }

            public int ItemCode { get; set; }

            public int FromDeptCode { get; set; }

            public int ToDeptCode { get; set; }

            public List<ProductionBatchRow> Rows { get; set; }
        }
        public class ProductionBatchRow
        {
            public string RefType { get; set; }

            public int RefNo { get; set; }

            public int ItemCode { get; set; }

            public string BarcodeNo { get; set; }

            public string BatchNo { get; set; }

            public decimal ApproxWeight { get; set; }
            
            public decimal ActualWeight { get; set; }

            public int Sno { get; set; }
        }
        public class ProductionBatchRequest
        {
            public int? Vno { get; set; }
            public string Vtype { get; set; }
        }

        public class CreateIntimationModel
        {
            public ImportExportExpensesEntryHeaderModel Header { get; set; }
            public List<ItemDetailImportExportExpensesEntryModel> ItemDetails { get; set; }
        }

    }

}

