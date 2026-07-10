using Azure;
using HarfBuzzSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseBillPassEntryRepository : IPurchaseBillPassEntryRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DbHelper _dbHelper;
        public PurchaseBillPassEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
        }

        public async Task<DebitNoteResponse> CalculateFrieghtPay(DebitNoteRequest request)
        {
            DebitNoteResponse response = new DebitNoteResponse();
            var generalSettings = await _globalVariableService.LoadGeneralSetting();

            DebitNoteCalculationState state = new DebitNoteCalculationState();
            var gv = _globalVariableService.GetGlobalVariables();

            response.txtFrtTaxVal = request.FreightAmountPay * request.FreightTaxPercent * 0.01m;
            state.frtAmt = 0;
            state.frtTax = 0;
            state.frtNarr = "";
            if(request.FreightAmountPay > 0 && request.FreightTax > 0)
            {
                var res = await _dbHelper.ExecuteScalarAsync($@"Select top 1 isnull(FREIGHT_AC,0) as FDrAc from POSTING_MAST where V_TYPE={request.VType} 
                                        and POST_TYPE={request.inputType} and COMP_CODE={gv.PubCompCode}");
                response.frtDrAcCode = int.TryParse(res, out int drAc) ? drAc : 0;
                string pubDefPOInMRN = (generalSettings.pubDefPOInMRN != null) ? generalSettings.pubDefPOInMRN : "";
                if(pubDefPOInMRN.Equals("YES", StringComparison.OrdinalIgnoreCase))
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

            pubDefPOInMRN = "yes"; //For Testing

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
                if(request.VType.Equals("STPB", StringComparison.OrdinalIgnoreCase))
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
        public bool IsValidVoucherType(string vType)
        {
            return vType == "RMPB" || vType == "BFPB" || vType == "STPB" || vType == "STJW";
        }

        //------------- Natural Rate ---------
        public decimal GetNaturalRate(List<DebitNoteItem> items)
        {
            foreach (var item in items)
            {
                if (item.ItemCode == 30001)
                {
                    return item.Amount;
                }
            }

            return 0;
        }

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
        public void CalculateQualityDifferenceNote(DebitNoteRequest request, DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state, DebitNoteResponse response)
        {
            var saudaDetails = GetSaudaDetails(item.PoType, item.PoNo);

            decimal qtyDiffQty = item.RecdQty - item.BillQty;
            decimal discItemRate = 0;

            Q15Result q15Result = new Q15Result();

            if (request.VType == "RMPB")
            {
                q15Result = CalculateQ15Difference(request, item, saudaDetails, taxPer, response);

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
        public Q15Result CalculateQ15Difference(DebitNoteRequest request, DebitNoteItem item, (decimal rate, int itemCode) saudaDetails, decimal taxPer, DebitNoteResponse response)
        {
            Q15Result result = new Q15Result();
            var gv = _globalVariableService.GetGlobalVariables();

            decimal discItemRate = 0;
            decimal abovePer = 0;
            decimal aboveRate = 0;

            string saudaReq = GetSaudaReqByItem(item.ItemCode);

            if (saudaReq.Equals("YES", StringComparison.OrdinalIgnoreCase))
            {
                var rmDiscount = GetRMDiscountDetails(saudaDetails.itemCode, item.ItemCode, request.vDate);

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
                decimal sItemQty = 0;

                foreach (var row in request.Items)
                {
                    if (row.ItemCode == saudaDetails.itemCode)
                    {
                        sItemQty += row.RecdQty;
                    }
                }

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
            var narrations = new List<string>();

            if (!string.IsNullOrWhiteSpace(anr1))
                narrations.Add(anr1);

            if (!string.IsNullOrWhiteSpace(anr2))
                narrations.Add(anr2);

            if (!string.IsNullOrWhiteSpace(anr3))
                narrations.Add(anr3);

            result.Narration = string.Join(", ", narrations);
            //result.Narration = anr3 + ", " + anr2 + ", " + anr1;

            return result;
        }

        //------------- Quality Difference Without PO ---------
        public void CalculateQualityDifferenceWithoutPO(DebitNoteRequest request, DebitNoteItem item, decimal taxPer, DebitNoteCalculationState state, DebitNoteResponse response)
        {
            item.POLandRate = GetApprovedPOLandRate(item.PoType, item.PoNo);

            decimal discItemRate = 0;

            if (request.VType == "RMPB")
            {
                var discount = GetDiscountRate(request.billToPartyCode, item.ItemCode);

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

            qtyDiffAmt = Math.Round( qtyDiffAmt - qtyDiffTax, MidpointRounding.AwayFromZero);

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
            if(request.VType != "RIMP" && request.mrnNo >= 0)
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
        private void CalculateExcessQtyRateDifference(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!request.VType.Equals("RMPB", StringComparison.OrdinalIgnoreCase))
                return;

            decimal taxPer = GetTaxPer(request);

            int saudaNo = GetSaudaNo(request.VNo);

            if (saudaNo <= 0)
                return;

            decimal totalPurchaseQty = GetTotalPurchaseQty(saudaNo, request.VNo);

            var sauda = GetSaudaInfo(saudaNo);

            if (sauda == null)
                return;

            decimal toleranceQty = GetToleranceQty(sauda.Qty);

            decimal currentQty = totalPurchaseQty + request.totalRcvdQty;

            decimal diffQty = 0;
            decimal diffRate = 0;

            if ((sauda.Qty + toleranceQty) < currentQty )
            {
                diffQty = Math.Abs((sauda.Qty + toleranceQty) - currentQty);
                decimal marketRate = GetMarketRate(sauda.VDate, sauda.ItemCode);
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
        private void CalculateOtherBottleDeduction(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!request.VType.Equals("RMPB", StringComparison.OrdinalIgnoreCase))
                return;

            int saudaNo = GetSaudaNo(request.VNo);

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
        private void ApplyPackingTolerance(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            decimal rateDebit = state.RateDiffDrGAmt + state.RateDiffDrGTax;

            if (rateDebit <= 0)
                return;

            if (request.totalPackingAmt <= 0)
                return;

            decimal packAmtPO = GetPOPackingAmount(
                request.Items[0].PoType,
                request.Items[0].PoNo);

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
        private void ApplyDebitNoteHold(DebitNoteRequest request, DebitNoteCalculationState state)
        {
            if (!IsDebitNoteHoldParty(request.billToPartyCode))
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
        private OrderRateDetailsDto GetItemOrderRatesByPO(string poType, int poNo, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var rateDetails = new OrderRateDetailsDto();

            string query = @"
                SELECT LAND_RATE, RATE, ISNULL(QTY,0) as QTY
                FROM ORDER2
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND ITEM_CODE = @ITEM_CODE";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
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

            string query = @"
                SELECT TOP 1
                    b.RATE,
                    b.ITEM_CODE
                FROM ORDER2 a
                LEFT JOIN SAUDA b
                    ON a.SAUDA_TYPE = b.V_TYPE
                   AND a.SAUDA_NO = b.V_NO
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                WHERE a.V_TYPE = @V_TYPE
                  AND a.V_NO = @V_NO
                  AND a.COMP_CODE = @COMP_CODE
                  AND a.BRANCH_CODE = @BRANCH_CODE";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@V_TYPE", poType);
                cmd.Parameters.AddWithValue("@V_NO", poNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        rate = dr["RATE"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(dr["RATE"]);

                        itemCode = dr["ITEM_CODE"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(dr["ITEM_CODE"]);
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

            string query = @"
                SELECT ISNULL(SAUDA_REQ, '')
                FROM ITEM_GROUP
                WHERE CODE = (
                    SELECT GROUP_CODE
                    FROM ITEM_MAST
                    WHERE CODE = @ITEM_CODE
                      AND COMP_CODE = @COMP_CODE
                )
                AND COMP_CODE = @COMP_CODE";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
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
        private RMDiscountDetails GetRMDiscountDetails(int saudaItemCode, int itemCode, DateTime voucherDate)
        {
            try
            {
                var RMDiscDetails = new RMDiscountDetails();
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    //=====================================================
                    // Query 1 : Check SAUDA_ITEM exists
                    //=====================================================
                    string query1 = @"
                    SELECT TOP 1 1
                    FROM RMDISC_MAST
                    WHERE SAUDA_ITEM = @SAUDA_ITEM
                      AND COMP_CODE = @COMP_CODE";

                    using (SqlCommand cmd = new SqlCommand(query1, con))
                    {
                        cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        RMDiscDetails.SaudaExists = cmd.ExecuteScalar() != null;
                    }
                    if (RMDiscDetails.SaudaExists)
                    {
                        //=====================================================
                        // Query 2 : Get SAUDA_ITEM discount rate
                        //=====================================================
                        string query2 = @"
                        select top 1 isnull(RATE,0) from RMDISC_MAST where SAUDA_ITEM=@SAUDA_ITEM and item_code=@ITEM_CODE and COMP_CODE=@COMP_CODE";

                        using (SqlCommand cmd = new SqlCommand(query2, con))
                        {
                            cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            var result = cmd.ExecuteScalar();
                            RMDiscDetails.DiscRate = Convert.ToDecimal(result);
                        }

                        //=====================================================
                        // Query 3 : Get Discount Details
                        //=====================================================
                        string query3 = @"
                        SELECT TOP 1
                            ISNULL(RATE,0) AS RATE,
                            ISNULL(ABOVE_PER,0) AS ABOVE_PER,
                            ISNULL(ABOVE_AMT,0) AS ABOVE_AMT
                        FROM RMDISC_MAST
                        WHERE ITEM_CODE = @ITEM_CODE
                          AND SAUDA_ITEM = @SAUDA_ITEM
                          AND COMP_CODE = @COMP_CODE
                          AND EFF_DATE < @EFF_DATE
                        ORDER BY EFF_DATE DESC";

                        using (SqlCommand cmd = new SqlCommand(query3, con))
                        {
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@EFF_DATE", voucherDate.Date);

                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    RMDiscDetails.Rate = dr["RATE"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["RATE"])
                                        : 0;

                                    RMDiscDetails.AbovePer = dr["ABOVE_PER"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["ABOVE_PER"])
                                        : 0;

                                    RMDiscDetails.AboveAmt = dr["ABOVE_AMT"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["ABOVE_AMT"])
                                        : 0;
                                }
                            }
                        }
                    }

                }
                return RMDiscDetails;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //-------------- Approved Land Rate ----------------
        private decimal GetApprovedPOLandRate(string poType, int poNo)
        {
            decimal rate = 0;

            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT TOP 1 ISNULL(RATE, 0)
            FROM ORDER2
            WHERE V_TYPE = @V_TYPE
              AND V_NO = @V_NO
              AND COMP_CODE = @COMP_CODE
              AND BRANCH_CODE = @BRANCH_CODE
              AND FAPROV_STATUS = 'Approved'";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@V_TYPE", poType);
                cmd.Parameters.AddWithValue("@V_NO", poNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                con.Open();

                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    rate = Convert.ToDecimal(result);
                }
            }

            return rate;
        }

        //-------------- Discount Rate ----------------
        private (bool exists, decimal discountRate) GetDiscountRate(int supplierCode, int itemCode)
        {
            decimal discountRate = 0;
            bool exists = false;

            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"
                SELECT TOP 1 ISNULL(ITEM_DIFF, 0)
                FROM DISC_MAST
                WHERE CODE = @CODE
                  AND ITEM_CODE = @ITEM_CODE
                  AND COMP_CODE = @COMP_CODE";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@CODE", supplierCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                con.Open();

                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    exists = true;
                    discountRate = Convert.ToDecimal(result);
                }
            }

            return (exists, discountRate);
        }

        //-------------- QC Details ----------------
        private QCDetailsDto GetQCDetails(string mrnType, int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            QCDetailsDto result = new QCDetailsDto();

            string query = @"
            SELECT
                ISNULL(DEDUCT_AMT,0) - ISNULL(ALLOW_AMT,0) AS DEDUCT_AMT,
                CONCAT(ISNULL(DEDUCT_NARR,''), ISNULL(ALLOW_NARR,'')) AS NARRATION
            FROM QC1
            WHERE MRN_TYPE = @MRN_TYPE
              AND MRN_NO = @MRN_NO
              AND COMP_CODE = @COMP_CODE
              AND BRANCH_CODE = @BRANCH_CODE
              AND YEAR_CODE = @YEAR_CODE";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@MRN_TYPE", mrnType);
            cmd.Parameters.AddWithValue("@MRN_NO", mrnNo);
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
        private int GetSaudaNo(int VNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
            SELECT TOP 1 SAUDA_NO
            FROM ORDER1
            WHERE COMP_CODE=@COMP_CODE
              AND V_TYPE='RORD'
              AND V_NO IN
              (
                    SELECT PO_NO
                    FROM PURCHASE2
                    WHERE COMP_CODE=@COMP_CODE
                      AND V_TYPE='RMPB'
                      AND V_NO=@V_NO
              )";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@V_NO", VNo);

            con.Open();

            object result = cmd.ExecuteScalar();

            return result == DBNull.Value || result == null
                ? 0
                : Convert.ToInt32(result);
        }

        //-------------- Total Purchase Qty ----------------
        private decimal GetTotalPurchaseQty(int saudaNo, int VNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
                SELECT ISNULL(SUM(RECD_QTY),0)
                FROM PURCHASE2
                WHERE V_TYPE='RMPB'
                  AND V_NO<>@V_NO
                  AND PO_NO IN
                  (
                        SELECT V_NO
                        FROM ORDER1
                        WHERE V_TYPE='RORD'
                          AND SAUDA_NO=@SAUDA_NO
                          AND COMP_CODE=@COMP_CODE
                  )";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@V_NO", VNo);
            cmd.Parameters.AddWithValue("@SAUDA_NO", saudaNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

            con.Open();

            object result = cmd.ExecuteScalar();

            return result == DBNull.Value || result == null
                ? 0
                : Convert.ToDecimal(result);
        }

        //-------------- Sauda Details ----------------
        private SaudaInfo GetSaudaInfo(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
                SELECT ITEM_CODE,
                       QTY,
                       RATE,
                       V_DATE
                FROM SAUDA
                WHERE COMP_CODE=@COMP_CODE
                  AND V_TYPE='PAUD'
                  AND V_NO=@V_NO";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@V_NO", saudaNo);

            con.Open();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (!dr.Read())
                return null;

            return new SaudaInfo
            {
                ItemCode = Convert.ToInt32(dr["ITEM_CODE"]),
                Qty = Convert.ToDecimal(dr["QTY"]),
                Rate = Convert.ToDecimal(dr["RATE"]),
                VDate = Convert.ToDateTime(dr["V_DATE"])
            };
        }

        //-------------- Tolerance Qty ----------------
        private decimal GetToleranceQty(decimal saudaQty)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
                SELECT TOP 1 TOLRANCE_QTY
                FROM TOLRANCE_MAST
                WHERE COMP_CODE=@COMP_CODE
                  AND V_TYPE='RMPB'
                  AND QTY>=@QTY
                ORDER BY QTY";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@QTY", saudaQty);

            con.Open();

            object result = cmd.ExecuteScalar();

            return result == DBNull.Value || result == null
                ? 0
                : Convert.ToDecimal(result);
        }

        //-------------- Market Rate Details ----------------
        private decimal GetMarketRate(DateTime saudaDate, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
                SELECT TOP 1 B.MAX_RATE
                FROM MARKET_RATE1 A
                INNER JOIN MARKET_RATE2 B
                    ON A.V_TYPE=B.V_TYPE
                   AND A.V_NO=B.V_NO
                   AND A.COMP_CODE=B.COMP_CODE
                   AND A.BRANCH_CODE=B.BRANCH_CODE
                   AND A.YEAR_CODE=B.YEAR_CODE
                WHERE A.COMP_CODE=@COMP_CODE
                  AND A.FAPROV_STATUS='Approved'
                  AND @SAUDA_DATE BETWEEN A.EFF_DATE AND A.EXP_DATE
                  AND B.ITEM_CODE=@ITEM_CODE
                ORDER BY A.V_DATE DESC,A.V_NO DESC";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@SAUDA_DATE", saudaDate);
            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

            con.Open();

            object result = cmd.ExecuteScalar();

            return result == DBNull.Value || result == null
                ? 0
                : Convert.ToDecimal(result);
        }

        //-------------- Market Rate Details ----------------
        private NaturalBottleDto? GetNaturalBottleDetails(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"
                SELECT
                    ISNULL(ONLY_NATURAL, 0) AS ONLY_NATURAL,
                    ITEM_CODE
                FROM SAUDA
                WHERE V_TYPE = 'PAUD'
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@V_NO", saudaNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

            con.OpenAsync();

            using SqlDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new NaturalBottleDto
                {
                    OnlyNatural = dr["ONLY_NATURAL"] != DBNull.Value &&
                                  Convert.ToInt32(dr["ONLY_NATURAL"]) == 1,

                    ItemCode = dr["ITEM_CODE"] != DBNull.Value
                        ? Convert.ToInt32(dr["ITEM_CODE"])
                        : 0
                };
            }

            return null;
        }

        private decimal GetPOPackingAmount(string poType, int poNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            decimal packAmt = 0;

            string query = @"
            SELECT ISNULL(SUM(PACK_AMT), 0)
            FROM ORDER2
            WHERE V_TYPE = @V_TYPE
              AND V_NO = @V_NO
              AND ISNULL(PACK_AMT, 0) > 0
              AND COMP_CODE = @COMP_CODE
              AND BRANCH_CODE = @BRANCH_CODE
              AND YEAR_CODE = @YEAR_CODE";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@V_TYPE", poType);
            cmd.Parameters.AddWithValue("@V_NO", poNo);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

            con.Open();

            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                packAmt = Convert.ToDecimal(result);
            }

            return packAmt;
        }

        private bool IsDebitNoteHoldParty(int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            const string query = @"
            SELECT 1
            FROM DEBITNOTEHOLD_MAST
            WHERE PARTY_CODE = @PARTY_CODE
              AND COMP_CODE = @COMP_CODE";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

            con.Open();

            return cmd.ExecuteScalar() != null;
        }

        private decimal GetTaxPer(DebitNoteRequest request)
        {
            if (request.Items.Count > 0)
            {
                var firstItem = request.Items[0];

                if (firstItem.CGSTPer > 0)
                {
                    return firstItem.CGSTPer + firstItem.SGSTPer;
                }
                else if (firstItem.IGSTPer > 0)
                {
                    return firstItem.IGSTPer;
                }
            }
            return 0;
        }
        
        private decimal FormatAmount(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
        
        private decimal FormatRate(decimal value)
        {
            return Math.Round(value, 4, MidpointRounding.AwayFromZero);
        }

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

    }
}
