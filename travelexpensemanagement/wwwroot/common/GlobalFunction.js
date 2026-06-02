$(document).ready(function () {
    
    /* =========================
       DESKTOP TAB SWITCHING
    ========================== */
    $('.erppage-tab').on('click', function () {
        var tabId = $(this).data('tab');
        $('.erppage-tab').removeClass('active');
        $(this).addClass('active');
        $('.erppage-tab-content').removeClass('active');
        $('#' + tabId).addClass('active');
    });


    /* =========================
       MOBILE ACCORDION SWITCHING
    ========================== */
    $('.erppage-accordion-header').on('click', function () {
        var parentTab = $(this).closest('.erppage-tab-content');
        // if already open then close
        if (parentTab.hasClass('active')) {
            parentTab.removeClass('active');
        } else {
            // close others
            $('.erppage-tab-content').removeClass('active');

            // open clicked
            parentTab.addClass('active');
        }
    });

    /* =========================
       AUTO DEFAULT ACTIVE TAB
    ========================== */
    if ($('.erppage-tab.active').length === 0) {
        $('.erppage-tab:first').addClass('active');
    }

    if ($('.erppage-tab-content.active').length === 0) {
        $('.erppage-tab-content:first').addClass('active');
    }


    /* =========================
       SYNC TAB CLICK WITH ACCORDION
    ========================== */
    //$('.erppage-tab').on('click', function () {
    //    var tabId = $(this).data('tab');

    //    $('.erppage-tab-content').removeClass('active');
    //    $('#' + tabId).addClass('active');
    //});
    
});
function focusElement(fieldId) {

    const $el = $('#' + fieldId);

    if (!$el.length) return;

    // 🔹 1. Handle TAB
    const $tab = $el.closest('.erppage-tab-content');
    if ($tab.length && !$tab.hasClass('active')) {
        const tabId = $tab.attr('id');

        $('.erppage-tab').removeClass('active');
        $('.erppage-tab-content').removeClass('active');

        $(`.erppage-tab[data-tab="${tabId}"]`).addClass('active');
        $tab.addClass('active');
    }

    // 🔹 2. Handle MODAL
    const $modal = $el.closest('.modal');

    if ($modal.length) {

        if (!$modal.hasClass('show')) {
            $modal.modal('show');

            $modal.one('shown.bs.modal', function () {
                setTimeout(() => applyFocus($el), 100);
            });

            return;
        }
    }

    // 🔹 3. Normal focus
    applyFocus($el);
}

function applyFocus($el) {

    $el.addClass('is-invalid').focus();

    const $modalBody = $el.closest('.modal-body');

    if ($modalBody.length) {
        $modalBody.animate({
            scrollTop: $el.position().top - 50
        }, 300);
    } else {
        $('html, body').animate({
            scrollTop: $el.offset().top - 120
        }, 400);
    }
}

function invalidateField(fieldId, message, type = "info") {
    showToast(message, { type: type });
    focusElement(fieldId);
}