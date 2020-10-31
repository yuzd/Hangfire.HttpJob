"use strict";

function ColorGenerator() {
    var self = this;

    function shuffle(arr) {
        var ctr = arr.length, temp, index;
        while (ctr > 0) {
            index = Math.floor(Math.random() * ctr);
            ctr--;
            temp = arr[ctr];
            arr[ctr] = arr[index];
            arr[index] = temp;
        }
        return arr;
    }

    self._index = 0;
    self._colorsList = shuffle([
        "#f44336",
        "#e91e63",
        "#9c27b0",
        "#673ab7",
        "#3f51b5",
        "#2196f3",
        "#39cccc",
        "#009688",
        "#4caf50",
        "#01ff70",
        "#acc236",
        "#ffc107",
        "#ff9800",
        "#ff5722",
        "#607d8b",
        "#85144b",
        "#001f3f"
    ]);
    self._colorCache = {};

    self.getColor = function (name) {
        if (self._colorCache[name] != undefined) {
            return self._colorCache[name];
        }

        var color;
        if (self._index < self._colorsList.length) {
            color = self._colorsList[self._index++];

        } else {
            color = self.getRandomColor();
        }

        self._colorCache[name] = color;
        return color;
    };

    self._hexSymbols = "0123456789abcdef".split("");
    self.getRandomColor = function () {
        var color = "#";
        for (var i = 0; i < 6; i++) {
            color += self._hexSymbols[Math.floor(Math.random() * 16)];
        }
        return color;
    };
}

// GRAPH
function SeriesGraph(element, tickFormat, colorGenerator, pollInterval) {
    var self = this;
    self._colorGenerator = colorGenerator;
    self._seriesIndex = 0;
    self._chart = new Chart(element,
        {
            type: "line",
            data: { datasets: [] },
            options: {
                aspectRatio: 1,
                scales: {
                    xAxes: [
                        {
                            type: "realtime",
                            realtime: { duration: 60 * 1000, delay: pollInterval + 1000 },
                            time: {
                                stepSize: 10,
                                unit: "second",
                                tooltipFormat: "LL LTS",
                                displayFormats: { second: "LTS", minute: "LTS" }
                            },
                            ticks: {
                                maxRotation: 0
                            }
                        }
                    ],
                    yAxes: [
                        {
                            ticks: {
                                beginAtZero: true,
                                precision: 0,
                                min: 0,
                                maxTicksLimit: 10,
                                suggestedMax: 10,
                                callback: tickFormat
                            }
                        }
                    ]
                },
                elements: { line: { tension: 0 }, point: { radius: 0 } },
                animation: { duration: 0 },
                hover: { animationDuration: 0 },
                responsiveAnimationDuration: 0,
                legend: { display: false },
                tooltips: {
                    position: "nearest",
                    mode: "index",
                    intersect: false,
                    callbacks: {
                        label: function (tooltipItem, data) {
                            var label = data.datasets[tooltipItem.datasetIndex].label;
                            label += ': ';
                            label += tickFormat(tooltipItem.yLabel);
                            return label;
                        }
                    }
                }
            }
        });

    self.appendData = function (timestamp, id, label, data) {
        var now = new Date(timestamp);

        var server = ko.utils.arrayFirst(self._chart.data.datasets,
            function (s) { return s.id === id; });

        if (server == null) {
            var seriesColor = self._colorGenerator.getColor(id);
            server = {
                id: id,
                label: label,
                borderColor: seriesColor,
                backgroundColor: Chart.helpers.color(seriesColor).alpha(0.3).rgbString(),
                fill: "origin",
                hidden: false,
                error:false,
                data: []
            };
            self._chart.data.datasets.push(server);
            self._seriesIndex++;
        }

        server.data.push({ x: now, y: data });
    };

    self.update = function () {
        self._chart.update();
    }
};

// UTILITY
var formatPercentage = function (value, index, values) {
    return numeral(value).format("0.[00]") + "%";
};

var formatBytes = function (value, index, values) {
    return numeral(value).format("0.[00] b");
};

// MODEL
var UtilizationViewModel = function (cpuGraph, memGraph, colorGenerator) {
    var self = this;
    self._cpuGraph = cpuGraph;
    self._memGraph = memGraph;
    self._colorGenerator = colorGenerator;

    self.serverList = ko.observableArray();

    self.hideSeries = function (row) {
        row.hidden(!row.hidden());
        var hidden = row.hidden();
        self._hideSeries(row.name, hidden, self._cpuGraph);
        self._hideSeries(row.name, hidden, self._memGraph);
    };

    self._hideSeries = function (id, hidden, graph) {
        var server = ko.utils.arrayFirst(graph._chart.data.datasets,
            function (s) { return s.id === id; });
        if (server != null) {
            server.hidden = hidden;
            graph.update();
        };
    };

    self.getServerView = function (name, data) {
        var server = ko.utils.arrayFirst(self.serverList(),
            function (s) { return s.name === name; });

        var cpuUsage = formatPercentage(data.cpuUsagePercentage);
        var ramUsage = formatBytes(data.workingMemorySet);
        var diskUsage = formatBytes(data.diskUsage);
        if (server == null) {
            server = {
                displayName: ko.observable(data.displayName),
                displayColor: ko.observable(self._colorGenerator.getColor(name)),
                hidden: ko.observable(false),
                error: ko.observable(data.error),
                name: name,
                processId: ko.observable(data.processId),
                processName: ko.observable(data.processName),
                cpuUsage: ko.observable(cpuUsage),
                cpuUsageRawValue: ko.observable(data.cpuUsagePercentage),
                ramUsage: ko.observable(ramUsage),
                ramUsageRawValue: ko.observable(data.workingMemorySet),
                diskUsage: ko.observable(diskUsage)
            };
            self.serverList.push(server);
        } else {
            server.processId(data.processId);
            server.processName(data.processName);
            server.cpuUsage(cpuUsage);
            server.cpuUsageRawValue(data.cpuUsagePercentage);
            server.ramUsage(ramUsage);
            server.ramUsageRawValue(data.workingMemorySet);
            server.diskUsage(diskUsage);
            server.error(data.error);
        }

        return server;
    };

    self.clear = function () {
        self.serverList([]);
        self._cpuGraph._chart.data.datasets = [];
        self._cpuGraph.update();
        self._memGraph._chart.data.datasets = [];
        self._memGraph.update();
    }
}

// UPDATER
var updater = function (viewModel, cpuGraph, memGraph, updateUrl) {
    $.get(updateUrl,
        function (data) {
            var newServerViews = [];

            for (var i = 0; i < data.length; i++) {
                var current = data[i];
                var server = viewModel.getServerView(current.name, current);
                cpuGraph.appendData(current.timestamp, server.name, server.displayName(), current.cpuUsagePercentage);
                memGraph.appendData(current.timestamp, server.name, server.displayName(), current.workingMemorySet);
                newServerViews.push(server);
            }

            cpuGraph.update();
            memGraph.update();

            viewModel.serverList(newServerViews);
            viewModel.serverList.orderField(viewModel.serverList.orderField());
        })
        .fail(function () { viewModel.clear() });
};

// INITIALIZATION
window.onload = function () {
    var updateUrl = $("#heartbeatConfig").data("pollurl");
    var updateInterval = parseInt($("#heartbeatConfig").data("pollinterval"));
    var showFullNameInPopup = $("#heartbeatConfig").data("showfullname") === "true";

    var colorGenerator = new ColorGenerator();
    var cpuGraph = new SeriesGraph("cpu-chart", formatPercentage, colorGenerator, updateInterval);
    var memGraph = new SeriesGraph("mem-chart", formatBytes, colorGenerator, updateInterval);
    var viewModel = new UtilizationViewModel(cpuGraph, memGraph, colorGenerator);
    ko.applyBindings(viewModel);

    updater(viewModel, cpuGraph, memGraph, updateUrl);
    setInterval(function () { updater(viewModel, cpuGraph, memGraph, updateUrl); }, updateInterval);
};
