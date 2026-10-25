const resizeObservers = new WeakMap();

let plotlyLoadPromise = null;

/**
 * Dynamically loads the offline plotly.min.js file on demand and returns a Promise.
 */

function ensurePlotlyLoaded() {
  if (window.Plotly) {
    return Promise.resolve();
  }

  if (plotlyLoadPromise) {
    return plotlyLoadPromise;
  }

  plotlyLoadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = '/lib/plotly/plotly.min.js';

    script.onload = () => resolve();
    script.onerror = (err) => {
      plotlyLoadPromise = null;
      reject(new Error(`Failed to load Plotly from ${script.src}`));
    };

    document.head.appendChild(script);
  });

  return plotlyLoadPromise;
}

/**
 * Renders or updates a Plotly chart inside a container element.
 * @param {HTMLElement} container 
 * @param {string} plotlyJson
 * @param {boolean} disableInteractions
 */
export async function renderPlot(container, plotlyJson, disableInteractions = false) {
  if (!container || !plotlyJson) return;

  try {
    await ensurePlotlyLoaded();

    if (!window.Plotly) {
      console.error("Plotly is still undefined after script load.");
      return;
    }

    const fig = JSON.parse(plotlyJson);
    const data = fig.data || [];
    const layout = { ...(fig.layout || {}) };
    const config = Object.assign(
      { responsive: true, displayModeBar: true, displaylogo: false },
      fig.config || {}
    );

    layout.autosize = true;
    delete layout.width;
    delete layout.height;

    if (disableInteractions) {
      Object.assign(config, {
        staticPlot: true,
        displayModeBar: false,
        scrollZoom: false,
        doubleClick: false
      });
    }

    await window.Plotly.react(container, data, layout, config);
    observeContainerResize(container);
  } catch (err) {
    console.error("Plotly rendering failed:", err);
  }
}

function observeContainerResize(container) {
  if (resizeObservers.has(container)) return;

  const observer = new ResizeObserver(() => {
    if (window.Plotly && container.data) {
      window.Plotly.Plots.resize(container);
    }
  });

  observer.observe(container);
  resizeObservers.set(container, observer);
}

export function purgePlot(container) {
  if (!container) return;

  const observer = resizeObservers.get(container);
  observer?.disconnect();
  resizeObservers.delete(container);

  if (window.Plotly) {
    window.Plotly.purge(container);
  }
}
