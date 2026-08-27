using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalFunction;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseBillPassEntryRepository : IPurchaseBillPassEntryRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DbHelper _dbHelper;
        private readonly GlobalFunction _globalFunction;
        public PurchaseBillPassEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalFunction globalFunction)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _globalFunction = globalFunction;
        }

        public decimal? oldBankAmt = 0.0m;
        public int? oldplno = 0;
        public async Task<decimal> CheckExistingTDS(string billNo, int drCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using var con = _dbConnection.GetErpConnection();
            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "CHeckExistingTDS");
            cmd.Parameters.Add("@BILL_NO", SqlDbType.VarChar).Value = billNo;
            cmd.Parameters.Add("@DEBIT_AC", SqlDbType.Int).Value = drCode;
            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;

            await con.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();

            return result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
        }

        public async Task<(int DebitAc, string DebitAcName)> GetLatestDebitAccount(string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using var con = _dbConnection.GetErpConnection();
            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "LatestDebitAc");
            cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = vType;
            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = gv.PubCompCode;
            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = gv.PubBranchCode;
            cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (
                    reader["DEBIT_AC"] != DBNull.Value ? Convert.ToInt32(reader["DEBIT_AC"]) : 0,
                    reader["NAME"]?.ToString() ?? string.Empty
                );
            }

            return (0, string.Empty);
        }

        public async Task<DebitNoteResponse> CalculateFrieghtPay(DebitNoteRequest request)
        {
            DebitNoteResponse response = new DebitNoteResponse();
            var generalSettings = await _globalVariableService.LoadGeneralSetting();

            DebitNoteCalculationState state = new DebitNoteCalculationState();
            var gv = _globalVariableService.GetGlobalVariables();

            response.txtFrtTaxVal = request.IsFreightTaxChanged
                                    ? request.FreightTax
                                    : request.FreightAmountPay * request.FreightTaxPercent * 0.01m;

            state.frtAmt = 0;
            state.frtTax = 0;
            state.frtNarr = "";
            if (request.FreightAmountPay > 0 && request.FreightTax > 0)
            {
                var res = await _dbHelper.ExecuteScalarAsync($@"Select top 1 isnull(FREIGHT_AC,0) as FDrAc from POSTING_MAST where V_TYPE={request.VType} 
                                        and POST_TYPE={request.inputType} and COMP_CODE={gv.PubCompCode}");
                response.frtDrAcCode = int.TryParse(res, out int drAc) ? drAc : 0;
                string pubDefPOInMRN = (generalSettings.pubDefPOInMRN != null) ? generalSettings.pubDefPOInMRN : "";

                if (pubDefPOInMRN.Equals("YES", StringComparison.OrdinalIgnoreCase))
                {
                    var res1 = await _dbHelper.ExecuteScalarAsync($@"Select LEFT(PRICE_TYPE,1) from Order1 
                                        where V_TYPE={request.Items[0].PoType} and V_NO={request.Items[0].PoNo} and comp_code={gv.PubCompCode} and Branch_code={gv.PubBranchCode}");
                    if (res1 == "F")
                    {
                        state.frtAmt = request.FreightAmountPay;
                        state.frtTax = response.txtFrtTaxVal;
                        state.frtNarr = $@" PO type is FOR but freight charged by supplier= {state.frtAmt:F2},tax={state.frtTax:F2}";
                    }
                }
            }
            return await CalculateDebitNote(request, response, state);

        }

        public async Task<DebitNoteResponse> CalculateDebitNote(DebitNoteRequest request)
        {
            var response = new DebitNoteResponse();
            var state = new DebitNoteCalculationState();

            return await CalculateDebitNote(request, response, state);
        }

        public async Task<DebitNoteResponse> CalculateDebitNote(DebitNoteRequest request, DebitNoteResponse response, DebitNoteCalculationState state)
        {
            //DebitNoteResponse response = new DebitNoteResponse();
            var generalSettings = await _globalVariableService.LoadGeneralSetting();

            decimal GraceFuel = 0;

            if (request == null || request.Items == null || request.Items.Count == 0)
                return response;

            if (!IsValidVoucherType(request.VType))
                return response;

            string pubDefPOInMRN = (generalSettings.pubDefPOInMRN != null) ? generalSettings.pubDefPOInMRN : "";

            //decimal naturalRate = GetNaturalRate(request.Items);

            //DebitNoteCalculationState state = new DebitNoteCalculationState();

            //---------------- Item Wise Calc ---------
            foreach (DebitNoteItem item in request.Items)
            {
                if (item.ItemCode <= 0)
                    continue;

                decimal taxPer = item.CGSTPer + item.SGSTPer + item.IGSTPer;

                if (pubDefPOInMRN.Equals("YES", StringComparison.OrdinalIgnoreCase))
                {
                    var poRateDetails = GetItemOrderRatesByPO(item.PoType, item.PoNo, item.ItemCode);

                    item.PORate = poRateDetails.Rate;
                    item.POLandRate = poRateDetails.LandRate;

                    if (poRateDetails.Exists)
                    {
                        CalculateRateDifferenceNote(item, taxPer, state);

                        CalculateQualityDifferenceNote(request, item, taxPer, state, response);
                    }
                    else
                    {
                        CalculateQualityDifferenceWithoutPO(request, item, taxPer, state, response);
                    }


                }
                if (request.VType.Equals("STPB", StringComparison.OrdinalIgnoreCase))
                {
                    CalculateWeightDifferenceNote(item, taxPer, state, GraceFuel);
                    //------------ when Bill Qty > PO Qty ----------------
                    CalculateDifferenceNoteWithPOQuantity(item, taxPer, state);
                }

            }
            //------------- Total-wise calc ------------
            CalculateTotalWeightDifference(request, state);
            CalculateQCDebitNote(request, state);
            CalculateFreightDifference(request, state);
            CalculateExcessQtyRateDifference(request, state);
            CalculateOtherBottleDeduction(request, state);
            FinalizeDebitNoteCalculation(request, state);
            RoundDebitNoteTax(state);
            ApplyDebitNoteHold(request, state);

            //------------ Rate Debit --------------
            response.RateDiffDebitAmt = state.RateDiffDrGAmt;
            response.RateDiffDebitTax = state.RateDiffDrGTax;
            response.RateDiffDebitNarration = state.RateDiffDrNarr;

            //------------ Quality Debit -------------
            response.QualityDiffDebitAmt = state.QltDiffDrAmt;
            response.QualityDiffDebitTax = state.QltDiffDrTax;
            response.QualityDiffDebitNarration = state.QltDiffDrNarr;

            //-------------- Weight Debit ---------------
            response.WeightDiffDebitAmt = state.QtyDiffGAmt;
            response.WeightDiffDebitTax = state.QtyDiffGTax;
            response.WeightDiffDebitNarration = state.QtyDiffNarr;

            //-------------- QC Debit -------------
            response.QCDebitAmt = state.QCDrAmt;
            response.QCDebitTax = state.QCDrTax;
            response.QCDebitNarration = state.QCDrNarr;

            return response;
        }

        //------------- Valid VType ---------
        private static readonly HashSet<string> ValidVoucherTypes = ["RMPB", "BFPB", "STPB", "STJW"];

        public bool IsValidVoucherType(string vType)
            => ValidVoucherTypes.Contains(vType);

        //------------- Natural Rate ---------
        public decimal GetNaturalRate(List<DebitNoteItem> items) => items.FirstOrDefault(x => x.ItemCode == 30001)?.Amount ?? 0;

        //------------- Rate Difference Note ---------
        public void CalculateRateDifferenceNote(DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state)
        {
            decimal rateDiffDrRate = item.LandRate - item.POLandRate;

            if (rateDiffDrRate <= 0)
                return;

            decimal rateDiffDrAmt = item.BillQty * rateDiffDrRate;

            decimal rateDiffDrTax = (rateDiffDrAmt / (100 + taxPer)) * taxPer;

            rateDiffDrAmt -= rateDiffDrTax;

            decimal amount = item.BillQty * rateDiffDrRate;

            state.RateDiffDrNarr +=
                $"{item.ItemName} Order Rate is {item.POLandRate:F2} " +
                $"but Bill rate is {item.LandRate} " +
                $"Weight is {item.BillQty} {item.Unit} " +
                $"@ {rateDiffDrRate:F4} Rs. {amount:F2}" + "\n";

            state.RateDiffDrGAmt += rateDiffDrAmt;
            state.RateDiffDrGTax += rateDiffDrTax;
        }

        //------------- Quality Difference Note ---------
        public async void CalculateQualityDifferenceNote(DebitNoteRequest request, DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state, DebitNoteResponse response)
        {
            var saudaDetails = GetSaudaDetails(item.PoType, item.PoNo);

            decimal qtyDiffQty = item.RecdQty - item.BillQty;
            decimal discItemRate = 0;

            Q15Result q15Result = new Q15Result();

            if (request.VType == "RMPB")
            {
                q15Result = await CalculateQ15Difference(request, item, saudaDetails, taxPer, response);

                discItemRate = q15Result.DiscItemRate;
            }
            CalculateQualityDifferenceAmount(item, qtyDiffQty, discItemRate, taxPer, q15Result, state);
        }

        //------------- Quality Difference Amount ---------
        public void CalculateQualityDifferenceAmount(DebitNoteItem item, decimal qtyDiffQty, decimal discItemRate, decimal taxPer, Q15Result q15Result,
            DebitNoteCalculationState state)
        {
            if (qtyDiffQty > 0 && discItemRate < 0)
            {
                decimal qltDiffDrRate = Math.Abs(discItemRate);
                decimal qltDiffTaxRate = qltDiffDrRate * taxPer / 100;

                qltDiffDrRate += qltDiffTaxRate;

                decimal qltDiffDrAmt = 0;
                decimal qltDiffDrTax = 0;
                string narration = string.Empty;

                if (qltDiffDrRate > 0)
                {
                    qltDiffDrAmt = Math.Round(qtyDiffQty * qltDiffDrRate, MidpointRounding.AwayFromZero);
                    qltDiffDrTax = Math.Round(FormatAmount((qltDiffDrAmt / (100 + taxPer)) * taxPer), MidpointRounding.AwayFromZero);
                    qltDiffDrAmt = Math.Round(qltDiffDrAmt - qltDiffDrTax, MidpointRounding.AwayFromZero);

                    narration =
                        $"{item.ItemName} is {qtyDiffQty} " +
                        $"@ {qltDiffDrRate:F2} " +
                        $"Rs. {(qtyDiffQty * qltDiffDrRate):F2}";

                    state.QltDiffDrAmt += qltDiffDrAmt;
                    state.QltDiffDrTax += qltDiffDrTax;

                    if (!string.IsNullOrWhiteSpace(narration))
                    {
                        if (!string.IsNullOrWhiteSpace(state.QltDiffDrNarr))
                            state.QltDiffDrNarr += " ";

                        state.QltDiffDrNarr += narration;
                    }
                }
            }

            state.QltDiffDrAmt += q15Result.Amount;
            state.QltDiffDrTax += q15Result.Tax;

            if (!string.IsNullOrWhiteSpace(q15Result.Narration))
            {
                if (!string.IsNullOrWhiteSpace(state.QltDiffDrNarr))
                    state.QltDiffDrNarr += " ";

                state.QltDiffDrNarr += q15Result.Narration;
            }
        }

        //------------- Q15 Difference ---------
        public async Task<Q15Result> CalculateQ15Difference(DebitNoteRequest request, DebitNoteItem item, (decimal rate, int itemCode) saudaDetails, decimal taxPer, DebitNoteResponse response)
        {
            Q15Result result = new Q15Result();
            var gv = _globalVariableService.GetGlobalVariables();

            decimal discItemRate = 0;
            decimal abovePer = 0;
            decimal aboveRate = 0;

            string saudaReq = GetSaudaReqByItem(item.ItemCode);

            if (saudaReq.Equals("YES", StringComparison.OrdinalIgnoreCase))
            {
                var rmDiscount = await GetRMDiscountDetails(saudaDetails.itemCode, item.ItemCode, request.vDate);

                if (rmDiscount.SaudaExists)
                {
                    discItemRate = rmDiscount.DiscRate;

                    if (rmDiscount.Rate > 0 || rmDiscount.AbovePer > 0 || rmDiscount.AboveAmt > 0)
                    {
                        discItemRate = rmDiscount.Rate;
                        abovePer = rmDiscount.AbovePer;
                        aboveRate = rmDiscount.AboveAmt;
                    }
                }
                else
                {
                    response.Warnings.Add($"Item {item.ItemName} not found in Discount Master. Please contact System Administrator.");
                }

            }

            result.DiscItemRate = discItemRate;

            decimal amt1 = 0;
            decimal at1 = 0;
            decimal amt2 = 0;
            decimal at2 = 0;
            decimal amt3 = 0;
            decimal at3 = 0;

            string anr1 = string.Empty;
            string anr2 = string.Empty;
            string anr3 = string.Empty;

            if (abovePer > 0 && ((gv.PubCompCode == "1" && saudaDetails.itemCode == 30001) || (gv.PubCompCode == "7" && saudaDetails.itemCode == 3)))
            {
                decimal sItemQty = request.Items.Where(x => x.ItemCode == saudaDetails.itemCode).Sum(x => x.RecdQty);

                if (item.RecdQty > (sItemQty * abovePer * 0.01m))
                {
                    decimal rqty = item.RecdQty;
                    decimal trqty = 0;
                    decimal rrate = Math.Abs(aboveRate) + discItemRate;

                    if (rrate > 0)
                    {
                        amt1 = Math.Round(rqty * rrate, MidpointRounding.AwayFromZero);
                        at1 = Math.Round(FormatAmount(amt1 * taxPer * 0.01m), MidpointRounding.AwayFromZero);

                        rrate = rrate + (rrate * taxPer / 100);

                        anr1 =
                            $"{item.ItemName} Qty > {abovePer}% " +
                            $"{rqty} @ {rrate:F2} " +
                            $"Rs. {(rqty * rrate):F2} ";

                        trqty += rqty;
                    }
                }
            }

            result.Amount = amt1 + amt2 + amt3;
            result.Tax = at1 + at2 + at3;

            result.Narration = string.Join(", ", new[] { anr1, anr2, anr3 }.Where(x => !string.IsNullOrWhiteSpace(x)));

            return result;
        }

        //------------- Quality Difference Without PO ---------
        public async void CalculateQualityDifferenceWithoutPO(DebitNoteRequest request, DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state, DebitNoteResponse response)
        {
            item.POLandRate = await GetApprovedPOLandRate(item.PoType, item.PoNo);

            decimal discItemRate = 0;

            if (request.VType == "RMPB")
            {
                var discount = await GetDiscountRate(request.billToPartyCode, item.ItemCode);

                if (discount.exists)
                {
                    discItemRate = discount.discountRate;
                }
                else
                {
                    string warning =
                        $"Discount rate not found of '{item.ItemName}' of Supplier '{request.billToPartyName}'.";

                    if (!response.Warnings.Contains(warning))
                        response.Warnings.Add(warning);
                }
            }

            decimal qltDiffDrRate = item.LandRate - (item.PORate + discItemRate);

            decimal qltDiffTaxRate = qltDiffDrRate * taxPer / 100;
            qltDiffDrRate += qltDiffTaxRate;

            if (qltDiffDrRate > 0)
            {
                decimal qltDiffDrAmt = Math.Round(item.BillQty * qltDiffDrRate, MidpointRounding.AwayFromZero);

                decimal qltDiffDrTax = Math.Round(FormatAmount((qltDiffDrAmt / (100 + taxPer)) * taxPer), MidpointRounding.AwayFromZero);

                qltDiffDrAmt = Math.Round(qltDiffDrAmt - qltDiffDrTax, MidpointRounding.AwayFromZero);

                state.QltDiffDrAmt += qltDiffDrAmt;
                state.QltDiffDrTax += qltDiffDrTax;

                state.QltDiffDrNarr +=
                    $"{item.ItemName} is {item.BillQty} " +
                    $"@ {qltDiffDrRate:F2} " +
                    $"Rs. {(item.BillQty * qltDiffDrRate):F2} ";

                decimal qltQtyDiff = item.RecdQty - item.BillQty;

                if (discItemRate < 0 && qltQtyDiff > 0)
                {
                    qltDiffDrRate = Math.Abs(discItemRate);

                    qltDiffTaxRate = qltDiffDrRate * taxPer / 100;
                    qltDiffDrRate += qltDiffTaxRate;
                    qltDiffDrAmt = Math.Round(qltQtyDiff * qltDiffDrRate, MidpointRounding.AwayFromZero);

                    qltDiffDrTax = Math.Round(FormatAmount((qltDiffDrAmt / (100 + taxPer)) * taxPer), MidpointRounding.AwayFromZero);
                    qltDiffDrAmt = Math.Round(qltDiffDrAmt - qltDiffDrTax, MidpointRounding.AwayFromZero);

                    state.QltDiffDrAmt += qltDiffDrAmt;
                    state.QltDiffDrTax += qltDiffDrTax;

                    state.QltDiffDrNarr +=
                        $"{item.ItemName} Weight Diff is {qltQtyDiff} " +
                        $"@ {qltDiffDrRate:F2} " +
                        $"Rs. {(qltQtyDiff * qltDiffDrRate):F2} ";
                }
            }
        }

        //------------- Weight Difference ---------
        private void CalculateWeightDifferenceNote(DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state,
            decimal graceFuel)
        {
            decimal qtyDiffQty = item.BillQty - item.RecdQty;

            if ((qtyDiffQty - graceFuel) <= 0)
                return;

            decimal qtyDiffAmt = Math.Round(item.LandRate * qtyDiffQty, MidpointRounding.AwayFromZero);

            decimal qtyDiffTax = Math.Round(FormatAmount((qtyDiffAmt / (100 + taxPer)) * taxPer), MidpointRounding.AwayFromZero);

            qtyDiffAmt = Math.Round(qtyDiffAmt - qtyDiffTax, MidpointRounding.AwayFromZero);

            state.QtyDiffGAmt += qtyDiffAmt;
            state.QtyDiffGTax += qtyDiffTax;

            if (!string.IsNullOrWhiteSpace(state.QtyDiffNarr))
                state.QtyDiffNarr += Environment.NewLine;

            state.QtyDiffNarr +=
                $"Short Material Recd of {item.ItemName} is {qtyDiffQty} " +
                $"Deduct Weight {qtyDiffAmt - graceFuel:F2} " +
                $"@ {item.LandRate:F4}";
        }

        //------------- Qty diff dr note (when Bill Qty > PO Qty) ---------
        private void CalculateDifferenceNoteWithPOQuantity(DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state)
        {
            var poQty = GetItemOrderRatesByPO(item.PoType, item.PoNo, item.ItemCode);

            decimal diffPoQty = item.RecdQty - poQty.Qty;

            if (diffPoQty <= 0)
                return;

            decimal qtyDiffAmt = FormatAmount(item.LandRate * diffPoQty);
            decimal qtyDiffTax = Math.Round(FormatAmount((qtyDiffAmt / (100 + taxPer)) * taxPer), MidpointRounding.AwayFromZero);
            qtyDiffAmt = Math.Round(qtyDiffAmt - qtyDiffTax, MidpointRounding.AwayFromZero);

            state.QtyDiffGAmt += qtyDiffAmt;
            state.QtyDiffGTax += qtyDiffTax;

            state.QtyDiffNarr +=
                $"Excess Bill Qty From PO Qty of {item.ItemName} is {diffPoQty:0.00} " +
                $"Deduct Weight {qtyDiffAmt} @ {item.LandRate:F4}";
        }

        //------------- Total Weight Difference ---------
        private void CalculateTotalWeightDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (request.VType.Equals("RMPB", StringComparison.OrdinalIgnoreCase))
            {
                CalculateRMPBWeightDifference(request, state);
            }
            else if (request.VType.Equals("BFPB", StringComparison.OrdinalIgnoreCase))
            {
                CalculateBFPBWeightDifference(request, state);
            }
        }

        //------------- RMPB Weight Difference ---------
        private void CalculateRMPBWeightDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            decimal qtyDiffQty = Math.Abs(request.totalBillQty - request.totalRcvdQty);

            if (request.totalRcvdQty >= request.totalBillQty)
                return;

            if (qtyDiffQty == 0)
                return;

            decimal graceRM;

            var gv = _globalVariableService.GetGlobalVariables();

            if (qtyDiffQty >= 50 || gv.PubCompCode == "2")
            {
                graceRM = 0;
            }
            else if (request.totalBillQty > 15000)
            {
                graceRM = 30;
            }
            else if (request.totalBillQty > 9000)
            {
                graceRM = 15;
            }
            else
            {
                graceRM = 10;
            }

            if ((qtyDiffQty - graceRM) > 0)
            {
                decimal deductQty = qtyDiffQty - graceRM;

                decimal ldRate = FormatRate((request.totalNetAmt - request.totalTCSAmt) / request.totalBillQty);

                decimal taxPer = GetTaxPer(request);

                decimal amount = Math.Round(((deductQty * ldRate) / (100 + taxPer)) * 100, MidpointRounding.AwayFromZero);

                decimal tax = Math.Round(amount * taxPer / 100, MidpointRounding.AwayFromZero);

                state.QtyDiffGAmt += amount;
                state.QtyDiffGTax += tax;

                state.QtyDiffNarr =
                    $"Short Material Recd is {qtyDiffQty} " +
                    $"Deduct Weight {deductQty} @ {ldRate:F4}";
            }
        }

        //------------- BFPB Weight Difference ---------
        private void CalculateBFPBWeightDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            decimal qtyDiffQty = request.totalBillQty - request.totalRcvdQty;

            decimal graceFuel;

            if (request.isSealedVehicle)
            {
                graceFuel = request.totalBillQty * 1.5m / 100;
            }
            else
            {
                graceFuel = Math.Round(request.totalBillQty * 0.6m / 100, MidpointRounding.AwayFromZero);
            }

            if (qtyDiffQty <= graceFuel)
                return;

            decimal deductQty = qtyDiffQty - graceFuel;

            decimal ldRate = FormatRate((request.totalNetAmt - request.totalTCSAmt) / request.totalBillQty);

            decimal taxPer = GetTaxPer(request);

            decimal amount = Math.Round(((deductQty * ldRate) / (100 + taxPer)) * 100, MidpointRounding.AwayFromZero);

            decimal tax = Math.Round(amount * taxPer / 100, MidpointRounding.AwayFromZero);

            state.QtyDiffGAmt += amount;
            state.QtyDiffGTax += tax;

            state.QtyDiffNarr =
                $"Short Material Recd is {qtyDiffQty} " +
                $"Deduct Weight {deductQty} @ {ldRate:F4}";
        }

        //------------- QC Debit Note ---------
        private void CalculateQCDebitNote(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (request.VType != "RIMP" && request.mrnNo >= 0)
            {
                decimal taxPer = GetTaxPer(request);

                var qc = GetQCDetails(request.mrnType, request.mrnNo);

                state.QCDrAmt = Math.Round((qc.DeductAmount / (100 + taxPer)) * 100, MidpointRounding.AwayFromZero);

                state.QCDrTax = Math.Round(state.QCDrAmt * taxPer / 100, MidpointRounding.AwayFromZero);

                state.QCDrNarr = qc.Narration;
            }
        }

        //------------- Frieght Difference ---------
        private void CalculateFreightDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            state.RateDiffDrGAmt += state.frtAmt;
            state.RateDiffDrGTax += state.frtTax;

            if (!string.IsNullOrWhiteSpace(state.frtNarr))
            {
                if (!string.IsNullOrWhiteSpace(state.RateDiffDrNarr))
                    state.RateDiffDrNarr += ", ";

                state.RateDiffDrNarr += state.frtNarr;
            }
        }

        //------------- Excess Qty Rate Difference From Market rate and sauda rate ---------
        private async void CalculateExcessQtyRateDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!request.VType.Equals("RMPB", StringComparison.OrdinalIgnoreCase))
                return;

            decimal taxPer = GetTaxPer(request);

            int saudaNo = await GetSaudaNo(request.VNo);

            if (saudaNo <= 0)
                return;

            decimal totalPurchaseQty = await GetTotalPurchaseQty(saudaNo, request.VNo);

            var sauda = GetSaudaInfo(saudaNo);

            if (sauda == null)
                return;

            decimal toleranceQty = await GetToleranceQty(sauda.Qty);

            decimal currentQty = totalPurchaseQty + request.totalRcvdQty;

            decimal diffQty = 0;
            decimal diffRate = 0;

            if ((sauda.Qty + toleranceQty) < currentQty)
            {
                diffQty = Math.Abs((sauda.Qty + toleranceQty) - currentQty);
                decimal marketRate = await GetMarketRate(sauda.VDate, sauda.ItemCode);
                if (marketRate > 0 && sauda.Rate > 0 && marketRate < sauda.Rate)
                {
                    diffRate = sauda.Rate - marketRate;
                }
            }

            decimal diffAmount = FormatAmount(diffQty * diffRate);

            if (diffAmount <= 0)
                return;

            state.RateDiffDrGAmt += diffAmount;

            state.RateDiffDrGTax = Math.Round(state.RateDiffDrGAmt * taxPer / 100, MidpointRounding.AwayFromZero);

            if (!string.IsNullOrWhiteSpace(state.RateDiffDrNarr))
                state.RateDiffDrNarr += " ";

            state.RateDiffDrNarr +=
                $"Excess Qty {diffQty} received, charged @ RateDiff = {diffRate}";
        }

        //------------- Deduction For Item Received other than Natural Bottle ---------
        private async void CalculateOtherBottleDeduction(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!request.VType.Equals("RMPB", StringComparison.OrdinalIgnoreCase))
                return;

            int saudaNo = await GetSaudaNo(request.VNo);

            if (saudaNo <= 0)
                return;

            var sauda = GetNaturalBottleDetails(saudaNo);

            if (sauda == null)
                return;

            // ONLY_NATURAL = 1
            if (!sauda.OnlyNatural)
                return;

            bool otherBottleReceived = request.Items.Any(x => x.ItemCode != sauda.ItemCode);

            if (!otherBottleReceived)
                return;

            decimal taxPer = GetTaxPer(request);

            decimal amount = request.totalRcvdQty * 1.5m;

            decimal tax = Math.Round(amount * taxPer / 100, MidpointRounding.AwayFromZero);

            state.QltDiffDrAmt += amount;
            state.QltDiffDrTax += tax;

            string narration =
                $"Deduct for other Bottle (As per Sauda Premium Rate) " +
                $"{request.totalRcvdQty} Kg @ 1.5 => {amount} + Tax => {tax}";

            if (!string.IsNullOrWhiteSpace(state.QltDiffDrNarr))
                state.QltDiffDrNarr += " ";

            state.QltDiffDrNarr += narration;
        }

        //------------- Final Debit Notes ---------
        private void FinalizeDebitNoteCalculation(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            decimal totalDebit =
                state.QltDiffDrAmt + state.QltDiffDrTax +
                state.RateDiffDrGAmt + state.RateDiffDrGTax +
                state.QCDrAmt + state.QCDrTax +
                state.QtyDiffGAmt + state.QtyDiffGTax;

            // If total debit <= 10 then clear all debit notes
            if (totalDebit <= 10)
            {
                state.QltDiffDrAmt = 0;
                state.QltDiffDrTax = 0;
                state.QltDiffDrNarr = string.Empty;

                state.RateDiffDrGAmt = 0;
                state.RateDiffDrGTax = 0;
                state.RateDiffDrNarr = string.Empty;

                state.QCDrAmt = 0;
                state.QCDrTax = 0;
                state.QCDrNarr = string.Empty;

                state.QtyDiffGAmt = 0;
                state.QtyDiffGTax = 0;
                state.QtyDiffNarr = string.Empty;
            }

            // If Amount=0 and Tax<=1 then Tax=0
            if (state.QltDiffDrAmt == 0 && state.QltDiffDrTax <= 1)
                state.QltDiffDrTax = 0;

            if (state.RateDiffDrGAmt == 0 && state.RateDiffDrGTax <= 1)
                state.RateDiffDrGTax = 0;

            if (state.QCDrAmt == 0 && state.QCDrTax <= 1)
                state.QCDrTax = 0;

            if (state.QtyDiffGAmt == 0 && state.QtyDiffGTax <= 1)
                state.QtyDiffGTax = 0;

            // Packing tolerance adjustment
            ApplyPackingTolerance(request, state);
        }

        //------------- Packing Tolerance ---------
        private async void ApplyPackingTolerance(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            decimal rateDebit = state.RateDiffDrGAmt + state.RateDiffDrGTax;

            if (rateDebit <= 0 || request.totalPackingAmt <= 0)
                return;

            decimal packAmtPO = await GetPOPackingAmount(request.Items[0].PoType, request.Items[0].PoNo);

            if (packAmtPO <= 0)
                return;

            decimal rdf15Per = Math.Round(packAmtPO * 0.15m, 0, MidpointRounding.AwayFromZero);

            if (request.totalPackingAmt >= packAmtPO &&
                request.totalPackingAmt <= Math.Round(packAmtPO + packAmtPO * 0.15m, 0))
            {
                if (rateDebit <= rdf15Per)
                {
                    state.RateDiffDrGAmt = 0;
                    state.RateDiffDrGTax = 0;
                    state.RateDiffDrNarr = string.Empty;
                }
            }
        }

        //------------- Rounded Debit Notes ---------
        private void RoundDebitNoteTax(DebitNoteCalculationState state)
        {
            state.QltDiffDrTax = AdjustTaxDecimal(state.QltDiffDrTax);
            state.RateDiffDrGTax = AdjustTaxDecimal(state.RateDiffDrGTax);
            state.QCDrTax = AdjustTaxDecimal(state.QCDrTax);
            state.QtyDiffGTax = AdjustTaxDecimal(state.QtyDiffGTax);
        }

        //------------- Hold Debit Notes ---------
        private async void ApplyDebitNoteHold(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!await IsDebitNoteHoldParty(request.billToPartyCode))
                return;

            state.QltDiffDrAmt = 0;
            state.QltDiffDrTax = 0;

            state.RateDiffDrGAmt = 0;
            state.RateDiffDrGTax = 0;

            state.QCDrAmt = 0;
            state.QCDrTax = 0;

            state.QtyDiffGAmt = 0;
            state.QtyDiffGTax = 0;
        }

        //---------------------------------- HELPERS ------------------------------
        //-------------- Items Order Rates By PO ----------------
        public OrderRateDetailsDto GetItemOrderRatesByPO(string poType, int poNo, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var rateDetails = new OrderRateDetailsDto();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "OrderRatesByPO");
                    cmd.Parameters.AddWithValue("@V_TYPE", poType);
                    cmd.Parameters.AddWithValue("@V_NO", poNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            rateDetails.Exists = true;
                            rateDetails.LandRate = dr["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(dr["LAND_RATE"]) : 0;
                            rateDetails.Rate = dr["RATE"] != DBNull.Value ? Convert.ToDecimal(dr["RATE"]) : 0;
                            rateDetails.Qty = dr["QTY"] != DBNull.Value ? Convert.ToDecimal(dr["QTY"]) : 0;
                        }
                    }
                }
            }

            return rateDetails;
        }

        //-------------- Sauda Details By PO ----------------
        private (decimal rate, int itemCode) GetSaudaDetails(string poType, int poNo)
        {
            decimal rate = 0;
            int itemCode = 0;

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "SaudaDetails");
                cmd.Parameters.AddWithValue("@V_TYPE", poType);
                cmd.Parameters.AddWithValue("@V_NO", poNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        rate = dr["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RATE"]);
                        itemCode = dr["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ITEM_CODE"]);
                    }
                }
            }

            return (rate, itemCode);
        }

        //-------------- Sauda Required Or Not By Item ----------------
        private string GetSaudaReqByItem(int itemCode)
        {
            string saudaReq = string.Empty;

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SaudaReq");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        saudaReq = result.ToString();
                    }
                }
            }

            return saudaReq;
        }

        //-------------- Discount Details ----------------
        private async Task<RMDiscountDetails> GetRMDiscountDetails(int saudaItemCode, int itemCode, DateTime voucherDate)
        {
            try
            {
                var RMDiscDetails = new RMDiscountDetails();
                var gv = _globalVariableService.GetGlobalVariables();

                // Query 1 : Check SAUDA_ITEM exists
                var parameters = new Dictionary<string, object>
                    {
                        {"@Action", "SaudaExists"},
                        {"@COMP_CODE", gv.PubCompCode},
                        {"@SAUDA_ITEM", saudaItemCode},
                    };
                string saudaExists = await _dbHelper.GetExecuteScalarAsync<string>("sp_PurchaseBillPassEntryDirect", parameters, true);
                RMDiscDetails.SaudaExists = saudaExists != null;

                if (RMDiscDetails.SaudaExists)
                {
                    // Query 2 : Get SAUDA_ITEM discount rate
                    var parameters1 = new Dictionary<string, object>
                    {
                        {"@Action", "SaudaDiscRate"},
                        {"@COMP_CODE", gv.PubCompCode},
                        {"@SAUDA_ITEM", saudaItemCode},
                        {"@ITEM_CODE", itemCode},
                    };
                    RMDiscDetails.DiscRate = await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", parameters1, true);
                }

                // Query 3 : Get Discount Details
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DiscDetails");
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@EFF_DATE", voucherDate.Date);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                RMDiscDetails.Rate = dr["RATE"] != DBNull.Value ? Convert.ToDecimal(dr["RATE"]) : 0;
                                RMDiscDetails.AbovePer = dr["ABOVE_PER"] != DBNull.Value ? Convert.ToDecimal(dr["ABOVE_PER"]) : 0;
                                RMDiscDetails.AboveAmt = dr["ABOVE_AMT"] != DBNull.Value ? Convert.ToDecimal(dr["ABOVE_AMT"]) : 0;
                            }
                        }
                    }
                    return RMDiscDetails;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //-------------- Approved Land Rate ----------------
        private async Task<decimal> GetApprovedPOLandRate(string poType, int poNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var parameters = new Dictionary<string, object>
            {
                {"@Action", "AppPOLandRate"},
                {"@V_TYPE", poType},
                {"@V_NO", poNo},
                {"@COMP_CODE", gv.PubCompCode},
                {"@BRANCH_CODE", gv.PubBranchCode}
            };
            decimal rate = await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", parameters, true);
            return rate;
        }

        //-------------- Discount Rate ----------------
        private async Task<(bool exists, decimal discountRate)> GetDiscountRate(int supplierCode, int itemCode)
        {
            decimal discountRate = 0;
            bool exists = false;

            var gv = _globalVariableService.GetGlobalVariables();
            var parameters = new Dictionary<string, object>
            {
                {"@Action", "DiscRate"},
                {"@PARTY_CODE", supplierCode},
                {"@ITEM_CODE", itemCode},
                {"@COMP_CODE", gv.PubCompCode}
            };
            var result = await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", parameters, true);

            if (result >= 0)
            {
                exists = true;
                discountRate = result;
            }

            return (exists, discountRate);
        }

        //-------------- QC Details ----------------
        private QCDetailsDto GetQCDetails(string mrnType, int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            QCDetailsDto result = new QCDetailsDto();

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "QCDetails");
            cmd.Parameters.AddWithValue("@REF_TYPE", mrnType);
            cmd.Parameters.AddWithValue("@REF_NO", mrnNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            con.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                result.DeductAmount = Convert.ToDecimal(dr["DEDUCT_AMT"]);
                result.Narration = Convert.ToString(dr["NARRATION"]);
            }

            return result;
        }

        //-------------- Sauda No ----------------
        private async Task<int> GetSaudaNo(int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            return await _dbHelper.GetExecuteScalarAsync<int>("sp_PurchaseBillPassEntryDirect", new Dictionary<string, object>
            { ["@Action"] = "GetSaudaNo", ["@COMP_CODE"] = gv.PubCompCode, ["@V_NO"] = vNo }, true);
        }

        //-------------- Total Purchase Qty ----------------
        private async Task<decimal> GetTotalPurchaseQty(int saudaNo, int VNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var parameters = new Dictionary<string, object>
            {
                {"@V_NO", VNo},
                {"@SAUDA_NO", saudaNo},
                {"@COMP_CODE", gv.PubCompCode}
            };
            decimal result = await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", parameters, true);
            return result;
        }

        //-------------- Sauda Details ----------------
        private SaudaInfo GetSaudaInfo(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();


            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "SaudaInfo");
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@V_NO", saudaNo);

            con.Open();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read()) return null;

            return new SaudaInfo
            {
                ItemCode = Convert.ToInt32(dr["ITEM_CODE"]),
                Qty = Convert.ToDecimal(dr["QTY"]),
                Rate = Convert.ToDecimal(dr["RATE"]),
                VDate = Convert.ToDateTime(dr["V_DATE"])
            };
        }

        //-------------- Tolerance Qty ----------------
        private async Task<decimal> GetToleranceQty(decimal saudaQty)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            return await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", new Dictionary<string, object>
            { ["@Action"] = "ToleranceQty", ["@COMP_CODE"] = gv.PubCompCode, ["@QTY"] = saudaQty }, true);
        }

        //-------------- Market Rate Details ----------------
        private async Task<decimal> GetMarketRate(DateTime saudaDate, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            return await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", new Dictionary<string, object>
            { ["@Action"] = "MarketRate", ["@COMP_CODE"] = gv.PubCompCode, ["@EFF_DATE"] = saudaDate, ["@ITEM_CODE"] = itemCode }, true);
        }

        //-------------- Market Rate Details ----------------
        private NaturalBottleDto? GetNaturalBottleDetails(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", con);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "NaturalBottleDetails");
            cmd.Parameters.AddWithValue("@V_NO", saudaNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

            con.OpenAsync();
            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new NaturalBottleDto
                {
                    OnlyNatural = dr["ONLY_NATURAL"] != DBNull.Value && Convert.ToInt32(dr["ONLY_NATURAL"]) == 1,
                    ItemCode = dr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(dr["ITEM_CODE"]) : 0
                };
            }

            return null;
        }

        private async Task<decimal> GetPOPackingAmount(string poType, int poNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            return await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", new Dictionary<string, object>
            {
                ["@Action"] = "POPackingAmt",
                ["@V_TYPE"] = poType,
                ["@V_NO"] = poNo,
                ["@COMP_CODE"] = gv.PubCompCode,
                ["@BRANCH_CODE"] = gv.PubBranchCode,
                ["@YEAR_CODE"] = gv.PubFYearCode
            }, true);
        }

        private async Task<bool> IsDebitNoteHoldParty(int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            return await _dbHelper.GetExecuteScalarAsync<int>("sp_PurchaseBillPassEntryDirect",
                new Dictionary<string, object>
                { ["@Action"] = "CheckDebitNoteHold", ["@PARTY_CODE"] = partyCode, ["@COMP_CODE"] = gv.PubCompCode }, true) == 1;
        }

        private decimal GetTaxPer(DebitNoteRequest request)
        {
            var item = request.Items.FirstOrDefault();
            if (item == null)
                return 0;

            return item.CGSTPer > 0 ? item.CGSTPer + item.SGSTPer : item.IGSTPer;
        }

        private decimal FormatAmount(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private decimal FormatRate(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

        private decimal AdjustTaxDecimal(decimal tax)
        {
            if (tax <= 0)
                return tax;

            string[] parts = tax.ToString("0.00").Split('.');

            if (parts.Length != 2)
                return tax;

            string decimalPart = parts[1];

            if (decimalPart.Length == 2)
            {
                int decValue = Convert.ToInt32(decimalPart);

                if (decValue % 2 != 0)
                {
                    tax += 0.01m;
                }
            }

            return tax;
        }

        private async Task<decimal> GetSaudaQty(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var parameters = new Dictionary<string, object>
            {
                { "@Action", "GetSaudaQty" },
                { "@COMP_CODE", gv.PubCompCode },
                { "@V_NO", saudaNo }
            };
            decimal qty = await _dbHelper.GetExecuteScalarAsync<decimal>("sp_PurchaseBillPassEntryDirect", parameters, true);
            return qty;
        }

        //==========================Validations=======================
        public async Task<PurchaseQtyValidationResult> CheckPurchaseQtyExcess(int vNo, decimal currentRecQty)
        {
            // 1. Get Sauda No
            var saudaNo = await GetSaudaNo(vNo);

            if (saudaNo == 0)
            {
                return new PurchaseQtyValidationResult
                {
                    IsExcess = false
                };
            }

            // 2. Get Total Purchase Qty
            var totalPurchaseQty = await GetTotalPurchaseQty(vNo, saudaNo);

            // 3. Get Sauda Qty
            var saudaQty = await GetSaudaQty(saudaNo);

            // 4. Get Tolerance Qty
            var toleranceQty = await GetToleranceQty(saudaQty);

            var totalQty = totalPurchaseQty + currentRecQty;
            var allowedQty = saudaQty + toleranceQty;

            return new PurchaseQtyValidationResult
            {
                IsExcess = totalQty > allowedQty,
                TotalPurchaseQty = totalQty,
                AllowedQty = allowedQty
            };
        }

        public async Task<PurchaseRowValidationResult> ValidatePurchaseRow(string vType, int itemCode, string itemName, string billHsnCode,
            decimal qty, decimal freightAmount, string poType, int poNo, string mrnType, int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();

            var result = new PurchaseRowValidationResult
            {
                IsValid = true,
                pubDefPOInMRN = gs.pubDefPOInMRN
            };

            //=============================
            // Freight Term Validation
            //=============================
            if (freightAmount > 0 && gs.pubDefPOInMRN == "Yes")
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@Action", "GetPriceType" },
                    { "@V_TYPE", poType },
                    { "@V_NO", poNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };

                string freightTerm = await _dbHelper.GetExecuteScalarAsync<string>("sp_PurchaseBillPassEntryDirect", parameters, true);

                if (!string.IsNullOrWhiteSpace(freightTerm))
                {
                    if (freightTerm == "F")
                    {
                        result.FreightWarning = true;
                        result.FreightMessage = $"Freight Term not matched with PO Freight Term. Kindly check PO No.: {poType}{poNo}";
                    }
                }
                else
                {
                    string saudaQuery = @"SELECT DISTINCT SAUDA_NO FROM ORDER1 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO
                                            AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                    int saudaNo = await _dbHelper.GetExecuteScalarAsync<int>(saudaQuery, parameters);
                    if (saudaNo > 0)
                    {
                        string frtTermQuery = @"SELECT LEFT(FRT_TERM,1) FROM SAUDA WHERE V_TYPE='PAUD' AND V_NO=@V_NO
                                                AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                        var saudaParams = new Dictionary<string, object>
                        {
                            { "@V_NO", saudaNo },
                            { "@COMP_CODE", gv.PubCompCode },
                            { "@BRANCH_CODE", gv.PubBranchCode }
                        };

                        string saudaFreightTerm = await _dbHelper.GetExecuteScalarAsync<string>(frtTermQuery, saudaParams);

                        if (saudaFreightTerm?.Trim() == "F")
                        {
                            result.FreightWarning = true;
                            result.FreightMessage = $"Freight Term not matched with Sauda Freight Term. Kindly check Sauda No.: {saudaNo}";
                        }
                    }
                }
            }

            //=============================
            // HSN Validation
            //=============================
            if (!string.IsNullOrEmpty(billHsnCode) && billHsnCode.Length > 2)
            {
                string hsnQuery = @"SELECT LEFT(HSN_CODE,4) FROM ITEM_MAST WHERE CODE=@ITEM_CODE AND COMP_CODE=@COMP_CODE";

                var hsnParams = new Dictionary<string, object>
                {
                    { "@ITEM_CODE", itemCode },
                    { "@COMP_CODE", gv.PubCompCode }
                };

                string itemHsn = await _dbHelper.GetExecuteScalarAsync<string>(hsnQuery, hsnParams);

                if (billHsnCode.Substring(0, 4) != itemHsn)
                {
                    result.HsnMismatch = true;
                    result.HsnMessage = $"Wrong HSN code as per Item Master vs Purchase Bill. Document approval is required for {itemName}.";
                    result.Item_vs_Bill_HSNCodeDiff = true;
                }
            }

            //=============================
            // QC Validation
            //=============================
            var qcResult = await ValidatePendingQc(vType, itemCode, itemName, qty, mrnType, mrnNo);

            if (!qcResult.IsValid)
            {
                result.IsValid = false;
                result.QcPending = true;
                result.QcMessage = qcResult.Message;
            }

            return result;
        }

        public async Task<ValidationResult> ValidatePendingQc(string vType, int itemCode, string itemName, decimal qty, string mrnType, int mrnNo)
        {
            // Skip validation if quantity is zero or voucher type is RIMP
            if (qty <= 0 || vType.Equals("RIMP", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult
                {
                    IsValid = true
                };
            }

            var gv = _globalVariableService.GetGlobalVariables();

            // Check whether QC is required for the item
            string qcYnQuery = @"SELECT ISNULL(QC_YN, '') FROM ITEM_MAST WHERE CODE = @ITEM_CODE AND COMP_CODE = @COMP_CODE";

            var qcYnParams = new Dictionary<string, object>
            {
                { "@ITEM_CODE", itemCode },
                { "@COMP_CODE", gv.PubCompCode }
            };

            string qcYn = await _dbHelper.GetExecuteScalarAsync<string>(qcYnQuery, qcYnParams);

            if (!string.Equals(qcYn, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult
                {
                    IsValid = true
                };
            }

            // Check whether QC has been generated
            string qcNoQuery = @"SELECT TOP 1 CONCAT(a.V_TYPE, a.V_NO) FROM QC1 a
                                INNER JOIN QC2 b ON a.V_TYPE = b.V_TYPE AND a.V_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE AND a.BRANCH_CODE = b.BRANCH_CODE
                                AND a.YEAR_CODE = b.YEAR_CODE
                                WHERE b.ITEM_CODE = @ITEM_CODE AND a.MRN_TYPE = @MRN_TYPE AND a.MRN_NO = @MRN_NO AND a.COMP_CODE = @COMP_CODE AND a.BRANCH_CODE = @BRANCH_CODE";

            var qcNoParams = new Dictionary<string, object>
            {
                { "@ITEM_CODE", itemCode },
                { "@MRN_TYPE", mrnType },
                { "@MRN_NO", mrnNo },
                { "@COMP_CODE", gv.PubCompCode },
                { "@BRANCH_CODE", gv.PubBranchCode }
            };

            string qcNo = await _dbHelper.GetExecuteScalarAsync<string>(qcNoQuery, qcNoParams);

            if (!string.IsNullOrWhiteSpace(qcNo))
            {
                // Uncomment if QC approval validation is required later.

                /*
                string statusQuery = @"SELECT FAPROV_STATUS FROM QC1 WHERE CONCAT(V_TYPE, V_NO) = @QC_NO
                      AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                var statusParams = new Dictionary<string, object>
                {
                    { "@QC_NO", qcNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@YEAR_CODE", gv.PubFYearCode }
                };

                string status = await _dbHelper.GetExecuteScalarAsync<string>(statusQuery, statusParams);

                if (!string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return new QcValidationResult
                    {
                        IsValid = false,
                        Message = $"QC Generated But Not Approved. QC No. => {qcNo}"
                    };
                }
                */
            }
            else
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"QC pending of {itemName}"
                };
            }


            return new ValidationResult
            {
                IsValid = true
            };
        }

        public async Task<ValidationResult> ValidatePoSaudaApproval(int itemCode, string itemName, string poType, int poNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();

            // Nothing to validate
            if (itemCode <= 0 || poNo <= 0)
            {
                return new ValidationResult { IsValid = true };
            }

            //=============================
            // PO Approval Validation
            //=============================
            if (gs.pubDefPOInMRN == "Yes")
            {
                string poQuery = @"SELECT 1 FROM ORDER1 WHERE FAPROV_STATUS = 'Approved' AND V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                var poParams = new Dictionary<string, object>
                {
                    { "@V_TYPE", poType },
                    { "@V_NO", poNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };
                int approved = await _dbHelper.GetExecuteScalarAsync<int>(poQuery, poParams);
                if (approved != 1)
                {
                    return new ValidationResult {IsValid = false, Message = $"PO No. {poType}{poNo} of Item {itemName} is not approved."};
                }
            }

            //=============================
            // Sauda Approval Validation
            //=============================
            string saudaQuery = @"SELECT TOP 1 SAUDA_NO FROM ORDER2 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
            var saudaParams = new Dictionary<string, object>
            {
                { "@V_TYPE", poType },
                { "@V_NO", poNo },
                { "@COMP_CODE", gv.PubCompCode },
                { "@BRANCH_CODE", gv.PubBranchCode }
            };
            int saudaNo = await _dbHelper.GetExecuteScalarAsync<int>(saudaQuery, saudaParams);

            if (saudaNo > 0)
            {
                string approvalQuery = @"SELECT TOP 1 1 FROM SAUDA WHERE FAPROV_STATUS = 'Approved' AND V_TYPE = 'PAUD' AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                var approvalParams = new Dictionary<string, object>
                {
                    { "@V_NO", saudaNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };
                int saudaApproved = await _dbHelper.GetExecuteScalarAsync<int>(approvalQuery, approvalParams);
                if (saudaApproved != 1)
                {
                    return new ValidationResult {IsValid = false, Message = $"Sauda No. {saudaNo} of Item {itemName} is not approved."};
                }
            }

            return new ValidationResult {IsValid = true};
        }

        public async Task<ValidationResult> ValidatePartyGst(string gstType, string partyCode, string gstNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(gstNo))
            {
                return new ValidationResult {IsValid = true};
            }

            string query;
            var parameters = new Dictionary<string, object>
            {
                { "@COMP_CODE", gv.PubCompCode },
                { "@CODE", partyCode }
            };

            if (gstType.Equals("BillTo", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT LTRIM(RTRIM(GSTIN)) FROM SUBGROUP_ADDRESS WHERE GSTIN = @GSTIN AND COMP_CODE = @COMP_CODE AND CODE = @CODE";
                parameters.Add("@GSTIN", gstNo);
                string dbGstNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                if (!string.Equals(dbGstNo?.Trim(), gstNo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Mismatch GST No from Master Record."
                    };
                }
            }
            else if (gstType.Equals("ShipTo", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT GSTIN FROM SUBGROUP_ADDRESS WHERE IS_DEFAULT = 1 AND COMP_CODE = @COMP_CODE AND CODE = @CODE";
                string dbGstNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                if (!string.Equals(dbGstNo?.Trim(), gstNo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult {IsValid = false, Message = "Mismatch GST No from Master Record."};
                }
            }

            return new ValidationResult {IsValid = true};
        }

        //==============================Save & Update======================
        public async Task<RepositoryResponse> SavePurchaseBillPassEntry(PurchaseWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            if (data == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid data!" };
            }

            var model = data.header;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            var (fAppStatus, fAppRemark) = await GetApprovalStatusAsync(tran, model);

                            var (priceType, gstHold) = await GetPurchaseCalculationBeforeSaveAsync(tran, data, model);

                            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con, tran);
                            //Purchase1
                            var docID = model.V_TYPE + model.V_NO;
                            AddPurchaseParameters(cmd, model, docID, priceType, gstHold, fAppStatus, fAppRemark);

                            //Delete From Purchase2
                            await DeleteRecordsAsync(con, tran, "PURCHASE2", model.V_TYPE, model.V_NO);

                            // PURCHASE2
                            DataTable dtPurchase2 = ConvertToPurchase2TVP(data.lineRows, docID);
                            SqlParameter tvpParam = cmd.Parameters.AddWithValue("@PURCHASE2_TYPE", dtPurchase2);
                            tvpParam.SqlDbType = SqlDbType.Structured;
                            tvpParam.TypeName = "dbo.PURCHASE2_TYPE";

                            //Delete Attachments
                            await DeleteRecordsAsync(con, tran, "IMG_TABLE", model.V_TYPE, model.V_NO);
                            //Attachments
                            await SaveAttachmentsAsync(con, tran, data.Attachement, "InsertAttachments", docID, model);
                            //EPR
                            await SaveAttachmentsAsync(con, tran, data.EPRAttachments, "InsertEPRAttachments", docID, model);

                            await cmd.ExecuteNonQueryAsync();
                            tran.Commit();

                            await PostPurchaseSaveAsync(con, model, fAppStatus);

                            return new RepositoryResponse { status = true, message = "Purchase saved successfully." };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return new RepositoryResponse { status = false, message = "SQL Error: " + ex.Message };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Error: " + ex.Message };
            }
        }

        private async Task<(string FAppStatus, string FAppRemark)> GetApprovalStatusAsync(SqlTransaction tran, PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();

            decimal crNoteAmt = (model.QTY_CR_AMT ?? 0) + (model.QTY_CR_TAX ?? 0) + (model.QC_CR_AMT ?? 0) + (model.QC_CR_TAX ?? 0) +
                                (model.RDF_CR_AMT ?? 0) + (model.RDF_CR_TAX ?? 0) + (model.QLT_CR_AMT ?? 0) + (model.QLT_CR_TAX ?? 0);

            bool isFinalApprovalBody = false;
            bool isFinalApprovalBodyCN = false;

            isFinalApprovalBody = "FINAL".Equals(
                await _dbHelper.ExecuteScalarAsynctran<string>(@"SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE=@USER_CODE AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@USER_CODE", g.PubUserId),
                        new("@DOC_CODE", model.V_TYPE),
                        new("@COMP_CODE", g.PubCompCode)
                    }, tran), StringComparison.OrdinalIgnoreCase);

            isFinalApprovalBodyCN = "FINAL".Equals(await _dbHelper.ExecuteScalarAsynctran<string>(
                    @"SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE FLAG_A='C' AND USER_CODE=@USER_CODE AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@USER_CODE", g.PubUserId),
                        new("@DOC_CODE", model.V_TYPE),
                        new("@COMP_CODE", g.PubCompCode)
                    }, tran), StringComparison.OrdinalIgnoreCase);

            string fAppStatus = "";
            string fAppRemark = "";

            if (crNoteAmt > 0 && isFinalApprovalBodyCN)
            {
                fAppStatus = "Approved";
                fAppRemark = "Document Approved.";
            }
            else if (crNoteAmt == 0 && isFinalApprovalBody)
            {
                fAppStatus = "Approved";
                fAppRemark = "Document Approved.";
            }

            return (fAppStatus, fAppRemark);
        }
        private async Task<(string PriceType, string GstHold)> GetPurchaseCalculationBeforeSaveAsync(SqlTransaction tran, PurchaseWrapper data,
            PURCHASE1 model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            //=======================
            //      Price Type
            //=======================
            string priceType = string.Empty;

            if (data.lineRows?.Any() == true)
            {
                var firstRow = data.lineRows.First();

                priceType = await _dbHelper.ExecuteScalarAsynctran<string>(
                    @"SELECT ISNULL(PRICE_TYPE,'') FROM ORDER1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                    AND BRANCH_CODE=@BRANCH_CODE",
                    new()
                    {
                        new("@V_TYPE", firstRow.PO_TYPE),
                        new("@V_NO", firstRow.PO_NO),
                        new("@COMP_CODE", globalVar.PubCompCode),
                        new("@BRANCH_CODE", globalVar.PubBranchCode)
                    },
                    tran);
            }

            //=======================
            //      GST Hold
            //=======================
            string gstHold = "No";

            if (model.INPUT_TYPE == "Input GST" || model.INPUT_TYPE == "GST Input" || model.INPUT_TYPE == "Local" || model.INPUT_TYPE == "Central" || model.INPUT_TYPE == "Import")
            {
                object result = await _dbHelper.ExecuteScalarAsynctran<object>(
                    @"SELECT TOP 1 1 FROM GSTHOLD_MAST WHERE PARTY_CODE=@PARTY_CODE AND COMP_CODE=@COMP_CODE",
                    new()
                    {
                        new("@PARTY_CODE", model.PARTY_CODE),
                        new("@COMP_CODE", globalVar.PubCompCode)
                    },
                    tran);

                bool gstHoldExists = result != null && Convert.ToInt32(result) == 1;

                if (!gstHoldExists)
                {
                    object releaseExistResult = await _dbHelper.ExecuteScalarAsynctran<object>(
                        @"SELECT TOP 1 1 FROM GSTHOLD_RELEASE WHERE REF_TYPE=@REF_TYPE AND REF_NO=@REF_NO AND COMP_CODE=@COMP_CODE
                        AND BRANCH_CODE=@BRANCH_CODE",
                        new()
                        {
                            new("@REF_TYPE", model.V_TYPE),
                            new("@REF_NO", model.V_NO),
                            new("@COMP_CODE", globalVar.PubCompCode),
                            new("@BRANCH_CODE", globalVar.PubBranchCode)
                        },
                        tran);
                    bool releaseExist = releaseExistResult != null && Convert.ToInt32(releaseExistResult) == 1;
                    gstHold = releaseExist ? "No" : "Yes";
                }
            }

            if ((model.CGST_AMT ?? 0) +
                (model.SGST_AMT ?? 0) +
                (model.IGST_AMT ?? 0) <= 0)
            {
                gstHold = "No";
            }

            return (priceType, gstHold);
        }

        private void AddPurchaseParameters(SqlCommand cmd, PURCHASE1 model, string docID, string priceType, string gstHold, string fAppStatus, string fAppRemark)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            cmd.CommandType = CommandType.StoredProcedure;

            // PURCHASE1
            cmd.Parameters.AddWithValue("@Action", "INSERTANDUPDATE");
            cmd.Parameters.AddWithValue("@SubAction", model.ACTION);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
            cmd.Parameters.AddWithValue("@DOC_ID", docID ?? "");

            cmd.Parameters.AddWithValue("@REF_TYPE", model.REF_TYPE ?? "");
            cmd.Parameters.AddWithValue("@REF_NO", model.REF_NO);

            //------------ Bill Details -----------
            cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
            cmd.Parameters.AddWithValue("@BILL_ADDRESSID", model.BILL_ADDRESSID);
            cmd.Parameters.AddWithValue("@BILL_ADD1", model.BILL_ADD1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADD2", model.BILL_ADD2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_ADD3", model.BILL_ADD3 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_GST", model.BILL_GST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_PINCODE", model.BILL_PINCODE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@DISP_ADDRESS", model.DISP_ADDRESS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DISP_CITY", model.DISP_CITY ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@CURRENCY", model.CURRENCY ?? (object)DBNull.Value);

            //------------ Ship Details -----------
            cmd.Parameters.AddWithValue("@SHIP_CODE", model.SHIP_CODE);
            cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", model.SHIP_ADDRESSID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD1", model.SHIP_ADD1 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD2", model.SHIP_ADD2 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_ADD3", model.SHIP_ADD3 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_GST", model.SHIP_GST ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SHIP_PINCODE", model.SHIP_PINCODE ?? (object)DBNull.Value);

            //------------ Document Details -----------
            cmd.Parameters.AddWithValue("@BILL_NO", model.BILL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BILL_DATE", model.BILL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@CHALL_NO", model.CHALL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CHALL_DATE", model.CHALL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@BL_NO", model.BL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BL_DT", model.BL_DT ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@WAYBILL_NO", model.WAYBILL_NO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_DATE", model.EWB_DATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_INVNO", model.EWB_INVNO ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EWB_EXPDATE", model.EWB_EXPDATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@DEBIT_AC", model.DEBIT_AC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CREDIT_AC", model.CREDIT_AC ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@INPUT_TYPE", model.INPUT_TYPE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@STATUS", model.STATUS);
            cmd.Parameters.AddWithValue("@EXCH_RATE", model.EXCH_RATE ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);

            //------------ Item Total -----------

            cmd.Parameters.AddWithValue("@RECD_QTY", model.RECD_QTY);
            cmd.Parameters.AddWithValue("@BILL_QTY", model.BILL_QTY);
            cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT);
            cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT);
            cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT);
            cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT);
            cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT);
            cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT);
            cmd.Parameters.AddWithValue("@CESS_AMT", model.CESS_AMT);
            cmd.Parameters.AddWithValue("@VAT_AMT", model.VAT_AMT);
            cmd.Parameters.AddWithValue("@OTH_AMT", model.OTH_AMT);
            cmd.Parameters.AddWithValue("@TCS_PER", model.TCS_PER);
            cmd.Parameters.AddWithValue("@TCS_AMT", model.TCS_AMT);
            cmd.Parameters.AddWithValue("@ROUND_OFF", model.ROUND_OFF);

            cmd.Parameters.AddWithValue("@TDS_ACT", model.TDS_ACT);
            cmd.Parameters.AddWithValue("@TDS_PER", model.TDS_PER);
            cmd.Parameters.AddWithValue("@TDS_AMT", model.TDS_AMT);

            cmd.Parameters.AddWithValue("@TDS_PER194Q", model.TDS_PER194Q);
            cmd.Parameters.AddWithValue("@TDS_AMT194Q", model.TDS_AMT194Q);

            cmd.Parameters.AddWithValue("@BANK_RATE", model.BANK_RATE);
            cmd.Parameters.AddWithValue("@BANK_AMT", model.BANK_AMT);
            cmd.Parameters.AddWithValue("@DIFF_AMT", model.DIFF_AMT);

            cmd.Parameters.AddWithValue("@PL_NO", model.PL_NO);
            cmd.Parameters.AddWithValue("@PL_DATE", model.PL_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@BILLAMT_USD", model.BILLAMT_USD ?? (object)DBNull.Value);

            //------------ Logistic Details -----------

            cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE);
            cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TRANSPORT_NAME);

            cmd.Parameters.AddWithValue("@TRUCK_NO", model.TRUCK_NO);
            cmd.Parameters.AddWithValue("@CONTAINER_NO", model.CONTAINER_NO);

            cmd.Parameters.AddWithValue("@GR_NO", model.GR_NO);
            cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@SEALED_VEHICLE", model.SEALED_VEHICLE);

            // Freight

            cmd.Parameters.AddWithValue("@FRTPAY_AMT", model.FRTPAY_AMT);
            cmd.Parameters.AddWithValue("@FRTPAY_TAXPER", model.FRTPAY_TAXPER);
            cmd.Parameters.AddWithValue("@FRTPAY_TAX", model.FRTPAY_TAX);
            cmd.Parameters.AddWithValue("@FRTPAY_DRAC", model.FRTPAY_DRAC);
            cmd.Parameters.AddWithValue("@FRTPAY_CRAC", model.FRTPAY_CRAC);
            cmd.Parameters.AddWithValue("@FRTPAY_NAR", model.FRTPAY_NAR);

            cmd.Parameters.AddWithValue("@FRT_TDSPER", model.FRT_TDSPER);
            cmd.Parameters.AddWithValue("@FRT_TDS", model.FRT_TDS);

            // Transport GST

            cmd.Parameters.AddWithValue("@TRP_GSTNO", model.TRP_GSTNO);
            cmd.Parameters.AddWithValue("@TRP_TAXTYPE", model.TRP_TAXTYPE);
            cmd.Parameters.AddWithValue("@TRP_BILLNO", model.TRP_BILLNO);
            cmd.Parameters.AddWithValue("@TRP_BILLDATE", model.TRP_BILLDATE ?? (object)DBNull.Value);

            // Weigh Bridge

            cmd.Parameters.AddWithValue("@WB_AMT", model.WB_AMT);
            cmd.Parameters.AddWithValue("@WB_TDSPER", model.WB_TDSPER);
            cmd.Parameters.AddWithValue("@WB_TDS", model.WB_TDS);
            cmd.Parameters.AddWithValue("@WB_DRACT", model.WB_DRACT);
            cmd.Parameters.AddWithValue("@WB_CRACT", model.WB_CRACT);
            cmd.Parameters.AddWithValue("@WB_NARR", model.WB_NARR);

            // Unloading

            cmd.Parameters.AddWithValue("@UL_AMT", model.UL_AMT);
            cmd.Parameters.AddWithValue("@UL_TDSPER", model.UL_TDSPER);
            cmd.Parameters.AddWithValue("@UL_TDS", model.UL_TDS);
            cmd.Parameters.AddWithValue("@UL_DRACT", model.UL_DRACT);
            cmd.Parameters.AddWithValue("@UL_CRACT", model.UL_CRACT);
            cmd.Parameters.AddWithValue("@UL_NARR", model.UL_NARR);

            //------------ CR/DR Note Details -----------

            cmd.Parameters.AddWithValue("@DR_FROM_TPT", model.DR_FROM_TPT);

            cmd.Parameters.AddWithValue("@QLT_DR_AMT", model.QLT_DR_AMT);
            cmd.Parameters.AddWithValue("@QLT_DR_TAX", model.QLT_DR_TAX);
            cmd.Parameters.AddWithValue("@QLT_DR_NAR", model.QLT_DR_NAR);

            cmd.Parameters.AddWithValue("@QLT_CR_AMT", model.QLT_CR_AMT);
            cmd.Parameters.AddWithValue("@QLT_CR_TAX", model.QLT_CR_TAX);
            cmd.Parameters.AddWithValue("@QLT_CR_NAR", model.QLT_CR_NAR);

            cmd.Parameters.AddWithValue("@RDF_DR_AMT", model.RDF_DR_AMT);
            cmd.Parameters.AddWithValue("@RDF_DR_TAX", model.RDF_DR_TAX);
            cmd.Parameters.AddWithValue("@RDF_DR_NAR", model.RDF_DR_NAR);

            cmd.Parameters.AddWithValue("@RDF_CR_AMT", model.RDF_CR_AMT);
            cmd.Parameters.AddWithValue("@RDF_CR_TAX", model.RDF_CR_TAX);
            cmd.Parameters.AddWithValue("@RDF_CR_NAR", model.RDF_CR_NAR);

            cmd.Parameters.AddWithValue("@QTY_DR_AMT", model.QTY_DR_AMT);
            cmd.Parameters.AddWithValue("@QTY_DR_TAX", model.QTY_DR_TAX);
            cmd.Parameters.AddWithValue("@QTY_DR_NAR", model.QTY_DR_NAR);

            cmd.Parameters.AddWithValue("@QTY_CR_AMT", model.QTY_CR_AMT);
            cmd.Parameters.AddWithValue("@QTY_CR_TAX", model.QTY_CR_TAX);
            cmd.Parameters.AddWithValue("@QTY_CR_NAR", model.QTY_CR_NAR);

            cmd.Parameters.AddWithValue("@QC_DR_AMT", model.QC_DR_AMT);
            cmd.Parameters.AddWithValue("@QC_DR_TAX", model.QC_DR_TAX);
            cmd.Parameters.AddWithValue("@QC_DR_NAR", model.QC_DR_NAR);

            cmd.Parameters.AddWithValue("@QC_CR_AMT", model.QC_CR_AMT);
            cmd.Parameters.AddWithValue("@QC_CR_TAX", model.QC_CR_TAX);
            cmd.Parameters.AddWithValue("@QC_CR_NAR", model.QC_CR_NAR);

            cmd.Parameters.AddWithValue("@OTH_DR_AMT", model.OTH_DR_AMT);
            cmd.Parameters.AddWithValue("@OTH_DR_TAX", model.OTH_DR_TAX);
            cmd.Parameters.AddWithValue("@OTH_DR_NAR", model.OTH_DR_NAR);

            cmd.Parameters.AddWithValue("@HOLD_PAY", model.HOLD_PAY);
            cmd.Parameters.AddWithValue("@HOLD_REASON", model.HOLD_REASON);
            cmd.Parameters.AddWithValue("@HOLD_DATE", model.HOLD_DATE ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FAPROV_STATUS", fAppStatus);
            cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fAppRemark);

            cmd.Parameters.AddWithValue("@PRICE_TYPE", priceType);
            cmd.Parameters.AddWithValue("@TAX_HOLD", gstHold);

            // Audit Fields
            if (model.ACTION == "INSERT")
            {
                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "A");
            }
            else
            {
                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "E");
            }
            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
        }

        private async Task DeleteRecordsAsync(SqlConnection con, SqlTransaction tran, string tableName, string vType, int? vNo)
        {
            var g = _globalVariableService.GetGlobalVariables();

            string sql = $@"DELETE FROM {tableName} WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                        AND V_TYPE=@V_TYPE AND V_NO=@V_NO";

            await ExecuteQueryAsync(con, sql, tran, new()
            {
                new("@YEAR_CODE", g.PubFYearCode),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@V_TYPE", vType),
                new("@V_NO", vNo)
            });
        }

        private async Task SaveAttachmentsAsync(SqlConnection con, SqlTransaction tran, IEnumerable<PurchaseBillAttachments> attachments,
        string action, string docId, PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();
            int rowId = 1;

            foreach (var attachment in attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FILE_NAME))
                    continue;

                byte[] fileBytes = Convert.FromBase64String(attachment.FILE_DATA);

                using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con, tran)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@DOC_ID", docId);
                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                cmd.Parameters.AddWithValue("@ROWID", rowId);

                cmd.Parameters.AddWithValue("@FILE_NAME", attachment.FILE_NAME);
                cmd.Parameters.AddWithValue("@FILE_Path", attachment.FILE_NAME);
                cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary).Value = fileBytes;

                cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@AED", "A");
                cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                await cmd.ExecuteNonQueryAsync();

                rowId++;
            }
        }

        public DataTable ConvertToPurchase2TVP(List<PURCHASE2> list, string docID)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new DataTable("PURCHASE2_TYPE");

            // Add columns exactly as in PURCHASE2_TYPE (order and types must match)
            dt.Columns.Add("SNO", typeof(int));
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("ITEM_NAME", typeof(string));
            dt.Columns.Add("MAKE_CODE", typeof(int));
            dt.Columns.Add("HSN_CODE", typeof(string));
            dt.Columns.Add("RCM_YN", typeof(string));
            dt.Columns.Add("INPUT_YN", typeof(string));
            dt.Columns.Add("UOM_CODE", typeof(int));
            dt.Columns.Add("UOM_NAME", typeof(string));
            dt.Columns.Add("DEPT_CODE", typeof(int));
            dt.Columns.Add("NOS", typeof(int));
            dt.Columns.Add("PLUS_MINUSQTY", typeof(decimal));
            dt.Columns.Add("WB_QTY", typeof(decimal));
            dt.Columns.Add("RECD_QTY", typeof(decimal));
            dt.Columns.Add("BILL_QTY", typeof(decimal));
            dt.Columns.Add("USD_RATE", typeof(decimal));
            dt.Columns.Add("EXCH_RATE", typeof(decimal));
            dt.Columns.Add("RATE", typeof(decimal));
            dt.Columns.Add("AMOUNT", typeof(decimal));
            dt.Columns.Add("DISC_PER", typeof(decimal));
            dt.Columns.Add("DISC_AMT", typeof(decimal));
            dt.Columns.Add("PACK_PER", typeof(decimal));
            dt.Columns.Add("PACK_AMT", typeof(decimal));
            dt.Columns.Add("TAX_CODE", typeof(int));
            dt.Columns.Add("CGST_PER", typeof(decimal));
            dt.Columns.Add("CGST_AMT", typeof(decimal));
            dt.Columns.Add("SGST_PER", typeof(decimal));
            dt.Columns.Add("SGST_AMT", typeof(decimal));
            dt.Columns.Add("IGST_PER", typeof(decimal));
            dt.Columns.Add("IGST_AMT", typeof(decimal));
            dt.Columns.Add("CESS_PER", typeof(decimal));
            dt.Columns.Add("CESS_AMT", typeof(decimal));
            dt.Columns.Add("VAT_PER", typeof(decimal));
            dt.Columns.Add("VAT_AMT", typeof(decimal));
            dt.Columns.Add("OTH_AMT", typeof(decimal));
            dt.Columns.Add("NET_AMT", typeof(decimal));
            dt.Columns.Add("LAND_RATE", typeof(decimal));
            dt.Columns.Add("LAND_AMT", typeof(decimal));
            dt.Columns.Add("POLAND_RATE", typeof(decimal));
            dt.Columns.Add("PO_RATE", typeof(decimal));
            dt.Columns.Add("BIN_LOCATION", typeof(string));
            dt.Columns.Add("BIN_CODE", typeof(int));
            dt.Columns.Add("PO_TYPE", typeof(string));
            dt.Columns.Add("PO_NO", typeof(int));
            dt.Columns.Add("SAUDA_TYPE", typeof(string));
            dt.Columns.Add("SAUDA_NO", typeof(int));
            dt.Columns.Add("KANTA_TYPE", typeof(string));
            dt.Columns.Add("KANTA_NO", typeof(int));
            dt.Columns.Add("REQ_TYPE", typeof(string));
            dt.Columns.Add("REQ_NO", typeof(int));
            dt.Columns.Add("GATE_TYPE", typeof(string));
            dt.Columns.Add("GATE_NO", typeof(int));
            dt.Columns.Add("REF_TYPE", typeof(string));
            dt.Columns.Add("REF_NO", typeof(int));
            dt.Columns.Add("QC_TYPE", typeof(string));
            dt.Columns.Add("QC_NO", typeof(int));
            dt.Columns.Add("PASS_TYPE", typeof(string));
            dt.Columns.Add("PASS_NO", typeof(int));
            dt.Columns.Add("EMPTY_YN", typeof(string));
            dt.Columns.Add("MACH_CODE", typeof(int));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
            dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
            dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
            dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
            dt.Columns.Add("FINAL_LOCK", typeof(string));
            dt.Columns.Add("DRNOTE_AMT", typeof(decimal));
            dt.Columns.Add("CRNOTE_AMT", typeof(decimal));
            dt.Columns.Add("QLTDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("RDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("QCDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("QTYDIFF_DRAMT", typeof(decimal));
            dt.Columns.Add("OTH_DRAMT", typeof(decimal));

            int sno = 1;

            foreach (var item in list)
            {
                dt.Rows.Add(
                    sno++,
                    item.ITEM_CODE ?? (object)DBNull.Value,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.MAKE_CODE ?? (object)DBNull.Value,
                    item.HSN_CODE ?? (object)DBNull.Value,
                    item.RCM_YN ?? (object)DBNull.Value,
                    item.INPUT_YN ?? (object)DBNull.Value,
                    item.UOM_CODE ?? (object)DBNull.Value,
                    item.UOM_NAME ?? (object)DBNull.Value,
                    item.DEPT_CODE ?? (object)DBNull.Value,
                    item.NOS ?? (object)DBNull.Value,
                    item.PLUS_MINUSQTY ?? (object)DBNull.Value,
                    item.WB_QTY ?? (object)DBNull.Value,
                    item.RECD_QTY ?? (object)DBNull.Value,
                    item.BILL_QTY ?? (object)DBNull.Value,
                    item.USD_RATE ?? (object)DBNull.Value,
                    item.EXCH_RATE ?? (object)DBNull.Value,
                    item.RATE ?? (object)DBNull.Value,
                    item.AMOUNT ?? (object)DBNull.Value,
                    item.DISC_PER ?? (object)DBNull.Value,
                    item.DISC_AMT ?? (object)DBNull.Value,
                    item.PACK_PER ?? (object)DBNull.Value,
                    item.PACK_AMT ?? (object)DBNull.Value,
                    item.TAX_CODE ?? (object)DBNull.Value,
                    item.CGST_PER ?? (object)DBNull.Value,
                    item.CGST_AMT ?? (object)DBNull.Value,
                    item.SGST_PER ?? (object)DBNull.Value,
                    item.SGST_AMT ?? (object)DBNull.Value,
                    item.IGST_PER ?? (object)DBNull.Value,
                    item.IGST_AMT ?? (object)DBNull.Value,
                    item.CESS_PER ?? (object)DBNull.Value,
                    item.CESS_AMT ?? (object)DBNull.Value,
                    item.VAT_PER ?? (object)DBNull.Value,
                    item.VAT_AMT ?? (object)DBNull.Value,
                    item.OTH_AMT ?? (object)DBNull.Value,
                    item.NET_AMT ?? (object)DBNull.Value,
                    item.LAND_RATE ?? (object)DBNull.Value,
                    item.LAND_AMT ?? (object)DBNull.Value,
                    item.POLAND_RATE ?? (object)DBNull.Value,
                    item.PO_RATE ?? (object)DBNull.Value,
                    item.BIN_LOCATION ?? (object)DBNull.Value,
                    item.BIN_CODE ?? (object)DBNull.Value,
                    item.PO_TYPE ?? (object)DBNull.Value,
                    item.PO_NO ?? (object)DBNull.Value,
                    item.SAUDA_TYPE ?? (object)DBNull.Value,
                    item.SAUDA_NO ?? (object)DBNull.Value,
                    item.KANTA_TYPE ?? (object)DBNull.Value,
                    item.KANTA_NO ?? (object)DBNull.Value,
                    item.REQ_TYPE ?? (object)DBNull.Value,
                    item.REQ_NO ?? (object)DBNull.Value,
                    item.GATE_TYPE ?? (object)DBNull.Value,
                    item.GATE_NO ?? (object)DBNull.Value,
                    item.REF_TYPE ?? (object)DBNull.Value,
                    item.REF_NO ?? (object)DBNull.Value,
                    item.QC_TYPE ?? (object)DBNull.Value,
                    item.QC_NO ?? (object)DBNull.Value,
                    item.PASS_TYPE ?? (object)DBNull.Value,
                    item.PASS_NO ?? (object)DBNull.Value,
                    item.EMPTY_YN ?? (object)DBNull.Value,
                    item.MACH_CODE ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.RATE_MONTHLY ?? (object)DBNull.Value,
                    item.RATE_QUARTERLY ?? (object)DBNull.Value,
                    item.RATE_ANNUALY ?? (object)DBNull.Value,
                    item.RATE_SPECIAL ?? (object)DBNull.Value,
                    item.FINAL_LOCK ?? (object)DBNull.Value,
                    item.DRNOTE_AMT ?? (object)DBNull.Value,
                    item.CRNOTE_AMT ?? (object)DBNull.Value,
                    item.QLTDIFF_DRAMT ?? (object)DBNull.Value,
                    item.RDIFF_DRAMT ?? (object)DBNull.Value,
                    item.QCDIFF_DRAMT ?? (object)DBNull.Value,
                    item.QTYDIFF_DRAMT ?? (object)DBNull.Value,
                    item.OTH_DRAMT ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        private async Task PostPurchaseSaveAsync(SqlConnection con, PURCHASE1 model, string fAppStatus)
        {
            int? vno = model.V_NO;
            string? vType = model.V_TYPE;
            await DeleteOldPLLedgerAsync(con, vType, oldBankAmt, oldplno);

            await DeleteLedgerAsync(con, vType, vno);

            await ProcessApprovalAsync(con, fAppStatus, vType, vno, model.V_DATE);

            await _globalFunction.StockValuationAsync(con, vType, vno);

            await UpdateLandAmountAsync(con, vType, vno);
        }

        private async Task DeleteOldPLLedgerAsync(SqlConnection con, string vType, decimal? oldBankAmt, int? oldPlNo)
        {
            if (vType != "RIMP" || oldBankAmt == 0 || oldPlNo <= 0)
                return;

            var g = _globalVariableService.GetGlobalVariables();

            const string sql = @"DELETE FROM LEDGER2 WHERE V_TYPE='RIPL' AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE 
                                AND YEAR_CODE=@YEAR_CODE";

            await ExecuteQueryAsync(con, query: sql, parameters: new()
            {
                new("@V_NO", oldPlNo),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@YEAR_CODE", g.PubFYearCode)
            });
        }

        private static readonly Dictionary<string, (string HoldType, string BillHoldType)> VoucherMap = new()
        {
            ["STPB"] = ("HTPB", "STPH"),
            ["BFPB"] = ("HFPB", "BFPH"),
            ["RIMP"] = ("HIMP", "RIMH"),
            ["RMPB"] = ("HMPB", "RMPH"),
            ["SRPB"] = ("HRPB", "SRPH"),
            ["STJW"] = ("HTJW", "STJH"),
            ["JBPB"] = ("HBPB", "JBPH"),
            ["STDP"] = ("HTDP", "STDH"),
            ["RMDP"] = ("HMDP", "RMDH"),
            ["SIDP"] = ("HIDP", "SIDH")
        };

        private async Task DeleteLedgerAsync(SqlConnection con, string vType, int? vNo)
        {
            var g = _globalVariableService.GetGlobalVariables();

            VoucherMap.TryGetValue(vType, out var types);

            string sql = @"DELETE FROM LEDGER2 WHERE V_TYPE IN (@V_TYPE,@HOLD,@BILL) AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                            AND YEAR_CODE=@YEAR_CODE;

                           DELETE FROM LEDGER_OS WHERE V_TYPE IN (@V_TYPE,@HOLD,@BILL) AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                            AND YEAR_CODE=@YEAR_CODE;";

            await ExecuteQueryAsync(con, query: sql, parameters: new()
            {
                new("@V_TYPE", vType),
                new("@HOLD", types.HoldType ?? string.Empty),
                new("@BILL", types.BillHoldType ?? string.Empty),
                new("@V_NO", vNo),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@YEAR_CODE", g.PubFYearCode)
            });
        }

        private async Task ProcessApprovalAsync(SqlConnection con, string approvalStatus, string vType, int? vNo, DateTime? vDate)
        {
            if (!string.Equals(approvalStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                return;

            var g = _globalVariableService.GetGlobalVariables();

            // Existing Ledger Posting Method
            //await _accountPostingService.ACTPostingPurchase("LEDGER2", vDate, vDate, vType, vNo); //Implement Later

            await ExecuteQueryAsync(con, query: "sp_PurchaseBillPassEntryDirect", parameters: new()
            {
                new("@Action", "UpdateApprovalOnSave"),
                new("@USER_CODE", g.PubUserId),
                new("@V_TYPE", vType),
                new("@V_NO", vNo),
                new("@COMP_CODE", g.PubCompCode),
                new("@BRANCH_CODE", g.PubBranchCode),
                new("@YEAR_CODE", g.PubFYearCode)
            }, isProcOrQry: true);

            //await loadPendingApprovals(); //Implement Later
        }

        private async Task UpdateLandAmountAsync(SqlConnection con, string vType, int? vNo)
        {
            var g = _globalVariableService.GetGlobalVariables();
            using var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddRange(new[]
            {
                new SqlParameter("@Action", "GetDataToUpdateLandAMtOnSave"),
                new SqlParameter("@comp_code", g.PubCompCode),
                new SqlParameter("@branch_code", g.PubBranchCode),
                new SqlParameter("@year_code", g.PubFYearCode),
                new SqlParameter("@v_type", vType),
                new SqlParameter("@v_no", vNo)
            });

            var rows = new List<LandAmountRow>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new LandAmountRow
                {
                    CompCode = Convert.ToInt32(reader["COMP_CODE"]),
                    BranchCode = Convert.ToInt32(reader["BRANCH_CODE"]),
                    YearCode = Convert.ToInt32(reader["YEAR_CODE"]),
                    ItemCode = Convert.ToInt32(reader["ITEM_CODE"]),
                    Sno = Convert.ToInt32(reader["SNO"]),
                    PoNo = Convert.ToInt32(reader["PO_NO"]),
                    MrnNo = Convert.ToInt32(reader["MRN_NO"]),
                    MrnType = reader["MRN_TYPE"].ToString()!,
                    VType = reader["V_TYPE"].ToString()!,
                    VNo = Convert.ToInt32(reader["V_NO"]),
                    LandAmt = CalculateLandAmount(reader)
                });
            }
            await reader.CloseAsync();
            foreach (var row in rows)
            {
                await UpdatePurchaseLandAmountAsync(con, row);
            }
        }

        private decimal CalculateLandAmount(SqlDataReader dr)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            decimal itemAmt = Convert.ToDecimal(dr["ITEM_AMT"] == DBNull.Value ? 0m : dr["ITEM_AMT"]);
            decimal billAmt = Convert.ToDecimal(dr["BillBasicAmt"] == DBNull.Value ? 0m : dr["BillBasicAmt"]);

            if (billAmt == 0)
                return 0;

            decimal gstAmt = Convert.ToDecimal(dr["GST_AMT"]);
            decimal frtAmt = 0;
            decimal debitAmt = 0;
            decimal debitGst = 0;
            decimal creditAmt = 0;
            decimal creditGst = 0;
            decimal importAmt = 0;
            decimal importGst = 0;
            decimal plAmt = 0;
            decimal netAmt;

            decimal itemBasicAmt = Convert.ToDecimal(dr["ItemBasicAmt"] == DBNull.Value ? 0m : dr["ItemBasicAmt"]);

            // P/L Amount
            if (Convert.ToDecimal(dr["NAMOUNT"] == DBNull.Value ? 0m : dr["NAMOUNT"]) > 0 &&
                Convert.ToDecimal(dr["BANK_AMT"] == DBNull.Value ? 0m : dr["BANK_AMT"]) > 0)
            {
                plAmt = Convert.ToDecimal(dr["BANK_AMT"] == DBNull.Value ? 0m : dr["BANK_AMT"]) -
                    Convert.ToDecimal(dr["NAMOUNT"] == DBNull.Value ? 0m : dr["NAMOUNT"]);
            }
            else if (Convert.ToDecimal(dr["PL_AMT"] == DBNull.Value ? 0m : dr["PL_AMT"]) != 0)
            {
                plAmt = Convert.ToDecimal(dr["PL_AMT"] == DBNull.Value ? 0m : dr["PL_AMT"]) != 0
                    ? -Convert.ToDecimal(dr["PL_AMT"] == DBNull.Value ? 0m : dr["PL_AMT"])
                    : Math.Abs(Convert.ToDecimal(dr["PL_AMT"] == DBNull.Value ? 0m : dr["PL_AMT"]));
            }

            if (itemBasicAmt != 0)
            {
                if (Convert.ToDecimal(dr["FrtAmt"] == DBNull.Value ? 0m : dr["FrtAmt"]) != 0)
                {
                    frtAmt = Convert.ToDecimal(dr["FrtAmt"] == DBNull.Value ? 0m : dr["FrtAmt"]) * itemBasicAmt / billAmt;
                }

                if (Convert.ToInt32(gv.PubFYearCode) >= 9)
                {
                    //debitAmt = Convert.ToDecimal(dr["ItemRowDrAmt"] == DBNull.Value ? 0m : dr["ItemRowDrAmt"]); // ItemRowDrAmt is missing uncomment after discussion
                    debitGst = 0;
                    //creditAmt = Convert.ToDecimal(dr["ItemRowCrAmt"] == DBNull.Value ? 0m : dr["ItemRowCrAmt"]); // ItemRowCrAmt is missing uncomment after discussion
                    creditGst = 0;
                }
                else
                {
                    decimal dbAmt = Convert.ToDecimal(dr["DebitAmt"] == DBNull.Value ? 0m : dr["DebitAmt"]);
                    decimal dbTax = Convert.ToDecimal(dr["DebitTax"] == DBNull.Value ? 0m : dr["DebitTax"]);
                    decimal crAmt = Convert.ToDecimal(dr["CreditAmt"] == DBNull.Value ? 0m : dr["CreditAmt"]);
                    decimal crTax = Convert.ToDecimal(dr["CreditTax"] == DBNull.Value ? 0m : dr["CreditTax"]);

                    if (dbAmt != 0)
                        debitAmt = dbAmt * itemBasicAmt / billAmt;
                    if (dbTax != 0)
                        debitGst = dbTax * itemBasicAmt / billAmt;
                    if (crAmt != 0)
                        creditAmt = crAmt * itemBasicAmt / billAmt;
                    if (crTax != 0)
                        creditGst = crTax * itemBasicAmt / billAmt;
                }

                decimal impAmt = Convert.ToDecimal(dr["IMPORT_AMT"] == DBNull.Value ? 0m : dr["IMPORT_AMT"]);
                decimal impTax = Convert.ToDecimal(dr["IMPORT_TAX"] == DBNull.Value ? 0m : dr["IMPORT_TAX"]);

                if (impAmt != 0)
                    importAmt = impAmt * itemBasicAmt / billAmt;
                if (impTax != 0)
                    importGst = impTax * itemBasicAmt / billAmt;
                if (plAmt != 0)
                    plAmt = plAmt * itemBasicAmt / billAmt;
            }

            string inputType = dr["INPUT_TYPE"] as string ?? string.Empty;

            bool excludeGST =
                inputType == "Input Vat" ||
                inputType == "GST Input" ||
                inputType == "Import" ||
                inputType == "Local" ||
                inputType == "Central" ||
                inputType == "Input Capital";

            netAmt = excludeGST
                ? itemAmt + frtAmt - debitAmt + creditAmt + importAmt + plAmt
                : itemAmt + frtAmt - debitAmt + creditAmt + importAmt + plAmt + gstAmt + creditGst - debitGst + importGst;

            return Math.Round(netAmt, 2);
        }

        private async Task UpdatePurchaseLandAmountAsync(SqlConnection con, LandAmountRow row)
        {
            await ExecuteQueryAsync(con, query: "sp_PurchaseBillPassEntryDirect", parameters: new()
            {
                new("@Action", "UpdateP2LAmtOnSave"),
                new("@LAND_AMT", row.LandAmt),
                new("@COMP_CODE", row.CompCode),
                new("@BRANCH_CODE", row.BranchCode),
                new("@YEAR_CODE", row.YearCode),
                new("@V_TYPE", row.VType),
                new("@V_NO", row.VNo),
                new("@SNO", row.Sno),
                new("@ITEM_CODE", row.ItemCode)
            }, isProcOrQry: true);

            // If V_TYPE <> "RRET" Then update corresponding MRN also
            if (!row.VType.Equals("RRET", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteQueryAsync(con, query: "sp_PurchaseBillPassEntryDirect", parameters: new()
                {
                    new("@Action", "UpdateP2LAmtWithMRNOnSave"),
                    new("@LAND_AMT", row.LandAmt),
                    new("@COMP_CODE", row.CompCode),
                    new("@BRANCH_CODE", row.BranchCode),
                    new("@YEAR_CODE", row.YearCode),
                    new("@REF_TYPE", row.MrnType),
                    new("@REF_NO", row.MrnNo),
                    new("@PO_NO", row.PoNo),
                    new("@ITEM_CODE", row.ItemCode)
                }, isProcOrQry: true);
            }
        }

        private async Task ExecuteQueryAsync(SqlConnection con, string query, SqlTransaction? tran = null, List<SqlParameter>? parameters = null, bool isProcOrQry = false)
        {
            using var cmd = new SqlCommand(query, con, tran);
            cmd.CommandType = isProcOrQry ? CommandType.StoredProcedure : CommandType.Text;
            if (parameters?.Any() == true)
                cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }

        //============================Get By Id====================
        public async Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            PURCHASE1 header = null;
            List<PURCHASE2> items = new();
            List<PurchaseBillAttachments> attachments = new();
            List<PurchaseBillAttachments> eprAttachments = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();

                // Header (PURCHASE1)
                if (rdr.Read())
                {
                    header = new PURCHASE1
                    {
                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                        V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : null,
                        V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : null,
                        PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : null,
                        EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : null,
                        PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : null,
                        //PARTY_NAME = rdr["PARTY_NAME"]?.ToString(),
                        EXCH_RATE = rdr["EXCH_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXCH_RATE"]) : null,
                        CREDIT_AC = rdr["CREDIT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["CREDIT_AC"]) : null,
                        DEBIT_AC = rdr["DEBIT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["DEBIT_AC"]) : null,
                        BILL_ADD1 = rdr["BILL_ADD1"]?.ToString(),
                        BILL_ADD2 = rdr["BILL_ADD2"]?.ToString(),
                        BILL_ADD3 = rdr["BILL_ADD3"]?.ToString(),
                        BILL_CITY = rdr["BILL_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_CITY"]) : null,
                        BILL_STATE = rdr["BILL_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_STATE"]) : null,
                        BILL_PINCODE = rdr["BILL_PINCODE"]?.ToString(),
                        BILL_ADDRESSID = rdr["BILL_ADDRESSID"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_ADDRESSID"]) : null,
                        BILL_GST = rdr["BILL_GST"]?.ToString(),
                        SHIP_CODE = rdr["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CODE"]) : null,
                        SHIP_ADD1 = rdr["SHIP_ADD1"]?.ToString(),
                        SHIP_ADD2 = rdr["SHIP_ADD2"]?.ToString(),
                        SHIP_ADD3 = rdr["SHIP_ADD3"]?.ToString(),
                        SHIP_CITY = rdr["SHIP_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CITY"]) : null,
                        SHIP_STATE = rdr["SHIP_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_STATE"]) : null,
                        SHIP_PINCODE = rdr["SHIP_PINCODE"]?.ToString(),
                        SHIP_ADDRESSID = rdr["SHIP_ADDRESSID"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_ADDRESSID"]) : null,
                        SHIP_GST = rdr["SHIP_GST"]?.ToString(),
                        BILL_NO = rdr["BILL_NO"]?.ToString(),
                        BILL_DATE = rdr["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["BILL_DATE"]) : null,
                        CHALL_NO = rdr["CHALL_NO"]?.ToString(),
                        CHALL_DATE = rdr["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["CHALL_DATE"]) : null,
                        UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : null,
                        GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                        GATE_NO = rdr["GATE_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GATE_NO"]) : null,
                        REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                        REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : null,
                        PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                        PASS_NO = rdr["PASS_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PASS_NO"]) : null,
                        TRANSIT_NO = rdr["TRANSIT_NO"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSIT_NO"]) : null,
                        WAYBILL_NO = rdr["WAYBILL_NO"]?.ToString(),
                        TRANSPORT_CODE = rdr["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_CODE"]) : null,
                        TRANSPORT_NAME = rdr["TRANSPORT_NAME"]?.ToString(),
                        TRANSPORT_AC = rdr["TRANSPORT_AC"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_AC"]) : null,
                        GR_NO = rdr["GR_NO"]?.ToString(),
                        GR_DATE = rdr["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["GR_DATE"]) : null,
                        TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                        CONTAINER_NO = rdr["CONTAINER_NO"]?.ToString(),
                        SEALED_VEHICLE = rdr["SEALED_VEHICLE"] != DBNull.Value ? Convert.ToInt32(rdr["SEALED_VEHICLE"]) : null,
                        INPUT_TYPE = rdr["INPUT_TYPE"]?.ToString(),
                        EXPS_TYPE = rdr["EXPS_TYPE"]?.ToString(),
                        REMARKS = rdr["REMARKS"]?.ToString(),
                        STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : null,
                        RECD_QTY = rdr["RECD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RECD_QTY"]) : null,
                        BILL_QTY = rdr["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_QTY"]) : null,
                        AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : null,
                        DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : null,
                        DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : null,
                        PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : null,
                        PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : null,
                        CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : null,
                        CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : null,
                        SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : null,
                        SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : null,
                        IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : null,
                        IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : null,
                        CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : null,
                        CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : null,
                        VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : null,
                        VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : null,
                        OTH_AMT = rdr["OTH_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_AMT"]) : null,
                        TCS_PER = rdr["TCS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_PER"]) : null,
                        TCS_AMT = rdr["TCS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_AMT"]) : null,
                        ROUND_OFF = rdr["ROUND_OFF"] != DBNull.Value ? Convert.ToDecimal(rdr["ROUND_OFF"]) : null,
                        NAMOUNT = rdr["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["NAMOUNT"]) : null,
                        DIFF_AMT = rdr["DIFF_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DIFF_AMT"]) : null,
                        BANK_AMT = rdr["BANK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["BANK_AMT"]) : null,
                        BANK_RATE = rdr["BANK_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["BANK_RATE"]) : null,
                        PL_NO = rdr["PL_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PL_NO"]) : null,
                        PL_DATE = rdr["PL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["PL_DATE"]) : null,
                        BILLAMT_USD = rdr["BILLAMT_USD"] != DBNull.Value ? Convert.ToDecimal(rdr["BILLAMT_USD"]) : null,
                        FRTPAY_AMT = rdr["FRTPAY_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_AMT"]) : null,
                        FRTPAY_TAXPER = rdr["FRTPAY_TAXPER"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_TAXPER"]) : null,
                        FRTPAY_TAX = rdr["FRTPAY_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["FRTPAY_TAX"]) : null,
                        FRTPAY_NAR = rdr["FRTPAY_NAR"]?.ToString(),
                        FRTPAY_DRAC = rdr["FRTPAY_DRAC"] != DBNull.Value ? Convert.ToInt32(rdr["FRTPAY_DRAC"]) : null,
                        FRTPAY_CRAC = rdr["FRTPAY_CRAC"] != DBNull.Value ? Convert.ToInt32(rdr["FRTPAY_CRAC"]) : null,
                        FRT_TDSPER = rdr["FRT_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_TDSPER"]) : null,
                        FRT_TDS = rdr["FRT_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_TDS"]) : null,
                        DR_FROM_TPT = rdr["DR_FROM_TPT"]?.ToString(),
                        TDS_ACT = rdr["TDS_ACT"] != DBNull.Value ? Convert.ToInt32(rdr["TDS_ACT"]) : null,
                        TDS_PER = rdr["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_PER"]) : null,
                        TDS_AMT = rdr["TDS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_AMT"]) : null,
                        WB_AMT = rdr["WB_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_AMT"]) : null,
                        WB_TDSPER = rdr["WB_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_TDSPER"]) : null,
                        WB_TDS = rdr["WB_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_TDS"]) : null,
                        WB_DRACT = rdr["WB_DRACT"] != DBNull.Value ? Convert.ToInt32(rdr["WB_DRACT"]) : null,
                        WB_CRACT = rdr["WB_CRACT"] != DBNull.Value ? Convert.ToInt32(rdr["WB_CRACT"]) : null,
                        WB_NARR = rdr["WB_NARR"]?.ToString(),
                        UL_AMT = rdr["UL_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_AMT"]) : null,
                        UL_TDSPER = rdr["UL_TDSPER"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_TDSPER"]) : null,
                        UL_TDS = rdr["UL_TDS"] != DBNull.Value ? Convert.ToDecimal(rdr["UL_TDS"]) : null,
                        UL_DRACT = rdr["UL_DRACT"] != DBNull.Value ? Convert.ToInt32(rdr["UL_DRACT"]) : null,
                        UL_CRACT = rdr["UL_CRACT"] != DBNull.Value ? Convert.ToInt32(rdr["UL_CRACT"]) : null,
                        UL_NARR = rdr["UL_NARR"]?.ToString(),
                        QLT_DR_AMT = rdr["QLT_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_DR_AMT"]) : null,
                        QLT_DR_TAX = rdr["QLT_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_DR_TAX"]) : null,
                        QLT_DR_NAR = rdr["QLT_DR_NAR"]?.ToString(),
                        QLT_CR_AMT = rdr["QLT_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_CR_AMT"]) : null,
                        QLT_CR_TAX = rdr["QLT_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QLT_CR_TAX"]) : null,
                        QLT_CR_NAR = rdr["QLT_CR_NAR"]?.ToString(),
                        RDF_DR_AMT = rdr["RDF_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_DR_AMT"]) : null,
                        RDF_DR_TAX = rdr["RDF_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_DR_TAX"]) : null,
                        RDF_DR_NAR = rdr["RDF_DR_NAR"]?.ToString(),
                        RDF_CR_AMT = rdr["RDF_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_CR_AMT"]) : null,
                        RDF_CR_TAX = rdr["RDF_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["RDF_CR_TAX"]) : null,
                        RDF_CR_NAR = rdr["RDF_CR_NAR"]?.ToString(),
                        QTY_DR_AMT = rdr["QTY_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_DR_AMT"]) : null,
                        QTY_DR_TAX = rdr["QTY_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_DR_TAX"]) : null,
                        QTY_DR_NAR = rdr["QTY_DR_NAR"]?.ToString(),
                        QTY_CR_AMT = rdr["QTY_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_CR_AMT"]) : null,
                        QTY_CR_TAX = rdr["QTY_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY_CR_TAX"]) : null,
                        QTY_CR_NAR = rdr["QTY_CR_NAR"]?.ToString(),
                        QC_DR_AMT = rdr["QC_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_DR_AMT"]) : null,
                        QC_DR_TAX = rdr["QC_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_DR_TAX"]) : null,
                        QC_DR_NAR = rdr["QC_DR_NAR"]?.ToString(),
                        QC_CR_AMT = rdr["QC_CR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_CR_AMT"]) : null,
                        QC_CR_TAX = rdr["QC_CR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["QC_CR_TAX"]) : null,
                        QC_CR_NAR = rdr["QC_CR_NAR"]?.ToString(),
                        OTH_DR_AMT = rdr["OTH_DR_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_DR_AMT"]) : null,
                        OTH_DR_TAX = rdr["OTH_DR_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_DR_TAX"]) : null,
                        OTH_DR_NAR = rdr["OTH_DR_NAR"]?.ToString(),
                        QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                        QC_NO = rdr["QC_NO"] != DBNull.Value ? Convert.ToInt32(rdr["QC_NO"]) : null,
                        DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : null,
                        TAX_HOLD = rdr["TAX_HOLD"]?.ToString(),
                        PRICE_TYPE = rdr["PRICE_TYPE"]?.ToString(),
                        FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                        FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                        HOLD_PAY = rdr["HOLD_PAY"]?.ToString(),
                        HOLD_REASON = rdr["HOLD_REASON"]?.ToString(),
                        HOLD_DATE = rdr["HOLD_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["HOLD_DATE"]) : null,
                        IMPORT_AMT = rdr["IMPORT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IMPORT_AMT"]) : null,
                        IMPORT_TAX = rdr["IMPORT_TAX"] != DBNull.Value ? Convert.ToDecimal(rdr["IMPORT_TAX"]) : null,
                        INVLAND_AMT = rdr["INVLAND_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["INVLAND_AMT"]) : null,
                        RCM_NO = rdr["RCM_NO"]?.ToString(),
                        DRNOTE_MAILSEND = rdr["DRNOTE_MAILSEND"] != DBNull.Value ? Convert.ToInt32(rdr["DRNOTE_MAILSEND"]) : null,
                        FRT_BILLNO = rdr["FRT_BILLNO"] != DBNull.Value ? Convert.ToInt32(rdr["FRT_BILLNO"]) : null,
                        FRT_BILLDT = rdr["FRT_BILLDT"] != DBNull.Value ? Convert.ToDateTime(rdr["FRT_BILLDT"]) : null,
                        FRT_PASSDT = rdr["FRT_PASSDT"] != DBNull.Value ? Convert.ToDateTime(rdr["FRT_PASSDT"]) : null,
                        FRT_CHQ = rdr["FRT_CHQ"]?.ToString(),
                        FRT_REMARK = rdr["FRT_REMARK"]?.ToString(),
                        GSTRMAIL_PARTYCNTR = rdr["GSTRMAIL_PARTYCNTR"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRMAIL_PARTYCNTR"]) : null,
                        GSTRMAIL_BILLCNTR = rdr["GSTRMAIL_BILLCNTR"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRMAIL_BILLCNTR"]) : null,
                        TDS_PER194Q = rdr["TDS_PER194Q"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_PER194Q"]) : null,
                        TDS_AMT194Q = rdr["TDS_AMT194Q"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_AMT194Q"]) : null,
                        DISP_ADDRESS = rdr["DISP_ADDRESS"]?.ToString(),
                        DISP_CITY = rdr["DISP_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["DISP_CITY"]) : null,
                        GSTRECO_REFTYPE = rdr["GSTRECO_REFTYPE"]?.ToString(),
                        GSTRECO_REFNO = rdr["GSTRECO_REFNO"] != DBNull.Value ? Convert.ToInt32(rdr["GSTRECO_REFNO"]) : null,
                        STOREIMG_FLG = rdr["STOREIMG_FLG"] != DBNull.Value ? Convert.ToInt32(rdr["STOREIMG_FLG"]) : null,
                        RET_TYPE = rdr["RET_TYPE"]?.ToString(),
                        FEXCH_USD = rdr["FEXCH_USD"] != DBNull.Value ? Convert.ToDecimal(rdr["FEXCH_USD"]) : null,
                        TRP_GSTNO = rdr["TRP_GSTNO"]?.ToString(),
                        TRP_BILLNO = rdr["TRP_BILLNO"]?.ToString(),
                        TRP_BILLDATE = rdr["TRP_BILLDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["TRP_BILLDATE"]) : null,
                        TRP_TAXTYPE = rdr["TRP_TAXTYPE"]?.ToString(),
                        MONTH_3B = rdr["MONTH_3B"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_3B"]) : null,
                        MONTH_3BN = rdr["MONTH_3BN"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_3BN"]) : null,
                        TRP_MONTH3B = rdr["TRP_MONTH3B"] != DBNull.Value ? Convert.ToDateTime(rdr["TRP_MONTH3B"]) : null,
                        MTH_REVYN3B = rdr["MTH_REVYN3B"]?.ToString(),
                        TRP_MTHREVYN3B = rdr["TRP_MTHREVYN3B"]?.ToString(),
                        MONTH_2B = rdr["MONTH_2B"] != DBNull.Value ? Convert.ToDateTime(rdr["MONTH_2B"]) : null,
                        EWB_DATE = rdr["EWB_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_DATE"]) : null,
                        EWB_EXPDATE = rdr["EWB_EXPDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_EXPDATE"]) : null,
                        EWB_INVNO = rdr["EWB_INVNO"]?.ToString(),
                        PL_AMT = rdr["PL_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PL_AMT"]) : null,
                        CURRENCY = rdr["CURRENCY"] != DBNull.Value ? Convert.ToInt32(rdr["CURRENCY"]) : null,
                        UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : null,
                        EINV_PARTY = rdr["EINV_PARTY"] != DBNull.Value ? Convert.ToInt32(rdr["EINV_PARTY"]) : null
                    };
                }

                //  Items (PURCHASE2)
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        items.Add(new PURCHASE2
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                            SNO = rdr["SNO"] != DBNull.Value ? Convert.ToInt32(rdr["SNO"]) : 0,
                            ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                            ITEM_NAME = rdr["ITEM_NAME"]?.ToString(),
                            MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                            HSN_CODE = rdr["HSN_CODE"]?.ToString(),
                            RCM_YN = rdr["RCM_YN"]?.ToString(),
                            INPUT_YN = rdr["INPUT_YN"]?.ToString(),
                            UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                            UNIT = rdr["UOM_NAME"]?.ToString(),
                            DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                            NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                            PLUS_MINUSQTY = rdr["PLUS_MINUSQTY"] != DBNull.Value ? Convert.ToDecimal(rdr["PLUS_MINUSQTY"]) : 0,
                            WB_QTY = rdr["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_QTY"]) : 0,
                            RECD_QTY = rdr["RECD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RECD_QTY"]) : 0,
                            BILL_QTY = rdr["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_QTY"]) : 0,
                            USD_RATE = rdr["USD_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["USD_RATE"]) : 0,
                            EXCH_RATE = rdr["EXCH_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXCH_RATE"]) : 0,
                            RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                            AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                            DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                            DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                            PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                            PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                            TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                            CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                            CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                            SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                            SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                            IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                            IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                            CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                            CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                            VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : 0,
                            VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : 0,
                            OTH_AMT = rdr["OTH_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_AMT"]) : 0,
                            NET_AMT = rdr["NET_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_AMT"]) : 0,
                            LAND_RATE = rdr["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_RATE"]) : 0,
                            LAND_AMT = rdr["LAND_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_AMT"]) : 0,
                            POLAND_RATE = rdr["POLAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["POLAND_RATE"]) : 0,
                            PO_RATE = rdr["PO_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["PO_RATE"]) : 0,
                            BIN_LOCATION = rdr["BIN_LOCATION"]?.ToString(),
                            BIN_CODE = rdr["BIN_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BIN_CODE"]) : 0,
                            PO_TYPE = rdr["PO_TYPE"]?.ToString(),
                            PO_NO = rdr["PO_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PO_NO"]) : 0,
                            SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                            SAUDA_NO = rdr["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SAUDA_NO"]) : 0,
                            KANTA_TYPE = rdr["KANTA_TYPE"]?.ToString(),
                            KANTA_NO = rdr["KANTA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["KANTA_NO"]) : 0,
                            REQ_TYPE = rdr["REQ_TYPE"]?.ToString(),
                            REQ_NO = rdr["REQ_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REQ_NO"]) : 0,
                            GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                            GATE_NO = rdr["GATE_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GATE_NO"]) : 0,
                            REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                            REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                            QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                            QC_NO = rdr["QC_NO"] != DBNull.Value ? Convert.ToInt32(rdr["QC_NO"]) : 0,
                            PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                            PASS_NO = rdr["PASS_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PASS_NO"]) : 0,
                            EMPTY_YN = rdr["EMPTY_YN"]?.ToString(),
                            MACH_CODE = rdr["MACH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MACH_CODE"]) : 0,
                            REMARKS = rdr["REMARKS"]?.ToString(),
                            RATE_MONTHLY = rdr["RATE_MONTHLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_MONTHLY"]) : 0,
                            RATE_QUARTERLY = rdr["RATE_QUARTERLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_QUARTERLY"]) : 0,
                            RATE_ANNUALY = rdr["RATE_ANNUALY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_ANNUALY"]) : 0,
                            RATE_SPECIAL = rdr["RATE_SPECIAL"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_SPECIAL"]) : 0,
                            FINAL_LOCK = rdr["FINAL_LOCK"]?.ToString(),
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString()
                        });
                    }
                }

                // Attachments
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        attachments.Add(new PurchaseBillAttachments
                        {
                            FILE_NAME = rdr["FILE_NAME"]?.ToString(),
                            FILE_DATA = rdr["IMG_FILE"] != DBNull.Value
                                            ? Convert.ToBase64String((byte[])rdr["IMG_FILE"])
                                            : null,
                            FILE_Path = rdr["FILE_Path"]?.ToString()
                        });
                    }
                }

                // EPR Attachments
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        eprAttachments.Add(new PurchaseBillAttachments
                        {
                            FILE_NAME = rdr["FILE_NAME"]?.ToString(),
                            FILE_DATA = rdr["IMG_FILE"] != DBNull.Value
                                            ? Convert.ToBase64String((byte[])rdr["IMG_FILE"])
                                            : null,
                            FILE_Path = rdr["FILE_Path"]?.ToString()
                        });
                    }
                }

                if (header != null)
                {
                    oldBankAmt = header.BANK_AMT;
                    oldplno = header.PL_NO;
                }

                var result = new FullPurchaseBillResponse
                {
                    Header = header,
                    Items = items,
                    Attachments = attachments,
                    EprAttachments = eprAttachments
                };
                return new RepositoryResponseData<FullPurchaseBillResponse> { status = true, data = result };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<FullPurchaseBillResponse> { status = false, message = "Error fetching quotation" + ex.Message };
            }
        }

        //===============Calculate TDS Button Click================
        public async Task<PBTdsCalculation> CalculateTDS(PURCHASE1 model)
        {
            var g = _globalVariableService.GetGlobalVariables();

            var result = new PBTdsCalculation();

            // Advance TDS
            string advTdsQuery = @"SELECT ISNULL(SUM(AMT),0) FROM LEDGER2 WHERE BILL_NO=@BILL_NO AND DR_CODE=@DR_CODE AND COMP_CODE=@COMP_CODE
                                    AND BRANCH_CODE=@BRANCH_CODE";

            result.AdvTds = await _dbHelper.GetExecuteScalarAsync<decimal>(advTdsQuery, new Dictionary<string, object>
            {
                { "@BILL_NO", $"{model.REF_TYPE}{model.REF_NO}" },
                { "@DR_CODE", model.PARTY_CODE },
                { "@COMP_CODE", g.PubCompCode },
                { "@BRANCH_CODE", g.PubBranchCode }
            });

            // Purchase Details
            string purchaseQuery = @"SELECT AMOUNT, ISNULL(QLT_DR_AMT,0)+ISNULL(RDF_DR_AMT,0)+ISNULL(QTY_DR_AMT,0)+ISNULL(QC_DR_AMT,0)+ISNULL(OTH_DR_AMT,0) AS DrNote,
                                    ISNULL(QLT_CR_AMT,0)+ISNULL(RDF_CR_AMT,0)+ISNULL(QTY_CR_AMT,0)+ISNULL(QC_CR_AMT,0) AS CrNote FROM PURCHASE1
                                    WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";

            using var con = _dbConnection.GetErpConnection();
            await con.OpenAsync();
            using var cmd = new SqlCommand(purchaseQuery, con);
            cmd.Parameters.AddRange(new[]
            {
                new SqlParameter("@V_TYPE", model.V_TYPE),
                new SqlParameter("@V_NO", model.V_NO),
                new SqlParameter("@COMP_CODE", g.PubCompCode),
                new SqlParameter("@BRANCH_CODE", g.PubBranchCode),
                new SqlParameter("@YEAR_CODE", g.PubFYearCode)
            });

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result.NetAmt = reader["AMOUNT"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["AMOUNT"]);
                result.DrNote = reader["DrNote"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["DrNote"]);
                result.CrNote = reader["CrNote"] == DBNull.Value ? 0M : Convert.ToDecimal(reader["CrNote"]);
            }
            else
            {
                result.NetAmt = model.AMOUNT ?? 0;
                result.DrNote = (model.QTY_DR_AMT ?? 0) + (model.RDF_DR_AMT ?? 0) + (model.QC_DR_AMT ?? 0) + (model.QLT_DR_AMT ?? 0) + (model.OTH_DR_AMT ?? 0);
                result.CrNote = (model.QTY_CR_AMT ?? 0) + (model.RDF_CR_AMT ?? 0) + (model.QC_CR_AMT ?? 0) + (model.QLT_CR_AMT ?? 0);
            }
            result.Tds194Q = result.NetAmt - result.DrNote - result.AdvTds + result.CrNote;
            return result;
        }

        //===============Copy From====================
        public RepositoryResponseList<CopyFromMenuItem> GetCopyFromMenu(string docType)
        {
            var list = new List<CopyFromMenuItem>();

            try
            {
                string qry = @"Select Code, Name
                   from DOCTYPE_MAST
                   where Code='PORD'";

                if (docType == "BFPB")
                    qry += " UNION ALL Select Code, Name from DOCTYPE_MAST where Code='BFRC'";
                else if (docType == "RMPB")
                    qry += " UNION ALL Select Code, Name from DOCTYPE_MAST where Code='RCPT'";
                else if (docType == "RIMP")
                    qry += " UNION ALL Select Code, Name from DOCTYPE_MAST where Code='RCPI'";
                else if (docType == "STPB")
                    qry += " UNION ALL Select Code, Name from DOCTYPE_MAST where Code='SRPU'";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand(qry, con);
                    con.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        string code = dr["Code"].ToString();
                        list.Add(new CopyFromMenuItem
                        {
                            Code = code,
                            Name = dr["Name"].ToString(),
                            //Modal = GetModalId(code)
                        });
                    }
                }

                return new RepositoryResponseList<CopyFromMenuItem> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<CopyFromMenuItem> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponseData<CopyFromGridResponse> GetCopyFromData(CopyFromRequest request)
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();
                using SqlCommand? cmd = BuildCopyFromCommand(request, con);

                if (cmd == null)
                {
                    return new RepositoryResponseData<CopyFromGridResponse>
                    {
                        status = true,
                        data = new CopyFromGridResponse()
                    };
                }

                con.Open();

                DataTable dt = new();

                using (SqlDataAdapter da = new(cmd))
                {
                    da.Fill(dt);
                }

                // Columns
                var columns = dt.Columns.Cast<DataColumn>()
                    .Select(col =>
                    {
                        string field = col.ColumnName;
                        string title = col.ColumnName;

                        int start = col.ColumnName.IndexOf(" (");

                        if (start >= 0 && col.ColumnName.EndsWith(")"))
                        {
                            field = col.ColumnName[..start];
                            title = col.ColumnName[(start + 2)..^1];
                        }

                        return new
                        {
                            OriginalColumn = col.ColumnName,
                            Field = field.ToUpperInvariant(),
                            Title = title
                        };
                    })
                    .ToList();


                // Rows
                var rows = dt.AsEnumerable()
                    .Select(row =>
                    {
                        var dict = new Dictionary<string, object?>();

                        foreach (var col in columns)
                        {
                            dict[col.Field] =
                                row[col.OriginalColumn] == DBNull.Value
                                    ? null
                                    : row[col.OriginalColumn];
                        }

                        return dict;
                    })
                    .ToList();


                // Response columns
                var responseColumns = columns
                    .Select(col => new CopyFromColumn
                    {
                        Field = col.Field,
                        Title = col.Title
                    })
                    .ToList();

                return new RepositoryResponseData<CopyFromGridResponse>
                {
                    status = true,
                    data = new CopyFromGridResponse
                    {
                        Columns = responseColumns,
                        Rows = rows
                    }
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<CopyFromGridResponse>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }

        private SqlCommand? BuildCopyFromCommand(CopyFromRequest request, SqlConnection con)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string action = $"CopyFrom{request.CurrentVType}";

            var supportedActions = new[]
            {
                "CopyFromPORD",
                "CopyFromRCPI",
                "CopyFromSRPU"
            };

            if (!supportedActions.Contains(action))
                return null;

            var cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@PARTY_CODE", request.BillTo);
            cmd.Parameters.AddWithValue("@BILL_NO", request.BillNo ?? "");
            cmd.Parameters.AddWithValue("@V_NO", request.VNo);
            cmd.Parameters.AddWithValue("@V_TYPE", request.vType);
            cmd.Parameters.AddWithValue("@CopyFromVtype", request.CurrentVType);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            return cmd;
        }

        //=================Pending Approval List=============
        public RepositoryResponseData<List<PendingApprovalModel>> GetPendingApprovalList()
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                var gv = _globalVariableService.GetGlobalVariables();

                using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Action", "GetPendingApprovalList");
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                con.Open();

                using SqlDataReader reader = cmd.ExecuteReader();

                List<PendingApprovalModel> list = new();

                while (reader.Read())
                {
                    list.Add(new PendingApprovalModel
                    {
                        Type = reader["Type"]?.ToString(),
                        DocID = reader["DocID"].ToString(),
                        DocDate = reader["Doc Date"]?.ToString(),
                        SendBy = reader["Send By"]?.ToString(),
                        SendDate = reader["Send Date"]?.ToString(),
                        SendTo = reader["Send To"]?.ToString(),
                        Status = reader["Status"]?.ToString(),
                        ApprovalStatus = reader["Approval Status"]?.ToString(),
                        Remarks = reader["Remarks"]?.ToString(),
                        CreatedBy = reader["Created By"]?.ToString(),
                        CreatedDate = reader["Created Date"]?.ToString(),
                        PartyName = reader["Party Name"]?.ToString(),
                        BillAmount = reader["Bill Amount"] == DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(reader["Bill Amount"])
                    });
                }

                return new RepositoryResponseData<List<PendingApprovalModel>>
                {
                    status = true,
                    data = list
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<PendingApprovalModel>>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }
        //====================Advance TDS==============
        public RepositoryResponseData<List<AdvanceTdsModel>> GetAdvanceTdsList(string billNo, int drCode)
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                var gv = _globalVariableService.GetGlobalVariables();

                string qry = @"SELECT  a.V_TYPE AS Vtype, a.V_NO AS VNo, FORMAT(a.V_DATE, 'dd/MM/yyyy') AS VDate, a.AMT AS Amount,
                                            b.NAME AS PartyName FROM Ledger2 a LEFT JOIN SUBGROUP_MAST b ON a.DR_CODE = b.CODE AND a.COMP_CODE = 
                                            b.COMP_CODE WHERE a.BILL_NO = @BILL_NO AND a.DR_CODE = @DR_CODE AND a.COMP_CODE = @COMP_CODE AND 
                                            a.BRANCH_CODE = @BRANCH_CODE ORDER BY a.V_DATE, a.V_NO";

                using SqlCommand cmd = new(qry, con);

                cmd.Parameters.AddWithValue("@BILL_NO", billNo);
                cmd.Parameters.AddWithValue("@DR_CODE", drCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                con.Open();

                var list = new List<AdvanceTdsModel>();

                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new AdvanceTdsModel
                    {
                        Vtype = reader["Vtype"] == DBNull.Value ? null : reader["Vtype"].ToString(),
                        VNo = reader["VNo"] == DBNull.Value ? null : reader["VNo"].ToString(),
                        VDate = reader["VDate"] == DBNull.Value ? null : reader["VDate"].ToString(),
                        Amount = reader["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Amount"]),
                        PartyName = reader["PartyName"] == DBNull.Value ? null : reader["PartyName"].ToString()
                    });
                }

                return new RepositoryResponseData<List<AdvanceTdsModel>> { status = true, data = list };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<AdvanceTdsModel>> { status = false, message = ex.Message };
            }
        }

        //====================Calculate Dr Cr Note Amount Button==============
        public async Task<RepositoryResponseData<DrCrCalculationResponse>> CalculateRowDrCrAmount(CalculateDrCrRequest request)
        {
            if (request == null)
            {
                return new RepositoryResponseData<DrCrCalculationResponse> { status = false, message = "Invalid request!" };
            }

            var result = new DrCrCalculationResponse();

            foreach (var gridRow in request.Rows)
            {
                if (gridRow != null && gridRow.ItemCode > 0)
                {
                    result.Rows.Add(new DrCrRowAmount
                    {
                        ItemCode = gridRow.ItemCode
                    });
                }
            }
            // 1. QC Dr Note
            result = await CalculateRowQCDrNote(request, result);

            // 2. Rate Difference Dr Note
            result = CalculateRowRateDiffDrNote(request, result);

            // 3. Weight Difference Dr Note
            result = CalculateRowWeightDiffDrNote(request, result);

            // 4. Quality Difference Dr Note
            result = await CalculateRowQualityDiffDrNote(request, result);

            // 5. Final Dr Note
            result = CalculateFinalDrNoteAmt(result);

            // 5. Header Amt Diff with Grid
            result = CalculateDrCrHeaderGridDifference(request, result);

            return new RepositoryResponseData<DrCrCalculationResponse>
            {
                status = true,
                message = "Dr/Cr amount calculated successfully.",
                data = result
            };
        }

        private async Task<DrCrCalculationResponse> CalculateRowQCDrNote(CalculateDrCrRequest request, DrCrCalculationResponse result)
        {
            if (request == null || result == null)
                return result;

            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"SELECT b.ITEM_CODE AS ItemCode, SUM(ISNULL(b.DEDU_AMT1, 0)) AS DedAmt,
                    CASE
                        WHEN SUM(ISNULL(b.DEDU_AMT1, 0)) > SUM(ISNULL(b.ALLOW_AMT, 0))
                            THEN SUM(ISNULL(b.DEDU_AMT1, 0)) - SUM(ISNULL(b.ALLOW_AMT, 0))
                        ELSE SUM(ISNULL(b.DEDU_AMT1, 0))
                    END AS QcDrAmt
                FROM qc1 a
                LEFT JOIN qc2 b ON a.V_TYPE = b.V_TYPE AND a.V_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE
                WHERE a.MRN_TYPE = @MRN_TYPE AND a.MRN_NO = @MRN_NO AND a.COMP_CODE = @COMP_CODE AND a.BRANCH_CODE = @BRANCH_CODE AND a.YEAR_CODE = @YEAR_CODE
                GROUP BY b.ITEM_CODE 
                ORDER BY b.ITEM_CODE;
            ";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@MRN_TYPE", request.RefType),
                new SqlParameter("@MRN_NO", request.RefVNo),
                new SqlParameter("@COMP_CODE", gv.PubCompCode),
                new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
            };

            // ---------------------------------------------------------
            // Read QC Dr Note data
            // ---------------------------------------------------------

            var qcLookup = new Dictionary<int, decimal>();

            await using var con = _dbConnection.GetErpConnection();
            await using var cmd = new SqlCommand(query, con);

            cmd.CommandType = CommandType.Text;

            if (parameters != null && parameters.Count > 0)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            await con.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int itemCode = reader["ItemCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ItemCode"]);
                decimal qcDrAmt = reader["QcDrAmt"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QcDrAmt"]);
                if (itemCode > 0)
                {
                    qcLookup[itemCode] = qcDrAmt;
                }
            }

            // ---------------------------------------------------------
            // Apply QC Dr Note amount to each grid row
            // ---------------------------------------------------------

            foreach (var gridRow in request.Rows)
            {
                if (gridRow == null || gridRow.ItemCode <= 0)
                    continue;

                // Find existing response row
                var rowResult = result.Rows.FirstOrDefault(x => x.ItemCode == gridRow.ItemCode);
                if (rowResult == null)
                    continue;

                // -----------------------------------------------------
                // Find QC amount for current item
                // -----------------------------------------------------

                if (!qcLookup.TryGetValue(gridRow.ItemCode, out decimal qcDrAmt))
                {
                    rowResult.QCDiffDrNoteAmt = 0;
                    continue;
                }

                decimal taxPer = gridRow.CGSTPer + gridRow.SGSTPer + gridRow.IGSTPer;

                if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
                {
                    decimal denominator = 100m + taxPer;
                    rowResult.QCDiffDrNoteAmt = Math.Round((qcDrAmt / denominator) * 100m, 0, MidpointRounding.AwayFromZero);
                }
                else
                {
                    rowResult.QCDiffDrNoteAmt = Math.Round(qcDrAmt, 0, MidpointRounding.AwayFromZero);
                }
            }

            return result;
        }

        private DrCrCalculationResponse CalculateRowRateDiffDrNote(CalculateDrCrRequest request, DrCrCalculationResponse result)
        {
            if (request == null || result == null)
                return result;

            foreach (var gridRow in request.Rows)
            {
                if (gridRow == null || gridRow.ItemCode <= 0)
                    continue;

                if (gridRow.LandRate != gridRow.POLandRate)
                {
                    decimal diffAmt = gridRow.LandRate - gridRow.POLandRate;
                    decimal rateDiffAmt = Math.Round(diffAmt * gridRow.BillQty, 0, MidpointRounding.AwayFromZero);
                    decimal taxPer = gridRow.CGSTPer + gridRow.SGSTPer + gridRow.IGSTPer;

                    // Find the corresponding response row
                    var rowResult = result.Rows.FirstOrDefault(x => x.ItemCode == gridRow.ItemCode);
                    if (rowResult == null)
                        continue;

                    if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
                    {
                        rowResult.RateDiffDrAmt = Math.Round((rateDiffAmt / (100m + taxPer)) * 100m, 0, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        rowResult.RateDiffDrAmt = Math.Round(rateDiffAmt, 0, MidpointRounding.AwayFromZero);
                    }
                }
            }

            return result;
        }

        private DrCrCalculationResponse CalculateRowWeightDiffDrNote(CalculateDrCrRequest request, DrCrCalculationResponse result)
        {
            if (request == null || result == null)
                return result;

            foreach (var gridRow in request.Rows)
            {
                if (gridRow == null || gridRow.ItemCode <= 0)
                    continue;

                decimal actAmt = gridRow.RecdQty * gridRow.Rate;
                decimal diffAmt = gridRow.Amount - actAmt;

                // Find existing response row
                var rowResult = result.Rows.FirstOrDefault(x => x.ItemCode == gridRow.ItemCode);
                if (rowResult == null) continue;

                rowResult.WeightDiffDrAmt = Math.Round(diffAmt, 0, MidpointRounding.AwayFromZero);
            }

            return result;
        }

        private async Task<DrCrCalculationResponse> CalculateRowQualityDiffDrNote(CalculateDrCrRequest request, DrCrCalculationResponse result)
        {
            if (request == null || result == null)
                return result;

            var gs = await _globalVariableService.LoadGeneralSetting();
            var gv = _globalVariableService.GetGlobalVariables();
            decimal amt1 = 0.0m, amt2 = 0.0m, amt3 = 0.0m, at1 = 0.0m, at2 = 0.0m, at3 = 0.0m, Q15DrAmt = 0.0m, Q15DrTax = 0.0m,
                qltDiffDrAmt = 0.0m, qltDiffDrRate = 0.0m, QltQtyDiff = 0.0m;
            string anr1 = "", anr2 = "", anr3 = "", Q15Narr = "";

            foreach (var gridRow in request.Rows)
            {
                decimal discItemRate = 0.0m;
                if (gridRow == null || gridRow.ItemCode <= 0)
                    continue;

                var rowResult = result.Rows.FirstOrDefault(x => x.ItemCode == gridRow.ItemCode);

                if (rowResult == null)
                    continue;

                decimal taxPer = gridRow.CGSTPer + gridRow.SGSTPer + gridRow.IGSTPer;

                string DefPOInMRN = gs.pubDefPOInMRN;

                if (string.Equals(DefPOInMRN, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    string orderQry = $@"select 1 from ORDER2 where V_TYPE='{gridRow.POType}' and V_NO={gridRow.PONo} and COMP_CODE={gv.PubCompCode} 
                                        and BRANCH_CODE={gv.PubBranchCode} and ITEM_CODE={gridRow.ItemCode}";
                    bool isOrderExist = await _dbHelper.IsDataExist(orderQry);
                    if (isOrderExist)
                    {
                        rowResult.poLandRate = gridRow.POLandRate;
                        //Unused
                        //string saudaQry = $@"select top 1 b.RATE from ORDER2 a 
                        //               left join SAUDA b on a.SAUDA_TYPE =b.V_TYPE and a.SAUDA_NO=b.V_NO and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE 
                        //               where a.V_TYPE='{gridRow.POType}' and a.V_NO={gridRow.PONo} and a.COMP_CODE={gv.PubCompCode} and a.BRANCH_CODE={gv.PubBranchCode}";

                        //decimal saudaRate = await _dbHelper.GetExecuteScalarAsync<decimal>(saudaQry);

                        string saudaItemQry = $@"select top 1 b.ITEM_CODE from ORDER2 a 
                                       left join SAUDA b on a.SAUDA_TYPE =b.V_TYPE and a.SAUDA_NO=b.V_NO and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE 
                                       where a.V_TYPE='{gridRow.POType}' and a.V_NO={gridRow.PONo} and a.COMP_CODE={gv.PubCompCode} and a.BRANCH_CODE={gv.PubBranchCode}";

                        int saudaItemCode = await _dbHelper.GetExecuteScalarAsync<int>(saudaItemQry);

                        decimal qtyDiffQty = gridRow.RecdQty - gridRow.BillQty;

                        // -----------------------------------------------------
                        // RMPB specific logic
                        // -----------------------------------------------------
                        if (string.Equals(request.VType, "RMPB", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal abovePer = 0m;
                            decimal aboveRate = 0m;

                            string saudaReqQry = $@"Select isnull(SAUDA_REQ,'') from ITEM_GROUP Where code=(Select Group_code from Item_mast 
                                                    where code={gridRow.ItemCode} and Comp_code={gv.PubCompCode}) and Comp_code={gv.PubCompCode}";

                            string saudaReq = await _dbHelper.GetExecuteScalarAsync<string>(saudaReqQry);

                            if (string.Equals(saudaReq, "Yes", StringComparison.OrdinalIgnoreCase))
                            {
                                string rmDiscQry = $@"select top 1 isnull(RATE,0) from RMDISC_MAST where SAUDA_ITEM={saudaItemCode} and COMP_CODE={gv.PubCompCode}";

                                bool discItemExists = await _dbHelper.IsDataExist(rmDiscQry);

                                if (discItemExists)
                                {
                                    string rmDiscItemQry = $@"select top 1 isnull(RATE,0) from RMDISC_MAST where SAUDA_ITEM={saudaItemCode} and item_code={gridRow.ItemCode}
                                                                and COMP_CODE={gv.PubCompCode}";
                                    discItemRate = await _dbHelper.GetExecuteScalarAsync<decimal>(rmDiscItemQry);

                                    string rmDiscRateQry = $@"Select top 1 isnull(RATE,0) as Rate, isnull(ABOVE_PER,0) as ABOVE_PER, isnull(ABOVE_AMT,0) as ABOVE_AMT 
                                                                from RMDISC_MAST where item_code={gridRow.ItemCode} and comp_code={gv.PubCompCode} and SAUDA_ITEM=
                                                                {saudaItemCode} and EFF_DATE< '{request.vDate:yyyyMMdd}' Order by EFF_DATE DESC";
                                    var rmDiscRateRes = await _dbHelper.ExecuteQueryAsync(rmDiscRateQry);
                                    if (rmDiscRateRes.Rows.Count > 0)
                                    {
                                        var row = rmDiscRateRes.Rows[0];

                                        discItemRate = row["Rate"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Rate"]);
                                        abovePer = row["ABOVE_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ABOVE_PER"]);
                                        aboveRate = row["ABOVE_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ABOVE_AMT"]);
                                    }
                                    else
                                    {
                                        rowResult.rowWarningMsg += $@"Item {gridRow.ItemName} not found in discount master, Please contact System Administrator.";
                                    }
                                }

                            }

                            // -------------------------------------------------
                            // Quality difference / quantity above percentage
                            // -------------------------------------------------
                            decimal sItemQty = 0m;
                            if (abovePer > 0 && ((gv.PubCompCode == "1" && saudaItemCode == 30001) || (gv.PubCompCode == "7" && saudaItemCode == 3)))
                            {
                                foreach (var row in request.Rows)
                                {
                                    if (row == null)
                                        continue;

                                    if (row.ItemCode == saudaItemCode)
                                    {
                                        sItemQty += row.BillQty;
                                    }
                                }
                                if (gridRow.RecdQty > (sItemQty * abovePer * 0.01m))
                                {
                                    decimal rQty = gridRow.RecdQty;
                                    decimal trqty = 0.0m;
                                    decimal rRate = Math.Abs(aboveRate) + discItemRate;
                                    if (rRate > 0)
                                    {
                                        amt1 = Math.Round(rQty * rRate, 0, MidpointRounding.AwayFromZero);
                                        at1 = Math.Round(amt1 * taxPer * 0.01m, 0, MidpointRounding.AwayFromZero);
                                        rRate += rRate * taxPer / 100m;
                                        anr1 = $"{gridRow.ItemName} Qty > {abovePer}% " + $"{rQty} @ {rRate} Rs. {rQty * rRate}";
                                        trqty += rQty;
                                    }
                                }
                                Q15DrAmt = amt1 + amt2 + amt3;
                                Q15DrTax = at1 + at2 + at3;
                                Q15Narr = anr3 + ", " + anr2 + ", " + anr1;
                            }
                        }

                        // -----------------------------------------------------
                        // Quality difference based on negative discount rate
                        // -----------------------------------------------------
                        if (qtyDiffQty > 0 && discItemRate < 0)
                        {
                            qltDiffDrRate = Math.Abs(discItemRate);
                            decimal qltDiffTaxRate = qltDiffDrRate * taxPer / 100m;
                            qltDiffDrRate += qltDiffTaxRate;

                            if (qltDiffDrRate > 0)
                            {
                                qltDiffDrAmt = Math.Round(qtyDiffQty * qltDiffDrRate, 0, MidpointRounding.AwayFromZero);
                                if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
                                {
                                    rowResult.QualityDiffDrAmt = Math.Round((qltDiffDrAmt / (100m + taxPer)) * 100m, 0, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    rowResult.QualityDiffDrAmt = Math.Round(qltDiffDrAmt, 0, MidpointRounding.AwayFromZero);
                                }
                            }
                        }
                        Q15DrAmt = 0.0m; Q15DrTax = 0.0m; qltDiffDrAmt = 0.0m;
                    }
                    else
                    {
                        string poLDRateQry = $@"select RATE from ORDER2 where V_TYPE='{gridRow.POType}' and V_NO={gridRow.PONo} and COMP_CODE={gv.PubCompCode} 
                        and BRANCH_CODE={gv.PubBranchCode} and FAPROV_STATUS='Approved'";

                        decimal poLandRate = await _dbHelper.GetExecuteScalarAsync<decimal>(poLDRateQry);

                        if (poLandRate > 0)
                        {
                            rowResult.poLandRate = poLandRate;
                        }
                        else
                        {
                            rowResult.poLandRate = gridRow.POLandRate;
                        }

                        // RMPB
                        if (string.Equals(request.VType, "RMPB", StringComparison.OrdinalIgnoreCase))
                        {
                            string itemDiffQry = $@"select top 1 isnull(ITEM_DIFF,0) from DISC_MAST where code={request.BillTo} and ITEM_CODE={gridRow.ItemCode} and COMP_CODE={gv.PubCompCode}";
                            bool isItemDiffExists = await _dbHelper.IsDataExist(itemDiffQry);
                            if (isItemDiffExists)
                            {
                                discItemRate = await _dbHelper.GetExecuteScalarAsync<decimal>(itemDiffQry);
                            }
                            else
                            {
                                rowResult.rowWarningMsg += $@"Discount rate not found of {gridRow.ItemName} of Supplier {request.billToName}";
                            }
                        }

                        qltDiffDrRate = gridRow.LandRate - (gridRow.PORate + discItemRate);
                        decimal qltDiffTaxRate = qltDiffDrRate * taxPer / 100m;
                        qltDiffDrRate += qltDiffTaxRate;

                        if (qltDiffDrRate > 0)
                        {
                            qltDiffDrAmt = Math.Round(gridRow.BillQty * qltDiffDrRate, 0, MidpointRounding.AwayFromZero);
                            if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
                            {
                                rowResult.QualityDiffDrAmt = Math.Round((qltDiffDrAmt / (100m + taxPer)) * 100m, 0, MidpointRounding.AwayFromZero);
                            }
                            else
                            {
                                rowResult.QualityDiffDrAmt = Math.Round(qltDiffDrAmt, 0, MidpointRounding.AwayFromZero);
                            }
                            // -----------------------------------------------------
                            // Additional quantity difference
                            // -----------------------------------------------------
                            QltQtyDiff = gridRow.RecdQty - gridRow.BillQty;

                            if (discItemRate < 0 && QltQtyDiff > 0)
                            {
                                qltDiffDrRate = Math.Abs(discItemRate);
                                qltDiffTaxRate = qltDiffDrRate * taxPer / 100m;
                                qltDiffDrRate += qltDiffTaxRate;
                                qltDiffDrAmt = Math.Round(QltQtyDiff * qltDiffDrRate, 0, MidpointRounding.AwayFromZero);
                                if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
                                {
                                    rowResult.QCDiffDrNoteAmt = Math.Round((qltDiffDrAmt / (100m + taxPer)) * 100m, 0, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    rowResult.QCDiffDrNoteAmt = Math.Round(qltDiffDrAmt, 0, MidpointRounding.AwayFromZero);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private DrCrCalculationResponse CalculateFinalDrNoteAmt(DrCrCalculationResponse result)
        {
            if (result == null || result.Rows == null)
                return result;
            foreach (var rowResult in result.Rows)
            {
                if (rowResult == null || rowResult.ItemCode <= 0)
                    continue;
                rowResult.FinalDrNote = rowResult.QCDiffDrNoteAmt +
                                            rowResult.RateDiffDrAmt +
                                            rowResult.WeightDiffDrAmt +
                                            rowResult.QualityDiffDrAmt +
                                            rowResult.OtherDrNoteAmt;
            }
            return result;
        }

        private DrCrCalculationResponse CalculateDrCrHeaderGridDifference(CalculateDrCrRequest request, DrCrCalculationResponse result)
        {
            if (request == null || result == null)
                return result;

            decimal totDrAmtHeader = 0m;
            decimal totCrAmtHeader = 0m;

            decimal totDrAmtDetail = 0m;
            decimal totCrAmtDetail = 0m;

            // ---------------------------------------------------------
            // Header Dr / Cr Amount
            // ---------------------------------------------------------
            if (string.Equals(request.InputType, "GST Input", StringComparison.OrdinalIgnoreCase))
            {
                totDrAmtHeader = request.QualityDiffDebitAmt + request.RateDiffDebitAmt + request.QCDebitAmt + request.WeightDiffDebitAmt + request.OthDebitAmt;
                totCrAmtHeader = request.QualityDiffCreditAmt + request.RateDiffCreditAmt + request.QCCreditAmt + request.WeightDiffCreditAmt;
            }
            else
            {
                totDrAmtHeader = request.QualityDiffDebitAmt + request.RateDiffDebitAmt + request.QCDebitAmt + request.WeightDiffDebitAmt + request.OthDebitAmt
                                + request.QualityDiffDebitTax + request.RateDiffDebitTax + request.QCDebitTax + request.WeightDiffDebitTax + request.OthDebitTax;
                totCrAmtHeader = request.QualityDiffCreditAmt + request.RateDiffCreditAmt + request.QCCreditAmt + request.WeightDiffCreditAmt + request.QualityDiffCreditTax
                                + request.RateDiffCreditTax + request.QCCreditTax + request.WeightDiffCreditTax;
            }

            foreach (var row in result.Rows)
            {
                if (row == null || row.ItemCode <= 0)
                    continue;

                totDrAmtDetail += row.FinalDrNote;
                totCrAmtDetail += row.FinalCrNote;
            }

            // ---------------------------------------------------------
            // Header vs Grid Difference
            // ---------------------------------------------------------
            result.DrAmtHeader = Math.Round(totDrAmtHeader, 0, MidpointRounding.AwayFromZero);
            result.CrAmtHeader = Math.Round(totCrAmtHeader, 0, MidpointRounding.AwayFromZero);
            result.DrAmtGrid = Math.Round(totDrAmtDetail, 0, MidpointRounding.AwayFromZero);
            result.CrAmtGrid = Math.Round(totCrAmtDetail, 0, MidpointRounding.AwayFromZero);
            result.DrAmtDiff = Math.Abs(totDrAmtDetail) - totDrAmtHeader;
            result.CrAmtDiff = Math.Abs(totCrAmtDetail) - totCrAmtHeader;

            return result;
        }

        //Reports
        public async Task<bool> UpdatePassDetails()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using SqlConnection con = _dbConnection.GetErpConnection();
            await con.OpenAsync();
            using SqlTransaction tran = con.BeginTransaction();

            try
            {
                string query1 = @"UPDATE purchase2 SET pass_type = '', pass_no = 0 WHERE v_type IN ('RCPT','RCPI','BFRC','SRPU','SRJW') AND ISNULL(pass_no, 0) = 0
                                AND comp_code = @comp_code AND year_code = @year_code AND branch_code = @branch_code;";

                using (SqlCommand cmd = new SqlCommand(query1, con, tran))
                {
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    await cmd.ExecuteNonQueryAsync();
                }

                string query2 = @"UPDATE mrn2 SET mrn2.pass_type = pass2.v_type, mrn2.pass_no = pass2.v_no FROM purchase2 mrn2 INNER JOIN purchase2 pass2
                                  ON mrn2.v_type = pass2.ref_type AND mrn2.v_no = pass2.ref_no AND mrn2.comp_code = pass2.comp_code AND mrn2.year_code = pass2.year_code
                                    AND mrn2.item_code = pass2.item_code WHERE ISNULL(mrn2.pass_no, 0) = 0 AND pass2.comp_code = @comp_code AND pass2.year_code = @year_code
                                    AND pass2.branch_code = @branch_code AND mrn2.v_type IN ('RCPT','RCPI','BFRC','SRPU','SRJW');";

                using (SqlCommand cmd = new SqlCommand(query2, con, tran))
                {
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        List<SqlParameter> GetCommonParams()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            return new List<SqlParameter>
            {
                new SqlParameter("@COMP_CODE", gv.PubCompCode),
                new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
            };
        }
        public async Task<bool> GenerateImportBillAsync(int vNo, string vType, DateTime vDate, int plNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                // ---------------------------------------------------------
                // 1. Check Exchange Rate
                // ---------------------------------------------------------
                string exchangeRateQuery = @"SELECT ISNULL(EXCH_RATE, 0) FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                                            AND YEAR_CODE = @YEAR_CODE";

                var parameters = GetCommonParams();
                parameters.Add(new SqlParameter("@V_NO", vNo));
                parameters.Add(new SqlParameter("@V_TYPE", vType));

                var exchangeRate = await _dbHelper.ExecuteScalarAsync(exchangeRateQuery, parameters);

                if (string.IsNullOrWhiteSpace(exchangeRate) ||
                    Convert.ToDecimal(exchangeRate, CultureInfo.InvariantCulture) == 0)
                {
                    throw new Exception("Please check, Exchange Rate is 0.");
                }


                // ---------------------------------------------------------
                // 2. Delete existing IMPORT1
                // ---------------------------------------------------------
                string deleteImport1 = @"DELETE FROM IMPORT1 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var deleteImport1Parameters = GetCommonParams();
                deleteImport1Parameters.Add(new SqlParameter("@V_NO", vNo));
                deleteImport1Parameters.Add(new SqlParameter("@V_TYPE", vType));
                _dbHelper.ExecuteNonQuery(deleteImport1, deleteImport1Parameters);

                // ---------------------------------------------------------
                // 3. Delete existing IMPORT2
                // ---------------------------------------------------------
                string deleteImport2 = @"DELETE FROM IMPORT2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var deleteImport2Parameters = GetCommonParams();
                deleteImport2Parameters.Add(new SqlParameter("@V_NO", vNo));
                deleteImport2Parameters.Add(new SqlParameter("@V_TYPE", vType));
                _dbHelper.ExecuteNonQuery(deleteImport2, deleteImport2Parameters);


                // ---------------------------------------------------------
                // 4. Insert IMPORT2
                // ---------------------------------------------------------
                
                var insertImport2Params = GetCommonParams();
                insertImport2Params.Add(new SqlParameter("@Action", "InsertImport2"));
                insertImport2Params.Add(new SqlParameter("@WSID", gv.PubWorkStationID));
                insertImport2Params.Add(new SqlParameter("@USER_CODE", gv.PubUserId));
                insertImport2Params.Add(new SqlParameter("@V_NO", vNo));
                insertImport2Params.Add(new SqlParameter("@V_TYPE", vType));

                _dbHelper.ExecuteNonQuery("sp_PurchaseBillPassEntryDirect", insertImport2Params, true);


                // ---------------------------------------------------------
                // 5. Get BL_NO
                // ---------------------------------------------------------
                string blNoQuery = @"SELECT CHALL_NO FROM PURCHASE1 WHERE REF_NO = @V_NO AND REF_TYPE = @V_TYPE AND V_TYPE IN ('RMDP', 'SIDP') AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                                    AND YEAR_CODE = @YEAR_CODE AND ISNULL(CHALL_NO, '') <> ''";
                var blNoParameters = GetCommonParams();
                blNoParameters.Add(new SqlParameter("@V_NO", vNo));
                blNoParameters.Add(new SqlParameter("@V_TYPE", vType));
                string blNo = await _dbHelper.ExecuteScalarAsync(blNoQuery, blNoParameters);


                // ---------------------------------------------------------
                // 6. Get Shipping Line Name
                // ---------------------------------------------------------
                string shipNameQuery = @"SELECT B.NAME FROM PURCHASE1 A LEFT JOIN SUBGROUP_MAST B ON A.PARTY_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE WHERE A.REF_NO = @V_NO AND A.REF_TYPE = 
                                        @V_TYPE AND A.V_TYPE IN ('RMDP', 'SIDP') AND A.COMP_CODE = @COMP_CODE AND A.BRANCH_CODE = @BRANCH_CODE AND A.YEAR_CODE = @YEAR_CODE AND A.EXPS_TYPE = 
                                        'Shipping Line'";
                var shipNameParameters = GetCommonParams();
                shipNameParameters.Add(new SqlParameter("@V_NO", vNo));
                shipNameParameters.Add(new SqlParameter("@V_TYPE", vType));
                string shipName = await _dbHelper.ExecuteScalarAsync(shipNameQuery, shipNameParameters);


                // ---------------------------------------------------------
                // 7. Get Import Quantity
                // ---------------------------------------------------------
                string importQtyQuery = @"SELECT COUNT(*) FROM IMPORT2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var importQtyParameters = GetCommonParams();
                importQtyParameters.Add(new SqlParameter("@V_NO", vNo));
                importQtyParameters.Add(new SqlParameter("@V_TYPE", vType));
                string importQty = await _dbHelper.ExecuteScalarAsync(importQtyQuery, importQtyParameters);


                // ---------------------------------------------------------
                // 8. Get USD Rate
                // ---------------------------------------------------------
                string usdRateQuery = @"SELECT ISNULL(AVG(USD_RATE), 0) FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var usdRateParameters = GetCommonParams();
                usdRateParameters.Add(new SqlParameter("@V_NO", vNo));
                usdRateParameters.Add(new SqlParameter("@V_TYPE", vType));
                string usdRate = await _dbHelper.ExecuteScalarAsync(usdRateQuery, usdRateParameters);


                // ---------------------------------------------------------
                // 9. Get USD Amount
                // ---------------------------------------------------------
                string usdAmountQuery = @"SELECT ISNULL(SUM(AMOUNT / EXCH_RATE), 0) FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = 
                                        @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var usdAmtParameters = GetCommonParams();
                usdAmtParameters.Add(new SqlParameter("@V_NO", vNo));
                usdAmtParameters.Add(new SqlParameter("@V_TYPE", vType));
                string usdAmount = await _dbHelper.ExecuteScalarAsync(usdAmountQuery, usdAmtParameters);


                // ---------------------------------------------------------
                // 10. Insert IMPORT1
                // ---------------------------------------------------------
                var insertImport1Params = GetCommonParams();
                insertImport1Params.Add(new SqlParameter("@Action", "InsertImport1"));
                insertImport1Params.Add(new SqlParameter("@WSID", gv.PubWorkStationID));
                insertImport1Params.Add(new SqlParameter("@USER_CODE", gv.PubUserId));
                insertImport1Params.Add(new SqlParameter("@BL_NO", (object)blNo ?? DBNull.Value));
                insertImport1Params.Add(new SqlParameter("@SHIP_NAME", (object)shipName ?? DBNull.Value));
                insertImport1Params.Add(new SqlParameter("@QTY", Convert.ToInt32(importQty)));
                insertImport1Params.Add(new SqlParameter("@USD", Convert.ToDecimal(usdRate)));
                insertImport1Params.Add(new SqlParameter("@USD_AMT", Convert.ToDecimal(usdAmount)));
                insertImport1Params.Add(new SqlParameter("@V_NO", vNo));
                insertImport1Params.Add(new SqlParameter("@V_TYPE", vType));

                _dbHelper.ExecuteNonQuery("sp_PurchaseBillPassEntryDirect", insertImport1Params, true);


                // ---------------------------------------------------------
                // 11. Get expense details
                // ---------------------------------------------------------
                string expenseQuery = @"SELECT REF_TYPE, REF_NO, COMP_CODE, EXPS_TYPE, ISNULL(SUM(NAMOUNT), 0) AS Amt, ISNULL(SUM(CGST_AMT + SGST_AMT + IGST_AMT), 0) AS TaxAmt, 
                                        ISNULL(SUM(TDS_PER), 0) AS TDS_PER, ISNULL(SUM(TDS_AMT), 0) AS TDS_AMT FROM PURCHASE1 
                                        WHERE REF_NO = @V_NO AND REF_TYPE = @V_TYPE AND V_TYPE IN ('RMDP', 'SIDP') AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE
                                        GROUP BY REF_TYPE, REF_NO, COMP_CODE, EXPS_TYPE";
                var expenseParameters = GetCommonParams();
                expenseParameters.Add(new SqlParameter("@V_NO", vNo));
                expenseParameters.Add(new SqlParameter("@V_TYPE", vType));
                DataTable expenseTable = await _dbHelper.ExecuteQueryAsync(expenseQuery, expenseParameters);


                // ---------------------------------------------------------
                // 12. Update IMPORT1 expense-wise
                // ---------------------------------------------------------

                foreach (DataRow row in expenseTable.Rows)
                {
                    string expType = row["EXPS_TYPE"]?.ToString();
                    decimal amount = row["Amt"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Amt"]);
                    decimal taxAmount = row["TaxAmt"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TaxAmt"]);
                    decimal tdsPer = row["TDS_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TDS_PER"]);
                    decimal tdsAmount = row["TDS_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(row["TDS_AMT"]);
                    string updateColumns = expType switch
                    {
                        "Custom Duty" => "CUSTOM = @AMOUNT, TAX_CUST = @TAX_AMOUNT",
                        "Shipping Line" => "SHIPPING = @AMOUNT, TAX_SHIP = @TAX_AMOUNT, TDS_PER1 = @TDS_PER, TDS_AMT1 = @TDS_AMOUNT",
                        "Container" => "CONT = @AMOUNT, TAX_CONT = @TAX_AMOUNT, TDS_PER2 = @TDS_PER, TDS_AMT2 = @TDS_AMOUNT",
                        "Clearing Agent" => "CLEARING = @AMOUNT, TAX_CLEA = @TAX_AMOUNT, TDS_PER = @TDS_PER, TDS_AMT = @TDS_AMOUNT",
                        "Freight" => "FREIGHT1 = @AMOUNT, TAX_FRT1 = @TAX_AMOUNT, TDS_PER3 = @TDS_PER, TDS_AMT3 = @TDS_AMOUNT",
                        "Detention" => "DETETATION = @AMOUNT, TAX_DET = @TAX_AMOUNT, TDS_PER4 = @TDS_PER, TDS_AMT4 = @TDS_AMOUNT",
                        "Clearing Exps" => "CLEXP = @AMOUNT, TAX_CLEX = @TAX_AMOUNT, CLTDS_PER = @TDS_PER, CLTDS_AMT = @TDS_AMOUNT",
                        "Lab Test" => "OTHEXPS = @AMOUNT, TAX_OTHEXPS = @TAX_AMOUNT, TDSPER_OTHEXPS = @TDS_PER, TDSAMT_OTHEXPS = @TDS_AMOUNT",

                        _ => null
                    };

                    if (string.IsNullOrWhiteSpace(updateColumns))
                        continue;

                    string updateImport1 = $@"UPDATE IMPORT1 SET {updateColumns} WHERE V_TYPE = @REF_TYPE AND V_NO = @REF_NO AND COMP_CODE = @REF_COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                                            AND YEAR_CODE = @YEAR_CODE";

                    _dbHelper.ExecuteNonQuery(updateImport1, new List<SqlParameter>
                    {
                        new SqlParameter("@AMOUNT", amount),
                        new SqlParameter("@TAX_AMOUNT", taxAmount),
                        new SqlParameter("@TDS_PER", tdsPer),
                        new SqlParameter("@TDS_AMOUNT", tdsAmount),
                        new SqlParameter("@REF_TYPE", row["REF_TYPE"]),
                        new SqlParameter("@REF_NO", row["REF_NO"]),
                        new SqlParameter("@REF_COMP_CODE", row["COMP_CODE"]),
                        new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                        new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
                    });
                }


                // ---------------------------------------------------------
                // 13. Update IMPORT1 AMOUNT from IMPORT2
                // ---------------------------------------------------------

                string updateAmount = @"UPDATE IMPORT1 SET AMOUNT = (SELECT SUM(AMOUNT) FROM IMPORT2 WHERE IMPORT2.V_NO = IMPORT1.V_NO AND IMPORT2.V_TYPE = IMPORT1.V_TYPE AND IMPORT2.COMP_CODE 
                                        = IMPORT1.COMP_CODE AND IMPORT2.BRANCH_CODE = IMPORT1.BRANCH_CODE AND IMPORT2.YEAR_CODE = IMPORT1.YEAR_CODE)
                                        WHERE IMPORT1.COMP_CODE = @COMP_CODE AND IMPORT1.BRANCH_CODE = @BRANCH_CODE AND IMPORT1.YEAR_CODE = @YEAR_CODE";

                var updateAmountParameter = GetCommonParams();
                _dbHelper.ExecuteNonQuery(updateAmount, updateAmountParameter);


                // ---------------------------------------------------------
                // 14. Update ORD_WASTE
                // ---------------------------------------------------------
                string updateOrdWaste = @"UPDATE IMPORT2 SET ORD_WASTE = SAUDA.WASTE_PER FROM IMPORT2, SAUDA WHERE SAUDA.V_TYPE = 'PAUD' AND IMPORT2.ORD_NO = SAUDA.V_NO AND IMPORT2.COMP_CODE = 
                                        SAUDA.COMP_CODE AND IMPORT2.BRANCH_CODE = SAUDA.BRANCH_CODE";
                _dbHelper.ExecuteNonQuery(updateOrdWaste);


                // ---------------------------------------------------------
                // 15. Calculate IMPORT1 NAMOUNT
                // ---------------------------------------------------------
                string updateNetAmount = @"UPDATE IMPORT1 SET NAMOUNT = ISNULL(AMOUNT, 0) + ISNULL(CUSTOM, 0) + ISNULL(SHIPPING, 0) + ISNULL(CONT, 0) + ISNULL(FREIGHT1, 0) + ISNULL(FREIGHT2, 0)
                                        + ISNULL(FREIGHT3, 0) + ISNULL(FREIGHT4, 0) + ISNULL(CLEARING, 0) + ISNULL(DETETATION, 0) + ISNULL(CLEXP, 0) + ISNULL(OTHEXPS, 0)";
                _dbHelper.ExecuteNonQuery(updateNetAmount);


                // ---------------------------------------------------------
                // 16. Delete TEMP_VOUCHER
                // ---------------------------------------------------------
                _dbHelper.ExecuteNonQuery("DELETE FROM TEMP_VOUCHER");


                // ---------------------------------------------------------
                // 17. Get PURCHASE1 expense/reference records
                // ---------------------------------------------------------
                string purchaseRefQuery = @"SELECT * FROM PURCHASE1 WHERE REF_TYPE = @V_TYPE AND REF_NO = @V_NO AND V_TYPE IN ('RMDP', 'SIDP') AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                                            AND YEAR_CODE = @YEAR_CODE";
                var purchaseRefParameters = GetCommonParams();
                purchaseRefParameters.Add(new SqlParameter("@V_NO", vNo));
                purchaseRefParameters.Add(new SqlParameter("@V_TYPE", vType));
                DataTable purchaseRefTable = await _dbHelper.ExecuteQueryAsync(purchaseRefQuery, purchaseRefParameters);


                // ---------------------------------------------------------
                // 18. Insert DR entries from original voucher into
                //     TEMP_VOUCHER
                // ---------------------------------------------------------
                string insertTempDr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, DR_AMT, SNO) 
                                        SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, AMT, SNO FROM LEDGER2
                                        WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND V_DATE BETWEEN @V_DATE AND @V_DATE AND DR_CODE IS NOT NULL
                                        AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                var insertTempDrParameter = GetCommonParams();
                insertTempDrParameter.Add(new SqlParameter("@V_NO", vNo));
                insertTempDrParameter.Add(new SqlParameter("@V_TYPE", vType));
                insertTempDrParameter.Add(new SqlParameter("@V_DATE", vDate.Date));
                _dbHelper.ExecuteNonQuery(insertTempDr, insertTempDrParameter);


                // ---------------------------------------------------------
                // 19. Insert CR entries from original voucher
                // ---------------------------------------------------------
                string insertTempCr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, CR_AMT, SNO)
                                        SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, CR_CODE, AMT, SNO FROM LEDGER2 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND V_DATE 
                                        BETWEEN @V_DATE AND @V_DATE AND CR_CODE IS NOT NULL AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var insertTempCrParameter = GetCommonParams();
                insertTempCrParameter.Add(new SqlParameter("@V_NO", vNo));
                insertTempCrParameter.Add(new SqlParameter("@V_TYPE", vType));
                insertTempCrParameter.Add(new SqlParameter("@V_DATE", vDate.Date));
                _dbHelper.ExecuteNonQuery(insertTempCr, insertTempCrParameter);


                // ---------------------------------------------------------
                // 20. Insert RIPL DR entries
                // ---------------------------------------------------------
                string insertRiplDr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, DR_AMT, SNO)
                                        SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, AMT, SNO FROM LEDGER2 WHERE V_TYPE = 'RIPL' AND V_NO = @PL_NO AND DR_CODE IS NOT NULL
                                        AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                var insertRiplDrParameter = GetCommonParams();
                insertRiplDrParameter.Add(new SqlParameter("@PL_NO", plNo));
                _dbHelper.ExecuteNonQuery(insertRiplDr, insertRiplDrParameter);

                // ---------------------------------------------------------
                // 21. Insert RIPL CR entries
                // ---------------------------------------------------------
                string insertRiplCr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, CR_AMT, SNO) 
                                        SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, CR_CODE, AMT, SNO FROM LEDGER2 
                                        WHERE V_TYPE = 'RIPL' AND V_NO = @PL_NO AND CR_CODE IS NOT NULL AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                var insertRiplCrParameter = GetCommonParams();
                insertRiplCrParameter.Add(new SqlParameter("@PL_NO", plNo));
                _dbHelper.ExecuteNonQuery(insertRiplCr, insertRiplCrParameter);


                // ---------------------------------------------------------
                // 22. Insert ledger entries for RMDP / SIDP references
                // ---------------------------------------------------------
                foreach (DataRow row in purchaseRefTable.Rows)
                {
                    string referenceType = row["V_TYPE"]?.ToString();
                    int referenceNo = Convert.ToInt32(row["V_NO"]);
                    DateTime referenceDate = Convert.ToDateTime(row["V_DATE"]);

                    string insertReferenceDr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, DR_AMT, SNO)
                                                SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, AMT, SNO FROM LEDGER2
                                                WHERE V_TYPE = @REF_TYPE AND V_NO = @REF_NO AND V_DATE BETWEEN @REF_DATE AND @REF_DATE AND DR_CODE IS NOT NULL
                                                AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                    var insertReferenceDrParameter = GetCommonParams();
                    insertReferenceDrParameter.Add(new SqlParameter("@REF_TYPE", referenceType));
                    insertReferenceDrParameter.Add(new SqlParameter("@REF_NO", referenceNo));
                    insertReferenceDrParameter.Add(new SqlParameter("@REF_DATE", referenceDate.Date));
                    _dbHelper.ExecuteNonQuery(insertReferenceDr, insertReferenceDrParameter);


                    // -----------------------------------------------------
                    // 23. CR entries for referenced vouchers
                    // -----------------------------------------------------

                    string insertReferenceCr = @"INSERT INTO TEMP_VOUCHER (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DR_CODE, CR_AMT, SNO ) 
                                                SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, CR_CODE, AMT, SNO FROM LEDGER2
                                                WHERE V_TYPE = @REF_TYPE AND V_NO = @REF_NO AND V_DATE BETWEEN @REF_DATE AND @REF_DATE AND CR_CODE IS NOT NULL
                                                AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    var insertReferenceCrParameter = GetCommonParams();
                    insertReferenceCrParameter.Add(new SqlParameter("@REF_TYPE", referenceType));
                    insertReferenceCrParameter.Add(new SqlParameter("@REF_NO", referenceNo));
                    insertReferenceCrParameter.Add(new SqlParameter("@REF_DATE", referenceDate.Date));
                    _dbHelper.ExecuteNonQuery(insertReferenceCr, insertReferenceCrParameter);
                }


                // ---------------------------------------------------------
                // Report functionality intentionally excluded.
                // ---------------------------------------------------------

                return true;
            }
            catch
            {
                throw;
            }
        }

        //Address
        public AddressDetails GetAddByParty(int code, int addressId)
        {
            var addressDetails = new AddressDetails();

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection connection = _dbConnection.GetErpConnection())
            {
                //using (SqlCommand cmd = new SqlCommand("Select top(1) ADD1,ADD2,ADD3,PINCODE,GSTIN,CITY_CODE from SUBGROUP_ADDRESS where COMP_CODE = @COMP_CODE AND Code = @PCODE", connection))
                using (SqlCommand cmd = new SqlCommand(
                    $@"Select a.Add1,a.Add2,a.Add3,a.GSTIN,a.City_Code,b.Name State,c.name City,a.Pincode,a.Distance , d.einv_party
                            from Subgroup_Address a left join STATE_MAST b on a.STATE_CODE=b.code left join CITY_MAST c on a.CITY_CODE=c.code
                            left join SUBGROUP_MAST d on d.CODE = a.code 
                            where a.comp_code=@COMP_CODE and a.Code=@CODE and a.Address_Id=@Address_Id",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@Address_Id", addressId);
                    connection.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            addressDetails.add1 = reader["ADD1"].ToString();
                            addressDetails.add2 = reader["ADD2"].ToString();
                            addressDetails.add3 = reader["ADD3"].ToString();
                            addressDetails.pincode = reader["PINCODE"].ToString();
                            addressDetails.gstin = reader["GSTIN"].ToString();
                            addressDetails.cityCode = reader["CITY_CODE"].ToString();
                            addressDetails.einv_party = reader["einv_party"] != DBNull.Value ? Convert.ToInt32(reader["einv_party"]) : 0;

                        }
                    }
                }
            }
            return addressDetails;
        }

        //Validate MRN
        public RepositoryResponseData<string> ValidateMRN(string mrnTypeNo, string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                //-------------------------------------------------------
                //Check Duplicate
                //-------------------------------------------------------

                string duplicateQuery = @"Select distinct V_TYPE+cast(V_NO as varchar) from PURCHASE2 where V_TYPE=@V_TYPE and V_NO<>@V_NO and REF_TYPE+cast(REF_NO as varchar)=@REF_TYPE_REF_NO 
                    and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";

                SqlCommand cmd = new SqlCommand(duplicateQuery, con);

                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@REF_TYPE_REF_NO", mrnTypeNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                var billPassNo = cmd.ExecuteScalar();

                if (billPassNo != null)
                {
                    return new RepositoryResponseData<string> { status = false, message = "MRN No exists in Purchase Bill Pass Entry No : " + billPassNo };
                }

                //-------------------------------------------------------
                //Check Approval
                //-------------------------------------------------------
                string MRNtype = mrnTypeNo.Substring(0, 4);
                string MRNo = mrnTypeNo.Substring(4);
                string approveQuery = @"SELECT FAPROV_STATUS FROM Purchase1 WHERE V_TYPE=@MRNType AND V_NO=@MRNNo AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                SqlCommand cmd2 = new SqlCommand(approveQuery, con);

                cmd2.Parameters.AddWithValue("@MRNType", MRNtype);
                cmd2.Parameters.AddWithValue("@MRNNo", MRNo);
                cmd2.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd2.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                var status = Convert.ToString(cmd2.ExecuteScalar());

                if (status != null && status.ToUpper() != "APPROVED")
                {
                    return new RepositoryResponseData<string> { status = false, message = "MRN No " + mrnTypeNo + " not approved." };
                }

                return new RepositoryResponseData<string> { status = true, data = MRNo };
            }
        }
        //Purchase header details by MRN change
        public async Task<PurchaseDetailsDto> GetPurchaseDetailsByMRN(string vType, int vNo)
        {
            PurchaseDetailsDto model = new PurchaseDetailsDto();

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // 1. Check Purchase Exists
                string purchaseSql = @"SELECT V_No FROM PURCHASE1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";
                var parameters = new Dictionary<string, object>
                {
                    {"@V_TYPE", vType },
                    {"@V_NO", vNo},
                    {"@COMP_CODE", gv.PubCompCode},
                    {"@BRANCH_CODE", gv.PubBranchCode},
                    {"@YEAR_CODE", gv.PubFYearCode}
                };
                model.V_No = await _dbHelper.GetExecuteScalarAsync<int>(purchaseSql, parameters);

                // 2. Deduction Details
                string deductionSql = @"SELECT DEDUCT_AMT, DEDUCT_NARR FROM QC1 WHERE MRN_TYPE=@V_TYPE AND MRN_NO=@V_NO";
                using (SqlCommand cmd = new SqlCommand(deductionSql, con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.DeductAmt = dr["DEDUCT_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DEDUCT_AMT"]);
                            model.DeductNarr = dr["DEDUCT_NARR"]?.ToString();
                        }
                    }
                }

                // 3. Purchase Header Details

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetPurchaseHeaderByMRN");
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.BILL_NO = dr["BILL_NO"]?.ToString();
                            model.BILL_DATE = dr["BILL_DATE"] as DateTime?;
                            model.CHALL_NO = dr["CHALL_NO"]?.ToString();
                            model.CHALL_DATE = dr["CHALL_DATE"] as DateTime?;
                            model.WAYBILL_NO = dr["WAYBILL_NO"]?.ToString();
                            model.TRANSIT_NO = dr["TRANSIT_NO"]?.ToString();
                            model.EXCH_RATE = dr["EXCH_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["EXCH_RATE"]);

                            model.PARTY_CODE = dr["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PARTY_CODE"]);
                            model.Party = dr["Party"]?.ToString();

                            model.BILL_ADD1 = dr["BILL_ADD1"]?.ToString();
                            model.BILL_ADD2 = dr["BILL_ADD2"]?.ToString();
                            model.BILL_ADD3 = dr["BILL_ADD3"]?.ToString();
                            model.BILL_CITY = dr["BILL_CITY"]?.ToString();
                            model.BILL_GST = dr["BILL_GST"]?.ToString();
                            model.BILL_PINCODE = dr["BILL_PINCODE"]?.ToString();
                            model.BILL_STATE = dr["BILL_STATE"]?.ToString();

                            model.SHIP_CODE = dr["SHIP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SHIP_CODE"]);
                            model.ShipTo = dr["ShipTo"]?.ToString();

                            model.SHIP_ADD1 = dr["SHIP_ADD1"]?.ToString();
                            model.SHIP_ADD2 = dr["SHIP_ADD2"]?.ToString();
                            model.SHIP_ADD3 = dr["SHIP_ADD3"]?.ToString();
                            model.SHIP_CITY = dr["SHIP_CITY"]?.ToString();
                            model.SHIP_GST = dr["SHIP_GST"]?.ToString();
                            model.SHIP_PINCODE = dr["SHIP_PINCODE"]?.ToString();
                            model.SHIP_STATE = dr["SHIP_STATE"]?.ToString();

                            model.REMARKS = dr["REMARKS"]?.ToString();
                            model.TRANSPORT_CODE = Convert.ToInt32(dr["TRANSPORT_CODE"]);
                            model.Transport = dr["Transport"]?.ToString();
                            model.TRANSPORT_NAME = dr["TRANSPORT_NAME"]?.ToString();
                            model.TRUCK_NO = dr["TRUCK_NO"]?.ToString();
                            model.CONTAINER_NO = dr["CONTAINER_NO"]?.ToString();
                            model.GR_NO = dr["GR_NO"]?.ToString();
                            model.GR_DATE = dr["GR_DATE"] as DateTime?;
                            model.FRTPAY_AMT = dr["FRTPAY_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_AMT"]);
                            model.FRTPAY_TAXPER = dr["FRTPAY_TAXPER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_TAXPER"]);
                            model.FRTPAY_TAX = dr["FRTPAY_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_TAX"]);
                            model.FRTPAY_NAR = dr["FRTPAY_NAR"]?.ToString();
                            model.HOLD_REASON = dr["HOLD_REASON"]?.ToString();
                            model.TCS_PER = dr["TCS_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["TCS_PER"]);
                            model.TCS_AMT = dr["TCS_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["TCS_AMT"]);
                            model.EWB_INVNO = dr["EWB_INVNO"]?.ToString();
                            model.EWB_DATE = dr["EWB_DATE"] as DateTime?;
                            model.EWB_EXPDATE = dr["EWB_EXPDATE"] as DateTime?;
                            model.EINV_PARTY = dr["Einv_Party"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Einv_Party"]);
                        }
                    }
                }
            }

            return model;
        }

        //Purchase items by MRN change
        public RepositoryResponseList<PurchaseItemDto> GetPurchaseItemsByMRN(string vType, int vNo)
        {
            List<PurchaseItemDto> items = new List<PurchaseItemDto>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetPurchaseItemByMRN");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                items.Add(new PurchaseItemDto
                                {
                                    ITEM_CODE = dr["ITEM_CODE"].ToString(),
                                    ITEM_NAME = dr["ITEM_NAME"].ToString(),
                                    Unit = dr["Unit"].ToString(),
                                    HSN_CODE = dr["HSN_CODE"].ToString(),
                                    NOS = dr["NOS"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["NOS"]),
                                    RECD_QTY = dr["RECD_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RECD_QTY"]),
                                    BILL_QTY = dr["BILL_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["BILL_QTY"]),
                                    USD_RATE = dr["USD_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["USD_RATE"]),
                                    EXCH_RATE = dr["EXCH_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["EXCH_RATE"]),
                                    RATE = dr["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RATE"]),
                                    PACK_PER = dr["PACK_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PACK_PER"]),
                                    PACK_AMT = dr["PACK_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PACK_AMT"]),
                                    DISC_PER = dr["DISC_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DISC_PER"]),
                                    DISC_AMT = dr["DISC_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DISC_AMT"]),
                                    TaxType = dr["TaxType"].ToString(),
                                    CGST_PER = dr["CGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["CGST_PER"]),
                                    SGST_PER = dr["SGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["SGST_PER"]),
                                    IGST_PER = dr["IGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["IGST_PER"]),
                                    VAT_PER = dr["VAT_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["VAT_PER"]),
                                    OTH_AMT = dr["OTH_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["OTH_AMT"]),
                                    PO_TYPE = dr["PO_TYPE"].ToString(),
                                    PO_NO = dr["PO_NO"].ToString(),
                                    REF_TYPE = dr["REF_TYPE"].ToString(),
                                    REF_NO = dr["REF_NO"] == DBNull.Value ? 0 : Convert.ToInt32(dr["REF_NO"]),
                                    REQ_TYPE = dr["REQ_TYPE"].ToString(),
                                    REQ_NO = dr["REQ_NO"].ToString(),
                                    KANTA_TYPE = dr["KANTA_TYPE"].ToString(),
                                    KANTA_NO = dr["KANTA_NO"].ToString(),
                                    Make = dr["Make"].ToString(),
                                    Department = dr["Department"].ToString(),
                                    DEPT_CODE = dr["DEPT_CODE"].ToString(),
                                    TAX_CODE = dr["TAX_CODE"].ToString(),
                                    MAKE_CODE = dr["MAKE_CODE"].ToString(),
                                    UOM_CODE = dr["UOM_CODE"].ToString()
                                });
                            }
                        }
                    }
                }

                return new RepositoryResponseList<PurchaseItemDto> { status = true, message = "Data fetched successfully.", data = items };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<PurchaseItemDto> { status = false, message = ex.Message };
            }
        }

        //Pack on basic
        public async Task<RepositoryResponseData<int>> GetPackOnBasic(int code)
        {
            int packOnBasic = 0;
            string query = @"SELECT PACK_ONBASIC FROM TAX_MAST WHERE code = @code AND Active = 1";
            var parameters = new Dictionary<string, object> { { "@CODE", code } };
            packOnBasic = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            return new RepositoryResponseData<int> { status = true, data = packOnBasic };
        }

        //HSN Code and Qty
        public async Task<RepositoryResponseData<(int HsnCode, decimal Qty)>> GetHsnCodeAndQty(int itemCode, string poType, int poNo)
        {
            int hsnCode = 0;
            decimal qty = 0;
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Get HSN_CODE
                string hsnQuery = @"SELECT ISNULL(HSN_CODE, 0) FROM ITEM_MAST WHERE Code = @ITEM_CODE AND Comp_code = @Comp_code";
                var hsnParameters = new Dictionary<string, object>
                    {
                        { "@ITEM_CODE", itemCode },
                        { "@COMP_CODE", gv.PubCompCode }
                    };
                hsnCode = await _dbHelper.GetExecuteScalarAsync<int>(hsnQuery, hsnParameters);

                // Get QTY
                string qtyQuery = @"SELECT ISNULL(QTY, 0) FROM ORDER2 WHERE Item_Code = @ITEM_CODE AND V_type = @V_type AND V_No = @V_No
                      AND Comp_code = @Comp_code AND Branch_code = @Branch_code";

                var qtyParameters = new Dictionary<string, object>
                    {
                        { "@ITEM_CODE", itemCode },
                        { "@V_TYPE", poType },
                        { "@V_NO", poNo },
                        { "@COMP_CODE", gv.PubCompCode },
                        { "@BRANCH_CODE", gv.PubBranchCode }
                    };
                qty = await _dbHelper.GetExecuteScalarAsync<decimal>(qtyQuery, qtyParameters);
            }

            return new RepositoryResponseData<(int HsnCode, decimal Qty)> { status = true, data = (hsnCode, qty) };
        }

        // Get Freight Credit Account by Transport Code
        public async Task<(int PartyCode, string PartyName)> GetFrtCrAcByTransCodeAsync(int transportCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // Get Party Code & Party Name
                string transportQuery = @"
                SELECT ISNULL(T.PARTY_CODE,0) AS PARTY_CODE, ISNULL(S.NAME,'') AS PARTY_NAME FROM TRANSPORT_MAST T LEFT JOIN SUBGROUP_MAST S ON T.PARTY_CODE = S.CODE AND T.COMP_CODE = S.COMP_CODE
                WHERE T.COMP_CODE = @COMP_CODE AND T.CODE = @CODE";

                int partyCode = 0;
                string partyName = "";

                using (SqlCommand cmd = new SqlCommand(transportQuery, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", transportCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            partyCode = reader["PARTY_CODE"] != DBNull.Value
                                ? Convert.ToInt32(reader["PARTY_CODE"])
                                : 0;

                            partyName = reader["PARTY_NAME"]?.ToString() ?? "";
                        }
                    }
                }
                return (partyCode, partyName);
            }

        }

        // Get Purchase Date by VType and VNo
        public async Task<RepositoryResponseData<DateTime>> GetPurchaseDate(string vType, int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT V_DATE FROM PURCHASE1 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
            var parameter = new Dictionary<string, object>
                                {
                                    { "@V_TYPE", vType },
                                    { "@V_NO", vNo },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@BRANCH_CODE", globalVar.PubBranchCode }
                                };
            DateTime purchaseDate = await _dbHelper.GetExecuteScalarAsync<DateTime>(query, parameter);
            return new RepositoryResponseData<DateTime> { status = true, data = purchaseDate };
        }
        // Get Purchase Amt
        public async Task<RepositoryResponseData<decimal>> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null,
            decimal? currentAmount = null)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query;
            var parameters = new Dictionary<string, object>
                                {
                                    { "@PARTY_CODE", partyCode },
                                    { "@V_TYPE", vType },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@YEAR_CODE", globalVar.PubFYearCode }
                                };

            if (vNo.HasValue && currentAmount.HasValue)
            {
                query = @"SELECT ISNULL(SUM(NAMOUNT), 0) + @CURRENT_AMOUNT FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE";

                parameters.Add("@V_NO", vNo.Value);
                parameters.Add("@CURRENT_AMOUNT", currentAmount.Value);
            }
            else
            {
                query = @"SELECT ISNULL(SUM(NAMOUNT), 0) FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE
                      AND YEAR_CODE = @YEAR_CODE";
            }

            decimal totalAmount = await _dbHelper.GetExecuteScalarAsync<decimal>(query, parameters);

            return new RepositoryResponseData<decimal> { status = true, data = totalAmount };
        }

        public async Task<RepositoryResponseData<string>> GetTDS206Apply(int partyCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT ISNULL(TDS_206APPLY, '') FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @PARTY_CODE";

            var parameters = new Dictionary<string, object>
                                {
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@PARTY_CODE", partyCode }
                                };

            string tds206Apply = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = tds206Apply };
        }
        // Get Posting Exist
        public async Task<RepositoryResponseData<bool>> IsPostingExist(string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT TOP 1 1 FROM POSTING_MAST WHERE V_TYPE = @V_TYPE AND DOC_TYPE = 'PURCHASE' AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
            var parameters = new Dictionary<string, object>
                                {
                                    { "@V_TYPE", vType },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@BRANCH_CODE", globalVar.PubBranchCode }
                                };

            int result = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            bool isPostingExist = result == 1;
            return new RepositoryResponseData<bool> { status = true, data = isPostingExist };
        }
        //Purchase PL DocId
        public async Task<RepositoryResponseData<string>> GetPLDocId(int vNo, int plNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @" SELECT TOP (1) Doc_Id FROM Purchase1 WHERE V_Type IN ( SELECT Code FROM Doctype_Mast WHERE Doctype = @DocType)
                        AND V_No <> @V_No AND PL_No = @PL_No AND Comp_Code = @COMP_CODE AND Branch_Code = @BRANCH_CODE AND Year_Code = @Year_Code";

            var parameters = new Dictionary<string, object>
                                 {
                                     { "@DocType", "Purchaseinvoice" },
                                     { "@V_No", vNo },
                                     { "@PL_No", plNo },
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@BRANCH_CODE", gv.PubBranchCode },
                                     { "@Year_Code", gv.PubFYearCode }
                                 };

            string docId = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = docId };
        }

        // Get Debit Account Type
        public async Task<RepositoryResponseData<string>> GetDebitAcType(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT isnull(gm.type,'') FROM GR_MAST gm  INNER JOIN MGROUP_MAST mg ON gm.code = mg.gr_code AND gm.comp_code = mg.comp_code 
                             INNER JOIN SUBGROUP_MAST sg ON mg.code = sg.group_code AND mg.comp_code = sg.comp_code WHERE sg.code =@code AND sg.comp_code = @COMP_CODE";
            var parameters = new Dictionary<string, object>
                                 {
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@Code", code }
                                 };

            string type = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = type };
        }

        //PO Type
        public async Task<RepositoryResponseData<string>> GetPOType(string poType, int poNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"Select isnull(POTYPE,'') from ORDER1 where V_TYPE=@V_TYPE and  V_NO=@V_NO and COMP_CODE=@COMP_CODE and Branch_Code=@Branch_Code";
            var parameters = new Dictionary<string, object>
                                 {
                                     { "@V_TYPE", poType },
                                     { "@V_NO", poNo },
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@Branch_Code", gv.PubBranchCode }
                                 };

            string type = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = type };
        }
        //Purchase or Sale Voucher No
        public async Task<RepositoryResponseData<string>> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = "";

            if (purchaseOrSale.Equals("PURCHASE", StringComparison.OrdinalIgnoreCase))
            {
                query += @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Purchase1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID AND Transport_Name = @Transport_Name AND GR_NO = @GR_NO 
                            AND V_Type NOT IN (SELECT Code FROM Doctype_Mast WHERE Doctype = 'MaterialReceipt' ) AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code 
                            AND Year_Code = @Year_Code";
            }
            else if (purchaseOrSale.Equals("SALE", StringComparison.OrdinalIgnoreCase))
            {
                query = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Sale1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID AND Transport_Name = @Transport_Name
                      AND GR_NO = @GR_NO AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code AND Year_Code = @Year_Code";
            }
            else
            {
                return new RepositoryResponseData<string> { status = false, message = "Invalid data." };
            }

            var parameters = new Dictionary<string, object>
                {
                    { "@DOCID", currentVoucher },
                    { "@Transport_Name", transportName },
                    { "@GR_NO", grNo },
                    { "@Comp_Code", gv.PubCompCode },
                    { "@Branch_Code", gv.PubBranchCode },
                    { "@Year_Code", gv.PubFYearCode }
                };

            string voucherNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

            return new RepositoryResponseData<string> { status = true, data = voucherNo };
        }

        //payment exists
        public async Task<RepositoryResponseData<bool>> CheckPaymentExists(string docType, int docNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT TOP 1 1 FROM LEDGER_OS WHERE V_TYPE = 'BPMT' AND DOC_TYPE = @V_TYPE AND DOC_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
            var parameters = new Dictionary<string, object>
                {
                    { "@V_TYPE", docType },
                    { "@V_NO", docNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };

            int result = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            bool exists = result == 1;
            return new RepositoryResponseData<bool> { status = true, data = exists };
        }

        //Duplicate Bill Check
        public async Task<(bool Exists, string DocId, DateTime? VDate)> CheckDuplicateBill(int partyCode, string billNo, int currentVNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT TOP 1 DOC_ID AS DocId, format(V_date, 'dd/MM/yyyy') AS VDate FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE
                                AND BILL_NO = @BILL_NO AND V_TYPE IN ('STPB','STDP','STJW','RMPB','BFPB','RIMP','RMDP','SIDP','SADP')
                                AND V_NO <> @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@PARTY_CODE", partyCode),
                    new SqlParameter("@BILL_NO", billNo),
                    new SqlParameter("@V_NO", currentVNo),
                    new SqlParameter("@COMP_CODE", gv.PubCompCode),
                    new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                    new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
                };

            DataTable dt = await _dbHelper.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count > 0)
            {
                string docId = dt.Rows[0]["DOC_ID"].ToString();
                DateTime? vDate = dt.Rows[0]["V_DATE"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(dt.Rows[0]["V_DATE"]);

                return (true, docId, vDate);
            }

            return (false, "", null);
        }

        public async Task<RepositoryResponseData<bool>> ValidateTaxType(int cityCode, decimal totalIGST, decimal totalCGST, decimal totalSGST)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT State_Code FROM CITY_MAST WHERE Code = @CITY_CODE";

            var parameters = new Dictionary<string, object>
                {
                    { "@CITY_CODE", cityCode }
                };

            int stateCode = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
            string stateType = gv.STATE_CODE == stateCode.ToString() ? "Local" : "Central/Other";

            if (gv.STATE_CODE == stateCode.ToString() && totalIGST > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = $"IGST not applicable as Party State type is {stateType}." };
            }
            if (gv.STATE_CODE != stateCode.ToString() && (totalCGST + totalSGST) > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = $"CGST/SGST not applicable as Party State type is {stateType}." };
            }
            if (totalIGST > 0 && (totalCGST + totalSGST) > 0)
            {
                return new RepositoryResponseData<bool> { status = true, data = false, message = "CGST + SGST + IGST all three types of tax are not applicable." };
            }

            return new RepositoryResponseData<bool> { status = true, data = true };
        }

        // Get CrDr Note Amount for Report
        public async Task<RepositoryResponseData<decimal>> GetCrDrNoteAmtForReport(string vType, int vNo, string drOrCr)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string qry = "";

            if (string.Equals(drOrCr, "Debit", StringComparison.OrdinalIgnoreCase))
            {
                qry += $@"select QLT_DR_AMT+QLT_DR_TAX+isnull(RDF_DR_AMT,0)+isnull(RDF_DR_TAX,0)+QTY_DR_AMT+QTY_DR_TAX+QC_DR_AMT+QC_DR_TAX+OTH_DR_AMT+OTH_DR_TAX-(QLT_CR_AMT+QLT_CR_TAX+
                                isnull(RDF_CR_AMT,0)+isnull(RDF_CR_TAX,0)+QTY_CR_AMT+QTY_CR_TAX+QC_CR_AMT+QC_CR_TAX) 
                                from Purchase1 where V_type='{vType}' and v_no={vNo} and Comp_code={gv.PubCompCode} and Branch_code={gv.PubBranchCode} and Year_Code={gv.PubFYearCode}";
            }
            else if (string.Equals(drOrCr, "Credit", StringComparison.OrdinalIgnoreCase))
            {
                qry += $@"select isnull(QLT_CR_AMT,0)+isnull(QLT_CR_TAX,0)+isnull(RDF_CR_AMT,0)+isnull(RDF_CR_TAX,0)+isnull(QTY_CR_AMT,0)+isnull(QTY_CR_TAX,0)+isnull(QC_CR_AMT,0)+
                                isnull(QC_CR_TAX,0) from Purchase1 where V_type='{vType}' and v_no={vNo} and Comp_code={gv.PubCompCode} and Branch_code={gv.PubBranchCode} and Year_Code={gv.PubFYearCode}";
            }

            var result = await _dbHelper.GetExecuteScalarAsync<decimal>(qry);
            return new RepositoryResponseData<decimal> { status = true, data = result };
        }
    }
}
