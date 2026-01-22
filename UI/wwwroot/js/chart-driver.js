// We use a Map to track chart instances by their Canvas ID
const chartRegistry = new Map();

export function initializeChart(canvasId, config) {
  const ctx = document.getElementById(canvasId);

  // Destroy existing chart if it exists (prevents memory leaks on re-renders)
  if (chartRegistry.has(canvasId)) {
    chartRegistry.get(canvasId).destroy();
  }

  // Initialize Chart.js
  const chart = new Chart(ctx, config);
  chartRegistry.set(canvasId, chart);
}

export function updateChartData(canvasId, newData) {
  const chart = chartRegistry.get(canvasId);
  if (chart) {
    chart.data = newData;
    chart.update('none'); // 'none' mode prevents full re-animation for performance
  }
}

export function disposeChart(canvasId) {
  const chart = chartRegistry.get(canvasId);
  if (chart) {
    chart.destroy();
    chartRegistry.delete(canvasId);
  }
}