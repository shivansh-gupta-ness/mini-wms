export {};

// FEAT-001 final guard specs (AC 25, 26, 27): source scans that complement the rendered checks in the component specs.
// The test tsconfig has no Node types, so Node modules load through untyped dynamic imports (as in scaffold.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { readdirSync, readFileSync, existsSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

const root: string = (globalThis as any).process.cwd(); // src/frontend
const appDir: string = join(root, 'src', 'app');
const locationsDir: string = join(appDir, 'features', 'locations');
const sharedDir: string = join(appDir, 'shared');
const thisFile = 'locations.guards.spec.ts';

const listFiles = (dir: string, keep: (name: string) => boolean): string[] =>
  !existsSync(dir)
    ? []
    : readdirSync(dir, { withFileTypes: true }).flatMap((entry: any) => {
        const full = join(dir, entry.name);
        return entry.isDirectory() ? listFiles(full, keep) : keep(entry.name) ? [full] : [];
      });

const isSource = (name: string): boolean => name.endsWith('.ts') && !name.endsWith('.spec.ts');
const isSpec = (name: string): boolean => name.endsWith('.spec.ts') && name !== thisFile;
const read = (file: string): string => readFileSync(file, 'utf8');
const names = (files: string[]): string[] => files.map((f) => f.slice(appDir.length + 1).replace(/\\/g, '/'));

const sources = [...listFiles(locationsDir, isSource), ...listFiles(sharedDir, isSource)];
const componentsDir = join(locationsDir, 'components');

// Titles of all it(...) calls in a spec source.
const titlesOf = (text: string): string[] => [...text.matchAll(/\bit\(\s*(['"`])((?:\\.|(?!\1).)*)\1/g)].map((m) => m[2]);

describe('FEAT-001 guards: server errors and API access (AC 26)', () => {
  it('FEAT-001.26 the scan finds the sources it guards', () => {
    expect(sources.length).toBeGreaterThan(10);
  });

  it('FEAT-001.26 every dialog component imports and calls applyServerErrors', () => {
    const dialogs = ['warehouse-rename-dialog.ts', 'zone-form-dialog.ts', 'bin-form-dialog.ts'];
    const offenders = dialogs.filter((d) => {
      const text = read(join(componentsDir, d));
      return !/import\s*\{[^}]*\bapplyServerErrors\b[^}]*\}/.test(text) || !text.includes('applyServerErrors(');
    });
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.26 only locations.api.ts imports ApiClient (the HttpClient and host scans live in locations.api.spec.ts)', () => {
    const importers = sources.filter((f) => /\bApiClient\b/.test(read(f)));
    expect(names(importers)).toEqual(['features/locations/data/locations.api.ts']);
  });

  it('FEAT-001.26 pages and components inject LocationsApi, never ApiClient or HttpClient', () => {
    const files = [...listFiles(join(locationsDir, 'pages'), isSource), ...listFiles(componentsDir, isSource)];
    const offenders = files.filter((f) => /inject\(\s*(ApiClient|HttpClient)\b/.test(read(f)));
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.26 the locations page uses LocationsApi', () => {
    expect(read(join(locationsDir, 'pages', 'locations.page.ts'))).toContain('LocationsApi');
  });
});

describe('FEAT-001 guards: accessibility sources (AC 25)', () => {
  // Coarse source scans. The rendered checks (roles, labels, focus, keyboard) are in the component specs.
  it('FEAT-001.25 no source removes the focus outline (the global :focus-visible rule lives in src/styles.scss)', () => {
    const offenders = sources.filter((f) => /outline\s*:\s*(none|0)\b/i.test(read(f)));
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.25 every button in an inline template has text or an aria label', () => {
    const offenders: string[] = [];
    for (const file of sources) {
      for (const m of read(file).matchAll(/<button\b([^>]*)>([\s\S]*?)<\/button>/g)) {
        const hasLabel = /aria-label(ledby)?\s*=/.test(m[1]);
        const hasText = m[2].replace(/<[^>]*>/g, '').trim().length > 0;
        if (!hasLabel && !hasText) {
          offenders.push(`${names([file])[0]}: <button${m[1].replace(/\s+/g, ' ')}>`);
        }
      }
    }
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.25 each file has at least as many mat-labels or aria-labels as inputs and selects', () => {
    const offenders = sources.filter((f) => {
      const text = read(f);
      const controls = (text.match(/<input\b/g) ?? []).length + (text.match(/<mat-select\b/g) ?? []).length;
      const labels = (text.match(/<mat-label\b/g) ?? []).length + (text.match(/aria-label\s*=/g) ?? []).length;
      return controls > labels;
    });
    expect(offenders).toEqual([]);
  });
});

describe('FEAT-001 guards: docs, naming and mapping (AC 27)', () => {
  const featureSpecs = [
    ...listFiles(locationsDir, isSpec),
    ...['confirm-dialog', 'empty-state', 'error-banner', 'status-chip'].flatMap((d) => listFiles(join(sharedDir, d), isSpec)),
  ];

  it('FEAT-001.27 TASK-016.2 REPO_CONTEXT.md stays at most 500 lines counting the trailing newline and documents the locations feature', () => {
    const text = read(join(root, 'REPO_CONTEXT.md'));
    expect(
      text.split(/\r?\n/).length,
      'REPO_CONTEXT.md is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent',
    ).toBeLessThanOrEqual(500);
    const missing = ['features/locations', 'locations.page.ts', 'LocationsApi', 'confirm-dialog', 'applyServerErrors', 'Reload'].filter(
      (term) => !text.includes(term),
    );
    expect(missing).toEqual([]);
  });

  it('FEAT-001.27 every test title in the locations and shared component specs starts with FEAT-001.', () => {
    expect(featureSpecs.length).toBeGreaterThan(8);
    const offenders = featureSpecs.flatMap((f) =>
      titlesOf(read(f))
        .filter((t) => !t.startsWith('FEAT-001.'))
        .map((t) => `${names([f])[0]}: ${t}`),
    );
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.27 every acceptance criterion from 16 to 26 has at least one titled test', () => {
    const titles = listFiles(appDir, isSpec).flatMap((f) => titlesOf(read(f)));
    const missing = Array.from({ length: 11 }, (_, i) => 16 + i).filter(
      (n) => !titles.some((t) => new RegExp(`^FEAT-001\\.${n}\\b`).test(t)),
    );
    expect(missing).toEqual([]);
  });

  it('FEAT-001.27 package.json keeps the test:coverage script', () => {
    const pkg = JSON.parse(read(join(root, 'package.json')));
    expect(pkg.scripts['test:coverage']).toContain('--coverage');
  });
});
