using System;
using System.Collections.Generic;

public class SalesTargetmodel
{
    public List<SalesTargetmodel_Detail> Details { get; set; } = new();
}

public class SalesTargetmodel_Detail
{

    public string? V_TYPE { get; set; }
    public int? V_NO { get; set; }
    public DateTime? V_DATE { get; set; }
    public string? DOC_ID { get; set; } 
    public DateTime? PLAN_MONTH { get; set; }
    public int? PARTY_CODE { get; set; }
    public int? ITEM_CODE { get; set; }
    public decimal? SOLD_QTY { get; set; }
    public decimal? TARGET_QTY { get; set; }
    public string? REMARKS { get; set; } 
    public string? Partyname { get; set; } 
    public string? Item_Name { get; set; } 
    public int? SRNO { get; set; }
    public int? salesThrough { get; set; }
    public decimal? MIN_PRICE { get; set; }
    public decimal? MAX_PRICE { get; set; }
    public decimal? APP_PRICE { get; set; }
    public decimal? VIR_PRICE { get; set; }
}