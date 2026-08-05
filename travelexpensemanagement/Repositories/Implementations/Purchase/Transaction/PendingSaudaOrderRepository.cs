using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Controllers.Purchase.Transaction.PendingSaudaOrderController;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PendingSaudaOrderRepository: IPendingSaudaOrderRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public PendingSaudaOrderRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService,LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _logService = logService;
        }
        public JsonResult GetddlDocType()
        {
            string query = $@"Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('Pendingsauda','PendingOrder','PendingDO','PendingGateOutward','PendingPR') order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return new JsonResult(moduleList);
        }
        public JsonResult GetdocNumber(string vType)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                string query = @"
                SELECT ISNULL(MAX(V_NO),0)+1
                FROM PENDING_ORDERSAUDA
                WHERE V_TYPE=@V_TYPE
                AND COMP_CODE=@CompCode
                AND BRANCH_CODE=@BranchCode
                AND YEAR_CODE=@YearCode";
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            nextVNo = Convert.ToInt32(result);
                        }
                    }
                }
                return new JsonResult(new
                {
                    success = true,
                    nextVNo,
                    vType
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        public JsonResult GetfilterType(string vType)
        {
            string query = "";

            switch (vType)
            {
                case "PNPO":
                case "PNRM":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='PurchaseOrder'
                      order by Name";
                    break;

                case "PNPU":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='PurchaseSauda'
                      order by Name";
                    break;

                case "PNSO":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='SalesOrder'
                      order by Name";
                    break;

                case "PNSU":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='Salessauda'
                      order by Name";
                    break;

                case "PNPR":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='PendingPR'
                      order by Name";
                    break;

                case "PNNR":
                    query = @"Select Code,Name
                      from DOCTYPE_MAST
                      where DOCTYPE='GateOutward'
                      order by Name";
                    break;
            }

            var list = _dropdownService.GetDropdownList(query);

            return new JsonResult(list);
        }

        [HttpGet]
        public JsonResult GetStatus()
        {
            string query = @"SELECT Code, Name FROM DOCSTATUS_MAST WHERE V_TYPE = 'Document' ORDER BY Code";
            var list = _dropdownService.GetDropdownList(query);
            return new JsonResult(list);
        }
        [HttpGet]
        public JsonResult GetPendingData(string vType, string refType, string status, string source, DateTime fromDate, DateTime toDate, string itemSearch)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "";
            List<object> list = new();
            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();
            //===================== PNSU UPDATE =====================
            if (vType == "PNSU")
            {
                string updateQuery = @"
                    UPDATE SAUDA SET SALE_QTY=0, SALE_TRUCK=0, ORD_QTY=0 WHERE V_TYPE='SAUD' AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode;
                    UPDATE SAUDA SET SALE_QTY=
                    (
                        SELECT SUM(QTY) FROM SALE2  LEFT JOIN DOCTYPE_MAST B ON SALE2.V_TYPE=B.CODE WHERE SALE2.SAUDA_TYPE=SAUDA.V_TYPE
                        AND SALE2.SAUDA_NO=SAUDA.V_NO AND SALE2.COMP_CODE=SAUDA.COMP_CODE AND SALE2.BRANCH_CODE=SAUDA.BRANCH_CODE
                        AND SALE2.STATUS<>2 AND B.DOCTYPE='SalesInvoice'
                    )
                    FROM SAUDA LEFT JOIN SALE2 ON SALE2.SAUDA_TYPE=SAUDA.V_TYPE   AND SALE2.SAUDA_NO=SAUDA.V_NO
                    AND SALE2.COMP_CODE=SAUDA.COMP_CODE AND SALE2.BRANCH_CODE=SAUDA.BRANCH_CODE
                    LEFT JOIN DOCTYPE_MAST B ON SALE2.V_TYPE=B.CODE WHERE SAUDA.COMP_CODE=@CompCode
                    AND SAUDA.BRANCH_CODE=@BranchCode AND SAUDA.V_TYPE='SAUD' AND SALE2.STATUS<>2 AND B.DOCTYPE='SalesInvoice';
                    
                    UPDATE SAUDA
                    SET ORD_QTY=
                    (
                        SELECT SUM(QTY) FROM ORDER2 WHERE ORDER2.SAUDA_TYPE=SAUDA.V_TYPE AND ORDER2.SAUDA_NO=SAUDA.V_NO  
                        AND ORDER2.COMP_CODE=SAUDA.COMP_CODE AND ORDER2.BRANCH_CODE=SAUDA.BRANCH_CODE
                    )
                    FROM SAUDA INNER JOIN ORDER2 ON ORDER2.SAUDA_TYPE=SAUDA.V_TYPE AND ORDER2.SAUDA_NO=SAUDA.V_NO AND ORDER2.COMP_CODE=SAUDA.COMP_CODE
                    AND ORDER2.BRANCH_CODE=SAUDA.BRANCH_CODE WHERE SAUDA.COMP_CODE=@CompCode  AND SAUDA.BRANCH_CODE=@BranchCode
                    AND SAUDA.V_TYPE='SAUD';";

                using SqlCommand updateCmd = new(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                updateCmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                updateCmd.ExecuteNonQuery();
            }

            //===================== PNPU / PNSU =====================
            if (vType == "PNPU" || vType == "PNSU")
            {
                query = @"
                    SELECT
                    A.V_TYPE RefType,
                    A.V_NO RefNo,
                    FORMAT(A.V_DATE,'dd/MM/yyyy') RefDate,
                    B.NAME Party,
                    A.PARTY_CODE PartyCode,
                    C.SHORTNAME Item,
                    A.ITEM_CODE ItemCode,
                    ISNULL(A.QTY,0) Qty,
                    ISNULL(A.RATE,0) Rate,
                    ISNULL(A.SALE_QTY,0) AdjQty,
                    ISNULL(A.QTY,0)-ISNULL(A.SALE_QTY,0) BalQty,
                    A.REMARK Remarks,
                    IIF(A.STATUS=1,'Open','Close') Status,
                    FORMAT(DATEADD(DAY,ISNULL(A.DELIVERY_DAYS,0),A.V_DATE),'dd/MM/yyyy') DeliveryDate,
                    '' ValidityDate
                    FROM SAUDA A
                    LEFT JOIN SUBGROUP_MAST B
                    ON A.PARTY_CODE=B.CODE
                    AND A.COMP_CODE=B.COMP_CODE
                    LEFT JOIN ITEM_MAST C
                    ON A.ITEM_CODE=C.CODE
                    AND A.COMP_CODE=C.COMP_CODE
                    LEFT JOIN CITY_MAST CM
                    ON A.CITY_CODE=CM.CODE
                    WHERE
                    A.V_TYPE=@RefType
                    AND A.COMP_CODE=@CompCode
                    AND A.BRANCH_CODE=@BranchCode
                    AND A.STATUS=@Status
                    AND A.V_DATE BETWEEN @FromDate AND @ToDate";

            }
            else if (vType == "PNNR")
            {
                query = @"
                    SELECT
                        A.V_TYPE RefType,
                        A.V_NO RefNo,
                        FORMAT(A.V_DATE,'dd/MM/yyyy') RefDate,
                        C.NAME Party,
                        C.Code as partyCode,
                        B.ITEM_NAME Item,
                        B.ITEM_CODE as itemCode,
                        ISNULL(B.QTY,0) Qty,
                        0 Rate,
                        ISNULL(B.ADJ_QTY,0) AdjQty,
                        ISNULL(B.QTY,0)-ISNULL(B.ADJ_QTY,0) BalQty,
                        A.REMARKS Remarks,
                        IIF(B.STATUS=1,'Open','Close') Status,
                        A.PARTY_CODE PartyCode,
                        B.ITEM_CODE ItemCode,
                        '' DeliveryDate,
                        '' ValidityDate
                    FROM GATE1 A
                    LEFT JOIN GATE2 B
                        ON A.V_TYPE=B.V_TYPE
                        AND A.V_NO=B.V_NO
                        AND A.COMP_CODE=B.COMP_CODE
                        AND A.BRANCH_CODE=B.BRANCH_CODE
                        AND A.YEAR_CODE=B.YEAR_CODE
                    LEFT JOIN SUBGROUP_MAST C
                        ON A.PARTY_CODE=C.CODE
                        AND A.COMP_CODE=C.COMP_CODE
                    LEFT JOIN CITY_MAST CM
                        ON C.CITY_CODE=CM.CODE
                    WHERE
                        A.V_TYPE=@RefType
                        AND A.COMP_CODE=@CompCode
                        AND A.BRANCH_CODE=@BranchCode
                        AND TRY_CONVERT(date,A.V_DATE,103) BETWEEN @FromDate AND @ToDate";

                if (!string.IsNullOrWhiteSpace(status))
                    query += " AND B.STATUS=@Status";

                if (!string.IsNullOrWhiteSpace(source))
                    query += source == "Domestic" ? " AND CM.Country_Code=1" : " AND CM.Country_Code<>1";

                if (!string.IsNullOrWhiteSpace(itemSearch))
                    query += " AND B.ITEM_NAME LIKE @Item";

                query += " ORDER BY A.V_NO";
            }

            else if (vType == "PNDO")
            {
                query = @"
                SELECT
                    A.V_TYPE RefType,
                    A.V_NO RefNo,
                    FORMAT(A.V_DATE,'dd/MM/yyyy') RefDate,
                    C.NAME Party,
                    C.Code as partyCode,
                    B.ITEM_NAME Item,
                    B.ITEM_CODE as itemCode,
                    ISNULL(B.QTY,0) Qty,
                    ISNULL(B.RATE,0) Rate,
                    ISNULL(B.ADJ_QTY,0) AdjQty,
                    ISNULL(B.QTY,0)-ISNULL(B.ADJ_QTY,0) BalQty,
                    A.REMARK Remark,
                    IIF(B.STATUS=1,'Open','Close') Status,
                    A.BILL_CODE PartyCode,
                    B.ITEM_CODE ItemCode,
                    '' DeliveryDate,
                    '' ValidityDate
                FROM DO1 A
                LEFT JOIN DO2 B
                    ON A.V_TYPE=B.V_TYPE
                    AND A.V_NO=B.V_NO
                    AND A.COMP_CODE=B.COMP_CODE
                    AND A.BRANCH_CODE=B.BRANCH_CODE
                    AND A.YEAR_CODE=B.YEAR_CODE
                LEFT JOIN SUBGROUP_MAST C
                    ON A.BILL_CODE=C.CODE
                    AND A.COMP_CODE=C.COMP_CODE
                LEFT JOIN CITY_MAST CM
                    ON C.CITY_CODE=CM.CODE
                WHERE
                    A.V_TYPE='DOGT'
                    AND A.COMP_CODE=@CompCode
                    AND A.BRANCH_CODE=@BranchCode
                    AND TRY_CONVERT(date,A.V_DATE,103) BETWEEN @FromDate AND @ToDate";

                if (!string.IsNullOrWhiteSpace(status))
                    query += " AND B.STATUS=@Status";

                if (!string.IsNullOrWhiteSpace(source))
                    query += source == "Domestic" ? " AND CM.Country_Code=1" : " AND CM.Country_Code<>1";

                if (!string.IsNullOrWhiteSpace(itemSearch))
                    query += " AND B.ITEM_NAME LIKE @Item";

                query += " ORDER BY A.V_NO";
            }
            else if (vType == "PNPR")
            {
                query = @"
                    SELECT
                        A.V_TYPE RefType,
                        A.V_NO RefNo,
                        FORMAT(A.V_DATE,'dd/MM/yyyy') RefDate,
                        C.NAME Party,
                        C.Code as partyCode,
                        D.NAME Item,
                        D.CODE as itemCode,
                        ISNULL(B.REQ_QTY,0) Qty,
                        ISNULL(B.RATE,0) Rate,
                        ISNULL(B.ADJ_QTY,0) AdjQty,
                        ISNULL(B.REQ_QTY,0)-ISNULL(B.ADJ_QTY,0) BalQty,
                        B.REMARKS Remarks,
                        IIF(B.STATUS=1,'Open','Close') Status,
                        C.CODE PartyCode,
                        B.ITEM_CODE ItemCode,
                        '' DeliveryDate,
                        '' ValidityDate
                    FROM PREQUEST1 A
                    LEFT JOIN PREQUEST2 B
                        ON A.V_TYPE=B.V_TYPE
                        AND A.V_NO=B.V_NO
                        AND A.COMP_CODE=B.COMP_CODE
                        AND A.BRANCH_CODE=B.BRANCH_CODE
                        AND A.YEAR_CODE=B.YEAR_CODE
                    LEFT JOIN ITEMDEPT_MAST C
                        ON A.DEPT_CODE=C.CODE
                        AND C.COMP_CODE=A.COMP_CODE
                    LEFT JOIN ITEM_MAST D
                        ON B.ITEM_CODE=D.CODE
                        AND D.COMP_CODE=B.COMP_CODE
                    WHERE
                        A.V_TYPE='STPI'
                        AND A.COMP_CODE=@CompCode
                        AND A.BRANCH_CODE=@BranchCode
                        AND TRY_CONVERT(date,A.V_DATE,103) BETWEEN @FromDate AND @ToDate";

                if (!string.IsNullOrWhiteSpace(status))
                    query += " AND B.STATUS=@Status";

                if (!string.IsNullOrWhiteSpace(itemSearch))
                    query += " AND D.NAME LIKE @Item";

                query += " ORDER BY A.V_NO";
            }
            else
            {
                query = @"
                SELECT
                    A.V_TYPE RefType,
                    A.V_NO RefNo,
                    FORMAT(A.V_DATE,'dd/MM/yyyy') RefDate,
                    C.NAME Party,
                    C.Code as partyCode,
                    B.ITEM_NAME Item,
                    B.ITEM_CODE as itemCode,
                    ISNULL(B.QTY,0) Qty,
                    ISNULL(B.RATE,0) Rate,
                    ISNULL(B.ADJ_QTY,0) AdjQty,
                    ISNULL(B.QTY,0)-ISNULL(B.ADJ_QTY,0) BalQty,
                    A.REMARKS Remarks,
                    IIF(B.STATUS=1,'Open','Close') Status,
                    A.PARTY_CODE PartyCode,
                    B.ITEM_CODE ItemCode,
                    FORMAT(A.DELIVERY_DATE,'dd/MM/yyyy') DeliveryDate,
                    FORMAT(A.VALIDITY_DATE,'dd/MM/yyyy') ValidityDate
                FROM ORDER1 A
                LEFT JOIN ORDER2 B
                    ON A.V_TYPE=B.V_TYPE
                    AND A.V_NO=B.V_NO
                    AND A.COMP_CODE=B.COMP_CODE
                    AND A.BRANCH_CODE=B.BRANCH_CODE
                    AND A.YEAR_CODE=B.YEAR_CODE
                LEFT JOIN SUBGROUP_MAST C
                    ON A.PARTY_CODE=C.CODE
                    AND A.COMP_CODE=C.COMP_CODE
                LEFT JOIN CITY_MAST CM
                    ON C.CITY_CODE=CM.CODE
                WHERE
                    A.V_TYPE=@RefType
                    AND A.COMP_CODE=@CompCode
                    AND A.BRANCH_CODE=@BranchCode
                    AND TRY_CONVERT(date,A.V_DATE,103) BETWEEN @FromDate AND @ToDate";
                
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query += " AND A.STATUS=@Status";
                    query += " AND B.STATUS=@Status";
                }

                if (!string.IsNullOrWhiteSpace(source))
                    query += source == "Domestic" ? " AND CM.Country_Code=1" : " AND CM.Country_Code<>1";

                if (!string.IsNullOrWhiteSpace(itemSearch))
                    query += " AND B.ITEM_NAME LIKE @Item";

                query += " ORDER BY A.V_NO";
            }
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@RefType", refType);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = fromDate.Date;
            cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = toDate.Date;
            if (!string.IsNullOrWhiteSpace(status))
            {
                cmd.Parameters.AddWithValue("@Status", status);
            }
            if (!string.IsNullOrWhiteSpace(itemSearch))
            {
                cmd.Parameters.AddWithValue("@Item", itemSearch.Trim() + "%");
            }
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new
                {
                    refType = dr["RefType"],
                    refNo = dr["RefNo"],
                    refDate = dr["RefDate"],
                    party = dr["Party"],
                    partyCode = dr["PartyCode"] == DBNull.Value ? "" : dr["PartyCode"],
                    item = dr["Item"],
                    itemCode = dr["ItemCode"] == DBNull.Value ? "" : dr["ItemCode"],
                    qty = dr["Qty"],
                    rate = dr["Rate"],
                    adjQty = dr["AdjQty"],
                    balQty = dr["BalQty"],
                    remarks = dr["Remarks"],
                    status = dr["Status"],
                    validityDate = dr["ValidityDate"],
                    deliveryDate = dr["DeliveryDate"]
                });
            }
            return new JsonResult(list);
        }
        [HttpPost]
        public IActionResult SaveData([FromBody] PendingSaudaOrderSaveModel request)
        {
            bool isApprovalBody = false;
            bool isFinalApprovalBody = false;
            string fappstatus = "";
            string fappRemark = "";
            if (request == null || request.Details == null || !request.Details.Any())
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No data received."
                });
            }
            // Convert request.VDate from string to DateTime
            if (!DateTime.TryParse(request.VDate, out DateTime parsedVDate))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid date format."
                });
            }
            var result = CheckValidDate("GATE1", request.VDate, request.VType, request.VNo);
            var gv = _globalVariableService.GetGlobalVariables();
            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();
            SqlTransaction tran = con.BeginTransaction();
            try
            {
                string approvalCheck = @" SELECT 1 FROM DOC_APPROSTAGE  WHERE USER_CODE=@USER_CODE
                AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE";
                using (SqlCommand cmd = new SqlCommand(approvalCheck, con, tran))
                {
                    cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@DOC_CODE", request.VType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    if (cmd.ExecuteScalar() != null)
                    {
                        isApprovalBody = true;
                    }
                }
                string finalApprovalQuery = @" SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE=@USER_CODE
                AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE";
                using (SqlCommand cmd = new SqlCommand(finalApprovalQuery, con, tran))
                {
                    cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@DOC_CODE", request.VType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    string approvUser = Convert.ToString(cmd.ExecuteScalar());
                    if (approvUser == "FINAL")
                    {
                        isFinalApprovalBody = true;
                    }
                }
                if (isFinalApprovalBody)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }
                // Validation
                if (request.Details.Any(x => (x.Status == "3" || x.Status == "Close") && string.IsNullOrWhiteSpace(x.NewRemarks)))
                {
                    tran.Rollback();
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Please enter reason for Close."
                    });
                }
                // Delete Old Data
                string deleteQuery = @"DELETE FROM PENDING_ORDERSAUDA WHERE COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE
                 AND YEAR_CODE=@YEAR_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO";

                using (SqlCommand cmd = new SqlCommand(deleteQuery, con, tran))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", request.VType);
                    cmd.Parameters.AddWithValue("@V_NO", request.VNo);
                    cmd.ExecuteNonQuery();
                }
                // Insert
                int sno = 1;
                string snoQuery = @" SELECT ISNULL(MAX(SNO),0) + 1 FROM PENDING_ORDERSAUDA WHERE COMP_CODE=@COMP_CODE
                      AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO";
                using (SqlCommand cmdSno = new SqlCommand(snoQuery, con, tran))
                {
                    cmdSno.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmdSno.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmdSno.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmdSno.Parameters.AddWithValue("@V_TYPE", request.VType);
                    cmdSno.Parameters.AddWithValue("@V_NO", request.VNo);
                    sno = Convert.ToInt32(cmdSno.ExecuteScalar());
                }

                foreach (var item in request.Details)
                {
                    string insertQuery = @"INSERT INTO PENDING_ORDERSAUDA
                    (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, REF_TYPE, REF_NO, REF_DATE,
                    PARTY_NAME, ITEM_NAME, QTY, RATE, ADJ_QTY, BAL_QTY, REMARKS,
                    ADD_QTY, STATUS, REASON,SNO, VALIDITY_DATE, DELIVERY_DATE, ITEM_CODE, PARTY_CODE, AED, WSID, LIP, LID)
                    VALUES
                    (@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @REF_TYPE, @REF_NO, @REF_DATE, @PARTY_NAME,
                    @ITEM_NAME, @QTY, @RATE, @ADJ_QTY, @BAL_QTY, @REMARKS, @ADD_QTY, @STATUS, @REASON,@SNO, @VALIDITY_DATE,
                    @DELIVERY_DATE, @ITEM_CODE, @PARTY_CODE, 'A', @WSID, @LIP, @LID)";

                    using SqlCommand cmd = new SqlCommand(insertQuery, con, tran);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", request.VType);
                    cmd.Parameters.AddWithValue("@V_NO", request.VNo);
                    cmd.Parameters.AddWithValue("@REF_TYPE", item.RefType);
                    cmd.Parameters.AddWithValue("@REF_NO", item.RefNo);
                    cmd.Parameters.AddWithValue("@REF_DATE", string.IsNullOrWhiteSpace(item.RefDate) ? DBNull.Value : DateTime.Parse(item.RefDate));
                    cmd.Parameters.AddWithValue("@PARTY_NAME", item.Party);
                    cmd.Parameters.AddWithValue("@ITEM_NAME", item.Item);
                    cmd.Parameters.AddWithValue("@QTY", item.Qty);
                    cmd.Parameters.AddWithValue("@RATE", item.Rate);
                    cmd.Parameters.AddWithValue("@ADJ_QTY", item.AdjQty);
                    cmd.Parameters.AddWithValue("@BAL_QTY", item.BalQty);
                    cmd.Parameters.AddWithValue("@REMARKS", item.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@ADD_QTY", item.NewQty);
                    cmd.Parameters.AddWithValue("@STATUS", item.Status == "Open" ? 1 : 3);
                    cmd.Parameters.AddWithValue("@REASON", item.NewRemarks ?? "");
                    cmd.Parameters.AddWithValue("@SNO", sno);
                    cmd.Parameters.AddWithValue("@VALIDITY_DATE", string.IsNullOrWhiteSpace(item.ValidityDate)
                        ? DBNull.Value : DateTime.Parse(item.ValidityDate));
                    cmd.Parameters.AddWithValue("@DELIVERY_DATE", string.IsNullOrWhiteSpace(item.DeliveryDate)
                        ? DBNull.Value : DateTime.Parse(item.DeliveryDate));
                    cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", item.PartyCode);
                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.ExecuteNonQuery();
                }
                string action = "INSERT";
                _logService.InsertLog("PENDING_ORDERSAUDA", "Pending Sauda Order", "TRANSACTION", action, request.VType, request.VNo.ToString(), DateTime.Parse(request.VDate));
                tran.Commit();
                if (isFinalApprovalBody)
                {
                    UpdateOrderSaudaStatus(request.VType, request.VNo, gv);
                }
                _logService.InsertLog("PENDING_ORDERSAUDA", "Pending Sauda Order", "TRANSACTION", action, request.VType, request.VNo.ToString(), DateTime.Parse(request.VDate));
                return new JsonResult(new
                {
                    success = true,
                    message = "Data saved successfully."
                });
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        private (bool Success, string Message) CheckValidDate(string tableName, string vDate, string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            // Convert string to DateTime
            if (!DateTime.TryParse(vDate, out DateTime docDate))
            {
                return (false, "Invalid document date.");
            }
            using SqlConnection con = _dbConnection.GetErpConnection();
            con.Open();

            // 1. Login Date / Today's Date
            if (docDate.Date > DateTime.Today)
                return (false, "Date can not be greater than Login date.");

            // 2. Previous Date Validation
            string sql = $@" SELECT MAX(V_DATE) FROM {tableName} WHERE V_TYPE=@VType AND V_NO<@VNo
                AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode AND YEAR_CODE=@YearCode";

            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@VType", vType);
                cmd.Parameters.AddWithValue("@VNo", vNo);
                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                object obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value)
                {
                    DateTime maxDate = Convert.ToDateTime(obj);
                    if (docDate.Date < maxDate.Date)
                        return (false, "Entry not allowed for previous date.");
                }
            }
            // 3. Next Date Validation
            sql = $@" SELECT TOP 1 DOC_ID + ' of dated : ' + CONVERT(varchar, V_DATE, 103) FROM {tableName}
                    WHERE V_TYPE=@VType AND V_NO>@VNo AND V_DATE<@VDate AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode
                    AND YEAR_CODE=@YearCode ORDER BY V_DATE";

            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@VType", vType);
                cmd.Parameters.AddWithValue("@VNo", vNo);
                cmd.Parameters.AddWithValue("@VDate", docDate);
                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                object obj = cmd.ExecuteScalar();

                if (obj != null && obj != DBNull.Value)
                    return (false, "Entry not allowed for next date. " + obj.ToString());
            }
            // 4. Financial Year Validation
            sql = @" SELECT COUNT(*) FROM YEAR_MAST WHERE @VDate BETWEEN START_DATE AND END_DATE AND CODE=@YearCode";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@VDate", docDate);
                cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                int cnt = Convert.ToInt32(cmd.ExecuteScalar());
                if (cnt == 0)
                    return (false, $"Date must be within Financial Year ({gv.PubFYearCode})");
            }
            // 5. Server Date Validation
            if (docDate.Date > DateTime.Today)
                return (false, "Doc Date can not be greater than Server Date.");

            return (true, "");
        }
        private void UpdateOrderSaudaStatus(string vType, int vNo, dynamic gv)
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();
                con.Open();
                DataTable dt = new DataTable();
                string selectQuery = @" SELECT * FROM PENDING_ORDERSAUDA WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO
                AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                using (SqlCommand cmd = new SqlCommand(selectQuery, con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                    return;
                foreach (DataRow row in dt.Rows)
                {
                    string refType = Convert.ToString(row["REF_TYPE"]);
                    int refNo = Convert.ToInt32(row["REF_NO"]);
                    int status = Convert.ToInt32(row["STATUS"]);
                    int itemCode = Convert.ToInt32(row["ITEM_CODE"]);
                    int addQty = Convert.ToInt32(row["ADD_QTY"]);
                    string reason = Convert.ToString(row["REASON"]);
                    // PNPO / PNSO / PNRM
                    if (vType == "PNPO" || vType == "PNSO" || vType == "PNRM")
                    {

                        ExecuteQuery($@" UPDATE Order2 SET STATUS={status} WHERE ITEM_CODE={itemCode}
                            AND V_TYPE='{refType}' AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode}
                            AND BRANCH_CODE={gv.PubBranchCode}");

                        bool orderClose = Convert.ToInt32(
                            ExecuteScalar($@"SELECT COUNT(*) FROM Order2 WHERE ISNULL(STATUS,0)=1
                            AND V_TYPE='{refType}' AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}")) == 0;

                        if (orderClose)
                        {
                            ExecuteQuery($@" UPDATE Order1 SET STATUS={status} WHERE V_TYPE='{refType}' AND V_NO={refNo}
                            AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                        }
                    }
                    // PNDO
                    else if (vType == "PNDO")
                    {
                        ExecuteQuery($@" UPDATE DO2 SET STATUS={status} WHERE ITEM_CODE={itemCode} AND V_TYPE='{refType}'
                        AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                        ExecuteQuery($@" UPDATE DO1 SET STATUS={status} WHERE V_TYPE='{refType}'
                        AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                    }
                    // PNNR
                    else if (vType == "PNNR")
                    {
                        ExecuteQuery($@" UPDATE Gate2 SET STATUS={status} WHERE ITEM_CODE={itemCode}
                        AND V_TYPE='{refType}' AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                        ExecuteQuery($@" UPDATE Gate1 SET STATUS={status}, Remarks2='{reason}' WHERE V_TYPE='{refType}'
                        AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                    }
                    // PNPR
                    else if (vType == "PNPR")
                    {
                        ExecuteQuery($@" UPDATE PREQUEST1 SET STATUS={status} WHERE V_TYPE='{refType}'
                        AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}
                        UPDATE PREQUEST2 SET STATUS={status} WHERE ITEM_CODE={itemCode} AND V_TYPE='{refType}
                        AND V_NO={refNo} AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                    }
                    // SAUDA
                    else
                    {
                        ExecuteQuery($@" UPDATE SAUDA SET STATUS={status}, QTY=QTY+{addQty}, REMARK=LTRIM(RTRIM(
                        CONCAT(ISNULL(REMARK,''),' {reason}'))) WHERE V_TYPE='{refType}'   AND V_NO={refNo}
                        AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
                    }
                }
                // Approval Close
                ExecuteQuery($@" UPDATE approval_status SET STATUS='CLOSE', CLOSE_DATE=GETDATE(), Approval_code=8,
                Approval_remark='Approved', remarks='Document Approved' WHERE V_TYPE='{vType}' AND V_NO={vNo} 
                AND COMP_CODE={gv.PubCompCode} AND BRANCH_CODE={gv.PubBranchCode}");
            }
            catch (Exception ex)
            {
                throw new Exception("UpdateOrderSaudaStatus Error : " + ex.Message);
            }
        }
        private void ExecuteQuery(string query)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private object ExecuteScalar(string query)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }
        public class PendingSaudaOrderSaveModel
        {
            public string VType { get; set; }
            public int VNo { get; set; }
            public string VDate { get; set; }
            public List<PendingSaudaOrderModel> Details { get; set; }
        }
        public class PendingSaudaOrderModel
        {
            public int? PartyCode { get; set; }
            public int? ItemCode { get; set; }
            public string? RefType { get; set; }
            public string? RefNo { get; set; }
            public string? RefDate { get; set; }
            public string? Party { get; set; }
            public string? Item { get; set; }
            public decimal? Qty { get; set; }
            public decimal? Rate { get; set; }
            public decimal? AdjQty { get; set; }
            public decimal? BalQty { get; set; }
            public string? Remarks { get; set; }
            public decimal? NewQty { get; set; }
            public string? Status { get; set; }
            public string? NewRemarks { get; set; }
            public string? ValidityDate { get; set; }
            public string? DeliveryDate { get; set; }
        }

    }
}
