export {};

// The test tsconfig has no Node type definitions (config is out of scope for this ticket),
// so Node modules are loaded through untyped dynamic imports.
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { existsSync, readdirSync, readFileSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

// `ng test` runs with the working directory set to src/frontend.
const root: string = (globalThis as any).process.cwd();
const appDir = join(root, 'src', 'app');

const read = (...parts: string[]): string => readFileSync(join(root, ...parts), 'utf8');
const readJson = (...parts: string[]): any =>
  JSON.parse(
    read(...parts)
      .replace(/\/\*[\s\S]*?\*\//g, '')
      .replace(/^\s*\/\/.*$/gm, ''),
  );

const listTsFiles = (dir: string): string[] =>
  readdirSync(dir, { withFileTypes: true }).flatMap((entry: any) => {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      return listTsFiles(full);
    }
    return entry.name.endsWith('.ts') ? [full] : [];
  });

describe('TASK-007 scaffold: strict configuration (AC 1)', () => {
  it('TASK-007.1 tsconfig enables strict TypeScript', () => {
    const tsconfig = readJson('tsconfig.json');
    expect(tsconfig.compilerOptions.strict).toBe(true);
  });

  it('TASK-007.1 tsconfig enables strict Angular template and injection checks', () => {
    const options = readJson('tsconfig.json').angularCompilerOptions;
    expect(options.strictTemplates).toBe(true);
    expect(options.strictInjectionParameters).toBe(true);
    expect(options.strictInputAccessModifiers).toBe(true);
  });

  it('TASK-007.1 package.json has build, start and test scripts', () => {
    const scripts = readJson('package.json').scripts;
    expect(scripts['build']).toBeTruthy();
    expect(scripts['start']).toBeTruthy();
    expect(scripts['test']).toBeTruthy();
  });

  it('TASK-007.1 test script runs once without watch mode', () => {
    const testScript: string = readJson('package.json').scripts['test'];
    expect(testScript).toContain('--no-watch');
  });
});

describe('TASK-007 scaffold: dependencies (AC 2)', () => {
  const pkg = readJson('package.json');
  const all: string[] = Object.keys({ ...pkg.dependencies, ...pkg.devDependencies });

  it('TASK-007.2 Angular Material and CDK are runtime dependencies', () => {
    expect(Object.keys(pkg.dependencies)).toContain('@angular/material');
    expect(Object.keys(pkg.dependencies)).toContain('@angular/cdk');
  });

  it('TASK-007.2 no other UI or state library is a dependency', () => {
    const forbidden = [
      /^@ngrx\//,
      /^ngrx/,
      /^bootstrap$/,
      /^ng-bootstrap$/,
      /^@ng-bootstrap\//,
      /^primeng$/,
      /^primeicons$/,
      /^ng-zorro-antd$/,
      /^@akita\//,
      /^akita$/,
      /^@datorama\//,
      /^@ngxs\//,
      /^mobx/,
      /^tailwindcss$/,
      /^@tailwindcss\//,
      /^@ngneat\//,
      /^@taiga-ui\//,
      /^@clr\//,
      /^@ionic\//,
      /^@ionic$/,
      /^redux/,
      /^@reduxjs\//,
      /^zustand$/,
      /^@angular\/flex-layout$/,
      /^@ng-select\//,
      /^ngx-bootstrap$/,
    ];
    const found = all.filter((name) => forbidden.some((pattern) => pattern.test(name)));
    expect(found).toEqual([]);
  });
});

describe('TASK-007 scaffold: layout and standalone components (AC 3)', () => {
  it('TASK-007.3 core, shared and features folders exist under src/app', () => {
    expect(existsSync(join(appDir, 'core'))).toBe(true);
    expect(existsSync(join(appDir, 'shared'))).toBe(true);
    expect(existsSync(join(appDir, 'features'))).toBe(true);
  });

  it('TASK-007.3 no file under src/app declares an NgModule', () => {
    const offenders = listTsFiles(appDir).filter(
      (file) => !file.endsWith('scaffold.spec.ts') && /@NgModule\s*\(/.test(readFileSync(file, 'utf8')),
    );
    expect(offenders).toEqual([]);
  });

  it('TASK-007.3 no component under src/app opts out of standalone', () => {
    const offenders = listTsFiles(appDir).filter(
      (file) =>
        !file.endsWith('scaffold.spec.ts') && /standalone\s*:\s*false/.test(readFileSync(file, 'utf8')),
    );
    expect(offenders).toEqual([]);
  });
});

describe('TASK-007 scaffold: smoke test naming (AC 4)', () => {
  it('TASK-007.4 app.spec.ts contains the TASK-007.1 smoke test', () => {
    expect(read('src', 'app', 'app.spec.ts')).toContain('TASK-007.1');
  });
});

describe('TASK-007 scaffold: frontend REPO_CONTEXT.md (AC 6)', () => {
  const text = read('REPO_CONTEXT.md');
  const pkg = readJson('package.json');
  const version = (range: string): string => range.replace(/^[\^~]/, '');

  it('TASK-007.6 TASK-016.2 REPO_CONTEXT.md has at most 500 lines counting the trailing newline', () => {
    expect(
      text.split(/\r?\n/).length,
      'REPO_CONTEXT.md is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent',
    ).toBeLessThanOrEqual(500);
  });

  it('TASK-007.6 REPO_CONTEXT.md records the Angular and CLI versions from package.json', () => {
    expect(text).toContain(version(pkg.dependencies['@angular/core']));
    expect(text).toContain(version(pkg.devDependencies['@angular/cli']));
  });

  it('TASK-007.6 REPO_CONTEXT.md names the test runner', () => {
    expect(text).toContain('Vitest');
  });

  it('TASK-007.6 REPO_CONTEXT.md lists the folder layout', () => {
    expect(text).toContain('core');
    expect(text).toContain('shared');
    expect(text).toContain('features');
  });

  it('TASK-007.6 REPO_CONTEXT.md lists the build, test and run commands', () => {
    expect(text).toContain('npm ci');
    expect(text).toContain('npm run build');
    expect(text).toContain('npm test');
    expect(text).toContain('npm start');
  });
});
