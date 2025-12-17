export function initDashboard(id, componentInstance) {
  // If you included GridStack via CDN/Script tag in index.html, 
  // it's available as a global variable.
  var grid = GridStack.init({
    column: 100,
    cellHeight: 10,
    margin: 5,
    float: true,
    acceptWidgets: true,
    disableResize: true,
    draggable: {
      handle: '.grid-stack-item-content', // This prevents the button click issue
      scroll: true
    }
  }, document.getElementById(id));

  // Handle events
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

    componentInstance.invokeMethodAsync('OnDashboardUpdate', updates);
  });
}

export function resizeWidget(elementId) {
  // Find the specific grid item by the data-id we set
  let el = document.querySelector(`.grid-stack-item[data-id='${elementId}']`);
  if (el && el.gridstackNode && el.gridstackNode.grid) {
    // Tell GridStack to re-measure this specific item
    el.gridstackNode.grid.resizeToContent(el);
  }
}