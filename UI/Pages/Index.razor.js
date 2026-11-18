let _componentInstance = null;
const _dashboardObservers = new Map();
const BASE_COLUMN_COUNT = 100;

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
      cancel: '.grid-stack-interactive, .grid-stack-interactive *',
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

  const el = document.getElementById(id);
  if (!el) return;

  const columnCount = getResponsiveColumnCount(el.clientWidth);
  prepareResponsiveWidths(el, columnCount);

  const options = getGridOptions(columnCount);
  const grid = GridStack.init(options, el);

  attachEvents(grid);

  const initialItems = el.querySelectorAll('.grid-stack-item[gs-size-to-content="true"]');
  initialItems.forEach(item => {
    observeWidget(grid, item);
  });

  const observer = new ResizeObserver(() => {
    applyResponsiveColumns(id);
  });
  observer.observe(el);
  _dashboardObservers.set(id, observer);

  applyResponsiveColumns(id);
  resizeToContent(id, true);
}

export function refreshGrid(id) {
  const el = document.getElementById(id);
  if (el && el.gridstack) {
    const grid = el.gridstack;
    const allItems = el.querySelectorAll('.grid-stack-item');

    const newItems = Array.from(allItems).filter(item => !item.gridstackNode);
    const gridWidth = gridElementWidth(grid);

    newItems.forEach(item => {
      prepareResponsiveWidths(item, grid.getColumn(), gridWidth);
      grid.makeWidget(item);
      if (item.getAttribute('gs-size-to-content') === 'true') {
        observeWidget(grid, item);
      }
    });

    resizeToContent(id, true);
  }
}

export function resizeToContent(id, compact = false) {
  const el = document.getElementById(id);
  if (el && el.gridstack) {
    const grid = el.gridstack;
    const dynamicItems = el.querySelectorAll('.grid-stack-item[gs-size-to-content="true"]');
    dynamicItems.forEach(item => {
      grid.resizeToContent(item);
    });

    if (compact) {
      // Content-driven height changes can leave gaps in the initial layout.
      // Compact after measuring all widgets so the dashboard starts packed.
      grid.compact('compact', true);
    }
  }
}

export function updateWidgetWidth(gridId, widgetId, width) {
  const gridElement = document.getElementById(gridId);
  if (!gridElement || !gridElement.gridstack) return;

  const widget = Array.from(gridElement.querySelectorAll('.grid-stack-item'))
    .find(item => item.getAttribute('data-id') === widgetId);
  if (!widget || !widget.gridstackNode) return;

  widget.dataset.aresBaseWidth = width;
  gridElement.gridstack.update(widget, {
    w: getResponsiveWidgetWidth(
      width,
      gridElement.gridstack.getColumn(),
      getMinimumPixelWidth(widget),
      gridElementWidth(gridElement.gridstack)
    )
  });
}

export function disposeDashboard(id) {
  const observer = _dashboardObservers.get(id);
  if (observer) {
    observer.disconnect();
    _dashboardObservers.delete(id);
  }

  if (_dashboardObservers.size === 0) {
    _componentInstance = null;
  }
}

// --- INTERNAL HELPERS ---

function observeWidget(grid, item) {
  const content = item.querySelector('.grid-stack-item-content');
  if (!content) return;

  const innerWidget = content.firstElementChild;
  const targetToObserve = innerWidget ? innerWidget : content;

  let timeoutId;
  const observer = new ResizeObserver(() => {
    clearTimeout(timeoutId);

    timeoutId = setTimeout(() => {
      grid.resizeToContent(item);
    }, 50);
  });

  observer.observe(targetToObserve);
}
function getResponsiveColumnCount(width) {
  if (width <= 0) {
    return BASE_COLUMN_COUNT;
  }

  if (width < 900) {
    return 1;
  }

  if (width < 1600) {
    return 50;
  }

  return BASE_COLUMN_COUNT;
}

function getResponsiveWidgetWidth(baseWidth, columnCount, minimumPixelWidth, gridWidth) {
  const proportionalWidth = Math.max(1, Math.round(baseWidth * columnCount / BASE_COLUMN_COUNT));
  if (!minimumPixelWidth || !gridWidth) return proportionalWidth;

  const minimumColumnWidth = Math.ceil(minimumPixelWidth / (gridWidth / columnCount));
  return Math.min(columnCount, Math.max(proportionalWidth, minimumColumnWidth));
}

function getMinimumPixelWidth(item) {
  const minimumPixelWidth = Number(item.dataset.minimumWidth);
  return Number.isFinite(minimumPixelWidth) && minimumPixelWidth > 0
    ? minimumPixelWidth
    : 0;
}

function prepareResponsiveWidths(element, columnCount, gridWidth = element.clientWidth) {
  const items = element.matches?.('.grid-stack-item')
    ? [element]
    : element.querySelectorAll('.grid-stack-item');

  items.forEach(item => {
    const declaredWidth = Number(item.dataset.aresBaseWidth ?? item.getAttribute('gs-w'));
    if (!Number.isFinite(declaredWidth) || declaredWidth <= 0) return;

    item.dataset.aresBaseWidth = declaredWidth;
    item.setAttribute('gs-w', getResponsiveWidgetWidth(
      declaredWidth,
      columnCount,
      getMinimumPixelWidth(item),
      gridWidth
    ));
  });
}

function updateWidgetWidths(grid, previousColumnCount, columnCount) {
  const gridWidth = gridElementWidth(grid);
  let didUpdate = false;
  grid.batchUpdate();

  grid.getGridItems().forEach(item => {
    const currentWidth = item.gridstackNode?.w;
    if (!Number.isFinite(currentWidth)) return;

    const baseWidth = Number(item.dataset.aresBaseWidth)
      || currentWidth * BASE_COLUMN_COUNT / previousColumnCount;
    const responsiveWidth = getResponsiveWidgetWidth(
      baseWidth,
      columnCount,
      getMinimumPixelWidth(item),
      gridWidth
    );
    item.dataset.aresBaseWidth = baseWidth;

    if(currentWidth !== responsiveWidth) {
      grid.update(item, { w: responsiveWidth });
      didUpdate = true;
    }
  });

  grid.batchUpdate(false);
  return didUpdate;
}

function gridElementWidth(grid) {
  return grid.el?.clientWidth ?? 0;
}

function applyResponsiveColumns(gridId) {
  const el = document.getElementById(gridId);
  if (!el || !el.gridstack) return;

  const grid = el.gridstack;
  const newColumnCount = getResponsiveColumnCount(el.clientWidth);
  const previousColumnCount = grid.getColumn();

  const columnCountChanged = previousColumnCount !== newColumnCount;
  if (columnCountChanged) {
    // Move and scale existing positions as the column system changes. Using
    // 'none' here leaves old coordinates behind and can scatter the widgets.
    grid.column(newColumnCount, 'moveScale');
  }

  // The column count can remain unchanged while the dashboard becomes narrower.
  // Recheck minimum physical widget widths for every container resize.
  if (columnCountChanged || updateWidgetWidths(grid, previousColumnCount, newColumnCount)) {
    resizeToContent(gridId, true);
  }
}