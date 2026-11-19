// Frontend coverage gate (ADR-005): line coverage of src/app must be at least the threshold (default 90).
// Usage: node scripts/coverage-gate.mjs [--summary <file>] [--threshold <n>]
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const DEFAULT_SUMMARY = 'coverage/mini-wms/coverage-summary.json';
const DEFAULT_THRESHOLD = 90;

/** Pure check. Returns { passed, pct, covered, total, shortfall, message }. */
export function evaluate(summary, threshold = DEFAULT_THRESHOLD) {
  const lines = summary?.total?.lines;
  if (!lines || typeof lines.pct !== 'number' || Number.isNaN(lines.pct)) {
    throw new Error('coverage summary has no numeric total.lines.pct');
  }
  if (typeof lines.total !== 'number' || !(lines.total > 0)) {
    throw new Error('coverage summary counts no lines (is the coverage scope empty?)');
  }
  const pct = lines.pct;
  const shortfall = Math.max(0, Math.round((threshold - pct) * 100) / 100);
  const passed = pct >= threshold;
  const header = `Line coverage: ${pct}% (${lines.covered} of ${lines.total} lines); threshold ${threshold}%.`;
  const verdict = passed
    ? 'GATE PASSED.'
    : `GATE FAILED: line coverage ${pct}% is below the ${threshold}% threshold (shortfall ${shortfall} percentage points).`;
  return { passed, pct, covered: lines.covered, total: lines.total, shortfall, message: `${header}\n${verdict}` };
}

/** Value of `--name <value>`; undefined when the option is absent. A missing value or another flag is an error. */
function option(args, name) {
  const i = args.indexOf(name);
  if (i < 0) {
    return undefined;
  }
  const value = args[i + 1];
  if (value === undefined || value.startsWith('--')) {
    throw new Error(`option ${name} needs a value`);
  }
  return value;
}

export function run(args) {
  let file;
  let rawThreshold;
  try {
    file = option(args, '--summary') ?? DEFAULT_SUMMARY;
    rawThreshold = option(args, '--threshold');
  } catch (error) {
    console.error(`GATE FAILED: ${error.message}.`);
    return 1;
  }
  // A threshold that cannot fail (empty, 0, negative) or is not a finite number would switch the gate off.
  const threshold = rawThreshold === undefined ? DEFAULT_THRESHOLD : Number(rawThreshold);
  if (!Number.isFinite(threshold) || threshold <= 0 || (rawThreshold !== undefined && rawThreshold.trim() === '')) {
    console.error(`GATE FAILED: invalid threshold "${rawThreshold}" (use a number above 0).`);
    return 1;
  }
  let summary;
  try {
    summary = JSON.parse(readFileSync(file, 'utf8'));
  } catch (error) {
    console.error(`GATE FAILED: cannot read coverage summary "${file}": ${error.message}`);
    return 1;
  }
  let result;
  try {
    result = evaluate(summary, threshold);
  } catch (error) {
    console.error(`GATE FAILED: ${error.message} in "${file}".`);
    return 1;
  }
  (result.passed ? console.log : console.error)(result.message);
  return result.passed ? 0 : 1;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exit(run(process.argv.slice(2)));
}
