using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Spire.Doc.Documents;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Common.DropdownService
{
    public class DropdownService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly IMemoryCache _memoryCache;
        public DropdownService(DataBaseConnection dbConnection, IMemoryCache memoryCache)
        {
            _dbConnection = dbConnection;
            _memoryCache = memoryCache;
        }
        public List<object> GetDropdownList(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(), 
                        Text = reader[1].ToString()   
                    });
                }
            }
            return dropdownItems;   
        }
        public List<object> GetDropdownListcon(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString()
                    });
                }
            }
            return dropdownItems;
        }
        public List<object> GetDropdownListERP(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString()
                    });
                }
            }
            return dropdownItems;
        }
        public List<object> GetEmpdataList(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString(),
                        Dept_id = reader[2].ToString(),
                        Desg_id = reader[3].ToString()
                    });
                }
            }
            return dropdownItems;

        }
        public List<object> GetEmpReasonList(string query)
        {
            List<object> dropdownItems = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString(),
                        deductType = reader[2].ToString()
                    });
                }
            }
            return dropdownItems;

        }
        public List<DropdownModel> GetMultipleDropdownList(string commandText, CommandType commandType, Dictionary<string, object> parameters = null)
        {
            List<DropdownModel> dropdownItems = new List<DropdownModel>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(commandText, con))
                {
                    cmd.CommandType = commandType;

                    //  Add parameters
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dropdownItems.Add(new DropdownModel
                            {
                                Value = reader[0].ToString(),
                                Text = reader[1].ToString()
                            });
                        }
                    }
                }
            }
            return dropdownItems;
        }
        public class DropdownModel
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }
        private List<DropdownModel> ExecuteDropdown(string query, SqlParameter[] parameters = null)
        {
            var list = new List<DropdownModel>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new DropdownModel
                            {
                                Value = rdr["Value"].ToString(),
                                Text = rdr["Text"].ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
        public List<DropdownModel> GetDocType()
        {
            string query = @"SELECT Code AS Value, Name AS Text FROM DOCTYPE_MAST WHERE CODE IN ('CTIN','CTOT') ORDER BY Name";
            return ExecuteDropdown(query);
        }
        // City
        //    public List<DropdownModel> GetCity(string compCode, string term)
        //    {
        //        string query = @"
        //    SELECT TOP (50)
        //           Code AS Value,
        //           Name AS Text
        //    FROM City_MAST
        //    WHERE (@Term = '' OR Name LIKE '%' + @Term + '%')
        //    ORDER BY Name";

        //        return ExecuteDropdown(query, new[]
        //        {
        //    new SqlParameter("@Term", term ?? "")
        //});
        //    }

        public List<DropdownModel> GetCity(string compCode, string term)
        {
            string query = @" SELECT TOP (50) Value, Text  FROM ( SELECT Code AS Value, Name AS Text
            FROM City_MAST
            UNION
            SELECT City_Code AS Value,  City_Name AS Text FROM COURIER_TRACKING WHERE Comp_code = @CompCode
        ) X WHERE (@Term = '' OR Text LIKE '%' + @Term + '%')  ORDER BY Text";

            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode),
                new SqlParameter("@Term", term ?? "")
            });
        }


        // Party


        public List<DropdownModel> SearchParty(string compCode, string term)
        {
            string query = @"
SELECT TOP (50)
       Value,
       Text
FROM
(
    SELECT Code AS Value,
           Name AS Text
    FROM SUBGROUP_MAST
    WHERE Comp_code=@CompCode
      AND Nature NOT IN ('CASH','BANK','OTHERS')

    UNION

    SELECT Party_Code,
           Party_Name
    FROM COURIER_TRACKING
    WHERE Comp_code=@CompCode
) X
WHERE (@Term='' OR Text LIKE @Term + '%')
ORDER BY Text";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@CompCode", compCode),
        new SqlParameter("@Term", term ?? "")
    });
        }

        //public List<DropdownModel> GetCourier(string term)
        //{
        //    string query = @"SELECT DISTINCT COURIER_NAME AS Value, COURIER_NAME AS Text 
        //                     FROM COURIER_TRACKING 
        //                     WHERE COURIER_NAME IS NOT NULL AND COURIER_NAME <> '' 
        //                     ORDER BY COURIER_NAME";

        //    return ExecuteDropdown(query);
        //}

        public List<DropdownModel> GetCourier(string term)
        {
            string query = @"
        SELECT DISTINCT
               COURIER_NAME AS Value,
               COURIER_NAME AS Text
        FROM COURIER_TRACKING
        WHERE COURIER_NAME IS NOT NULL
          AND COURIER_NAME <> ''
          AND (@Term = '' OR COURIER_NAME LIKE '%' + @Term + '%')
        ORDER BY COURIER_NAME";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@Term", term ?? "")
    });
        }


        public List<DropdownModel> GetTransportName(string compCode, string term)
        {
            string query = @"SELECT TOP (50) Code AS Value,  Name AS Text FROM TRANSPORT_MAST
            WHERE COMP_CODE = @CompCode  AND (@Term = '' OR Name LIKE @Term + '%') ORDER BY Name ASC";

            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode),
                new SqlParameter("@Term", term ?? "")
            });
        }





        // Purpose
        //public List<DropdownModel> GetPurpose(string compCode, string term)
        //{
        //    string query = @"SELECT DISTINCT Purpose AS Value, Purpose AS Text 
        //                     FROM COURIER_TRACKING WHERE Purpose <> '' ORDER BY Purpose";

        //    return ExecuteDropdown(query);
        //}

        public List<DropdownModel> GetPurpose(string compCode, string term)
        {
            string query = @"
        SELECT DISTINCT
               Purpose AS Value,
               Purpose AS Text
        FROM COURIER_TRACKING
        WHERE Purpose <> ''
          AND (@Term = '' OR Purpose LIKE '%' + @Term + '%')
        ORDER BY Purpose";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@Term", term ?? "")
    });
        }

        // Employee
        //public List<DropdownModel> GetEmployee(string compCode)
        //{
        //    string query = @"SELECT Code AS Value, CAST(Code AS VARCHAR(20)) + ' | ' + Name AS Text FROM EMP_MAST WHERE RESIGN_DATE IS NULL 
        //                     AND Comp_code = @CompCode ORDER BY Name";

        //    return ExecuteDropdown(query, new[]
        //    {
        //        new SqlParameter("@CompCode", compCode)
        //    });
        //}

        public List<DropdownModel> GetEmployee(string compCode)
        {
            string query = @"
        SELECT
            Code AS Value,
            LTRIM(RTRIM(Code))
            + SPACE(10 - LEN(LTRIM(RTRIM(Code))))
            + '|'
            + SPACE(5)
            + CAST(Name AS VARCHAR(200)) AS Text
        FROM EMP_MAST
        WHERE RESIGN_DATE IS NULL
          AND Comp_code = @CompCode
        ORDER BY Name";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@CompCode", compCode)
    });
        }


        // DocType
        public List<DropdownModel> GetDocTypeWithParam(List<string> codes)
        {
            if (codes == null || !codes.Any())
                return new List<DropdownModel>();

            var parameters = new List<SqlParameter>();
            var inClause = new List<string>();

            for (int i = 0; i < codes.Count; i++)
            {
                string paramName = $"@Code{i}";
                inClause.Add(paramName);
                parameters.Add(new SqlParameter(paramName, codes[i]));
            }

            string query = $@"
                            SELECT Code AS Value, Name AS Text
                            FROM DOCTYPE_MAST
                            WHERE CODE IN ({string.Join(",", inClause)})
                            ORDER BY Name";

            return ExecuteDropdown(query, parameters.ToArray());
        }
        // DocStatus
        public List<DropdownModel> GetDocStatus()
        {
            string query = @"Select Code as Value, Name as Text from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            return ExecuteDropdown(query);
        }
        // GetAllParty
        public List<DropdownModel> GetAllParty(string compCode)
        {
            string query = @"select CODE as Value, NAME as Text from SUBGROUP_MAST where COMP_CODE =@CompCode and ACTIVE=1 order by NAME";

            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        // Place
        public List<DropdownModel> GetPlace(string compCode)
        {
            string query = @"select CODE as Value, NAME as Text from ITEMDEPT_MAST where COMP_CODE=@CompCode and TRAN_TYPE='Store' order by NAME";

            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        // Item
        public List<DropdownModel> GetItems(string compCode)
        {
            string query = @"select CODE as Value, NAME as Text, HSN_CODE, UNIT_NAME, UNIT_CODE from item_mast where COMP_CODE =@CompCode  order by NAME";

            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        public List<DropdownModel> GetQCIncharg(string compCode)
        {
            string query = @"
        SELECT 
            code AS Value,
            CONCAT(Name,'(',code,')') AS Text
        FROM EMP_MAST
        WHERE Comp_code = @CompCode
        AND Resign_date IS NULL
        AND Type IN ('Staff')
        ORDER BY Name
    ";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@CompCode", compCode)
    });
        }
        public List<DropdownModel> GetChem(string compCode)
        {
            string query = @"
        SELECT 
            code AS Value,
            CONCAT(Name,'(',code,')') AS Text
        FROM EMP_MAST
        WHERE Comp_code = @CompCode
        AND Resign_date IS NULL
        AND Type IN ('Staff','Semi Staff')
        ORDER BY Name
    ";

            return ExecuteDropdown(query, new[]
            {
        new SqlParameter("@CompCode", compCode)
    });
        }
        public List<DropdownModel> GetPartyName(string compCode)
        {
            string query = @" SELECT CODE AS Value, NAME AS Text FROM SUBGROUP_MAST  WHERE COMP_CODE = @CompCode ORDER BY NAME  ";
            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)    
            });
        }
        public List<DropdownModel> GetItemName(string compCode)
        {
            string query = @" SELECT a.CODE AS Value,a.NAME AS Text FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b ON a.MGROUP_CODE = b.CODE
                AND a.COMP_CODE = b.COMP_CODE WHERE a.COMP_CODE = @CompCode AND b.MGROUP_TYPE = 'Raw' AND a.ACTIVE = 1 ORDER BY a.NAME";
            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        public List<DropdownModel> GetItemMaster(string compCode)
        {
            string query = @"
            SELECT Code AS Value, NAME AS Text FROM ITEM_MAST WHERE COMP_CODE = @CompCode AND ACTIVE = 1 AND NAME <> '' ORDER BY NAME ";
            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        public List<DropdownModel> GetParticulars(string compCode)
        {
            string query = @"
                SELECT Code AS Value, NAME AS Text FROM QCP_MAST WHERE COMP_CODE = @CompCode AND ACTIVE = 1 AND NAME <> '' ORDER BY NAME ";
            return ExecuteDropdown(query, new[]
            {
                new SqlParameter("@CompCode", compCode)
            });
        }
        public List<DropdownModel> GetUnits()
        {
            string query = @"
                SELECT Code AS Value, NAME AS Text FROM QCPUNIT_MAST WHERE ACTIVE = 1 AND NAME <> '' ORDER BY NAME ";
            return ExecuteDropdown(query);
        }

    }
}

