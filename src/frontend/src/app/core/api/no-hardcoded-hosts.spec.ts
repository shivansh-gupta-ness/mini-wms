export {};

// The test tsconfig has no Node type definitions, so Node modules are loaded through untyped dynamic imports
// (same approach as scaffold.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { readdirSync, readFileSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

const appDir = join((globalThis as any).process.cwd(), 'src', 'app');

const listTsFiles = (dir: string): string[] =>
  readdirSync(dir, { withFileTypes: true }).flatMap((entry: any) => {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      return listTsFiles(full);
    }
    return entry.name.endsWith('.ts') && !entry.name.endsWith('.spec.ts') ? [full] : [];
  });

describe('TASK-009 no hard-coded API host (AC 1)', () => {
  it('TASK-009.1 the scan finds the production sources', () => {
    expect(listTsFiles(appDir).length).toBeGreaterThan(5);
  });

  it('TASK-009.1 no non-spec file under src/app contains http://, https:// or localhost', () => {
    const offenders = listTsFiles(appDir).filter((file) =>
      /http:\/\/|https:\/\/|localhost/i.test(readFileSync(file, 'utf8')),
    );
    expect(offenders).toEqual([]);
  });
});
