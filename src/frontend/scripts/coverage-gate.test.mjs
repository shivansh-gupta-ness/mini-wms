import { test, after } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { evaluate, run } from './coverage-gate.mjs';

const gateScript = join(dirname(fileURLToPath(import.meta.url)), 'coverage-gate.mjs');
const dir = mkdtempSync(join(tmpdir(), 'coverage-gate-'));
after(() => rmSync(dir, { recursive: true, force: true }));

const summaryOf = (pct) => ({ total: { lines: { total: 100, covered: Math.round(pct), skipped: 0, pct } } });
const fixture = (name, content) => {
  const file = join(dir, name);
  writeFileSync(file, typeof content === 'string' ? content : JSON.stringify(content));
  return file;
};

// Silence and capture console output of run().
const capture = (fn) => {
  const out = [];
  const err = [];
  const origLog = console.log;
  const origErr = console.error;
  console.log = (...a) => out.push(a.join(' '));
  console.error = (...a) => err.push(a.join(' '));
  try {
    return { code: fn(), out: out.join('\n'), err: err.join('\n') };
  } finally {
    console.log = origLog;
    console.error = origErr;
  }
};

test('TASK-010.4 evaluate fails at 84% with shortfall 6 and names percentage, threshold and shortfall', () => {
  const result = evaluate(summaryOf(84));
  assert.equal(result.passed, false);
  assert.equal(result.shortfall, 6);
  assert.match(result.message, /84%/);
  assert.match(result.message, /90%/);
  assert.match(result.message, /shortfall 6/);
});

test('TASK-010.4 evaluate passes at exactly 90% with shortfall 0', () => {
  const result = evaluate(summaryOf(90));
  assert.equal(result.passed, true);
  assert.equal(result.shortfall, 0);
});

test('TASK-010.4 evaluate passes at 95%', () => {
  const result = evaluate(summaryOf(95));
  assert.equal(result.passed, true);
  assert.equal(result.shortfall, 0);
});

test('TASK-010.4 evaluate fails at 89.99%', () => {
  const result = evaluate(summaryOf(89.99));
  assert.equal(result.passed, false);
  assert.ok(result.shortfall > 0);
});

test('TASK-010.4 evaluate with custom threshold 101 fails with a shortfall', () => {
  const result = evaluate(summaryOf(95), 101);
  assert.equal(result.passed, false);
  assert.equal(result.shortfall, 6);
  assert.match(result.message, /101%/);
});

test('TASK-010.4 evaluate throws when total.lines.pct is missing', () => {
  assert.throws(() => evaluate({ total: { lines: {} } }), /total\.lines\.pct/);
  assert.throws(() => evaluate({}), /total\.lines\.pct/);
});

test('TASK-010.4 evaluate throws when total.lines.pct is not a number', () => {
  assert.throws(() => evaluate({ total: { lines: { pct: 'Unknown' } } }), /total\.lines\.pct/);
});

test('TASK-010.4 run returns 0 for a 95% summary file', () => {
  const file = fixture('pass.json', summaryOf(95));
  const { code, out } = capture(() => run(['--summary', file]));
  assert.equal(code, 0);
  assert.match(out, /GATE PASSED/);
});

test('TASK-010.4 run returns 1 for an 84% summary file and reports the shortfall on stderr', () => {
  const file = fixture('fail.json', summaryOf(84));
  const { code, err } = capture(() => run(['--summary', file]));
  assert.equal(code, 1);
  assert.match(err, /shortfall 6/);
});

test('TASK-010.4 run returns 1 for a missing summary file and says it cannot read it', () => {
  const { code, err } = capture(() => run(['--summary', join(dir, 'does-not-exist.json')]));
  assert.equal(code, 1);
  assert.match(err, /cannot read/);
});

test('TASK-010.4 run returns 1 for a summary file with invalid JSON', () => {
  const file = fixture('invalid.json', '{ not json');
  const { code, err } = capture(() => run(['--summary', file]));
  assert.equal(code, 1);
  assert.match(err, /cannot read/);
});

test('TASK-010.4 run returns 1 for a summary without total.lines.pct', () => {
  const file = fixture('nopct.json', { total: {} });
  const { code, err } = capture(() => run(['--summary', file]));
  assert.equal(code, 1);
  assert.match(err, /total\.lines\.pct/);
});

test('TASK-010.4 run returns 1 for an invalid --threshold abc', () => {
  const file = fixture('pass2.json', summaryOf(95));
  const { code, err } = capture(() => run(['--summary', file, '--threshold', 'abc']));
  assert.equal(code, 1);
  assert.match(err, /invalid threshold/);
});

test('TASK-010.4 the real script exits 1 below the threshold and writes the failure text to stderr', () => {
  const file = fixture('spawn-fail.json', summaryOf(84));
  const result = spawnSync(process.execPath, [gateScript, '--summary', file, '--threshold', '90'], {
    encoding: 'utf8',
  });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /GATE FAILED/);
  assert.match(result.stderr, /shortfall 6/);
  assert.equal(result.stdout, '');
});

test('TASK-010.4 a report that counts no lines fails instead of reading as 100%', () => {
  assert.throws(
    () => evaluate({ total: { lines: { total: 0, covered: 0, skipped: 0, pct: 100 } } }),
    /counts no lines/,
  );
  const file = fixture('empty.json', { total: { lines: { total: 0, covered: 0, skipped: 0, pct: 100 } } });
  const result = capture(() => run(['--summary', file]));
  assert.equal(result.code, 1);
  assert.match(result.err, /GATE FAILED/);
});

for (const bad of ['', '0', '-5', 'Infinity', 'abc']) {
  test(`TASK-010.4 run rejects the threshold "${bad}" that would switch the gate off`, () => {
    const file = fixture('ok.json', summaryOf(95));
    const result = capture(() => run(['--summary', file, '--threshold', bad]));
    assert.equal(result.code, 1);
    assert.match(result.err, /invalid threshold/);
  });
}

test('TASK-010.4 run fails when --threshold has no value or is followed by another flag', () => {
  const file = fixture('ok2.json', summaryOf(95));
  assert.equal(capture(() => run(['--summary', file, '--threshold'])).code, 1);
  const flagged = capture(() => run(['--threshold', '--summary', file]));
  assert.equal(flagged.code, 1);
  assert.match(flagged.err, /needs a value/);
});

test('TASK-010.4 run fails when --summary has no value or is followed by another flag', () => {
  const result = capture(() => run(['--summary', '--threshold', '90']));
  assert.equal(result.code, 1);
  assert.match(result.err, /needs a value/);
});
