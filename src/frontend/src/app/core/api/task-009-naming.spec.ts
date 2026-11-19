export {};

// Untyped dynamic imports, as in scaffold.spec.ts (the test tsconfig has no Node types).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { readFileSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;

const appDir = join((globalThis as any).process.cwd(), 'src', 'app');

const specs = [
  ['core', 'api', 'api-client.spec.ts'],
  ['core', 'api', 'no-hardcoded-hosts.spec.ts'],
  ['core', 'auth', 'auth.interceptor.spec.ts'],
  ['core', 'errors', 'error.interceptor.spec.ts'],
  ['shared', 'forms', 'apply-server-errors.spec.ts'],
];

const titles = (file: string[]): string[] =>
  [...readFileSync(join(appDir, ...file), 'utf8').matchAll(/\bit\(\s*([`'"])(.*?)\1/g)].map((m: any) => m[2] as string);

describe('TASK-009 test naming (AC 6)', () => {
  for (const file of specs) {
    it(`TASK-009.6 every it() title in ${file.join('/')} starts with TASK-009.`, () => {
      const found = titles(file);
      expect(found.length).toBeGreaterThan(0);
      expect(found.filter((t) => !/^TASK-009\.\d/.test(t))).toEqual([]);
    });
  }
});
