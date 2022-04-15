const config = {
    type: 'bar',
    data: null,
    options: {
        locale: 'ru-RU',
        responsive: true,
        scales: {
            x: {
                time: {
                    // Luxon format string
                    tooltipFormat: 'DD T'
                },
                title: {
                    display: true,
                    text: 'даты'
                },
                stacked: true
            },
            y: {
                title: {
                    display: true,
                    text: 'литры'
                },
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
            $('input[name=dstart]').val(DateToVal(moment().startOf('week').add(1, 'days')));
            $('input[name=dend]').val(DateToVal(moment().endOf('week')));
            break;

        case '2':
            $('input[name=dstart]').val(DateToVal(moment().startOf('month').add(1, 'days')));
            $('input[name=dend]').val(DateToVal(moment().endOf('month')));
            break;

        case '3':
            $('input[name=dstart]').val(DateToVal(moment().startOf('year').add(1, 'days')));
            $('input[name=dend]').val(DateToVal(moment().endOf('year')));
            break;

        case '4':
            $('input[name=dstart]').val(DateToVal(moment().add(-6, 'days')));
            $('input[name=dend]').val(DateToVal(moment()));
            break;

        case '5':
            $('input[name=dstart]').val(DateToVal(moment().add(-1, 'months')));
            $('input[name=dend]').val(DateToVal(moment()));
            break;

        case '6':
            $('input[name=dstart]').val(DateToVal(moment().add(-1, 'years')));
            $('input[name=dend]').val(DateToVal(moment()));
            break;
    }

    Update();
});

function DateToVal(d) {
    return d.toISOString().slice(0, 10);
}

$(".target").change(function () {
    $('input[name=date_preset]').prop('checked', false);
    Update();
});

function ChangeType(e) {
    switch (e) {
        case '1':
            config.type = 'bar';
            config.options.scales.x.stacked = true;
            config.options.scales.y.stacked = true;
            break;

        case '2':
            config.type = 'line';
            config.options.scales.x.stacked = false;
            config.options.scales.y.stacked = false;
            break;
    }
}

$('input[name=group_by]').change(function () {
    if (this.value == '0') {
        $('input[name=graph_type][value=2]').prop("checked", true);
        $('input[name=graph_type][value=1]').attr("disabled", true);
        config.options.scales.x.type = 'time';
        ChangeType('2');
    }
    else {
        $('input[name=graph_type][value=1]').attr("disabled", false);
        config.options.scales.x.type = 'category';
    }
    Update();
});

$('input[name=graph_type]').change(function () {
    ChangeType(this.value);
    Update();
});

$('.bt-nav').click(function () {
    var dstart = new moment($('input[name=dstart]').val());
    var dend = new moment($('input[name=dend]').val());
    var diff = dend.diff(dstart, 'days');
    if ($(this).attr('id') == 'btn_prev') {
        diff = -diff;
    } else {
        diff += 2;
    }

    $('input[name=dstart]').val(DateToVal(dstart.add(diff, 'days')));
    $('input[name=dend]').val(DateToVal(dend.add(diff, 'days')));

    Update();
});

function Update() {
    $('#spinner').attr("hidden", false);
    var url = "/api/Graph/By";
    switch ($("input[name='group_by']:checked").val()) {
        case '0':
            url += "Hour";
            break;

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
    $('input[name=dstart]').val(DateToVal(moment().add(-1, 'months')));
    $('input[name=dend]').val(DateToVal(moment()));
    Update();
});