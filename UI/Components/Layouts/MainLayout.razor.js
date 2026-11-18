let _breakpointQuery;
let _breakpointHandler;

export function observeSidebarBreakpoint(componentInstance, breakpoint) {
  disposeSidebarBreakpoint();

  _breakpointQuery = window.matchMedia(`(max-width: ${breakpoint}px)`);
  _breakpointHandler = event => {
    componentInstance.invokeMethodAsync('SetSidebarBreakpointState', event.matches);
  };

  _breakpointHandler(_breakpointQuery);
  _breakpointQuery.addEventListener('change', _breakpointHandler);
}

export function disposeSidebarBreakpoint() {
  if (_breakpointQuery && _breakpointHandler) {
    _breakpointQuery.removeEventListener('change', _breakpointHandler);
  }

  _breakpointQuery = null;
  _breakpointHandler = null;
}