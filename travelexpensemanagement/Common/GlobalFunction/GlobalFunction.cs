using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using travelexpensemanagement.Common.Globalvariable;

namespace travelexpensemanagement.Common.GlobalFunction
{
    public class GlobalFunction
    {
        private readonly GlobalVariableService _globalVariableService;
        public GlobalFunction(GlobalVariableService globalVariableService)
        {
            _globalVariableService = globalVariableService;
        }
        public async Task StockValuationAsync(SqlConnection con, string vType, int? vNo)
        {
            var g = _globalVariableService.GetGlobalVariables();

            decimal FrtPayAmt;
            decimal ItemLandAmt;
            decimal GstAmt;
            decimal DebitAmt;
            decimal DebitGst;
            decimal CreditAmt;
            decimal CreditGst;
            decimal TCSAmt;
            decimal NetAmt;
            decimal impamt;
            decimal impgst;
            decimal PL_AMT = 0;

            string vtype1 = "";

            const string importSql = @"SELECT REF_TYPE, REF_NO, SUM(NAMOUNT) AS NAMOUNT, SUM(ISNULL(BANK_AMT,0)) AS PL_AMT, SUM(CGST_AMT) AS CGST,
                SUM(SGST_AMT) AS SGST, SUM(IGST_AMT) AS IGST, SUM(QLT_DR_AMT+RDF_DR_AMT+QTY_DR_AMT+QC_DR_AMT+OTH_DR_AMT) AS DEBIT_AMT,
                SUM(QLT_DR_TAX+RDF_DR_TAX+QTY_DR_TAX+QC_DR_TAX+OTH_DR_TAX) AS DEBIT_TAX, SUM(QLT_CR_AMT+RDF_CR_AMT+QTY_CR_AMT+QC_CR_AMT) AS CREDIT_AMT,
                SUM(QLT_CR_TAX+RDF_CR_TAX+QTY_CR_TAX+QC_CR_TAX) AS CREDIT_TAX FROM PURCHASE1
                WHERE REF_TYPE=@VTYPE AND REF_NO=@VNO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE
                GROUP BY REF_TYPE, REF_NO";

            
            var updateList = new List<ImportAmtRow>();

            using (var cmd = new SqlCommand(importSql, con)) 
            {
                cmd.Parameters.AddWithValue("@VTYPE", vType);
                cmd.Parameters.AddWithValue("@VNO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        updateList.Add(new ImportAmtRow
                            {
                               RefType =  reader["REF_TYPE"]?.ToString() ?? "",
                                RefNo = reader["REF_NO"],
                                NAmount = reader["NAMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["NAMOUNT"]),
                                Cgst = reader["CGST"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["CGST"]),
                                Sgst = reader["SGST"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["SGST"]),
                                Igst = reader["IGST"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["IGST"]),
                                DebitAmt = reader["DEBIT_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["DEBIT_AMT"]),
                                CreditAmt = reader["CREDIT_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["CREDIT_AMT"]),
                                DebitTax = reader["DEBIT_TAX"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["DEBIT_TAX"]),
                                CreditTax = reader["CREDIT_TAX"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["CREDIT_TAX"])
                            }
                        );
                    }
                } 
            }
            const string updateImport = @"UPDATE PURCHASE1 SET IMPORT_AMT=@NAMOUNT-@CGST-@SGST-@IGST-@DEBIT_AMT+@CREDIT_AMT,
                                                    IMPORT_TAX=@CGST+@SGST+@IGST-@DEBIT_TAX+@CREDIT_TAX
                                                    WHERE V_TYPE=@REF_TYPE AND V_NO=@REF_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

            foreach (var item in updateList)
            {
                await ExecuteQueryAsync(con, updateImport, new()
                {
                    new("@NAMOUNT", item.NAmount),
                    new("@CGST", item.Cgst),
                    new("@SGST", item.Sgst),
                    new("@IGST", item.Igst),
                    new("@DEBIT_AMT", item.DebitAmt),
                    new("@CREDIT_AMT", item.CreditAmt),
                    new("@DEBIT_TAX", item.DebitTax),
                    new("@CREDIT_TAX", item.CreditTax),
                    new("@REF_TYPE", item.RefType),
                    new("@REF_NO", item.RefNo),
                    new("@COMP_CODE", g.PubCompCode),
                    new("@BRANCH_CODE", g.PubBranchCode)
                });
            }
            if (vType == "STPB")
                vtype1 = "SRPU";
            else if (vType == "RMPB")
                vtype1 = "RCPT";
            else if (vType == "RIMP")
                vtype1 = "RCPI";
            else if (vType == "RRET" || vType == "SRET")
                vtype1 = "";
            else if (vType == "BFPB")
                vtype1 = "BFRC";

            const string landSql = @"SELECT purchase2.ITEM_CODE, purchase2.SNO, purchase2.DISC_AMT AS DDEDUCTION, purchase2.RECD_QTY,
            purchase2.REF_NO AS MRN_NO, purchase2.REF_TYPE AS MRN_TYPE, purchase2.AMOUNT AS ITEM_AMT, purchase1.* FROM PURCHASE2
            INNER JOIN PURCHASE1 ON purchase1.COMP_CODE = purchase2.COMP_CODE AND purchase1.V_TYPE = purchase2.V_TYPE AND purchase1.V_NO = purchase2.V_NO
            LEFT JOIN ITEM_MAST ON ITEM_MAST.CODE = purchase2.ITEM_CODE AND ITEM_MAST.COMP_CODE = purchase2.COMP_CODE LEFT JOIN ITEM_GROUP
                ON ITEM_GROUP.CODE = ITEM_MAST.GROUP_CODE AND ITEM_GROUP.COMP_CODE = ITEM_MAST.COMP_CODE 
            WHERE purchase2.COMP_CODE=@COMP_CODE AND purchase2.BRANCH_CODE=@BRANCH_CODE AND purchase2.YEAR_CODE=@YEAR_CODE AND purchase1.V_TYPE=@VTYPE
                AND purchase1.V_NO=@VNO 
            ORDER BY purchase1.V_TYPE,purchase1.V_NO";

            using (var cmd = new SqlCommand(landSql, con))
            {
                cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@VTYPE", vType);
                cmd.Parameters.AddWithValue("@VNO", vNo);

                var rows = new List<LandAmountRow>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rows.Add(new LandAmountRow
                        {
                            CompCode = reader["COMP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["COMP_CODE"]),
                            BranchCode = reader["BRANCH_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BRANCH_CODE"]),
                            VType = reader["V_TYPE"]?.ToString() ?? "",
                            VNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                            Sno = reader["SNO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SNO"]),
                            ItemCode = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                            MrnType = reader["MRN_TYPE"]?.ToString() ?? "",
                            MrnNo = reader["MRN_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["MRN_NO"]),

                            Amount = reader["AMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMOUNT"]),
                            ItemAmt = reader["ITEM_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["ITEM_AMT"]),
                            NAmount = reader["NAMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["NAMOUNT"]),

                            CgstAmt = reader["CGST_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CGST_AMT"]),
                            SgstAmt = reader["SGST_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SGST_AMT"]),
                            IgstAmt = reader["IGST_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IGST_AMT"]),

                            FrtPayAmt = reader["FRTPAY_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["FRTPAY_AMT"]),

                            QltDrAmt = reader["QLT_DR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QLT_DR_AMT"]),
                            RdfDrAmt = reader["RDF_DR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RDF_DR_AMT"]),
                            QtyDrAmt = reader["QTY_DR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY_DR_AMT"]),
                            QcDrAmt = reader["QC_DR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QC_DR_AMT"]),
                            OthDrAmt = reader["OTH_DR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OTH_DR_AMT"]),

                            QltDrTax = reader["QLT_DR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QLT_DR_TAX"]),
                            RdfDrTax = reader["RDF_DR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RDF_DR_TAX"]),
                            QtyDrTax = reader["QTY_DR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY_DR_TAX"]),
                            QcDrTax = reader["QC_DR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QC_DR_TAX"]),
                            OthDrTax = reader["OTH_DR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OTH_DR_TAX"]),

                            QltCrAmt = reader["QLT_CR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QLT_CR_AMT"]),
                            RdfCrAmt = reader["RDF_CR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RDF_CR_AMT"]),
                            QtyCrAmt = reader["QTY_CR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY_CR_AMT"]),
                            QcCrAmt = reader["QC_CR_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QC_CR_AMT"]),

                            QltCrTax = reader["QLT_CR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QLT_CR_TAX"]),
                            RdfCrTax = reader["RDF_CR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RDF_CR_TAX"]),
                            QtyCrTax = reader["QTY_CR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY_CR_TAX"]),
                            QcCrTax = reader["QC_CR_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QC_CR_TAX"]),

                            TcsAmt = reader["TCS_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TCS_AMT"]),
                            ImportAmt = reader["IMPORT_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IMPORT_AMT"]),
                            ImportTax = reader["IMPORT_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["IMPORT_TAX"]),
                            BankAmt = reader["BANK_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["BANK_AMT"]),

                            InputType = reader["INPUT_TYPE"]?.ToString() ?? ""
                        });
                    }
                }

                foreach (var row in rows)
                {

                    if (row.Amount == 0)
                        continue;

                    ItemLandAmt = row.NAmount * row.ItemAmt / row.Amount;

                    GstAmt = (row.CgstAmt + row.SgstAmt + row.IgstAmt) * row.ItemAmt / row.Amount;

                    FrtPayAmt = row.FrtPayAmt * row.ItemAmt / row.Amount;

                    DebitAmt = (row.QltDrAmt + row.RdfDrAmt + row.QtyDrAmt + row.QcDrAmt + row.OthDrAmt)
                                * row.ItemAmt / row.Amount;

                    DebitGst = (row.QltDrTax + row.RdfDrTax + row.QtyDrTax + row.QcDrTax + row.OthDrTax)
                                * row.ItemAmt / row.Amount;

                    CreditAmt = (row.QltCrAmt + row.RdfCrAmt + row.QtyCrAmt + row.QcCrAmt)
                                * row.ItemAmt / row.Amount;

                    CreditGst = (row.QltCrTax + row.RdfCrTax + row.QtyCrTax + row.QcCrTax)
                                * row.ItemAmt / row.Amount;

                    TCSAmt = row.TcsAmt * row.ItemAmt / row.Amount;

                    impamt = row.ImportAmt * row.ItemAmt / row.Amount;

                    impgst = row.ImportTax * row.ItemAmt / row.Amount;

                    PL_AMT = 0;

                    if (row.NAmount > 0 && row.BankAmt > 0)
                        PL_AMT = row.BankAmt - row.NAmount;

                    PL_AMT = PL_AMT * row.ItemAmt / row.Amount;

                    if (row.InputType == "Input Vat" ||
                        row.InputType == "GST Input" ||
                        row.InputType == "Import" ||
                        row.InputType == "Local" ||
                        row.InputType == "Central" ||
                        row.InputType == "Input Capital")
                    {
                        NetAmt = Math.Round(ItemLandAmt + FrtPayAmt - DebitAmt + CreditAmt - TCSAmt - GstAmt + impamt + PL_AMT, 2);
                    }
                    else
                    {
                        NetAmt = Math.Round(ItemLandAmt + FrtPayAmt - DebitAmt + CreditAmt - DebitGst - TCSAmt + CreditGst + impamt + impgst + PL_AMT, 2);
                    }

                    const string updatePurchase = @"UPDATE PURCHASE2 SET LAND_AMT=@LAND_AMT WHERE COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE
                                                    AND V_NO=@V_NO AND SNO=@SNO AND ITEM_CODE=@ITEM_CODE";

                    // Update PURCHASE2
                    await ExecuteQueryAsync(con, updatePurchase, new()
                    {
                        new("@LAND_AMT", NetAmt),
                        new("@COMP_CODE", row.CompCode),
                        new("@BRANCH_CODE", row.BranchCode),
                        new("@V_TYPE", row.VType),
                        new("@V_NO", row.VNo),
                        new("@SNO", row.Sno),
                        new("@ITEM_CODE", row.ItemCode)
                    });

                    const string updateMrn = @"UPDATE PURCHASE2 SET LAND_AMT=@LAND_AMT WHERE COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@MRN_TYPE
                                            AND V_NO=@MRN_NO AND ITEM_CODE=@ITEM_CODE";

                    // Update MRN
                    if (!row.VType.Equals("RRET", StringComparison.OrdinalIgnoreCase))
                    {
                        await ExecuteQueryAsync(con, updateMrn, new()
                        {
                            new("@LAND_AMT", NetAmt),
                            new("@COMP_CODE", row.CompCode),
                            new("@BRANCH_CODE", row.BranchCode),
                            new("@MRN_TYPE", row.MrnType),
                            new("@MRN_NO", row.MrnNo),
                            new("@ITEM_CODE", row.ItemCode)
                        });
                    }
                }
            }
        }
        private async Task ExecuteQueryAsync(SqlConnection con, string query, List<SqlParameter>? parameters = null)
        {
            using var cmd = new SqlCommand(query, con);
            if (parameters?.Any() == true)
                cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }

        public class LandAmountRow
        {
            public int CompCode { get; set; }
            public int BranchCode { get; set; }
            public string VType { get; set; } = "";
            public int VNo { get; set; }
            public int Sno { get; set; }
            public int ItemCode { get; set; }
            public string MrnType { get; set; } = "";
            public int MrnNo { get; set; }

            public decimal Amount { get; set; }
            public decimal ItemAmt { get; set; }
            public decimal NAmount { get; set; }
            public decimal CgstAmt { get; set; }
            public decimal SgstAmt { get; set; }
            public decimal IgstAmt { get; set; }
            public decimal FrtPayAmt { get; set; }

            public decimal QltDrAmt { get; set; }
            public decimal RdfDrAmt { get; set; }
            public decimal QtyDrAmt { get; set; }
            public decimal QcDrAmt { get; set; }
            public decimal OthDrAmt { get; set; }

            public decimal QltDrTax { get; set; }
            public decimal RdfDrTax { get; set; }
            public decimal QtyDrTax { get; set; }
            public decimal QcDrTax { get; set; }
            public decimal OthDrTax { get; set; }

            public decimal QltCrAmt { get; set; }
            public decimal RdfCrAmt { get; set; }
            public decimal QtyCrAmt { get; set; }
            public decimal QcCrAmt { get; set; }

            public decimal QltCrTax { get; set; }
            public decimal RdfCrTax { get; set; }
            public decimal QtyCrTax { get; set; }
            public decimal QcCrTax { get; set; }

            public decimal TcsAmt { get; set; }
            public decimal ImportAmt { get; set; }
            public decimal ImportTax { get; set; }
            public decimal BankAmt { get; set; }

            public string InputType { get; set; } = "";
        }

        public class ImportAmtRow{
            public string? RefType {get; set;}
            public object? RefNo {get; set;}
            public decimal? NAmount {get; set;}
            public decimal? Cgst {get; set;}
            public decimal? Sgst {get; set;}
            public decimal? Igst {get; set;}
            public decimal? DebitAmt {get; set;}
            public decimal? CreditAmt {get; set;}
            public decimal? DebitTax {get; set;}
            public decimal? CreditTax { get; set; }
        }
    }
}
