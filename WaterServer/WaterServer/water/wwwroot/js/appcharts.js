const config = {
    type: 'bar',
    data: null,
    options: {
        plugins: {
            title: {
                display: true,
                text: 'Расход за месяц по дням'
            },
        },
        responsive: true,
        scales: {
            x: {
                stacked: true,
            },
            y: {
                stacked: true
            }
        }
    }
};

const myChart = new Chart(
    document.getElementById('myChart'),
    config
);

$('input[type=radio][name=date_preset]').change(function () {
    switch (this.value) {
        case '1':
        case '4':
            $('input[name=group_by][value=1]').prop("checked", true);
            $('input[name=group_by][value=1]').attr("disabled", false);
            $('input[name=group_by][value=2]').attr("disabled", true);
            $('input[name=group_by][value=3]').attr("disabled", true);
            break;

        case '2':
        case '5':
            $('input[name=group_by][value=1]').prop("checked", true);
            $('input[name=group_by][value=1]').attr("disabled", false);
            $('input[name=group_by][value=2]').attr("disabled", false);
            $('input[name=group_by][value=3]').attr("disabled", true);
            break;

        default:
            $('input[name=group_by]').attr("disabled", false);
            $('input[name=group_by][value=3]').prop("checked", true);
            break;
    }
    switch (this.value) {
        case '1':
            $('input[name=dstart]').val(DateToVal(Date.tuesday()));
            $('input[name=dend]').val(DateToVal(Date.next().monday()));
            break;

        case '2':
            $('input[name=dstart]').val(DateToVal(Date.today().set({ day: 2 })));
            $('input[name=dend]').val(DateToVal(Date.today()));
            break;

        case '3':
            $('input[name=dstart]').val(DateToVal(Date.today().set({ month: 0, day: 2 })));
            $('input[name=dend]').val(DateToVal(Date.today()));
            break;

        case '4':
            $('input[name=dstart]').val(DateToVal(Date.today().addDays(-7)));
            $('input[name=dend]').val(DateToVal(Date.today()));
            break;

        case '5':
            $('input[name=dstart]').val(DateToVal(Date.today().addMonths(-1)));
            $('input[name=dend]').val(DateToVal(Date.today()));
            break;

        case '6':
            $('input[name=dstart]').val(DateToVal(Date.today().addMonths(-12)));
            $('input[name=dend]').val(DateToVal(Date.today()));
            break;
    }

    Update();
});

function DateToVal(d) {
    return d.toISOString().slice(0, 10);
}

function datediff(first, second) {
    return Math.round((second - first) / (1000 * 60 * 60 * 24));
}

$(".target").change(function () {
    $('input[name=date_preset]').prop('checked', false);
    Update();
});

$('input[name=group_by]').change(function () {
    Update();
});

$('.bt-nav').click(function () {
    var dstart = new Date($('input[name=dstart]').val());
    var dend = new Date($('input[name=dend]').val());
    var diff = datediff(dstart, dend);
    if ($(this).attr('id') == 'btn_prev') {
        diff = -diff;
    }

    $('input[name=dstart]').val(DateToVal(dstart.addDays(diff)));
    $('input[name=dend]').val(DateToVal(dend.addDays(diff)));

    Update();
});

function Update() {
    $('#spinner').attr("hidden", false);
    var url = "/api/Graph/By";
    switch ($("input[name='group_by']:checked").val()) {
        case '1':
            url += "Day";
            break;

        case '2':
            break;

        case '3':
            url += "Month";
            break;

        case '4':
            url += "Year";
            break;
    }
    $.ajax({
        type: "GET",
        url: url,
        data: {
            dateStart: $('input[name=dstart]').val(),
            dateEnd: $('input[name=dend]').val()
        },
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        cache: false,
        success: function (r) {
            config.data = r;
            myChart.update();
            $('#spinner').attr("hidden", true);
            $('input[name=dstart],input[name=dend]').removeClass('is-invalid');
        },
        error: function (r) {
            console.log("Error");
            $('#spinner').attr("hidden", true);
            $('input[name=dstart],input[name=dend]').addClass('is-invalid');
        },
        failure: function (r) {
            console.log("Fail");
            $('#spinner').attr("hidden", true);
        }
    });
}

$(document).ready(function () {
    $('input[name=dstart]').val(DateToVal(Date.today().add(-1).months()));
    $('input[name=dend]').val(DateToVal(Date.today()));

    Update();
});