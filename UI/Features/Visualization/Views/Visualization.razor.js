let _componentInstance = null;
let currentMode = 'desktop';


function getGridOptions(columnCount) {
  return {
    column: columnCount,
    cellHeight: 10,
    margin: 5,
    float: true,
    acceptWidgets: true,
    disableResize: false,
    oneColumnSize: 900,
    draggable: {
      handle: '.grid-stack-item-content',
      scroll: true
    }
  };
}

function attachEvents(grid) {
  if (!_componentInstance) return;

  grid.on('change', function (event, items) {
    if (!items) return;
    let updates = items.map(item => {
      return {
        id: item.el.getAttribute('data-id'),
        x: item.x ?? 0,
        y: item.y ?? 0,
        w: item.w,
        h: item.h
      };
    });
    _componentInstance.invokeMethodAsync('OnDashboardUpdate', updates);
  });
}

// --- EXPORTED FUNCTIONS ---
export function initDashboard(containerId, dotNetRef) {
  let grid = GridStack.init({
    cellHeight: '80px',
    margin: 10,
    minRow: 15,
    float: true
  }, `#${containerId}`);

  grid.on('change', function (event, items) {
    if (!items) return;

    const layoutChanges = items.map(item => ({
      UniqueId: item.el.getAttribute('data-id'),
      X: item.x,
      Y: item.y,
      W: item.w,
      H: item.h
    }));

    dotNetRef.invokeMethodAsync('OnDashboardUpdate', layoutChanges);
  });
}

export function refreshGrid(id) {
  var el = document.getElementById(id);
  if (el && el.gridstack) {
    var grid = el.gridstack;
    var allItems = el.querySelectorAll('.grid-stack-item');

    // Filter for new items only
    var newItems = Array.from(allItems).filter(item => !item.gridstackNode);

    newItems.forEach(item => {
      grid.makeWidget(item);
      if (item.getAttribute('gs-size-to-content') === 'true') {
        observeWidget(grid, item);
      }
    });
  }
}

export function resizeToContent(id) {
  setTimeout(() => {
    var el = document.getElementById(id);
    if (el && el.gridstack) {
      var grid = el.gridstack;
      var dynamicItems = el.querySelectorAll('.grid-stack-item[gs-size-to-content="true"]');
      dynamicItems.forEach(function (item) {
        grid.resizeToContent(item);
      });
    }
  }, 50);
}

// --- INTERNAL HELPERS ---

function observeWidget(grid, item) {
  const content = item.querySelector('.grid-stack-item-content div');
  if (!content) return;
  const observer = new ResizeObserver(() => {
    grid.resizeToContent(item);
  });
  observer.observe(content);
}

function handleResponsiveGrid(gridId) {
  var width = document.body.clientWidth;
  var el = document.getElementById(gridId);
  if (!el || !el.gridstack) return;

  var grid = el.gridstack;
  var newColCount = 100; // Default Desktop

  if (width < 900) {
    return;
  } else if (width < 1600) {
    newColCount = 50; // Laptop/Tablet Zoom
  }

  // Only update if changed
  if (grid.getColumn() !== newColCount) {
    grid.column(newColCount, 'none');
  }
}

// Hook resize listener
window.addEventListener('resize', () => handleResponsiveGrid('visualization-board'));