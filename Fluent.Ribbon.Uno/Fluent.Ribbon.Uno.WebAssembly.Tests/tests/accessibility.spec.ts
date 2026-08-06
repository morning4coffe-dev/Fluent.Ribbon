import AxeBuilder from '@axe-core/playwright';
import { expect, type Locator, type Page, test } from '@playwright/test';

const semanticsRootSelector = '#uno-semantics-root';

function semanticsRoot(page: Page): Locator {
  return page.locator(semanticsRootSelector);
}

async function activateAccessibility(page: Page): Promise<void> {
  await page.goto('/');

  const enableAccessibility = page.locator('#uno-enable-accessibility');
  await expect(enableAccessibility).toHaveAttribute(
    'aria-label',
    'Enable accessibility',
    { timeout: 60_000 },
  );

  await page.keyboard.press('Tab');
  await expect(enableAccessibility).toBeFocused();
  await page.keyboard.press('Space');
  await expect(enableAccessibility).toHaveCount(0);

  const root = semanticsRoot(page);
  await expect(root).toBeAttached();
  await expect(
    root.getByRole('group', {
      name: 'Fluent Ribbon Showcase',
      exact: true,
    }),
  ).toHaveCount(1, { timeout: 30_000 });
  await expect
    .poll(() => root.locator('[id^="uno-semantics-"]').count(), {
      message: 'Uno accessibility tree did not activate',
    })
    .toBeGreaterThan(20);
}

async function activateSemanticControl(control: Locator, key = 'Enter') {
  await control.focus();
  await expect(control).toBeFocused();
  await control.press(key);
}

async function selectRibbonTab(page: Page, name: string) {
  const tab = semanticsRoot(page).getByRole('tab', { name, exact: true });
  await activateSemanticControl(tab, 'Space');
  await expect(tab).toHaveAttribute('aria-selected', 'true');
}

test.beforeEach(async ({ page }) => {
  await activateAccessibility(page);
});

test('exposes representative Ribbon semantics without hidden or duplicate controls', async ({
  page,
}) => {
  const root = semanticsRoot(page);

  const ribbon = root.getByRole('group', {
    name: 'Fluent Ribbon Showcase',
    exact: true,
  });
  await expect(ribbon).toHaveCount(1);
  await expect(ribbon).toHaveAttribute('aria-label', 'Fluent Ribbon Showcase');

  const clipboardGroup = root.getByRole('group', {
    name: 'Clipboard',
    exact: true,
  });
  await expect(clipboardGroup).toHaveCount(1);
  await expect(clipboardGroup).toHaveAttribute('aria-label', 'Clipboard');

  const mainContent = root.getByRole('main', {
    name: 'Showcase content',
    exact: true,
  });
  await expect(mainContent).toHaveCount(1);
  await expect(mainContent).toHaveAttribute('aria-label', 'Showcase content');

  const toolbarsTab = root.getByRole('tab', {
    name: 'Toolbars',
    exact: true,
  });
  const insertTab = root.getByRole('tab', { name: 'Insert', exact: true });
  await expect(toolbarsTab).toHaveAttribute('aria-selected', 'true');
  await expect(insertTab).toHaveAttribute('aria-selected', 'false');
  await expect(toolbarsTab.locator('..')).toHaveAttribute('role', 'tablist');

  const formatPainter = root.getByRole('button', {
    name: 'Format Painter',
    exact: true,
  });
  await expect(formatPainter).toHaveCount(1);
  await expect(formatPainter).toHaveJSProperty('tagName', 'BUTTON');

  const bold = root.getByRole('button', { name: 'Bold', exact: true });
  await expect(bold).toHaveAttribute('aria-pressed', 'false');

  const dropdown = root.getByRole('button', {
    name: 'Watermark options',
    exact: true,
  });
  await expect(dropdown).toHaveCount(1);
  await expect(dropdown).toHaveJSProperty('tagName', 'BUTTON');

  const spinner = root.getByRole('slider', {
    name: 'Right margin',
    exact: true,
  });
  await expect(spinner).toHaveCount(1);
  await expect(spinner).toHaveJSProperty('tagName', 'INPUT');
  await expect(spinner).toHaveAttribute('type', 'range');
  await expect(spinner).toHaveAttribute('aria-valuenow', '1');
  await expect(spinner).toHaveAttribute('aria-valuemin', '0');
  await expect(spinner).toHaveAttribute('aria-valuemax', '1000');
  await expect(spinner.locator('[role], input, button')).toHaveCount(0);

  const showcaseHeading = root.getByRole('heading', {
    name: 'Fluent.Ribbon.Uno',
    exact: true,
    level: 1,
  });
  await expect(showcaseHeading).toHaveJSProperty('tagName', 'H1');
  await expect(showcaseHeading).toHaveAttribute('aria-level', '1');
  const controlsHeading = root.getByRole('heading', {
    name: 'Ported Controls Showcase',
    exact: true,
    level: 2,
  });
  await expect(controlsHeading).toHaveJSProperty('tagName', 'H2');
  await expect(controlsHeading).toHaveAttribute('aria-level', '2');

  await expect(root.locator('[aria-label="Insert font"]')).toHaveCount(0);
  await expect(root.locator('[aria-label="Confidential"]')).toHaveCount(0);
  await expect(
    root.getByRole('tab', { name: 'Design', exact: true }),
  ).toHaveCount(0);

  const singularControls = [
    ['group', 'Fluent Ribbon Showcase'],
    ['group', 'Clipboard'],
    ['main', 'Showcase content'],
    ['tab', 'Toolbars'],
    ['tab', 'Insert'],
    ['button', 'Format Painter'],
    ['button', 'Watermark options'],
    ['slider', 'Right margin'],
    ['heading', 'Fluent.Ribbon.Uno'],
    ['heading', 'Ported Controls Showcase'],
  ] as const;
  for (const [role, name] of singularControls) {
    await expect(
      root.getByRole(role, { name, exact: true }),
      `Duplicate or missing semantic ${role} '${name}'`,
    ).toHaveCount(1);
  }
});

test('operates representative controls through the semantic UI', async ({
  page,
}) => {
  const root = semanticsRoot(page);

  const bold = root.getByRole('button', { name: 'Bold', exact: true });
  await activateSemanticControl(bold, 'Space');
  await expect(bold).toHaveAttribute('aria-pressed', 'true');

  const spinner = root.getByRole('slider', {
    name: 'Right margin',
    exact: true,
  });
  await activateSemanticControl(spinner, 'ArrowUp');
  await expect(spinner).toHaveAttribute('aria-valuenow', '2');

  const tableToolsButton = root.getByRole('button', {
    name: 'Toggle Table Tools',
    exact: true,
  });
  await activateSemanticControl(tableToolsButton);
  await expect(
    root.getByRole('tab', { name: 'Design', exact: true }),
  ).toHaveCount(1);

  await selectRibbonTab(page, 'Insert');
  await expect(root.locator('[aria-label="Clipboard"]')).toHaveCount(0);

  const insertFont = root.getByRole('combobox', {
    name: 'Insert font',
    exact: true,
  });
  const fontOption = root.locator('[role="option"][aria-label="Arial"]');
  await expect(insertFont).toHaveAttribute('aria-expanded', 'false');
  await expect(fontOption).toHaveCount(0);
  await activateSemanticControl(insertFont, 'Space');
  await expect(insertFont).toHaveAttribute('aria-expanded', 'true');
  await expect(fontOption).toHaveCount(1);
  await insertFont.press('Escape');
  await expect(insertFont).toHaveAttribute('aria-expanded', 'false');
  await expect(fontOption).toHaveCount(0);

  const gallery = root.getByRole('listbox', {
    name: 'Insert gallery',
    exact: true,
  });
  await expect(gallery).toHaveCount(1);
  const firstGalleryOption = gallery.getByRole('option').first();
  await expect(firstGalleryOption).toHaveAttribute('aria-selected', 'false');
  await activateSemanticControl(firstGalleryOption, 'Space');
  await expect(firstGalleryOption).toHaveAttribute('aria-selected', 'true');

  await selectRibbonTab(page, 'Tests');
  const edit = root.getByRole('textbox', {
    name: 'Document title',
    exact: true,
  });
  await expect(edit).toHaveCount(1);
  await expect(edit).toHaveJSProperty('tagName', 'INPUT');
  await expect(edit).toHaveAttribute('type', 'text');
  await expect(edit).toHaveValue('Draft');
  await edit.fill('Updated title');
  await expect(edit).toHaveValue('Updated title');

  await selectRibbonTab(page, 'Modern extensions');
  const search = root.getByRole('textbox', {
    name: 'Search ribbon commands',
    exact: true,
  });
  await expect(search).toHaveCount(1);
  await expect(search).toHaveJSProperty('tagName', 'INPUT');
  await expect(search).toHaveAttribute('type', 'text');
  await expect(search).toHaveValue('');
  await search.fill('save');
  await expect(search).toHaveValue('save');

  await expect(root.locator('[aria-label="Document title"]')).toHaveCount(0);
});

test('has no serious or critical axe violations in the semantic UI', async ({
  page,
}) => {
  const root = semanticsRoot(page);
  const originalOpacity = await root.evaluate((element) => {
    const htmlElement = element as HTMLElement;
    const opacity = htmlElement.style.opacity;
    htmlElement.style.opacity = '1';
    return opacity;
  });

  try {
    // Uno's framework-owned root is only an invisible transport container. Scan each
    // application semantic root beneath it so axe evaluates the peer-generated UI.
    const results = await new AxeBuilder({ page })
      .include(`${semanticsRootSelector} > [id^="uno-semantics-"]`)
      .analyze();
    const blockingViolations = results.violations.filter(
      (violation) =>
        violation.impact === 'serious' || violation.impact === 'critical',
    );

    expect(
      blockingViolations,
      blockingViolations
        .map(
          (violation) =>
            `${violation.id}: ${violation.help}\n${violation.nodes
              .map((node) => `  ${node.target.join(' ')}: ${node.failureSummary}`)
              .join('\n')}`,
        )
        .join('\n\n'),
    ).toEqual([]);
  } finally {
    await root.evaluate((element, opacity) => {
      (element as HTMLElement).style.opacity = opacity;
    }, originalOpacity);
  }
});
