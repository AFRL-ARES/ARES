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

export function refreshGrid(id) {
  var el = document.getElementById(id);

  // Check if the grid is already initialized on this element
  if (el && el.gridstack) {
    var grid = el.gridstack;

    // This is the magic command. 
    // It tells GridStack: "Look at my children. If any aren't widgets yet, make them widgets."
    grid.makeWidget('.grid-stack-item');
  }
}