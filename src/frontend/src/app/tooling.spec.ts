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

  it('TASK-010.6 TASK-016.2 REPO_CONTEXT.md has at most 500 lines counting the trailing newline', () => {
    expect(
      text.split(/\r?\n/).length,
      'REPO_CONTEXT.md is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent',
    ).toBeLessThanOrEqual(500);
  });
});

// TASK-016 AC5: every REPO_CONTEXT.md is within 500 lines and listed by its nearest parent REPO_CONTEXT.md.
const { mkdtempSync, mkdirSync, writeFileSync, rmSync, readdirSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const osModule = 'node:os';
const { tmpdir } = (await import(/* @vite-ignore */ osModule)) as any;
const { dirname, relative, resolve, sep } = (await import(/* @vite-ignore */ pathModule)) as any;

const skippedFolders = ['bin', 'obj', 'TestResults', 'node_modules', 'dist', 'coverage', '.angular'];

const collectContextFiles = (folder: string, files: string[]): void => {
  if (existsSync(join(folder, 'REPO_CONTEXT.md'))) {
    files.push(join(folder, 'REPO_CONTEXT.md'));
  }
  for (const entry of readdirSync(folder, { withFileTypes: true })) {
    if (entry.isDirectory() && !skippedFolders.includes(entry.name)) {
      collectContextFiles(join(folder, entry.name), files);
    }
  }
};

const nearestParent = (rootFolder: string, file: string): string | null => {
  const rootFull = resolve(rootFolder);
  if (dirname(resolve(file)) === rootFull) {
    return null;
  }
  let up = dirname(dirname(resolve(file)));
  for (;;) {
    const candidate = join(up, 'REPO_CONTEXT.md');
    if (existsSync(candidate)) {
      return candidate;
    }
    if (up === rootFull || dirname(up) === up) {
      return null;
    }
    up = dirname(up);
  }
};

const escapeRegExp = (value: string): string => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

// Returns one problem per offence; each starts with the path of the file it names, relative to the root.
// The line count includes the trailing newline (owner decision). `prefix` is the root folder's repo-relative path, empty for a sample tree.
const repoContextProblems = (rootFolder: string, prefix = ''): string[] => {
  const problems: string[] = [];
  const files: string[] = [];
  collectContextFiles(rootFolder, files);
  for (const file of files.sort()) {
    const fromRoot = relative(rootFolder, file).split(sep).join('/');
    if (readFileSync(file, 'utf8').split(/\r?\n/).length > 500) {
      problems.push(
        `${fromRoot} is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent`,
      );
    }
    const parent = nearestParent(rootFolder, file);
    if (parent !== null) {
      const fromParent = relative(dirname(parent), file).split(sep).join('/');
      const parentFromRoot = relative(rootFolder, parent).split(sep).join('/');
      const parentFolderFromRoot = relative(rootFolder, dirname(parent)).split(sep).join('/');
      const parentPrefix = [prefix, parentFolderFromRoot].filter((part) => part.length > 0).join('/');
      const prefixForms = parentPrefix.length > 0 ? `(?:\\./|${escapeRegExp(`${parentPrefix}/`)})?` : '(?:\\./)?';
      const listed = new RegExp(
        `(?<![A-Za-z0-9_.\\-/])${prefixForms}${escapeRegExp(fromParent)}(?![A-Za-z0-9_\\-/])(?!\\.[A-Za-z0-9_\\-])`,
      );
      if (!listed.test(readFileSync(parent, 'utf8'))) {
        problems.push(`${fromRoot} is not listed as ${fromParent} in ${parentFromRoot}`);
      }
    }
  }
  return problems;
};

const lines = (count: number, ...listed: string[]): string =>
  [...listed, ...Array(Math.max(0, count - listed.length)).fill('line')].join('\n');

describe('TASK-016 tooling: REPO_CONTEXT.md files (AC 5)', () => {
  let tree: string;
  const write = (relativePath: string, content: string): void => {
    const target = join(tree, relativePath);
    mkdirSync(dirname(target), { recursive: true });
    writeFileSync(target, content);
  };

  beforeEach(() => {
    tree = mkdtempSync(join(tmpdir(), 'miniwms-ctx-'));
  });

  afterEach(() => {
    rmSync(tree, { recursive: true, force: true });
  });

  it('TASK-016.5 the real frontend tree has the root file and no problems', () => {
    expect(existsSync(join(root, 'REPO_CONTEXT.md'))).toBe(true);
    expect(repoContextProblems(root, 'src/frontend')).toEqual([]);
  });

  for (const listing of [
    'MiniWms.Api/REPO_CONTEXT.md',
    './MiniWms.Api/REPO_CONTEXT.md',
    'src/frontend/MiniWms.Api/REPO_CONTEXT.md',
    'See `src/frontend/MiniWms.Api/REPO_CONTEXT.md`.',
  ]) {
    it(`TASK-016.5 a child listed as "${listing}" passes`, () => {
      write('REPO_CONTEXT.md', lines(5, listing));
      write('MiniWms.Api/REPO_CONTEXT.md', lines(5));
      expect(repoContextProblems(tree, 'src/frontend')).toEqual([]);
    });
  }

  for (const listing of [
    'Other/MiniWms.Api/REPO_CONTEXT.md',
    '../MiniWms.Api/REPO_CONTEXT.md',
    'src/backend/MiniWms.Api/REPO_CONTEXT.md',
    'xsrc/frontend/MiniWms.Api/REPO_CONTEXT.md',
  ]) {
    it(`TASK-016.5 a child listed only as "${listing}" fails and the problem names the child`, () => {
      write('REPO_CONTEXT.md', lines(5, listing));
      write('MiniWms.Api/REPO_CONTEXT.md', lines(5));
      const problems = repoContextProblems(tree, 'src/frontend');
      expect(problems.length).toBe(1);
      expect(problems[0].startsWith('MiniWms.Api/REPO_CONTEXT.md')).toBe(true);
    });
  }

  for (const [listing, passes] of [
    ['src/frontend/MiniWms.Api/Deep/REPO_CONTEXT.md', true],
    ['./Deep/REPO_CONTEXT.md', true],
    ['src/frontend/Deep/REPO_CONTEXT.md', false],
  ] as [string, boolean][]) {
    it(`TASK-016.5 a grandchild listed in its parent as "${listing}" ${passes ? 'passes' : 'fails'}`, () => {
      write('REPO_CONTEXT.md', lines(5, 'MiniWms.Api/REPO_CONTEXT.md'));
      write('MiniWms.Api/REPO_CONTEXT.md', lines(5, listing));
      write('MiniWms.Api/Deep/REPO_CONTEXT.md', lines(5));
      expect(repoContextProblems(tree, 'src/frontend').length === 0).toBe(passes);
    });
  }

  it('TASK-016.5 a child of 499 lines plus a final newline passes', () => {
    write('REPO_CONTEXT.md', lines(5, 'Api/REPO_CONTEXT.md'));
    write('Api/REPO_CONTEXT.md', `${lines(499)}\n`);
    expect(repoContextProblems(tree)).toEqual([]);
  });

  it('TASK-016.5 a child of 500 lines plus a final newline fails and the problem names the child', () => {
    write('REPO_CONTEXT.md', lines(5, 'Api/REPO_CONTEXT.md'));
    write('Api/REPO_CONTEXT.md', `${lines(500)}\n`);
    const problems = repoContextProblems(tree);
    expect(problems.length).toBe(1);
    expect(problems[0].startsWith('Api/REPO_CONTEXT.md')).toBe(true);
    expect(problems[0]).toContain('is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files');
  });

  it('TASK-016.5 a valid tree with parent, child and grandchild passes', () => {
    write('REPO_CONTEXT.md', lines(5, '- `Api/REPO_CONTEXT.md`'));
    write('Api/REPO_CONTEXT.md', lines(5, '- `Deep/REPO_CONTEXT.md`'));
    write('Api/Deep/REPO_CONTEXT.md', lines(5));
    expect(repoContextProblems(tree)).toEqual([]);
  });

  it('TASK-016.5 a child of 501 lines fails and the problem names the child', () => {
    write('REPO_CONTEXT.md', lines(5, 'Api/REPO_CONTEXT.md'));
    write('Api/REPO_CONTEXT.md', lines(501));
    const problems = repoContextProblems(tree);
    expect(problems.length).toBe(1);
    expect(problems[0]).toContain('Api/REPO_CONTEXT.md');
  });

  it('TASK-016.5 a child of exactly 500 lines passes', () => {
    write('REPO_CONTEXT.md', lines(5, 'Api/REPO_CONTEXT.md'));
    write('Api/REPO_CONTEXT.md', lines(500));
    expect(repoContextProblems(tree)).toEqual([]);
  });

  it('TASK-016.5 an unlisted child fails and the problem names the child', () => {
    write('REPO_CONTEXT.md', lines(5));
    write('Api/REPO_CONTEXT.md', lines(5));
    const problems = repoContextProblems(tree);
    expect(problems.length).toBe(1);
    expect(problems[0]).toContain('Api/REPO_CONTEXT.md');
  });

  it('TASK-016.5 a child listed only by a longer folder name fails', () => {
    write('REPO_CONTEXT.md', lines(5, 'OtherFoo/REPO_CONTEXT.md'));
    write('Foo/REPO_CONTEXT.md', lines(5));
    write('OtherFoo/REPO_CONTEXT.md', lines(5));
    const problems = repoContextProblems(tree);
    expect(problems.length).toBe(1);
    expect(problems[0].startsWith('Foo/REPO_CONTEXT.md')).toBe(true);
  });

  for (const listing of ['Foo/REPO_CONTEXT.md.bak', 'Foo/REPO_CONTEXT.mdx']) {
    it(`TASK-016.5 a child listed only as ${listing} fails and the problem names the child`, () => {
      write('REPO_CONTEXT.md', lines(5, listing));
      write('Foo/REPO_CONTEXT.md', lines(5));
      const problems = repoContextProblems(tree);
      expect(problems.length).toBe(1);
      expect(problems[0].startsWith('Foo/REPO_CONTEXT.md')).toBe(true);
    });
  }

  for (const listing of [
    'See Foo/REPO_CONTEXT.md.',
    'See `Foo/REPO_CONTEXT.md`',
    '[Foo](Foo/REPO_CONTEXT.md)',
    'Foo/REPO_CONTEXT.md, and more',
    'Foo/REPO_CONTEXT.md: the foo layout',
    '| Foo/REPO_CONTEXT.md | foo |',
    'Foo/REPO_CONTEXT.md',
  ]) {
    it(`TASK-016.5 a child listed as "${listing}" passes`, () => {
      write('REPO_CONTEXT.md', lines(5, listing));
      write('Foo/REPO_CONTEXT.md', lines(5));
      expect(repoContextProblems(tree)).toEqual([]);
    });
  }

  it('TASK-016.5 a grandchild listed only in the top file fails and the problem names the grandchild', () => {
    write('REPO_CONTEXT.md', lines(5, 'Api/REPO_CONTEXT.md', 'Api/Deep/REPO_CONTEXT.md'));
    write('Api/REPO_CONTEXT.md', lines(5));
    write('Api/Deep/REPO_CONTEXT.md', lines(5));
    const problems = repoContextProblems(tree);
    expect(problems.length).toBe(1);
    expect(problems[0].startsWith('Api/Deep/REPO_CONTEXT.md')).toBe(true);
    expect(problems[0]).toContain(' in Api/REPO_CONTEXT.md');
  });

  for (const skipped of skippedFolders) {
    it(`TASK-016.5 a REPO_CONTEXT.md inside ${skipped} is ignored`, () => {
      write('REPO_CONTEXT.md', lines(5));
      write(`Api/${skipped}/REPO_CONTEXT.md`, lines(600));
      expect(repoContextProblems(tree)).toEqual([]);
    });
  }
});
