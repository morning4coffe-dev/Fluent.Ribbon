const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { stripTypeScriptTypes } = require('node:module');
const { join, resolve } = require('node:path');
const { runInNewContext } = require('node:vm');
const test = require('node:test');

assert.ok(process.env.UNO_SOURCE_TREE, 'Set UNO_SOURCE_TREE to the verified, patched Uno source checkout.');
const filename = join(resolve(process.env.UNO_SOURCE_TREE), 'src',
  'Uno.UI.Runtime.Skia.WebAssembly.Browser', 'ts', 'Runtime', 'SemanticElements.ts');
const javascript = stripTypeScriptTypes(readFileSync(filename, 'utf8'), { mode: 'transform' });

function fixture() {
  class Element {
    constructor(tagName) {
      this.tagName = tagName.toUpperCase();
      this.id = '';
      this.style = {};
      this.attributes = new Map();
      this.children = [];
      this.parentElement = null;
      this.listeners = new Map();
    }
    setAttribute(name, value) { this.attributes.set(name, String(value)); }
    getAttribute(name) { return this.attributes.get(name) ?? null; }
    removeAttribute(name) { this.attributes.delete(name); }
    addEventListener(name, callback) {
      const listeners = this.listeners.get(name) ?? [];
      listeners.push(callback);
      this.listeners.set(name, listeners);
    }
    emit(name, data = {}) {
      for (const callback of this.listeners.get(name) ?? []) {
        callback({ preventDefault() {}, ...data });
      }
    }
    appendChild(child) {
      child.remove();
      this.children.push(child);
      child.parentElement = this;
    }
    insertBefore(child, next) {
      child.remove();
      this.children.splice(this.children.indexOf(next), 0, child);
      child.parentElement = this;
    }
    removeChild(child) {
      const index = this.children.indexOf(child);
      assert.ok(index >= 0);
      this.children.splice(index, 1);
      child.parentElement = null;
    }
    remove() { this.parentElement?.removeChild(this); }
    get childElementCount() { return this.children.length; }
    get nextElementSibling() {
      if (!this.parentElement) return null;
      const siblings = this.parentElement.children;
      return siblings[siblings.indexOf(this) + 1] ?? null;
    }
    querySelectorAll(selector) {
      const attribute = selector.match(/^\[([^=\]]+)(?:="([^"]+)")?\]$/);
      assert.ok(attribute, `Unsupported unit-fixture selector ${selector}`);
      const result = [];
      for (const child of this.children) {
        if (child.attributes.has(attribute[1])
          && (attribute[2] === undefined || child.getAttribute(attribute[1]) === attribute[2])) {
          result.push(child);
        }
        result.push(...child.querySelectorAll(selector));
      }
      return result;
    }
  }
  const root = new Element('div');
  root.id = 'uno-semantics-root';
  function find(node, id) {
    if (node.id === id) return node;
    for (const child of node.children) {
      const found = find(child, id);
      if (found) return found;
    }
    return null;
  }
  const frames = [];
  const events = { focus: 0, select: 0 };
  const context = {
    document: {
      createElement: tag => new Element(tag),
      getElementById: id => find(root, id),
    },
    requestAnimationFrame: callback => { frames.push(callback); return frames.length; },
    console,
  };
  runInNewContext(javascript, context, { filename });
  context.Uno.UI.Runtime.Skia.Accessibility = {
    isDebugModeEnabled: () => false,
    getCallbacks: () => ({
      onFocus: () => events.focus++,
      onSelection: () => events.select++,
    }),
  };
  // Node's single-file TS transform does not resolve sibling namespace symbols.
  context.Accessibility = context.Uno.UI.Runtime.Skia.Accessibility;
  const api = context.Uno.UI.Runtime.Skia.SemanticElements;
  return {
    api, root, events,
    element: handle => context.document.getElementById(`uno-semantics-${handle}`),
    flush() {
      while (frames.length) frames.shift()();
    },
    add: (handle = 10, index = 0, label = 'Tab', x = 0) =>
      api.addVirtualizedItem(1, handle, index, 3, x, 0, 80, 24, 'tab', label),
  };
}

test('retired container generations cannot resurrect queued items', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'tablist', 'Tabs', false);
  f.add();
  f.api.unregisterVirtualizedContainer(1);
  f.api.registerVirtualizedContainer(1, 'tablist', 'Replacement', false);
  f.flush();
  assert.equal(f.element(10), null);
  assert.equal(f.element(1).childElementCount, 0);
});

test('a reused item survives stale queued removal without replacing its node', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'tablist', 'Tabs', false);
  f.add();
  f.flush();
  const original = f.element(10);
  f.api.removeVirtualizedItem(10);
  f.add(10, 2, 'Moved');
  f.flush();
  assert.equal(f.element(10), original);
  assert.equal(original.getAttribute('aria-posinset'), '3');
  assert.equal(original.getAttribute('aria-label'), 'Moved');
});

test('realized tab refresh preserves selection, geometry, node identity and one focus handler', () => {
  const f = fixture();
  f.api.createTabListElement(0, 1, null, 0, 0, 320, 24, 'Tabs');
  f.api.registerVirtualizedContainer(1, 'tablist', 'Tabs', false);
  f.api.createTabElement(1, 10, null, 0, 0, 80, 24, 'Tab', true, 1, 3);
  const original = f.element(10);
  f.add(10, 1, 'Renamed', 40);
  f.add(10, 2, 'Latest', 80);
  f.flush();
  assert.equal(f.element(10), original);
  assert.equal(original.style.left, '80px');
  assert.equal(original.getAttribute('role'), 'tab');
  assert.equal(original.getAttribute('aria-selected'), 'true');
  assert.equal(original.getAttribute('aria-label'), 'Latest');
  original.emit('focus');
  original.emit('keydown', { key: ' ' });
  assert.deepEqual(f.events, { focus: 1, select: 1 });
  f.api.updateSelectionState(10, false);
  f.add(10, 2, 'Latest');
  f.flush();
  assert.equal(original.getAttribute('aria-selected'), 'false');
});

test('refresh preserves the managed scaled rectangle instead of reverting to layout size', () => {
  const f = fixture();
  f.api.createTabListElement(0, 1, null, 300, 200, 640, 100, 'Scaled parent');
  f.api.registerVirtualizedContainer(1, 'tablist', 'Scaled parent', false);
  f.api.createTabElement(1, 10, null, 19, 11, 160, 48, 'Scaled tab', true, 1, 2);
  const original = f.element(10);
  f.api.addVirtualizedItem(1, 10, 0, 2, 19, 11, 160, 48, 'tab', 'Scaled tab');
  f.flush();
  assert.equal(f.element(10), original);
  assert.equal(original.style.left, '19px');
  assert.equal(original.style.top, '11px');
  assert.equal(original.style.width, '160px');
  assert.equal(original.style.height, '48px');
  assert.equal(original.getAttribute('aria-selected'), 'true');
});

test('refresh applies a rotated bounding rectangle in semantic-parent coordinates exactly once', () => {
  const f = fixture();
  f.api.createTabListElement(0, 1, null, 300, 200, 640, 200, 'Parent');
  f.api.registerVirtualizedContainer(1, 'tablist', 'Parent', false);
  f.api.createTabElement(1, 10, null, 19, 11, 160, 48, 'Tab', true, 1, 2);
  const original = f.element(10);
  // 80x24 scaled by two and rotated 90 degrees around its origin.
  f.api.addVirtualizedItem(1, 10, 0, 2, -29, 11, 48, 160, 'tab', 'Rotated tab');
  f.flush();
  assert.equal(f.element(10), original);
  assert.equal(original.parentElement, f.element(1));
  assert.equal(original.style.left, '-29px');
  assert.equal(original.style.top, '11px');
  assert.equal(original.style.width, '48px');
  assert.equal(original.style.height, '160px');
  assert.equal(f.element(1).style.left, '300px');
  assert.equal(f.element(1).style.top, '200px');
});

test('registration and realization remove stale roles, labels and multiselect state', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'listbox', 'Old', true);
  f.api.registerVirtualizedContainer(1, 'tablist', '', false);
  assert.equal(f.root.childElementCount, 1);
  assert.equal(f.element(1).getAttribute('role'), 'tablist');
  assert.equal(f.element(1).getAttribute('aria-label'), null);
  assert.equal(f.element(1).getAttribute('aria-multiselectable'), null);
  f.add();
  f.flush();
  f.add(10, 1, '');
  f.flush();
  assert.equal(f.element(10).getAttribute('aria-label'), null);
});

test('item count updates touch only realized nodes and queued mutations drain', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'listbox', 'Items', false);
  f.add(10, 0);
  f.add(11, 1);
  f.flush();
  f.api.updateVirtualizedItemCount(1, 1000000);
  assert.equal(f.element(1).childElementCount, 2);
  assert.equal(f.element(10).getAttribute('aria-setsize'), '1000000');
  assert.equal(f.element(11).getAttribute('aria-setsize'), '1000000');
  if (f.api.virtualizedItemMutations) {
    assert.equal(f.api.virtualizedItemMutations.size, 0);
  }
});

test('reindexed items remain in data order without replacing their DOM nodes', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'tablist', 'Tabs', false);
  f.add(10, 0, 'First');
  f.add(11, 1, 'Second');
  f.flush();
  const first = f.element(10);
  const second = f.element(11);
  f.add(10, 1, 'First');
  f.add(11, 0, 'Second');
  f.flush();
  assert.deepEqual(f.element(1).children, [second, first]);
  assert.equal(f.element(10), first);
  assert.equal(f.element(11), second);
});

test('queued realization uses the latest item count before its animation frame', () => {
  const f = fixture();
  f.api.registerVirtualizedContainer(1, 'listbox', 'Items', false);
  f.add(10, 0);
  f.api.updateVirtualizedItemCount(1, 1000000);
  f.flush();
  assert.equal(f.element(10).getAttribute('aria-setsize'), '1000000');
});

test('a retired item removal cannot remove a new peer-factory node with the same handle', () => {
  const f = fixture();
  f.api.createTabListElement(0, 1, null, 0, 0, 320, 24, 'Tabs');
  f.api.registerVirtualizedContainer(1, 'tablist', 'Tabs', false);
  f.api.createTabElement(1, 10, null, 0, 0, 80, 24, 'Old', true, 1, 3);
  f.api.removeVirtualizedItem(10);
  f.api.unregisterVirtualizedContainer(1);
  f.api.createTabListElement(0, 1, null, 0, 0, 320, 24, 'New tabs');
  f.api.registerVirtualizedContainer(1, 'tablist', 'New tabs', false);
  f.api.createTabElement(1, 10, null, 0, 0, 80, 24, 'New', true, 1, 3);
  const replacement = f.element(10);
  f.flush();
  assert.equal(f.element(10), replacement);
});
