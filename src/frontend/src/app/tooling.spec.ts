export {};

// Node modules are loaded through untyped dynamic imports (see scaffold.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { existsSync, readFileSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

// `ng test` runs with the working directory set to src/frontend.
const root: string = (globalThis as any).process.cwd();

const read = (...parts: string[]): string => readFileSync(join(root, ...parts), 'utf8');
const readJson = (...parts: string[]): any => JSON.parse(read(...parts));
const testOptions = (): any => readJson('angular.json').projects['mini-wms'].architect.test.options;

describe('TASK-010 tooling: coverage output (AC 2)', () => {
  it('TASK-010.2 angular.json coverage reporters include text and json-summary', () => {
    const reporters: string[] = testOptions().coverageReporters;
    expect(reporters).toContain('text');
    expect(reporters).toContain('json-summary');
  });

  it('TASK-010.2 the coverage folder is git-ignored', () => {
    const entries = read('.gitignore')
      .split(/\r?\n/)
      .map((line: string) => line.trim());
    expect(entries.some((line: string) => /^\/?coverage\/?$/.test(line))).toBe(true);
  });

  it('TASK-010.2 test:coverage runs the tests with --coverage and then the gate', () => {
    const script: string = readJson('package.json').scripts['test:coverage'];
    expect(script).toContain('--coverage');
    expect(script).toContain('--no-watch');
    expect(script.indexOf('--coverage')).toBeLessThan(script.indexOf('coverage-gate.mjs'));
    expect(script).toContain('&& node scripts/coverage-gate.mjs');
  });
});

describe('TASK-010 tooling: coverage scope (AC 3)', () => {
  it('TASK-010.3 only src/app TypeScript files are included', () => {
    expect(testOptions().coverageInclude).toEqual(['src/app/**/*.ts']);
  });

  it('TASK-010.3 spec files and declaration files are excluded', () => {
    const exclude: string[] = testOptions().coverageExclude;
    expect(exclude).toContain('src/app/**/*.spec.ts');
    expect(exclude).toContain('src/app/**/*.d.ts');
  });

  it('TASK-010.3 the gate script lives outside src/app so it is not counted', () => {
    expect(existsSync(join(root, 'scripts', 'coverage-gate.mjs'))).toBe(true);
    expect(existsSync(join(root, 'src', 'app', 'coverage-gate.mjs'))).toBe(false);
  });
});

describe('TASK-010 tooling: coverage provider and stale reports (AC 2)', () => {
  it('TASK-010.2 the coverage provider version equals the installed vitest version', () => {
    const provider: string = readJson('package.json').devDependencies['@vitest/coverage-v8'];
    const vitest: string = readJson('node_modules', 'vitest', 'package.json').version;
    expect(provider).toBe(vitest);
  });

  it('TASK-010.2 test:coverage removes the previous report before the tests run', () => {
    const script: string = readJson('package.json').scripts['test:coverage'];
    expect(script.indexOf('clean-coverage.mjs')).toBeGreaterThanOrEqual(0);
    expect(script.indexOf('clean-coverage.mjs')).toBeLessThan(script.indexOf('--coverage'));
  });
});

describe('TASK-010 tooling: frontend REPO_CONTEXT.md (AC 6)', () => {
  const text = read('REPO_CONTEXT.md');

  it('TASK-010.6 REPO_CONTEXT.md lists the test, coverage and gate commands', () => {
    expect(text).toContain('npm test');
    expect(text).toContain('npm run test:coverage');
    expect(text).toContain('npm run test:gate');
    expect(text).toContain('coverage-gate.mjs');
  });

  it('TASK-010.6 REPO_CONTEXT.md shows a --filter example with a TASK-010 id', () => {
    expect(text).toMatch(/--filter\s+"[^"]*TASK-010/);
  });

  it('TASK-010.6 REPO_CONTEXT.md stays within 78 lines', () => {
    expect(text.split(/\r?\n/).length).toBeLessThanOrEqual(78);
  });
});
