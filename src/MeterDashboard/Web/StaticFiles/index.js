if (!window.apiPort)
{
    window.apiPort = 5177;
}

const colors = [
    '#1f77b4',  // muted blue
    '#ff7f0e',  // safety orange
    '#2ca02c',  // cooked asparagus green
    '#d62728',  // brick red
    '#9467bd',  // muted purple
    '#8c564b',  // chestnut brown
    '#e377c2',  // raspberry yogurt pink
    '#7f7f7f',  // middle gray
    '#bcbd22',  // curry yellow-green
    '#17becf'   // blue-teal
];

class UnitList
{
    constructor(){
        // this.onSelectChange = undefined;
        this.selectedUnit = "Second";
        $("body").on("change", 'input[type=radio][name=unit]', (e) => {
            this.selectedUnit = e.target.value;
        });
    }
}

class MeteList
{
    constructor()
    {
        this.ulElement = $("#meters-list");
        this.items = new Set();
        this.selectedMeters = [];
        this.onSelectChange = undefined;
        $("body").on("change", $(".meter-item input[type='checkbox']"), () =>
        {
            this.selectedMeters = $(".meter-item input[type='checkbox']:checked")
                .map((i, e) => $(e).data("meter-name"))
                .toArray();
            this.onSelectChange();
        });
        $("body").on("click", "#clear-meter-filter", () =>
        {
            $(".meter-item input[type='checkbox']").prop("checked", false);
            this.selectedMeters = [];
            this.onSelectChange();
        });
    }

    add(meter)
    {
        if (this.items.has(meter))
            return;

        this.items.add(meter);
        this.ulElement.append(`
            <label class="side-area-list-item meter-item">
                <input type="checkbox" data-meter-name="${meter}">
                ${meter}
            </label>`)
    }

}

$(document).ready(function ()
{
    const baseUrl = `http://localhost:${window.apiPort}/meter-dashboard/api`;
    const loadingPlotsPanel = $(".loading-plots");

    const meterList = new MeteList();
    meterList.onSelectChange = onSelectedMetersChange;

    const unitList = new UnitList();

    let states = []

    let loadingAnimation = setInterval(function ()
    {
        if (loadingPlotsPanel.html().endsWith("..."))
            loadingPlotsPanel.html("Loading")
        else
            loadingPlotsPanel.append(".")
    }, 500);

    setInterval(refreshLoop, 1000);

    function onSelectedMetersChange()
    {
        $(".measurements-grid").empty();
        states = [];
    }

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
        if (measurement.instrumentType == "ActivityInstrument")
            createOrUpdatePlotForActivity(measurement, state);
        else if (measurement.instrumentType == "Histogram")
            createOrUpdatePlotForHistogram(measurement, state);
        else
            createOrUpdatePlotForMetric(measurement, state);

    }

    function createOrUpdatePlotForHistogram(measurement, state)
    {

        let data = []
        measurement.groups
            .filter(function (group)
            {
                return group.window == unitList.selectedUnit;
            })
            .forEach((group, i) =>
            {
                xs = group.xs.map(x => new Date(x + 'Z'));
                data.push({
                    x: xs,
                    y: group.ys.map(y => y.mean - 3 * y.stddev),
                    type: 'scatter',
                    fill: 'tozeroy',
                    fillcolor: '#00000000',
                    mode: 'none',
                    showlegend: false
                });
                data.push({
                    x: xs,
                    y: group.ys.map(y => y.mean + 3 * y.stddev),
                    fill: 'tonexty',
                    fillcolor: colors[i] + "30",
                    type: 'scatter',
                    mode: 'none',
                    showlegend: false
                });
                data.push({
                    x: xs,
                    y: group.ys.map(y => y.mean),
                    type: 'scatter',
                    line: {
                        color: colors[i],
                    },
                    name: Object.keys(group.tags).map(k => k + ": " + group.tags[k]).join("-")
                });
            });
        let layout = {
            xaxis: { fixedrange: true, type: 'date' },
            yaxis: { fixedrange: true },
            margin: { l: 30, r: 0, b: 20, t: 0 },
            height: 200,
            legend: {
                orientation: "h"
            }
            // plot_bgcolor:"transparent",
            // paper_bgcolor:"transparent"
        };
        if (measurement.groups.length == 1)
        {
            layout.showlegend = false;
        }
        const config = {
            displayModeBar: false,
            responsive: true,
            staticPlot: true
        };
        Plotly.react(state.elementName, data, layout, config);
    }

    function createOrUpdatePlotForActivity(measurement, state)
    {
        const group = measurement.groups.find(g => g.window == unitList.selectedUnit);
        const xs = group.xs.map(x => new Date(x + 'Z'));
        const data = [
            {
                x: xs,
                y: group.ys.map(y => y.occurrances),
                type: 'scatter',
                name: "Occurrances"
            },
            {
                x: xs,
                y: group.ys.map(y => Math.max(0, y.meanDuration - 3 * y.stddevDuration)),
                type: 'scatter',
                fill: 'tozeroy',
                fillcolor: '#00000000',
                mode: 'none',
                showlegend: false,
                yaxis: 'y2',
            },
            {
                x: xs,
                y: group.ys.map(y => y.meanDuration + 3 * y.stddevDuration),
                fill: 'tonexty',
                fillcolor: colors[1] + "30",
                type: 'scatter',
                mode: 'none',
                showlegend: false,
                yaxis: 'y2',
            },
            {
                x: xs,
                y: group.ys.map(y => y.meanDuration),
                type: 'scatter',
                line: {
                    color: colors[1],
                },
                name: "Duration (Mean\u00B13\u03C3)",
                yaxis: 'y2',
            }
        ]
        const layout = {
            xaxis: {
                fixedrange: true, 
                type: 'date'
            },
            yaxis: {
                fixedrange: true,
                tickfont: {
                    color: colors[0]
                }
            },
            yaxis2: {
                overlaying: 'y',
                side: 'right',
                tickfont: {
                    color: colors[1]
                }
            },
            margin: { l: 30, r: 90, b: 20, t: 0 },
            height: 200,
            legend: {
                orientation: "h"
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
        measurement.groups
            .filter(function (group)
            {
                return group.window == unitList.selectedUnit;
            })
            .forEach((group) =>
            {
                let xs = group.xs.map(x => new Date(x + 'Z'));
                let ys = group.ys;
                let tagKeys = Object.keys(group.tags);
                //state.lastDate = xs[xs.length - 1];
                data.push({
                    x: xs,
                    y: ys,
                    type: 'scatter',
                    name: Object.keys(group.tags).map(k => k + ": " + group.tags[k]).join("-")
                });
                $.each(tagKeys, (i, t) => allTagKeys.add(t));
            });
        let layout = {
            xaxis: { fixedrange: true, type: 'date'},
            yaxis: { fixedrange: true },
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
});