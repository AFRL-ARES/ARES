// wwwroot/js/plotlyInterop.js

const resizeObservers = new WeakMap();

// wwwroot/js/plotlyInterop.js

let plotlyLoadPromise = null;

/**
 * Dynamically loads the offline plotly.min.js file on demand and returns a Promise.
 */
function ensurePlotlyLoaded() {
  // 1. Already loaded on window
  if (window.Plotly) {
    return Promise.resolve();
  }

  // 2. Already in the middle of downloading/parsing
  if (plotlyLoadPromise) {
    return plotlyLoadPromise;
  }

  // 3. Inject script element dynamically
  plotlyLoadPromise = new Promise((resolve, reject) => {
    const script = document.createElement('script');

    // ADJUST THIS PATH to match where your 4.7MB file lives under wwwroot
    // e.g., '/lib/plotly/plotly.min.js' or '/js/plotly.min.js'
    script.src = '/lib/plotly/plotly.min.js';

    script.onload = () => resolve();
    script.onerror = (err) => {
      plotlyLoadPromise = null; // Allow retry on failure
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
    // Await the 4.7MB script download/parse completion
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