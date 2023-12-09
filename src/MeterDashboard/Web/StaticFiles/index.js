class MeteList
{
    constructor(ulElement)
    {
        this.ulElement = ulElement;
        this.items = new Set();
        this.selectedMeters = [];
        $("body").on("change", $(".meter-item input[type='checkbox']"), () =>
        {
            this.selectedMeters = $(".meter-item input[type='checkbox']:checked")
                .map((i,e) => $(e).data("meter-name"))
                .toArray();
        })
    }

    add(meter)
    {
        if (this.items.has(meter))
            return;

        this.items.add(meter);
        this.ulElement.append(`
            <label class="meter-item">
                <input type="checkbox" data-meter-name="${meter}">
                ${meter}
            </label>`)
    }

}

$(document).ready(function ()
{
    const baseUrl = "http://localhost:5177/meter-dashboard";
    const loadingPlotsPanel = $(".loading-plots");
    const meterList = new MeteList($(".meters-list"));
    let states = []

    let loadingAnimation = setInterval(function ()
    {
        if (loadingPlotsPanel.html().endsWith("..."))
            loadingPlotsPanel.html("Loading")
        else
            loadingPlotsPanel.append(".")
    }, 500)

    setInterval(refreshLoop, 1000);

    function refreshLoop()
    {
        $.ajax({
            url: baseUrl + "/measurements",
            data: JSON.stringify({
                filterMeterNames: meterList.selectedMeters
            }),
            contentType: 'application/json; charset=utf-8',
            traditional: true,
            type: 'POST',
            success: function (items)
            {
                if (items && loadingPlotsPanel.is(':visible'))
                {
                    clearInterval(loadingAnimation);
                    loadingPlotsPanel.hide();
                }

                $.each(items, createOrUpdatePlot);
            }
        });
    }

    function createOrUpdatePlot(index, measurement)
    {
        let state = states.find(s =>
            s.meterName == measurement.meterName &&
            s.instrumentName == measurement.instrumentName);
        if (!state)
        {
            $(".measurements-grid").append(
                `<div class="measurement-item">
                    <div class="meter-name">${measurement.meterName}</div>
                    <div class="instrument-name">${measurement.instrumentName}</div>
                    <div id="measurement-plot-${index}">
                </div>`
            );
            state = {
                meterName: measurement.meterName,
                instrumentName: measurement.instrumentName,
                elementName: `measurement-plot-${index}`
            }
            states.push(state);
            meterList.add(measurement.meterName);
        }
        if(measurement.instrumentType == "ActivityInstrument")
            createOrUpdatePlotForActivity(measurement, state);
        else
            createOrUpdatePlotForMetric(measurement, state);

    }

    function createOrUpdatePlotForActivity(measurement, state){
        const data = [
            {
                x: measurement.metrics[0].xs.map(x => new Date(x + 'Z')),
                y: measurement.metrics[0].ys.map(y => y.occurrances),
                type: 'scatter',
                name: "Occurrances",
            },
            {
                x: measurement.metrics[0].xs.map(x => new Date(x + 'Z')),
                y: measurement.metrics[0].ys.map(y => y.meanDuration),
                type: 'scatter',
                name: "Avg Duration",
                yaxis: 'y2',
            }
        ]
        const layout = {
            xaxis: getXaxis(),
            yaxis: { fixedrange: true },
            xaxis: { fixedrange: true },
            margin: { l: 30, r: 90, b: 20, t: 0 },
            height: 200,
            legend: {
                orientation: "h"
            },
            yaxis2: {
                overlaying: 'y',
                side: 'right',
            },
            // plot_bgcolor:"transparent",
            // paper_bgcolor:"transparent"
        };
        const config = {
            displayModeBar: false,
            responsive: true,
            staticPlot: true
        };
        Plotly.react(state.elementName, data, layout, config);
    }

    function createOrUpdatePlotForMetric(measurement, state)
    {
        let allTagKeys = new Set();
        let data = [];
        $.each(measurement.metrics, (i, metric) =>
        {
            let xs = metric.xs.map(x => new Date(x + 'Z'));
            let ys = metric.ys;
            let tagKeys = Object.keys(metric.tags);
            //state.lastDate = xs[xs.length - 1];
            data.push({
                x: xs,
                y: ys,
                type: 'scatter',
                name: Object.keys(metric.tags).map(k => k + ": " + metric.tags[k]).join("-")
            });
            $.each(tagKeys, (i, t) => allTagKeys.add(t));
        });
        let layout = {
            xaxis: getXaxis(),
            yaxis: { fixedrange: true },
            xaxis: { fixedrange: true },
            margin: { l: 30, r: 0, b: 20, t: 0 },
            height: 200,
            legend: {
                orientation: "h"
            }
            // plot_bgcolor:"transparent",
            // paper_bgcolor:"transparent"
        };
        const config = {
            displayModeBar: false,
            responsive: true,
            staticPlot: true
        };
        Plotly.react(state.elementName, data, layout, config);
    }

    function getXaxis()
    {
        let now = new Date();
        now.setMilliseconds(0);
        let oneMinuteAgo = new Date();
        oneMinuteAgo.setMilliseconds(0);
        oneMinuteAgo.setMinutes(now.getMinutes() - 1);
        return {
            type: 'date',
            range: [oneMinuteAgo, now]
        }
    }
});


// instrumentName
// instrumentUnit
// meterName
