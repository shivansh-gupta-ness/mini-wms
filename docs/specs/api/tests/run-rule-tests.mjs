// Rule tests for .spectral.yaml: lints each fixture and checks the exact set of rule codes reported.
// Plain Node, no dependencies. Usage: node docs/specs/api/tests/run-rule-tests.mjs (from the repo root).
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const SPECTRAL = '@stoplight/spectral-cli@6.17.1';
const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(here, '..', '..', '..', '..');

// fixture -> exact set of rule codes that must be reported (empty = clean)
const cases = [
  ['valid.yaml', []],
  ['missing-ac.yaml', ['operation-has-acceptance-criteria']],
  ['error-no-problem-json.yaml', ['error-response-is-problem-json']],
  ['problem-wrong-schema.yaml', ['error-response-uses-problem-schema']],
  ['list-missing-sort.yaml', ['list-operation-has-paging-params']],
  ['not-a-list.yaml', []],
];

const outDir = mkdtempSync(join(tmpdir(), 'spectral-rule-tests-'));
let failed = 0;
for (const [file, expected] of cases) {
  const target = join('docs', 'specs', 'api', 'tests', file);
  // The JSON report goes to a file, so log lines on stdout (deprecation warnings and so on) cannot break the parse.
  const report = join(outDir, `${file}.json`);
  const run = spawnSync(
    `npx --yes ${SPECTRAL} lint "${target}" --format json --output "${report}"`,
    { cwd: repoRoot, shell: true, encoding: 'utf8' },
  );
  let codes;
  try {
    // Spectral exits 1 when it reports errors (expected for the bad fixtures); a missing report means it did not run.
    const results = JSON.parse(readFileSync(report, 'utf8'));
    codes = [...new Set(results.map((r) => r.code))].sort();
  } catch (e) {
    console.log(`FAIL ${file}: Spectral did not produce a report (exit ${run.status}); is the npm registry reachable?\n${run.stdout}\n${run.stderr}`);
    failed++;
    continue;
  }
  const want = [...expected].sort();
  const ok = JSON.stringify(codes) === JSON.stringify(want);
  console.log(`${ok ? 'PASS' : 'FAIL'} ${file}: expected [${want.join(', ')}] got [${codes.join(', ')}]`);
  if (!ok) failed++;
}

rmSync(outDir, { recursive: true, force: true });
console.log(failed === 0 ? `All ${cases.length} rule tests passed.` : `${failed} of ${cases.length} rule tests failed.`);
process.exit(failed === 0 ? 0 : 1);
