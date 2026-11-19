import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';

// The test tsconfig has no Node type definitions, so Node modules are loaded through untyped
// dynamic imports (same approach as scaffold.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { readFileSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;
const root: string = (globalThis as any).process.cwd();

async function render(url: string): Promise<HTMLElement> {
  TestBed.configureTestingModule({ providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()] });
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  harness.detectChanges();
  const el = harness.routeNativeElement as HTMLElement;
  document.body.appendChild(el);
  return el;
}

const INTERACTIVE = 'a[href], button, [role="button"]';

function accessibleName(el: Element, doc: Document): string {
  const label = el.getAttribute('aria-label')?.trim();
  if (label) {
    return label;
  }
  const labelledBy = el.getAttribute('aria-labelledby');
  if (labelledBy) {
    const text = labelledBy
      .split(/\s+/)
      .map((id) => doc.getElementById(id)?.textContent?.trim() ?? '')
      .join(' ')
      .trim();
    if (text) {
      return text;
    }
  }
  return (el.textContent ?? '').trim();
}

// NOTE: jsdom does not compute :focus-visible or paint outlines. These tests prove structure,
// order, labels and the presence of the CSS rule. Real visible focus while tabbing MUST be
// checked manually in a browser (recorded in docs/reports/TASK-008/ac-results.md).
describe('TASK-008 keyboard and labels (AC 5)', () => {
  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('TASK-008.5 the first focusable element is the skip link and it targets main', async () => {
    const el = await render('/stock');
    const first = el.querySelector<HTMLElement>('a[href], button, [role="button"], [tabindex]:not([tabindex="-1"])');
    expect(first?.classList.contains('skip-link')).toBe(true);
    expect(first?.getAttribute('href')).toBe('#main-content');
  });

  it('TASK-008.5 main has the id the skip link targets', async () => {
    const el = await render('/stock');
    const target = el.querySelector('a.skip-link')?.getAttribute('href')?.replace('#', '');
    expect(target).toBeTruthy();
    expect(el.querySelector('main')?.id).toBe(target);
  });

  it('TASK-008.5 every link, button and role=button has a non-empty accessible name', async () => {
    const el = await render('/stock');
    const controls = Array.from(el.querySelectorAll(INTERACTIVE));
    expect(controls.length).toBeGreaterThan(0);
    const unnamed = controls.filter((c) => accessibleName(c, document) === '');
    expect(unnamed).toEqual([]);
  });

  it('TASK-008.5 every control on the not-found page has a non-empty accessible name', async () => {
    const el = await render('/does-not-exist');
    const unnamed = Array.from(el.querySelectorAll(INTERACTIVE)).filter((c) => accessibleName(c, document) === '');
    expect(unnamed).toEqual([]);
  });

  it('TASK-008.5 no interactive element has tabindex -1', async () => {
    const el = await render('/stock');
    expect(el.querySelectorAll('a[tabindex="-1"], button[tabindex="-1"], [role="button"][tabindex="-1"]').length).toBe(0);
  });

  it('TASK-008.5 nav links are native anchors in DOM order and can receive focus', async () => {
    const el = await render('/stock');
    const links = Array.from(el.querySelectorAll<HTMLAnchorElement>('nav[aria-label="Main navigation"] a'));
    expect(links.length).toBe(5);
    const all = Array.from(el.querySelectorAll('a'));
    const positions = links.map((l) => all.indexOf(l));
    expect(positions).toEqual([...positions].sort((a, b) => a - b));
    for (const link of links) {
      expect(link.tagName).toBe('A');
      link.focus();
      expect(document.activeElement).toBe(link);
    }
  });

  it('TASK-008.5 the skip link can receive focus', async () => {
    const el = await render('/stock');
    const skip = el.querySelector<HTMLAnchorElement>('a.skip-link');
    skip?.focus();
    expect(document.activeElement).toBe(skip);
  });

  it('TASK-008.5 styles.scss has a :focus-visible rule with an outline', () => {
    const css: string = readFileSync(join(root, 'src', 'styles.scss'), 'utf8');
    const match = css.match(/:focus-visible\s*\{([^}]*)\}/);
    expect(match).not.toBeNull();
    expect(match?.[1]).toMatch(/outline\s*:\s*(?!none|0\b)[^;]+;/);
  });
});

describe('TASK-008 test naming (AC 6)', () => {
  const specFiles = [
    ['src', 'app', 'core', 'layout', 'shell.spec.ts'],
    ['src', 'app', 'app.routes.spec.ts'],
    ['src', 'app', 'core', 'layout', 'a11y.spec.ts'],
  ];

  it('TASK-008.6 every test title in the TASK-008 spec files starts with TASK-008.<n>', () => {
    const bad: string[] = [];
    for (const parts of specFiles) {
      const text: string = readFileSync(join(root, ...parts), 'utf8');
      for (const m of text.matchAll(/^\s*it\(\s*([`'"])(.*?)\1/gm)) {
        if (!/^TASK-008\.\d+ /.test(m[2])) {
          bad.push(`${parts.join('/')}: ${m[2]}`);
        }
      }
    }
    expect(bad).toEqual([]);
  });
});
