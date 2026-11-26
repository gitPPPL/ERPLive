
var globalVars = window.globalVariables || { };
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];
let isRemovingItem = false;

let isRemovingParty = false;

$(document).ready(async function () {
    SetFYDate('DtToDate', LoginDate);
    SetFYDate('DtFromDate', LoginDate);

    LoadDropdown();

    $('#btn_Report').on('click', async function () {
        try {

            const SaleRegisterDrCrNote = $('#chk_SaleRegistercrdr').is(':checked');
            const TptFreightReport = $('#chk_tptfrieghtreport').is(':checked');
            const ToDate = $('#DtToDate').val();
            const FromDate = $('#DtFromDate').val();
            await DataInsert(SaleRegisterDrCrNote, TptFreightReport, FromDate, ToDate);
        TransitReport();

    } catch (error) {
        console.error("Error generating report:", error);
    }
    });

    $('#ddlItem').on('change', function () {

        const selectedItems = $(this).select2('data');
        const tbody = $('#tblSelectedItem tbody');

        selectedItems.forEach(item => {

            const itemId = String(item.id);
            const itemName = item.text;

            // Check duplicate Item ID
            const alreadyExists = tbody.find('tr').filter(function () {
                return String($(this).attr('data-id')) === itemId;
            }).length > 0;

            if (alreadyExists) {

                // Show message only during Add operation
                if (!isRemovingItem) {
                    toastr.info("Item already selected.");
                }

                return;
            }

            const row = `
            <tr data-id="${$('<div>').text(itemId).html()}">
                <td>${$('<div>').text(itemId).html()}</td>
                <td>${$('<div>').text(itemName).html()}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-danger btn-sm btnRemoveItem">
                        <i class="fa fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;

            tbody.append(row);
        });

        $('#divSelectedItemGrid').toggle(tbody.find('tr').length > 0);

    });

    $(document).on('click', '.btnRemoveItem', function () {

        const itemId = String($(this).closest('tr').attr('data-id'));

        // Remove row from grid
        $(this).closest('tr').remove();

        let selectedItems = $('#ddlItem').val() || [];

        if (!Array.isArray(selectedItems))
        {
            selectedItems = [selectedItems];
        }

        selectedItems = selectedItems.filter(id => String(id) !== itemId);

        // Prevent duplicate message during removal
        isRemovingItem = true;

        $('#ddlItem').val(selectedItems).trigger('change');

        isRemovingItem = false;

        $('#divSelectedItemGrid').toggle($('#tblSelectedItem tbody tr').length > 0);

    });

    $('#ddlforPartyName').on('change', function () {

        const selectedParties = $(this).select2('data');
        const tbody = $('#tblSelectedParty tbody');

        selectedParties.forEach(item => {

            const partyId = String(item.id);
            const partyName = item.text;

            // Check duplicate Party ID
            const alreadyExists = tbody.find('tr').filter(function () {
                return String($(this).attr('data-id')) === partyId;
            }).length > 0;

            if (alreadyExists) {

                // Show message only during Add operation
                if (!isRemovingParty) {
                    toastr.info("Party already selected.");
                }

                return;
            }

            const row = `
            <tr data-id="${$('<div>').text(partyId).html()}">
                <td>${$('<div>').text(partyId).html()}</td>
                <td>${$('<div>').text(partyName).html()}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-danger btn-sm btnRemoveParty">
                        <i class="fa fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;

            tbody.append(row);
        });

        $('#divSelectedPartyGrid').toggle(tbody.find('tr').length > 0);

    });

    $(document).on('click', '.btnRemoveParty', function () {

        const partyId = String($(this).closest('tr').attr('data-id'));

        // Remove row from grid
        $(this).closest('tr').remove();

        let selectedParties = $('#ddlforPartyName').val() || [];

        if (!Array.isArray(selectedParties)) {
            selectedParties = [selectedParties];
        }

        selectedParties = selectedParties.filter(id => String(id) !== partyId);

        // Prevent duplicate message during removal
        isRemovingParty = true;

        $('#ddlforPartyName').val(selectedParties).trigger('change');

        isRemovingParty = false;

        $('#divSelectedPartyGrid').toggle($('#tblSelectedParty tbody tr').length > 0);

    });

    $('#chkAllParties').on('change', function () {

        if ($(this).is(':checked')) {

            // Clear Party Grid
            $('#tblSelectedParty tbody').empty();

            // Hide Party Grid
            $('#divSelectedPartyGrid').hide();

            // Clear Party Dropdown
            $('#ddlforPartyName').val(null).trigger('change');
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
        }              
    ];

    filterControls.forEach(function (item) {

        $(item.select).on('change', function () {

            if (this.checked)
            {
                $(item.all).prop('checked', false);
                $(item.dropdown).show();
            }
            else
            {
                $(item.dropdown).hide();
                $(item.ddl).val('').trigger('change');
            }

        });

        $(item.all).on('change', function () {

            if (this.checked) {
                $(item.select).prop('checked', false);
                $(item.dropdown).hide();
                $(item.ddl).val('').trigger('change');
            }

        });

    });

    $('#chkSelectItem').on('change', function () {

        if ($(this).is(':checked')) {

            $('#chkForItem').prop('checked', false);

            $('#divItemDropdown').show();
            $('#divSelectedItemGrid').show();

        } else {

            $('#divItemDropdown').hide();
            $('#divSelectedItemGrid').hide();
        }
    });

    $('#chkForItem').on('change', function () {

        if ($(this).is(':checked')) {

            $('#chkSelectItem').prop('checked', false);

            $('#divItemDropdown').hide();
            $('#divSelectedItemGrid').hide();

            $('#tblSelectedItem tbody').empty();
            $('#ddlItem').val(null).trigger('change');
        }
    });

    $('#chkSelectPartyName').on('change', function () {

        if ($(this).is(':checked')) {

            $('#chkAllParties').prop('checked', false);

            $('#divPartyNameDropdown').show();
            $('#divSelectedPartyGrid').show();

        } else {

            $('#divPartyNameDropdown').hide();
            $('#divSelectedPartyGrid').hide();
        }
    });

    $('#chkAllParties').on('change', function () {

        if ($(this).is(':checked')) {

            $('#chkSelectPartyName').prop('checked', false);

            $('#divPartyNameDropdown').hide();
            $('#divSelectedPartyGrid').hide();

            $('#tblSelectedParty tbody').empty();
            $('#ddlforPartyName').val(null).trigger('change');
        }
    });
     
});

function handleSelection(config) {

    const selectChecked = $(config.select).is(':checked');
    const allChecked = $(config.all).is(':checked');

    if (selectChecked) {

        // Show dropdown and grid
        $(config.dropdown).show();
        $(config.grid).show();

    }
    else if (allChecked) {

        // Hide dropdown and grid
        $(config.dropdown).hide();
        $(config.grid).hide();

        // Clear grid
        $(config.table + ' tbody').empty();

        // Clear dropdown selection
        $(config.ddl).val(null).trigger('change');

    }
    else {

        // Hide dropdown and grid
        $(config.dropdown).hide();
        $(config.grid).hide();

        // Clear grid
        $(config.table + ' tbody').empty();
    }
}

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

        // Bind Item Names
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        // Initialize Select2 Multiple
        ddl.select2({
            placeholder: "--- Select Item ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%'
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
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        // Initialize Select2 without checkboxes
        ddl.select2({
            placeholder: "--- Select Party ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%'
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
        const isChecked = id => $(id).is(':checked');
        const getValue = id => $(id).val();

        const getArray = id => {
            const value = $(id).val();
            if (!value) return [];
            return Array.isArray(value) ? value : [value];
        };

        const escapeCrystalString = value =>
            String(value ?? '').replace(/'/g, "''");

        const crystalString = value => `'${escapeCrystalString(value)}'`;

        const crystalNumber = value => {
            const number = Number(value);
            return Number.isFinite(number) ? number : 0;
        };

        function crystalDate(value) {
            if (!value) {
                throw new Error("Please select both dates.");
            }

            const parts = value.split('-');

            if (parts.length !== 3) {
                throw new Error("Invalid date format: " + value);
            }

            const year = Number(parts[0]);
            const month = Number(parts[1]);
            const day = Number(parts[2]);

            return `DATE(${year},${month},${day})`;
        }

        function formatReportDate(value) {
            const parts = value.split('-');
            const year = Number(parts[0]);
            const month = Number(parts[1]);
            const day = Number(parts[2]);

            const months = [
                "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
            ];

            return `${String(day).padStart(2, '0')}/${months[month - 1]}/${year}`;
        }

        function addCondition(formula, condition) {
            if (!condition) return formula;
            return formula ? `${formula} AND ${condition}` : condition;
        }

        function numericInCondition(field, values) {
            const validValues = values
                .map(Number)
                .filter(value => Number.isFinite(value));

            if (validValues.length === 0) return "";

            return `${field} IN [${validValues.join(",")}]`;
        }

        const selectedItems = [];

        $('#tblSelectedItem tbody tr').each(function () {

            const itemId = $(this).find('td:eq(0)').text().trim();
            const itemName = $(this).find('td:eq(1)').text().trim();

            if (itemId)
            {
                selectedItems.push({ ItemId: itemId, ItemName: itemName  });
            }
        });

        const Item = selectedItems.map(x => x.ItemId);

        console.log("Selected Item Details:", selectedItems);
        console.log("Selected Item IDs:", Item);

        const ToDate = getValue('#DtToDate');
        const FromDate = getValue('#DtFromDate');

        if (!FromDate || !ToDate)
        {
            toastr.warning("Please select From Date and To Date.");
            return;
        }

        if (new Date(ToDate) < new Date(FromDate))
        {
            toastr.warning("Invalid Date");
            return;
        }

        const CompCode = crystalNumber(globalVars.CompCode);
        const BranchCode = crystalNumber(globalVars.BranchCode);

        const FromCrystalDate = crystalDate(FromDate);
        const ToCrystalDate = crystalDate(ToDate);

        const ChkQty = isChecked('#ChkQty');
        const DocumentType = getValue('#ddlforDocumentType');
        const ItemType = getValue('#ddlforItemType');
        const OsGroup = getValue('#ddlforOSGroup');
        const TransPort = getValue('#ddlforTransport');
        const ACGroup = getValue('#ddlforAcGroup');
        const SupplyType = getValue('#ddlForSupplyType');
        const Country = getValue('#ddlForCountry');

        const selectedParties = [];
        const partyCodeSet = new Set();

        $('#tblSelectedParty tbody tr').each(function () {

            const partyCode = $(this).find('td:eq(0)').text().trim();
            const partyName = $(this).find('td:eq(1)').text().trim();

            if (partyCode && !partyCodeSet.has(partyCode)) {

                partyCodeSet.add(partyCode);

                selectedParties.push({
                    PartyCode: partyCode,
                    PartyName: partyName
                });
            }
        });

        // Get only Party Codes for Crystal Report
        const Party = selectedParties.map(x => x.PartyCode);

        console.log("Selected Party Details:", selectedParties);
        console.log("Selected Party Codes:", Party);

        const Color = getValue('#ddlforColor');
        const DenierGram = getValue('#ddlDenierGram');
        const Godown = getValue('#ddlGodown');
        const PType = getValue('#ddlForPTypeGroup');
        const ItemMGroup = getValue('#ddlForItemMGroup');
        const SaleThrough = getValue('#ddlForSaleThrough');

        const SelectDocumentType = isChecked('#chkSelectDocumentType');
        const SelectItemType = isChecked('#chkSelectItemType');
        const SelectOSGroup = isChecked('#chkSelectOSGroup');
        const SelectTransport = isChecked('#chkSelectTransport');
        const SelectAcGroup = isChecked('#chkSelectAcGroup');
        const SelectSupplyType = isChecked('#chkSelectSupplyType');
        const SelectCountry = isChecked('#chkSelectCountry');

        const SelectItem = isChecked('#chkSelectItem');
        const SelectPartyName = isChecked('#chkSelectPartyName');

        const SelectColor = isChecked('#chkSelectColor');
        const SelectDenierGram = isChecked('#chkSelectDenierGram');
        const SelectGodown = isChecked('#chkSelectGodown');
        const SelectPtypeGroup = isChecked('#chkSelectPtypeGroup');
        const SelectItemMGroup = isChecked('#chkSelectItemMGroup');
        const SelectSaleThrough = isChecked('#chkSelectSaleThrough');

        const SalesRegister = isChecked('#chk_SaleRegister');
        const SalesRegisterDetail = isChecked('#chk_SaleRegisterDetails');
        const TptFreightReport = isChecked('#chk_tptfrieghtreport');
        const AgentWiseSale = isChecked('#chk_AgentWiseSale');
        const AnalysisTon = isChecked('#chk_Analysis');
        const AnalysisTruck = isChecked('#chk_AnalysisTruck');
        const MsMESaleList = isChecked('#chk_MSMESaleList');
        const PartyItemWise = isChecked('#chk_PartyItemWise');
        const SaleRegisterDrCrNote = isChecked('#chk_SaleRegistercrdr');
        const BileItemwise = isChecked('#chk_BileItemWise');
        const LoadUnLoadReport = isChecked('#chk_LoadUnlaodReport');
        const WBReport = isChecked('#chk_WBReport');
        const SalesThrough = isChecked('#chk_salesThrough');
        const ColourAnalysisTruck = isChecked('#chk_colurAnalysis');
        const ColourGrpAnalysisTruck = isChecked('#chk_colurGrpAnalysis');
        const DenierAnalysisTruck = isChecked('#chk_DenierAnalysisTruck');
        const GateToInvoiceTime = isChecked('#chk_GateToInvoiceTime');

        const MonthWiseCustomer = isChecked('#chk_MonthWiseCustomer');
        const MonthWiseItem = isChecked('#chk_MonthWiseItem');
        const MonthWisecolor = isChecked('#chk_MonthWisecolor');
        const MonthWiseItemGroup = isChecked('#chk_MonthwiseItemGroup');
        const MonthWisePartyItem = isChecked('#chk_MonthWisePartyItem');
        const MonthWiseAgent = isChecked('#chk_MonthWiseAgent');

        const ItemGroup = isChecked('#chk_ItemGroup');
        const ItemWise = isChecked('#chk_ItemWise');
        const PartyWise = isChecked('#chk_PartyWise');
        const ItemDateRateList = isChecked('#chk_ItemDateRateList');
        const SaleListDetail = isChecked('#chk_SaleListDetail');

        const SalesRegisterCancel = isChecked('#chk_SalesRegisterCancel');
        const SalesRegisterDetailCancel = isChecked('#chk_SalesRegisterDetailCancel');

        const Group = getValue('#ddlGroup');
        const Row = getValue('#ddlRow');
        const Column = getValue('#ddlColumn');

        let reportName = "";
        let selectionFormula = "";
        let formulaFields = {};
        let isCombinedReport = false;

        const combinedReportSelected = ItemGroup || ItemWise || PartyWise || ItemDateRateList || ColourAnalysisTruck || MonthWiseCustomer || MonthWiseAgent ||
        MonthWiseItemGroup || MonthWiseItem || MonthWisecolor || MonthWisePartyItem || SaleListDetail;

        if (combinedReportSelected)
        {

            isCombinedReport = true;

            selectionFormula = `{VW_SALES_COMBINED.V_DATE} IN ${FromCrystalDate} TO ${ToCrystalDate}`;

            selectionFormula = addCondition( selectionFormula, `{VW_SALES_COMBINED.COMP_CODE} = ${CompCode}` );

            selectionFormula = addCondition( selectionFormula, `{VW_SALES_COMBINED.BRANCH_CODE} = ${BranchCode}`);

            if (SelectDocumentType && DocumentType) {

                selectionFormula = addCondition( selectionFormula,  `{VW_SALES_COMBINED.V_TYPE} = ${crystalString(DocumentType)}` );

            } else
            {

                const documentTypes = [
                    "SalesInvoice",
                    "EComSalesInvoice",
                    "TradingSale",
                    "EComSalesReturn",
                    "SalesReturn",
                    "GSTCreditNote",
                    "GSTDebitNote"
                ];

                const typeConditions = documentTypes.map(type =>
                    `{DOCTYPE_MAST.DOCTYPE} = ${crystalString(type)}`
                );

                selectionFormula = addCondition(
                    selectionFormula,
                    `(${typeConditions.join(" OR ")})`
                );
            }

            // Item Type
            if (SelectItemType && ItemType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{VW_SALES_COMBINED.ITEM_TYPE} = ${crystalString(ItemType)}`
                );
            }

            // Agent / OS Group
            if (SelectOSGroup && OsGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.OS_CODE} = ${crystalNumber(OsGroup)}`
                );
            }

            // Account Group
            if (SelectAcGroup && ACGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.GROUP_CODE} = ${crystalNumber(ACGroup)}`
                );
            }

            // Transport
            if (SelectTransport && TransPort) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{VW_SALES_COMBINED.TRANSPORT_CODE} = ${crystalNumber(TransPort)}`
                );
            }

            // Color
            if (SelectColor && Color) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.COLOR_CODE} = ${crystalNumber(Color)}`
                );
            }

            // Denier / Gram
            if (SelectDenierGram && DenierGram) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.GRAM_CODE} = ${crystalNumber(DenierGram)}`
                );
            }

            // Godown
            if (SelectGodown && Godown) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{VW_SALES_COMBINED.GODOWN_CODE} = ${crystalNumber(Godown)}`
                );
            }

            // Supply Type
            if (SelectSupplyType && SupplyType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{VW_SALES_COMBINED.SUPPLY_TYPE} = ${crystalString(SupplyType)}`
                );
            }

            // Country
            if (SelectCountry && Country) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.COUNTRY_CODE} = ${crystalNumber(Country)}`
                );
            }

            // Product Type
            if (SelectPtypeGroup && PType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.PTYPE_CODE} = ${crystalNumber(PType)}`
                );
            }

            // Main Item Group
            if (SelectItemMGroup && ItemMGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.MGROUP_CODE} = ${crystalNumber(ItemMGroup)}`
                );
            }

            // Sale Through
            if (SelectSaleThrough && SaleThrough) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SAUDA.DEAL_THROUGH} = ${crystalNumber(SaleThrough)}`
                );
            }

            // Multiple Party Selection
            if (SelectPartyName && Party.length > 0) {

                const partyCondition = numericInCondition(
                    '{SUBGROUP_MAST.CODE}',
                    Party
                );

                selectionFormula = addCondition(
                    selectionFormula,
                    partyCondition
                );
            }

            // MULTIPLE ITEM SELECTION FROM GRID
            if (SelectItem && Item.length > 0) {

                const itemCondition = numericInCondition(
                    '{ITEM_MAST.CODE}',
                    Item
                );

                selectionFormula = addCondition(
                    selectionFormula,
                    itemCondition
                );
            }
        }

        // =========================================
        // NORMAL REPORT
        // =========================================

        else {

            selectionFormula =
                `{SALE1.V_DATE} IN ${FromCrystalDate} TO ${ToCrystalDate}`;

            selectionFormula = addCondition(
                selectionFormula,
                `{SALE1.COMP_CODE} = ${CompCode}`
            );

            selectionFormula = addCondition(
                selectionFormula,
                `{SALE1.BRANCH_CODE} = ${BranchCode}`
            );

            if (SalesRegisterCancel || SalesRegisterDetailCancel) {

                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.STATUS} = 2`
                );

            } else {

                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.STATUS} <> 2`
                );
            }

            if (SelectDocumentType && DocumentType) {

                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.V_TYPE} = ${crystalString(DocumentType)}`
                );

            } else {

                let documentTypes = [];

                if (TptFreightReport) {

                    documentTypes = [
                        "SalesInvoice", "EComSalesInvoice", "TradingSale",
                        "EComSalesReturn", "SalesReturn", "JobReturn",
                        "SaleChallan", "JobworkIssue", "JobworkReceived"
                    ];

                } else {

                    documentTypes = [
                        "SalesInvoice",
                        "EComSalesInvoice",
                        "TradingSale"
                    ];
                }

                const typeConditions = documentTypes.map(type =>
                    `{DOCTYPE_MAST.DOCTYPE} = ${crystalString(type)}`
                );

                selectionFormula = addCondition(
                    selectionFormula,
                    `(${typeConditions.join(" OR ")})`
                );
            }

            // Item Type
            if (SelectItemType && ItemType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.ITEM_TYPE} = ${crystalString(ItemType)}`
                );
            }

            // Agent / OS Group
            if (SelectOSGroup && OsGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.OS_CODE} = ${crystalNumber(OsGroup)}`
                );
            }

            // Account Group
            if (SelectAcGroup && ACGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.GROUP_CODE} = ${crystalNumber(ACGroup)}`
                );
            }

            // Transport
            if (SelectTransport && TransPort) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.TRANSPORT_CODE} = ${crystalNumber(TransPort)}`
                );
            }

            // Color
            if (SelectColor && Color) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.COLOR_CODE} = ${crystalNumber(Color)}`
                );
            }

            // Denier / Gram
            if (SelectDenierGram && DenierGram) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.GRAM_CODE} = ${crystalNumber(DenierGram)}`
                );
            }

            // Godown
            if (SelectGodown && Godown) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.GODOWN_CODE} = ${crystalNumber(Godown)}`
                );
            }

            // Supply Type
            if (SelectSupplyType && SupplyType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SALE1.SUPPLY_TYPE} = ${crystalString(SupplyType)}`
                );
            }

            // Country
            if (SelectCountry && Country) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SUBGROUP_MAST.COUNTRY_CODE} = ${crystalNumber(Country)}`
                );
            }

            // Product Type
            if (SelectPtypeGroup && PType) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.PTYPE_CODE} = ${crystalNumber(PType)}`
                );
            }

            // Main Item Group
            if (SelectItemMGroup && ItemMGroup) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{ITEM_MAST.MGROUP_CODE} = ${crystalNumber(ItemMGroup)}`
                );
            }

            // Sale Through
            if (SelectSaleThrough && SaleThrough) {
                selectionFormula = addCondition(
                    selectionFormula,
                    `{SAUDA.DEAL_THROUGH} = ${crystalNumber(SaleThrough)}`
                );
            }

            // Multiple Party Selection from Grid
            if (SelectPartyName && Party.length > 0) {

                const partyCondition = numericInCondition(
                    '{SUBGROUP_MAST.CODE}',
                    Party
                );

                selectionFormula = addCondition(
                    selectionFormula,
                    partyCondition
                );
            }
        }

        // =========================================
        // COMBINED REPORT NAME
        // =========================================

        if (isCombinedReport) {

            // SALE4 - Summary Reports
            if (PartyWise || ItemWise || ItemGroup) {

                reportName = "SALE4";

                if (PartyWise) {

                    formulaFields.GrpName1 = "{ITEM_GROUP.GROUP_TYPE}";
                    formulaFields.GrpName2 = "{SUBGROUP_MAST.NAME}";
                    formulaFields.RPTNAME = crystalString("SALE SUMMARY CUSTOMER WISE");

                } else if (ItemWise) {

                    formulaFields.GrpName1 = "{ITEM_GROUP.GROUP_TYPE}";
                    formulaFields.GrpName2 = "{ITEM_MAST.NAME}";
                    formulaFields.RPTNAME = crystalString("SALE SUMMARY ITEM WISE");

                } else if (ItemGroup) {

                    formulaFields.GrpName1 = "{ITEM_GROUP.GROUP_TYPE}";
                    formulaFields.GrpName2 = "{ITEM_GROUP.NAME}";
                    formulaFields.RPTNAME = crystalString("SALE SUMMARY ITEM-GROUP WISE");
                }
            }

            // SALE6
            else if (ItemDateRateList) {

                reportName = "SALE6";
                formulaFields.RPTNAME = crystalString("ITEM/DATE WISE SALE");
            }

            // SALE5
            else if (ColourAnalysisTruck) {

                reportName = "SALE5";
                formulaFields.RPTNAME = crystalString("SALE ANALYSIS COLOR WISE(TRUCK)");
            }

            // SALE25_PLPL
            else if (MonthWisePartyItem) {

                reportName = "SALE25_PLPL";

                formulaFields.Qty = ChkQty
                    ? "{VW_SALES_COMBINED.Qty}"
                    : "{VW_SALES_COMBINED.ITEM_AMT}+{VW_SALES_COMBINED.PACK_AMT}-{VW_SALES_COMBINED.DISC_AMT}";

                formulaFields.RPTNAME = crystalString("MONTH WISE SALE (PARTY/ITEM WISE)");
            }

            // SALE_COMBINED1
            else if (SaleListDetail) {

                reportName = "SALE_COMBINED1";
                formulaFields.RPTNAME = crystalString("Sale Register Details(Combined)");
            }

            // SALE25
            else if (
                MonthWiseCustomer || MonthWiseAgent ||
                MonthWiseItemGroup || MonthWiseItem || MonthWisecolor
            ) {

                reportName = "SALE25";

                formulaFields.Qty = ChkQty
                    ? "{VW_SALES_COMBINED.Qty}"
                    : "{VW_SALES_COMBINED.ITEM_AMT}+{VW_SALES_COMBINED.PACK_AMT}-{VW_SALES_COMBINED.DISC_AMT}";

                if (MonthWiseCustomer) {

                    formulaFields.GrpName1 = "{DOCTYPE_MAST.NAME}";
                    formulaFields.GrpName2 = "{SUBGROUP_MAST.NAME}";
                    formulaFields.RPTNAME = crystalString("MONTH WISE SALE (CUSTOMER WISE)");

                } else if (MonthWiseAgent) {

                    formulaFields.GrpName1 = "{ACOS_MAST.NAME}";
                    formulaFields.GrpName2 = "{SUBGROUP_MAST.NAME}";
                    formulaFields.RPTNAME = crystalString("MONTH WISE SALE (AGENT-CUSTOMER WISE)");

                } else if (MonthWiseItemGroup) {

                    formulaFields.GrpName1 = "{ITEM_GROUP.GROUP_TYPE}";
                    formulaFields.GrpName2 = "{ITEM_GROUP.NAME}";
                    formulaFields.RPTNAME = crystalString("MONTH WISE SALE (ITEM GROUP WISE)");

                } else if (MonthWiseItem) {

                    formulaFields.GrpName1 = "{ITEM_GROUP.NAME}";
                    formulaFields.GrpName2 = "{ITEM_MAST.NAME}";
                    formulaFields.RPTNAME = crystalString("MONTH WISE SALE (ITEM WISE)");

                } else if (MonthWisecolor) {

                    formulaFields.GrpName1 = "{SUBGROUP_MAST.NAME}";
                    formulaFields.GrpName2 = "{ITEM_MAST.COLOR_CODE}";
                    formulaFields.RPTNAME = crystalString("MONTH WISE SALE (COLOR WISE)");
                }
            }
        }

        // =========================================
        // NORMAL REPORT NAME
        // =========================================

        else {

            if (SalesRegister) {

                reportName = "SALE1";

                formulaFields.RPTNAME = crystalString(
                    DocumentType === "SART"
                        ? "Sales Return Register"
                        : "Sales Register"
                );

            } else if (SaleRegisterDrCrNote) {

                reportName = "SALE7";
                formulaFields.RPTNAME = crystalString("Sales Register (With Debit & Credit Note)");

            } else if (SalesRegisterCancel) {

                reportName = "SALE1";
                formulaFields.RPTNAME = crystalString("Sales Register-Cancel");

            } else if (SalesRegisterDetail) {

                reportName = "SALE115";
                formulaFields.RPTNAME = crystalString("Daily Dispatch Register");

            } else if (SalesThrough) {

                reportName = "SALE12";
                formulaFields.RPTNAME = crystalString("Sales Through Report");

            } else if (SalesRegisterDetailCancel) {

                reportName = "SALE115";
                formulaFields.RPTNAME = crystalString("Daily Dispatch Register-Cancel");

            } else if (BileItemwise) {

                reportName = "SALE31";
                formulaFields.RPTNAME = crystalString("BILL & ITEM WISE SALE");

            } else if (PartyItemWise) {

                reportName = "SALE30_PLPL";
                formulaFields.RPTNAME = crystalString("CUSTOMER/ITEM WISE SALE REPORT");

            } else if (TptFreightReport) {

                selectionFormula = addCondition(
                    selectionFormula,
                    "({SALE1.FRT_AMT}>0 OR {SALE1.INSU_AMT}>0)"
                );

                reportName = "FREIGHT2";
                formulaFields.RPTNAME = crystalString("TRANSPORT FREIGHT REPORT(SALE)");

            } else if (LoadUnLoadReport) {

                reportName = "FREIGHT5";

                selectionFormula = addCondition(
                    selectionFormula,
                    "{SALE1.LOAD_AMT}>0"
                );

                formulaFields.RPTNAME = crystalString("LOADING/UNLOADING REPORT(SALE)");

            } else if (WBReport) {

                reportName = "FREIGHT6";

                selectionFormula = addCondition(
                    selectionFormula,
                    "{SALE1.WB_AMT}>0"
                );

                formulaFields.RPTNAME = crystalString("WEIGH BRIDGE REPORT(SALE)");

            } else if (AgentWiseSale) {

                reportName = "SALE30";
                formulaFields.RPTNAME = crystalString("AGENT WISE SALE REPORT");

            } else if (ColourGrpAnalysisTruck) {

                reportName = "SALE5C";
                formulaFields.RPTNAME = crystalString("SALE ANALYSIS COLOR GROUP WISE(TRUCK)");

            } else if (AnalysisTruck) {

                reportName = "SALE5B";
                formulaFields.RPTNAME = crystalString("SALE ANALYSIS (TRUCK)");

            } else if (AnalysisTon) {

                reportName = "SALE5D";

                selectionFormula = addCondition(
                    selectionFormula,
                    "{@qty_truck} >= .51"
                );

                formulaFields.RPTNAME = crystalString("SALE ANALYSIS (in TONES)");

            } else if (DenierAnalysisTruck) {

                reportName = "SALE5A";
                formulaFields.RPTNAME = crystalString("SALE ANALYSIS COLOR/DENIER WISE(TRUCK)");

            } else if (MsMESaleList) {

                reportName = "SALE1_MSME";

                selectionFormula = addCondition(
                    selectionFormula,
                    "{SUBGROUP_MAST.MSME_NO} <> ''"
                );

                formulaFields.RPTNAME = crystalString("MSME SALE LIST");

            } else if (GateToInvoiceTime) {

                reportName = "SALE1_TIMEDIFF";
                formulaFields.RPTNAME = crystalString("INVOICE TIME REPORT");

            } else if (getValue('#ddlGroup') && getValue('#ddlRow') && getValue('#ddlColumn')) {

                reportName = "SALE8";

                if (Row === Column || Row === Group) {
                    toastr.warning("Row should be different from Column and Group.");
                    return;
                }

                const dynamicFields = {
                    ItemName: "{ITEM_MAST.NAME}",
                    Date: "{SALE2.V_DATE}",
                    Month: "totext({SALE2.V_DATE},'yyyy-MM')",
                    Year: "{YEAR_MAST.CURR_YEAR}",
                    PartyName: "{SUBGROUP_MAST.NAME}",
                    StateName: "{STATE_MAST.NAME}",
                    StateType: "{STATE_MAST.STATE_TYPE}",
                    ItemType: "{SALE1.ITEM_TYPE}",
                    ItemCat: "{ITEMCAT_MAST.NAME}",
                    ItemGram: "{ITEMGRAM_MAST.NAME}",
                    ItemGroup: "{ITEM_GROUP.NAME}",
                    ItemMGroup: "{ITEM_MGROUP.NAME}",
                    ItemMainGroup: "{ITEM_MGROUP.MGROUP_TYPE}",
                    ItemColor: "{COLOR_MAST.NAME}",
                    ItemMColor: "{COLORGROUP_MAST.NAME}",
                    ItemPType: "{ITEMPTYPE_MAST.NAME}",
                    ItemPTypeGrp: "{ITEMPTYPE_MAST.PTYPE}",
                    ItemHSN: "{SALE2.HSN_CODE}",
                    DocId: "{SALE1.DOC_ID}"
                };

                if (!dynamicFields[Group] || !dynamicFields[Row] || !dynamicFields[Column]) {
                    toastr.warning("Invalid Row, Column or Group selection.");
                    return;
                }

                formulaFields.NameRowGrp = dynamicFields[Group];
                formulaFields.NameRow = dynamicFields[Row];
                formulaFields.NameCol = dynamicFields[Column];
                formulaFields.RPTNAME = crystalString("SALES REPORT");
            }
        }

        // =========================================
        // VALIDATE REPORT
        // =========================================

        if (!reportName) {
            toastr.warning("Please select a report.");
            return;
        }

        // =========================================
        // REPORT PARAMETERS
        // =========================================

        const reportDateText =
            `From ${formatReportDate(FromDate)} To ${formatReportDate(ToDate)}`;

        formulaFields.comp_name = crystalString(globalVars.CompanyName || "");
        formulaFields.comp_add1 = crystalString(globalVars.Address1 || "");
        formulaFields.comp_add2 = crystalString(globalVars.Address2 || "");
        formulaFields.f1 = crystalString(reportDateText);

        const payload = {
            Reportname: reportName,
            Database: globalVars.Database ||
                (typeof database !== "undefined" ? database : ""),
            selectionFormula: selectionFormula,
            Parameters: formulaFields
        };

        console.log("Report Name:", reportName);
        console.log("Selection Formula:", selectionFormula);
        console.log("Selected Items:", selectedItems);
        console.log("Selected Parties:", selectedParties);
        console.log("Final Payload:", payload);

        // =========================================
        // PDF DOWNLOAD
        // =========================================

        const now = new Date();

        const timestamp =
            String(now.getDate()).padStart(2, '0') +
            String(now.getMonth() + 1).padStart(2, '0') +
            String(now.getFullYear()).slice(-2) + "_" +
            String(now.getHours()).padStart(2, '0') +
            String(now.getMinutes()).padStart(2, '0') +
            String(now.getSeconds()).padStart(2, '0');

        const response = await $.ajax({
            url: 'http://localhost:24085/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(payload),
            contentType: "application/json; charset=utf-8",
            xhrFields: {
                responseType: 'blob'
            }
        });

        if (!response || response.size === 0) {
            throw new Error("Crystal Reports returned an empty PDF.");
        }

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

        // Delay URL cleanup until browser has started the download
        setTimeout(() => URL.revokeObjectURL(fileUrl), 1000);

        toastr.success("Report generated successfully.");
    }
    catch (error) {

        console.error("Transit Report Error:", error);

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

                } catch {
                    errorMessage = responseText;
                }

            } catch (readError) {
                console.error("Error reading API response:", readError);
            }
        }

        toastr.error("Report generation failed: " + errorMessage);
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

async function DataInsert(SaleRegisterDrCrNote, TptFreightReport, FromDate, Todate) {
    try
    {
        const res = await $.ajax({
            url: '/PerformaRegisterReport/DataInsert',
            type: 'POST',
            data: { SaleRegisterDrCrNote: SaleRegisterDrCrNote, TptFreightReport: TptFreightReport, FromDate: FromDate, Todate: Todate },
            dataType: 'json'
         });               
    }
    catch (error)
    {
        console.error("DataInsert Error:", error);
        toastr.error(error.responseJSON?.message || "Error while inserting data.");
    }
}