$(document).ready(function () {

    itemMarketRatePagination = Pagination.create({

        pageSize: 10,

        paginationContainer: '#tablePagination',

        infoContainer: '#pageInfoText',

        loader: function (params) {

            $.ajax({
                url: '/ItemMarketRateList/GetAllItemRateList',
                type: 'GET',
                dataType: 'json',
                data: {
                    searchTerm: $('#searchBox').val(),
                    pageNumber: params.pageNumber,
                    pageSize: params.pageSize
                },
                success: function (res) {

                    params.callback({
                        data: res.itemRates,
                        totalCount: res.totalCount
                    });

                },
                error: function (xhr) {
                    toastr.error('Error loading data');
                }
            });
        },

        render: function (lists) {

            let tbody = $('#tblItemMarketRate tbody');
            console.log("list Data:", lists);
            tbody.empty();

            if (!lists || lists.length === 0) {
                tbody.append(`
                        <tr>
                            <td colspan="7" class="text-center">
                                No records found
                            </td>
                        </tr>
                    `);
                return;
            }

            $.each(lists, function (index, item) {

                let actions = '';

                if (window.permissions.canEdit) {
                    actions += `
                    <button class="act-btn edit btn-edit"
                            onclick="editItemMarketRate(${item.v_NO})">
                        <i class="fa fa-edit"></i>
                    </button>`;
                }

                actions += `
                <button class="act-btn view btn-view"
                        onclick="viewItemMarketRate(${item.v_NO})">
                    <i class="fa fa-eye"></i>
                </button>`;

                if (window.permissions.canDelete) {
                    actions += `
                    <button class="act-btn delete btn-delete"
                            onclick="deleteItemMarketRate(${item.v_NO}, '${item.v_TYPE}')">
                        <i class="fa fa-trash"></i>
                    </button>`;
                }

                tbody.append(`
                        <tr>
                            <td style="display:none;">${item.comP_CODE}</td>
                            <td style="display:none;">${item.brancH_CODE}</td>
                            <td style="display:none;">${item.yeaR_CODE}</td>

                            <td style="display:none;">${item.v_TYPE || ''}</td>
                            <td>${item.v_NO || ''}</td>
                            <td>${formatDate(item.v_DATE)}</td>
                            <td>${item.mgrouP_TYPE || ''}</td>
                            <td>${formatDate(item.efF_DATE)}</td>
                            <td>${formatDate(item.exP_DATE)}</td>
                            <td>${item.remarks || ''}</td>
                            <td class="action-col">
                                ${actions}
                            </td>
                        </tr>
                `);
            });
        }
    });
    itemMarketRatePagination.load();
});

$('#searchBox').on('keyup', function () {
    itemMarketRatePagination.load();
});

$('#pageSizeSelect').on('change', function () {

    itemMarketRatePagination.setPageSize(
        parseInt($(this).val())
    );

});

$('#prevBtn').click(function () {
    Pagination.prev();
});

$('#nextBtn').click(function () {
    Pagination.next();
});

function editItemMarketRate(code) {
    window.location.href = '/ItemMarketRate/Index?id=' + encodeURIComponent(code);
}

function viewItemMarketRate(code) {
    window.location.href = '/ItemMarketRate/Index?id=' + encodeURIComponent(code) + '&readOnly=true';
}

function deleteItemMarketRate(code, vType) {
    Swal.fire({
        title: 'Are you sure?',
        text :"This action cannot be undone.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/ItemMarketRateList/DeleteItemMarketRateByCode',
                type: 'POST',
                data: {
                    code: code,
                    vType: vType,
                    compCode: compCode,
                    branchCode: branchCode,
                    yearCode: yearCode
                },
                success: function (response) {
                    if (response.success) {
                        Swal.fire('Deleted!', response.message, 'success').then(() => {
                            itemMarketRatePagination.load();
                        });
                    } else {
                        Swal.fire('Failed', response.message, 'warning');
                    }
                },
                error: function () {
                    Swal.fire('Error!', 'An error occurred while deleting.', 'error');
                }
            });
        }
    });
}

function formatDate(dateString) {

    if (!dateString) return '';

    const date = new Date(dateString);

    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();

    return `${day}/${month}/${year}`;
}