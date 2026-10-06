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
 */
export async function renderPlot(container, plotlyJson) {
  if (!container || !plotlyJson) return;

  try {
    await ensurePlotlyLoaded();

    if (!window.Plotly) {
      console.error("Plotly is still undefined after script load.");
      return;
    }

    const fig = JSON.parse(plotlyJson);
    const data = fig.data || [];
    const layout = fig.layout || {};
    const config = Object.assign(
      { responsive: true, displayModeBar: true, displaylogo: false },
      fig.config || {}
    );

    if (!layout.autosize) {
      layout.autosize = true;
    }

    window.Plotly.react(container, data, layout, config);
  } catch (err) {
    console.error("Plotly rendering failed:", err);
  }
}

export function purgePlot(container) {
  if (container && window.Plotly) {
    window.Plotly.purge(container);
  }
}