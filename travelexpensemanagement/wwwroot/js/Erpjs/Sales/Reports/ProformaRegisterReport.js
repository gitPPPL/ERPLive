
var globalVars = window.globalVariables || { };
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];

$(document).ready(async function () {
    SetFYDate('DtToDate', LoginDate);
    SetFYDate('DtFromDate', LoginDate);
    LoadDropdown();

    $('#btn_Report').on('click', async function () {
    try {

        TransitReport();

    } catch (error) {
        console.error("Error generating report:", error);
    }
});

    $('#chkSelectDocumentType').on('change', function () {

        if (this.checked) {
            $('#chkAllDocumentType').prop('checked', false);
            $('#divDocumentTypeDropdown').show();
        } else {
            $('#divDocumentTypeDropdown').hide();
            $('#ddlforDocumentType').val('');
        }

    });

    $('#chkAllDocumentType').on('change', function () {

        if (this.checked) {
            $('#chkSelectDocumentType').prop('checked', false);
            $('#divDocumentTypeDropdown').hide();
            $('#ddlforDocumentType').val('');
        }

    });

    const filterControls = [

        {
            select: '#chkSelectColor',
            all: '#chkForColor',
            dropdown: '#divColorDropdown',
            ddl: '#ddlforColor'
        },
        {
            select: '#chkSelectDenierGram',
            all: '#chkDenierGram',
            dropdown: '#divDenierGramDropdown',
            ddl: '#ddlDenierGram'
        },
        {
            select: '#chkSelectGodown',
            all: '#chkGodown',
            dropdown: '#divGodownDropdown',
            ddl: '#ddlForGodown'
        },
        {
            select: '#chkSelectPtypeGroup',
            all: '#chkPtypeGroup',
            dropdown: '#divPtypeGroupDropdown',
            ddl: '#ddlForPTypeGroup'
        },
        {
            select: '#chkSelectItemMGroup',
            all: '#chkForItemMGroup',
            dropdown: '#divItemMGroupDropdown',
            ddl: '#ddlForItemMGroup'
        },
        {
            select: '#chkSelectSaleThrough',
            all: '#chkForSaleThrough',
            dropdown: '#divSaleThroughDropdown',
            ddl: '#ddlForSaleThrough'
        },

        {
            select: '#chkSelectItemType',
            all: '#chkAllItemTypes',
            dropdown: '#divItemTypeDropdown',
            ddl: '#ddlforItemType'
        },
        {
            select: '#chkSelectOSGroup',
            all: '#chkAllOSGroups',
            dropdown: '#divOSGroupDropdown',
            ddl: '#ddlforOSGroup'
        },
        {
            select: '#chkSelectTransport',
            all: '#chkAllTransports',
            dropdown: '#divTransportDropdown',
            ddl: '#ddlforTransport'
        },
        {
            select: '#chkSelectAcGroup',
            all: '#chkAllAcGroups',
            dropdown: '#divAcGroupDropdown',
            ddl: '#ddlforAcGroup'
        },
        {
            select: '#chkSelectSupplyType',
            all: '#chkForSupplyType',
            dropdown: '#divSupplyTypeDropdown',
            ddl: '#ddlForSupplyType'
        },
        {
            select: '#chkSelectCountry',
            all: '#chkForCountry',
            dropdown: '#divCountryDropdown',
            ddl: '#ddlForCountry'
        },
        {
            select: '#chkSelectItem',
            all: '#chkForItem',
            dropdown: '#divItemDropdown',
            ddl: '#ddlItem'
        },
        {
            select: '#chkSelectPartyName',
            all: '#chkAllParties',
            dropdown: '#divPartyNameDropdown',
            ddl: '#ddlforPartyName'
        }
    ];

    filterControls.forEach(function (item) {

        // Select checkbox
        $(item.select).on('change', function () {

            if (this.checked) {
                $(item.all).prop('checked', false);
                $(item.dropdown).show();
            } else {
                $(item.dropdown).hide();

                // Clear dropdown selection
                $(item.ddl).val('').trigger('change');
            }

        });

        // All checkbox
        $(item.all).on('change', function () {

            if (this.checked) {
                $(item.select).prop('checked', false);

                // Hide dropdown
                $(item.dropdown).hide();

                // Clear selected dropdown value
                $(item.ddl).val('').trigger('change');
            }

        });

    });


});


async function LoadDropdown() {
    try {
        await Promise.all([
            DDlDocumentType(),
            DDlItemType(),
            OSGroup(),
            DDlTransport(),
            DDlACGroup(),
            DDlCountry(),
            DDlItem(),
            DDlPartyName(),
            DDlcolur(),
            DDlDenierGram(),
            DDlGodown(),
            DDlPTypeGroup(),
            DDlPItemMGroup(),
            DDlSaleThrough()
        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function DDlDocumentType() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlDocumentType');
    const data = await res.json();

        const ddl = $('#ddlforDocumentType');

    ddl.empty();
        ddl.append('<option value="">--- Select Document Type ---</option>');

        data.forEach(item => {
        ddl.append(
            `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
        );
        });

    // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Document Type---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Document Type:", error);
    }
}

async function DDlItemType() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlItemType');
        const data = await res.json();
        const ddl = $('#ddlforItemType');
        ddl.empty().append('<option value="">---Select Item Type--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item Type:", error);
    }
}

async function OSGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/OSGroup');
        const data = await res.json();
        const ddl = $('#ddlforOSGroup');
        ddl.empty();
        ddl.append('<option value="">--- Select OS Group ---</option>');

        data.forEach(item => { ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` ); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select OS Group---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading OS Group:", error);
    }
}

async function DDlTransport() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlTransport');
        const data = await res.json();
        const ddl = $('#ddlforTransport');
        ddl.empty();
        ddl.append('<option value="">--- Select Transport ---</option>');

        data.forEach(item => { ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` ); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Transport---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Transport:", error);
    }
}

async function DDlACGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlACGroup');
        const data = await res.json();
        const ddl = $('#ddlforAcGroup');
        ddl.empty();
        ddl.append('<option value="">--- Select AC Group ---</option>');

        data.forEach(item => { ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select AC Group---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading AC Group:", error);
    }
}

async function DDlCountry()
{
    try {
        const res = await fetch('/PerformaRegisterReport/DDlCountry');
        const data = await res.json();
        const ddl = $('#ddlForCountry');
        ddl.empty();
        ddl.append('<option value="">--- Select Country ---</option>');

        data.forEach(item => { ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Country---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Country:", error);
    }
}

async function DDlItem() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlItem');

        if (!res.ok) {
            throw new Error('Failed to load Item Name');
        }

        const data = await res.json();
        const ddl = $('#ddlItem');

        // Destroy existing Select2
        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.empty();
        ddl.append('<option value="">--- Select Item Name ---</option>');

        // Bind Party Names
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        // Initialize Select2 Multiple Checkbox
        ddl.select2({
            placeholder: "--- Select Item ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%',
            templateResult: function (item) {

                if (!item.id) {
                    return item.text;
                }

                const checkbox = $('<span><input type="checkbox" class="me-2" />' + item.text + '</span>');

                checkbox.find('input').prop(
                    'checked',
                    item.element.selected
                );

                return checkbox;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });

    } catch (error) {
        console.error("Error loading Item :", error);
    }
}

async function DDlPartyName() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlPartyName');

        if (!res.ok) {
            throw new Error('Failed to load Party Names');
        }

        const data = await res.json();
        const ddl = $('#ddlforPartyName');

        // Destroy existing Select2
        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.empty();
        ddl.append('<option value="">--- Select Party Name ---</option>');

        // Bind Party Names
        data.forEach(item => { ddl.append(  `<option value="${item.value}">${item.text}</option>` );
        });

        // Initialize Select2 Multiple Checkbox
        ddl.select2({
            placeholder: "--- Select Party ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%',
            templateResult: function (item) {

                if (!item.id) {
                    return item.text;
                }

                const checkbox = $('<span><input type="checkbox" class="me-2" />' + item.text + '</span>');

                checkbox.find('input').prop(
                    'checked',
                    item.element.selected
                );

                return checkbox;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });

    } catch (error) {
        console.error("Error loading Party Names:", error);
    }
}

async function DDlcolur() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlcolur');
        const data = await res.json();
        const ddl = $('#ddlforColor');
        ddl.empty().append('<option value="">---Select Color--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item Type:", error);
    }
}

async function DDlDenierGram() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlDenierGram');
        const data = await res.json();
        const ddl = $('#ddlDenierGram');
        ddl.empty().append('<option value="">---Select Denier Gram--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Denier Gram:", error);
    }
}

async function DDlGodown() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlGodown');
        const data = await res.json();
        const ddl = $('#ddlForGodown');
        ddl.empty().append('<option value="">---Select Godown--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Godown:", error);
    }
}

async function DDlPTypeGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlPTypeGroup');
        const data = await res.json();
        const ddl = $('#ddlForPTypeGroup');
        ddl.empty().append('<option value="">---Select PType Group--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading PType Group:", error);
    }
}

async function DDlPItemMGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlPItemMGroup');
        const data = await res.json();
        const ddl = $('#ddlForItemMGroup');
        ddl.empty().append('<option value="">---Select Item M Group--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item M Group:", error);
    }
}

async function DDlSaleThrough() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlSaleThrough');
        const data = await res.json();
        const ddl = $('#ddlForSaleThrough');
        ddl.empty().append('<option value="">---Select Sale Through--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item M Group:", error);
    }
}

async function TransitReport() {
    try {

        // 1. Get form values
        const ToDate = $('#DtToDate').val();
        const FromDate = $('#DtFromDate').val();
        const ChkQty = $('#ChkQty').is(':checked') ? 1 : 0;

        const DocumentType = $('#ddlforDocumentType').val();
        const DocumentTypetext = $('#ddlforDocumentType option:selected').text();

        const DocumentTypechk = $('#chkForDocumentType').is(':checked') ? 1 : 0;
        const SelectDocumentType = $('#chkSelectDocumentType').is(':checked') ? 1 : 0;

        const ItemType = $('#ddlforItemType').val();
        const ItemTypechk = $('#chkAllItemTypes').is(':checked') ? 1 : 0;
        const SelectItemType = $('#chkSelectItemType').is(':checked') ? 1 : 0;

        const OsGroup = $('#ddlforOSGroup').val();
        const OsGroupchk = $('#chkAllOSGroups').is(':checked') ? 1 : 0;
        const SelectOSGroup = $('#chkSelectOSGroup').is(':checked') ? 1 : 0;

        const TransPort = $('#ddlforTransport').val();
        const TransPortchk = $('#chkAllTransports').is(':checked') ? 1 : 0;
        const SelectTransport = $('#chkSelectTransport').is(':checked') ? 1 : 0;

        const ACGroup = $('#ddlforAcGroup').val();
        const AccGroupchk = $('#chkAllAcGroups').is(':checked') ? 1 : 0;
        const SelectAcGroup = $('#chkSelectAcGroup').is(':checked') ? 1 : 0;

        const SupplyType = $('#ddlForSupplyType').val();
        const supplyTypechk = $('#chkForSupplyType').is(':checked') ? 1 : 0;
        const SelectSupplyType = $('#chkSelectSupplyType').is(':checked') ? 1 : 0;


        const Country = $('#ddlForCountry').val();
        const Countrychk = $('#chkForCountry').is(':checked') ? 1 : 0;
        const SelectCountry = $('#chkSelectCountry').is(':checked') ? 1 : 0;

        const Item = $('#ddlItem').val() || [];
        const ItemValues = Item.join(',');
        const Itemchk = $('#chkForItem').is(':checked') ? 1 : 0;
        const SelectItem = $('#chkSelectItem').is(':checked') ? 1 : 0;


        const Patycode = $('#ddlforPartyName').val() || [];
        const PartyValues = Patycode.join(',');
        const Partychk = $('#chkAllParties').is(':checked') ? 1 : 0;
        const SelectPartyName = $('#chkSelectPartyName').is(':checked') ? 1 : 0;

        const Color = $('#ddlforColor').val();
        const Colorchk = $('#chkForColor').is(':checked') ? 1 : 0;
        const SelectColor = $('#chkSelectColor').is(':checked') ? 1 : 0;

        const DenierGram = $('#ddlDenierGram').val();
        const DenierGramchk = $('#chkDenierGram').is(':checked') ? 1 : 0;
        const SelectDenierGram = $('#chkSelectDenierGram').is(':checked') ? 1 : 0;

        const Godown = $('#ddlDenierGram').val();
        const Godownchk = $('#chkDenierGram').is(':checked') ? 1 : 0;
        const SelectGodown = $('#chkSelectGodown').is(':checked') ? 1 : 0;

        const PType = $('#ddlForPTypeGroup').val();
        const PTypechk = $('#chkPtypeGroup').is(':checked') ? 1 : 0;
        const SelectPtypeGroup = $('#chkSelectPtypeGroup').is(':checked') ? 1 : 0;

        const ItemMGroup = $('#ddlForItemMGroup').val();
        const ItemMGroupchk = $('#chkForItemMGroup').is(':checked') ? 1 : 0;
        const chkSelectItemMGroup = $('#chkForItemMGroup').is(':checked') ? 1 : 0;

        const SaleThrough = $('#ddlForSaleThrough').val();
        const SaleThroughchk = $('#chkForSaleThrough').is(':checked') ? 1 : 0;
        const SelectSaleThrough = $('#chkSelectSaleThrough').is(':checked') ? 1 : 0;

        const SalesRegister = $('#chk_SaleRegister').is(':checked') ? 1 : 0;
        const SalesRegisterDetail = $('#chk_SaleRegisterDetails').is(':checked') ? 1 : 0;
        const TptFreightReport = $('#chk_tptfrieghtreport').is(':checked') ? 1 : 0;
        const AgentWiseSale = $('#chk_AgentWiseSale').is(':checked') ? 1 : 0;
        const AnalysisTon = $('#chk_Analysis').is(':checked') ? 1 : 0;
        const AnalysisTruck = $('#chk_AnalysisTruck').is(':checked') ? 1 : 0;
        const MsMESaleList = $('#chk_MSMESaleList').is(':checked') ? 1 : 0;
        const PartyItemWise = $('#chk_PartyItemWise').is(':checked') ? 1 : 0;
        const SaleRegisterDrCrNote = $('#chk_SaleRegistercrdr').is(':checked') ? 1 : 0;
        const BileItemwise = $('#chk_BileItemWise').is(':checked') ? 1 : 0;
        const LoadUnLoadReport = $('#chk_LoadUnlaodReport').is(':checked') ? 1 : 0;
        const WBReport = $('#chk_LoadUnlaodReport').is(':checked') ? 1 : 0;
        const SalesThrough = $('#chk_salesThrough').is(':checked') ? 1 : 0;
        const ColourAnalysisTruck = $('#chk_colurAnalysis').is(':checked') ? 1 : 0;
        const ColourGrpAnalysisTruck = $('#chk_colurGrpAnalysis').is(':checked') ? 1 : 0;
        const DenierAnalysisTruck = $('#chk_DenierAnalysisTruck').is(':checked') ? 1 : 0;
        const GateToInvoiceTime = $('#chk_GateToInvoiceTime').is(':checked') ? 1 : 0;
        const MonthWiseCustomer = $('#chk_MonthWiseCustomer').is(':checked') ? 1 : 0;
        const MonthWiseItem = $('#chk_MonthWiseItem').is(':checked') ? 1 : 0;
        const MonthWisecolor = $('#chk_MonthWisecolor').is(':checked') ? 1 : 0;
        const MonthWiseItemGroup = $('#chk_MonthwiseItemGroup').is(':checked') ? 1 : 0;
        const MonthWisePartyItem = $('#chk_MonthWisePartyItem').is(':checked') ? 1 : 0;
        const MonthWiseAgent = $('#chk_MonthWiseAgent').is(':checked') ? 1 : 0;
        const ItemGroup = $('#chk_ItemGroup').is(':checked') ? 1 : 0;
        const ItemWise = $('#chk_ItemWise').is(':checked') ? 1 : 0;
        const PartyWise = $('#chk_PartyWise').is(':checked') ? 1 : 0;
        const ItemDateRateList = $('#chk_ItemDateRateList').is(':checked') ? 1 : 0;
        const SaleListDetail = $('#chk_MonthWisePartyItem').is(':checked') ? 1 : 0;
        const SalesRegisterCancel = $('#chk_SalesRegisterCancel').is(':checked') ? 1 : 0;
        const SalesRegisterDetailCancel = $('#chk_SalesRegisterDetailCancel').is(':checked') ? 1 : 0;

        const Group = $('#ddlGroup').val();
        const Rwo = $('#ddlRow').val();
        const Column = $('#ddlColumn').val();

        let SelForMul = "";
        let formula = "";

        if (ItemGroup == 1 || ItemWise == 1 || PartyWise == 1 || ColourAnalysisTruck == 1 || MonthWiseCustomer == 1 || MonthWiseAgent == 1 ||
            MonthWiseItemGroup == 1 || MonthWiseItem == 1 || MonthWisecolor == 1 || ItemDateRateList == 1 || MonthWisePartyItem == 1)
        {        

            SelForMul = `{VW_SALES_COMBINED.V_DATE} IN DATE(${FromDate}) TO DATE(${ToDate})`;

            SelForMul += ` AND {VW_SALES_COMBINED.COMP_CODE} = ${globalVars.CompCode}`;

            SelForMul += ` AND {VW_SALES_COMBINED.BRANCH_CODE} = ${globalVars.BranchCode}`;


            if (SelectDocumentType == 1) {
                SelForMul += ` AND {VW_SALES_COMBINED.V_TYPE} = '${DocumentTypetext}'`;
            }
            else if (TptFreightReport == 1) {
                SelForMul += `AND ({DOCTYPE_MAST.DOCTYPE} = 'SalesInvoice' OR {DOCTYPE_MAST.DOCTYPE} = 'EComSalesInvoice' OR {DOCTYPE_MAST.DOCTYPE} = 'TradingSale' OR {DOCTYPE_MAST.DOCTYPE} = 'EComSalesReturn' or {DOCTYPE_MAST.DOCTYPE} = 'SalesReturn' or {DOCTYPE_MAST.DOCTYPE} = 'JobReturn' or {DOCTYPE_MAST.DOCTYPE} = 'SaleChallan' or {DOCTYPE_MAST.DOCTYPE} = 'JobworkIssue' or {DOCTYPE_MAST.DOCTYPE} = 'JobworkReceived')`;

            }
            else {
                SelForMul += ` AND ({DOCTYPE_MAST.DOCTYPE} = 'SalesInvoice' OR {DOCTYPE_MAST.DOCTYPE} = 'EComSalesInvoice' OR {DOCTYPE_MAST.DOCTYPE} = 'TradingSale' OR {DOCTYPE_MAST.DOCTYPE} = 'EComSalesReturn' or {DOCTYPE_MAST.DOCTYPE} = 'SalesReturn' OR {DOCTYPE_MAST.DOCTYPE} ='GSTCreditNote' OR {DOCTYPE_MAST.DOCTYPE} ='GSTDebitNote')`;

            }

            if (SelectItemType == 1)
            {
                SelForMul += `AND {VW_SALES_COMBINED.ITEM_TYPE} = '${ItemType}'`;
            }

            if (SelectOSGroup == 1)
            {
                SelForMul += `AND {SUBGROUP_MAST.OS_CODE} = ${OsGroup}`;
            }

            if (SelectAcGroup == 1) {
                SelForMul += `AND {SUBGROUP_MAST.GROUP_CODE} = ${ACGroup}`;
            }
            if (SelectTransport == 1)
            {
                SelForMul += `AND {VW_SALES_COMBINED.TRANSPORT_CODE} = ${ACGroup}`;
            }

            if (SelectColor == 1)
            {
                SelForMul += `AND {ITEM_MAST.COLOR_CODE} = ${Color}`;
            }
            if (SelectDenierGram == 1)
            {
                SelForMul += `AND {ITEM_MAST.GRAM_CODE} = ${DenierGram}`;
            }


            if (SelectGodown == 1)
            {
                SelForMul += `AND {VW_SALES_COMBINED.GODOWN_CODE} = ${Godown}`;
            }
            if (SelectSupplyType == 1)
            {
                SelForMul += `AND {VW_SALES_COMBINED.SUPPLY_TYPE} = ${SupplyType}`;
            }
            if (SelectCountry == 1)
            {
                SelForMul += `AND {SUBGROUP_MAST.COUNTRY_CODE} = ${Country}`;
            }
            if (SelectPtypeGroup == 1)
            {
                SelForMul += `AND {ITEM_MAST.PTYPE_CODE} = ${PType}`;
            }
            if (SelectPtypeGroup == 1)
            {
                SelForMul += `AND {ITEM_MAST.PTYPE_CODE} = ${PType}`;
            }
            if (chkSelectItemMGroup == 1)
            {
                SelForMul += `AND {ITEM_MAST.MGROUP_CODE} = ${ItemMGroup}`;
            }
            if (SelectSaleThrough == 1)
            {
                SelForMul += `AND {SAUDA.DEAL_THROUGH} = ${SalesThrough}`;
            }

            if (ItemGroup == 1 || ItemWise == 1 || PartyWise == 1 || ) {
                reportName = "SALE4";

                if (PartyWise == 1) {
                    formula = "{GrpName1} = ITEM_GROUP.GROUP_TYPE ";
                    formula += " AND {GrpName2} = SUBGROUP_MAST.NAME ";
                    formula += " AND {RPTNAME} = 'SALE SUMMARY CUSTOMER WISE' ";
                }

                else if (ItemWise == 1) {
                    formula = "{GrpName1} = ITEM_GROUP.GROUP_TYPE";
                    formula += " AND {GrpName2} = ITEM_MAST.NAME ";
                    formula += " AND {RPTNAME} = 'SALE SUMMARY ITEM WISE'";

                }

                else if (ItemGroup == 1) {
                    formula = "{GrpName1} = ITEM_GROUP.GROUP_TYPE";
                    formula += "  {GrpName2} = ITEM_MAST.NAME ";
                    formula += "  {RPTNAME} = 'SALE SUMMARY ITEM-GROUP WISE'";
                }

            }

            else if (ItemDateRateList == 1) {
                reportName = "SALE6";
                formula += " AND {RPTNAME} = 'ITEM/DATE WISE SALE'";
            }
            else if (ColourAnalysisTruck == 1) {
                reportName = "SALE5";
                formula += "  {RPTNAME} = 'SALE ANALYSIS COLOR WISE(TRUCK)'";
            }
            else if (MonthWisePartyItem == 1) {
                reportName = "SALE25_PLPL";

                if (ChkQty == 1) {
                    formula += " {Qty} = {VW_SALES_COMBINED.Qty}";

                }
                else {
                    formula += " {Qty} = {VW_SALES_COMBINED.ITEM_AMT}+{VW_SALES_COMBINED.PACK_AMT}-{VW_SALES_COMBINED.DISC_AMT}";
                }
                formula += "  {RPTNAME} = 'MONTH WISE SALE (PARTY/ITEM WISE)'";

            }

            else if (SaleListDetail == 1) {
                reportName = "SALE_COMBINED1";
                formula += "  {RPTNAME} = 'Sale Register Details(Combined)'";

            }
            else if (MonthWiseCustomer == 1)
            {
                reportName = "SALE25";

                if (ChkQty == 1)
                {
                    formula += "  {Qty} = 'VW_SALES_COMBINED.Qty'";
                }
                else
                {
                    formula += "  {Qty} = {VW_SALES_COMBINED.ITEM_AMT}+{VW_SALES_COMBINED.PACK_AMT}-{VW_SALES_COMBINED.DISC_AMT}";
                }

                      else if (MonthWiseCustomer == 1) {
                    formula += " {GrpName1} = {DOCTYPE_MAST.NAME}";
                    formula += " {GrpName2} = {SUBGROUP_MAST.NAME}";
                    formula += " {RPTNAME} = 'MONTH WISE SALE (CUSTOMER WISE)'";


                }

                else if (MonthWiseItem == 1) {
                    formula += " {GrpName1} = {ACOS_MAST.NAME}";
                    formula += " {GrpName2} = {SUBGROUP_MAST.NAME}";
                    formula += " {RPTNAME} = 'MONTH WISE SALE (AGENT-CUSTOMER WISE)'";

                }
                else if (MonthWiseItemGroup == 1) {
                    formula += " {GrpName1} = {ACOS_MAST.NAME}";
                    formula += " {GrpName2} = {SUBGROUP_MAST.NAME}";
                    formula += " {RPTNAME} = 'MONTH WISE SALE (AGENT-CUSTOMER WISE)'";
                }

                else if (MonthWiseItem == 1) {
                    formula += " {GrpName1} = {ITEM_GROUP.NAME}";
                    formula += " {GrpName2} = {ITEM_MAST.NAME}";
                    formula += " {RPTNAME} = 'MONTH WISE SALE (ITEM WISE)'";
                }

                else if (MonthWisecolor == 1) {
                    formula += " {GrpName1} = {SUBGROUP_MAST.NAME}";
                    formula += " {GrpName2} = {ITEM_MAST.COLOR_CODE}";
                    formula += " {RPTNAME} = 'MONTH WISE SALE (COLOR WISE)'";
                }



            }
      



         formula =
            "{TEMP_GST.COMP_CODE}=" + Number(globalVars.CompCode) +
            " and {TEMP_GST.WSID}= '" +
            (globalVars.PubWorkStationID || "").replace(/'/g, "''") + "' " +
            " and {TEMP_GST.USERID}=" + Number(globalVars.UserId) +
            " AND {TEMP_GST.V_TYPE}='INSU' " +
            " AND {SALE1.V_TYPE}='SAGT'";

        // 4. Prepare Formula Fields
        const Formulafield =
            "From " + ReportformatDate(FromDate) +
            " To " + ReportformatDate(ToDate);

        const payload = {
            Reportname: reportName,
            Database: database,
            selectionFormula: formula,

            Parameters: {
                comp_name: "'" + (globalVars.CompanyName || "") + "'",
                comp_add1: "'" + (globalVars.Address1 || "") + "'",
                comp_add2: "'" + (globalVars.Address2 || "") + "'",
                RPTNAME: "'STATEMENT OF INSURANCE DECLARATION'",
                F1: "'" + Formulafield + "'",
                OPAMT: openingBalance
            }
        };

        console.log("Selection Formula:", formula);
        console.log("Report Payload:", payload);

        // 5. Generate Timestamp
        const now = new Date();

        const timestamp =
            String(now.getDate()).padStart(2, '0') +
            String(now.getMonth() + 1).padStart(2, '0') +
            String(now.getFullYear()).slice(-2) + "_" +
            String(now.getHours()).padStart(2, '0') +
            String(now.getMinutes()).padStart(2, '0') +
            String(now.getSeconds()).padStart(2, '0');

        // 6. Generate Crystal Reports PDF
        //const response = await $.ajax({
        //    url: 'http://localhost:24085/Report/PendingQCReport',
        //    type: 'POST',
        //    data: JSON.stringify(payload),
        //    contentType: "application/json; charset=utf-8",
        //    xhrFields: {
        //        responseType: 'blob'
        //    }
        //});

        //// 7. Validate PDF response
        //if (!response || response.size === 0) {
        //    throw new Error("Crystal Reports returned an empty PDF.");
        //}

        // 8. Download PDF
        const fileName = `${reportName}_${timestamp}.pdf`;

        const fileUrl = URL.createObjectURL(
            new Blob([response], { type: 'application/pdf' })
        );

        const link = document.createElement('a');

        link.href = fileUrl;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        setTimeout(() => {
            URL.revokeObjectURL(fileUrl);
        }, 1000);

        toastr.success("Report generated successfully.");

    }
    catch (error) {

        console.error("Transit Report Error:", error);

        // 9. Read actual API error
        let errorMessage = error.message || "Unknown error";

        if (error.response instanceof Blob) {

            try {
                const responseText = await error.response.text();

                console.error("API Error Response:", responseText);

                try {
                    const errorJson = JSON.parse(responseText);

                    errorMessage =
                        errorJson.Message ||
                        errorJson.message ||
                        errorJson.ExceptionMessage ||
                        responseText;
                }
                catch {
                    errorMessage = responseText;
                }

            }
            catch (readError) {
                console.error("Error reading API response:", readError);
            }
        }

        console.error("Final Error:", errorMessage);

        toastr.error(
            "Report generation failed: " + errorMessage
        );
    }
}
function ReportformatDate(value) {

    if (!value) return "";

    const [year, month, day] = value.split("-").map(Number);

    const months = [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    if (!year || !month || !day || month > 12 || day > 31) {
        return "";
    }

    return `${String(day).padStart(2, '0')}/${months[month - 1]}/${year}`;
}