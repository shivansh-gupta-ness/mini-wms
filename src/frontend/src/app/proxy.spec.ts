export {};

// Node modules are loaded through untyped dynamic imports (see tooling.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { existsSync, readFileSync, readdirSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

// `ng test` runs with the working directory set to src/frontend.
const root: string = (globalThis as any).process.cwd();

const read = (...parts: string[]): string => readFileSync(join(root, ...parts), 'utf8');
const readJson = (...parts: string[]): any => JSON.parse(read(...parts));

// The real browser check of AC 1 to AC 3 (npm start, API on :5080, no CORS errors) is manual.
describe('TASK-011 tooling: dev proxy (AC 1)', () => {
  it('TASK-011.1 proxy.conf.json maps /api to http://localhost:5080 with secure false and changeOrigin true', () => {
    const entry = readJson('proxy.conf.json')['/api/'];
    expect(entry.target).toBe('http://localhost:5080');
    expect(entry.secure).toBe(false);
    expect(entry.changeOrigin).toBe(true);
  });

  it('TASK-011.1 angular.json serve uses proxy.conf.json and keeps its configurations', () => {
    const serve = readJson('angular.json').projects['mini-wms'].architect.serve;
    expect(serve.options.proxyConfig).toBe('proxy.conf.json');
    expect(Object.keys(serve.configurations ?? {}).length).toBeGreaterThan(0);
  });

  it('TASK-011.1 the proxy file lives outside src/app', () => {
    expect(existsSync(join(root, 'proxy.conf.json'))).toBe(true);
    expect(existsSync(join(root, 'src', 'app', 'proxy.conf.json'))).toBe(false);
    expect(readdirSync(join(root, 'src', 'app')).includes('proxy.conf.json')).toBe(false);
  });
});

describe('TASK-011 tooling: frontend REPO_CONTEXT.md (AC 6)', () => {
  const text = read('REPO_CONTEXT.md');

  it('TASK-011.6 REPO_CONTEXT.md explains how to run the API and the frontend together', () => {
    expect(text).toContain('dotnet run --project src/backend/MiniWms.Api');
    expect(text).toContain('npm start');
    expect(text).toContain('proxy.conf.json');
    expect(text).toContain('http://localhost:5080');
    expect(text).toContain('http://localhost:4200');
  });

  it('TASK-011.6 REPO_CONTEXT.md names the indicator text', () => {
    expect(text.includes('API: ok') || text.includes('API: unavailable')).toBe(true);
  });

  it('TASK-011.6 TASK-016.2 REPO_CONTEXT.md has at most 500 lines counting the trailing newline', () => {
    expect(
      text.split(/\r?\n/).length,
      'REPO_CONTEXT.md is over 500 lines counting the trailing newline: split it into child REPO_CONTEXT.md files next to the code they describe and list them in the parent',
    ).toBeLessThanOrEqual(500);
  });
});

describe('TASK-011 tooling: test names (AC 5)', () => {
  const files = ['src/app/core/layout/api-status.spec.ts', 'src/app/proxy.spec.ts'];

  it('TASK-011.5 every it() title in the TASK-011 spec files starts with TASK-011.<n>', () => {
    const titles: string[] = [];
    for (const file of files) {
      for (const m of read(...file.split('/')).matchAll(/\bit(?:\.each\([\s\S]*?\))?\(\s*(['"`])(.*?)\1/g)) {
        titles.push(m[2]);
      }
    }
    expect(titles.length).toBeGreaterThan(10);
    expect(titles.filter((t) => !/^TASK-011\.\d/.test(t))).toEqual([]);
  });
});
