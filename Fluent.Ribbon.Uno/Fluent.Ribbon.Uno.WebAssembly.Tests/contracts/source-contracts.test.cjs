const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join, resolve } = require('node:path');
const test = require('node:test');

const controls = resolve(__dirname, '..', '..', 'Fluent.Ribbon.Uno.Controls');

function source(...parts) {
  return readFileSync(join(controls, ...parts), 'utf8');
}

test('menu names resolve the polymorphic header and retain access-key/text formatting', () => {
  const presentation = source('Controls', 'MenuItem.Presentation.cs');
  const member = presentation.match(
    /internal string GetMenuHeaderName\(\)\s*\{([\s\S]*?)^\s{4}\}/m,
  );
  assert.ok(member, 'The shared menu accessible-name resolver must remain available.');
  const body = member[1];
  assert.match(body, /var\s+header\s*=\s*\(\(IHeaderedControl\)this\)\.Header\s*;/);
  assert.match(body, /header is string text && RecognizesAccessKey/);
  assert.match(body, /ParseAccessText\(text\)\.Text/);
  assert.match(body, /AutomationPeerHelpers\.GetObjectName\(header\)/);

  const quickAccessItem = source('Controls', 'QuickAccessMenuItem.cs');
  assert.match(
    quickAccessItem,
    /class QuickAccessMenuItem\s*:\s*MenuItem,\s*IHeaderedControl/,
  );
  assert.match(quickAccessItem, /public new object\?\s+Header/);
});

test('split buttons use a valid WASM role without changing the native control type', () => {
  const peers = source('Automation', 'Peers', 'RibbonButtonAutomationPeers.cs');
  const start = peers.indexOf('public partial class RibbonSplitButtonAutomationPeer');
  const end = peers.indexOf('public partial class RibbonTextBoxAutomationPeer', start);
  assert.ok(start >= 0 && end > start, 'The split-button peer must remain a distinct peer.');
  const splitPeer = peers.slice(start, end);

  assert.match(
    splitPeer,
    /GetAutomationControlTypeCore\(\)\s*#if __WASM__\s*(?:\/\/[^\n]*\n\s*)*=> AutomationControlType\.Button;\s*#else\s*=> AutomationControlType\.SplitButton;\s*#endif/,
  );
  assert.match(splitPeer, /RibbonDropDownButtonAutomationPeer,\s*IInvokeProvider/);
  assert.match(splitPeer, /OwnerSplitButton\.InvokePrimaryAction\(\)/);
});
