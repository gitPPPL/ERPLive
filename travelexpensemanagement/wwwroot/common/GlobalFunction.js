$(document).ready(function () {
    
    /* =========================
       DESKTOP TAB SWITCHING
    ========================== */
    $('.erppage-tab').on('click', function () {
        var tabId = $(this).data('tab');

        // remove active from all tabs
        $('.erppage-tab').removeClass('active');

        // add active to clicked tab
        $(this).addClass('active');

        // hide all tab content
        $('.erppage-tab-content').removeClass('active');

        // show selected content
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
    $('.erppage-tab').on('click', function () {
        var tabId = $(this).data('tab');

        $('.erppage-tab-content').removeClass('active');
        $('#' + tabId).addClass('active');
    });
    
});

