let _componentInstance = null;

function getGridOptions(columnCount) {
  return {
    column: columnCount,
    cellHeight: 10,
    margin: 5,
    float: true,
    acceptWidgets: true,
    disableResize: true,
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

export function initDashboard(id, componentInstance) {
  _componentInstance = componentInstance;

  const options = getGridOptions(100);
  const grid = GridStack.init(options, document.getElementById(id));

  attachEvents(grid);

  const el = document.getElementById(id);
  if (el) {
    const initialItems = el.querySelectorAll('.grid-stack-item[gs-size-to-content="true"]');
    initialItems.forEach(item => {
      observeWidget(grid, item);
    });
  }

  resizeToContent(id);
}

export function refreshGrid(id) {
  const el = document.getElementById(id);
  if (el && el.gridstack) {
    const grid = el.gridstack;
    const allItems = el.querySelectorAll('.grid-stack-item');

    const newItems = Array.from(allItems).filter(item => !item.gridstackNode);

    newItems.forEach(item => {
      grid.makeWidget(item);
      if (item.getAttribute('gs-size-to-content') === 'true') {
        observeWidget(grid, item);
      }
    });
  }
}

export function resizeToContent(id) {
  const el = document.getElementById(id);
  if (el && el.gridstack) {
    const grid = el.gridstack;
    const dynamicItems = el.querySelectorAll('.grid-stack-item[gs-size-to-content="true"]');
    dynamicItems.forEach(item => {
      grid.resizeToContent(item);
    });
  }
}

// --- INTERNAL HELPERS ---

function observeWidget(grid, item) {
  const content = item.querySelector('.grid-stack-item-content');
  if (!content) return;

  const observer = new ResizeObserver(() => {
    grid.resizeToContent(item);
  });

  observer.observe(content);
}

function handleResponsiveGrid(gridId) {
  const width = document.body.clientWidth;
  const el = document.getElementById(gridId);
  if (!el || !el.gridstack) return;

  const grid = el.gridstack;
  let newColCount = 100; // Default Desktop

  if (width < 900) {
    return;
  } else if (width < 1600) {
    newColCount = 50;
  }

  // Only update if changed
  if (grid.getColumn() !== newColCount) {
    grid.column(newColCount, 'none');
  }
}

// Hook resize listener
window.addEventListener('resize', () => handleResponsiveGrid('my-dashboard'));