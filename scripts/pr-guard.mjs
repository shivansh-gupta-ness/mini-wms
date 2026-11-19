#!/usr/bin/env node
// pr-guard: plain script, no AI, runs as a required GitHub check on every PR.
// Checks: ticket + allowed paths, blocked paths, size limits, knowledge-map globs, licences.
// Secret scanning (gitleaks) runs as the next workflow step.
// Env: BASE_REF (e.g. origin/main), HEAD_REF (sha), BRANCH, PR_LABELS (comma list)
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const BASE = process.env.BASE_REF || 'origin/main';
const HEAD = process.env.HEAD_REF || 'HEAD';
const BRANCH = process.env.BRANCH || git(['rev-parse', '--abbrev-ref', 'HEAD']).trim();
const LABELS = (process.env.PR_LABELS || '').split(',').map((s) => s.trim()).filter(Boolean);
const MAX_LINES = 5000, MAX_FILES = 60;
const failures = [];
const fail = (m) => failures.push(m);

function git(args) { return execFileSync('git', args, { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 }); }
// "**/" = any folders (or none); a trailing "**" = anything; "*" = within one path segment.
const globToRe = (g) => new RegExp('^' + g.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*\*\//g, '\u0000').replace(/\*\*/g, '\u0001').replace(/\*/g, '[^/]*').replace(/\u0000/g, '(?:.*/)?').replace(/\u0001/g, '.*') + '$');
const matchAny = (file, globs) => globs.some((g) => globToRe(g.replace(/^\.?\//, '')).test(file));
const flow = (v) => (v || '').replace(/^\[|\]$/g, '').split(',').map((s) => s.trim().replace(/^['"]|['"]$/g, '')).filter(Boolean);

// Bootstrap/maintenance PRs (setup/*, chore/*) or the `bootstrap` label skip the ticket-scope and size rules.
const exempt = /^(setup|chore)\//.test(BRANCH) || LABELS.includes('bootstrap');

const files = git(['diff', '--name-only', `${BASE}...${HEAD}`]).split('\n').filter(Boolean);
const numstat = git(['diff', '--numstat', `${BASE}...${HEAD}`]).split('\n').filter(Boolean).map((l) => l.split('\t'));
const changedLines = numstat.reduce((s, [a, d]) => s + (Number(a) || 0) + (Number(d) || 0), 0);
console.log(`pr-guard: branch=${BRANCH} files=${files.length} changed-lines=${changedLines} exempt=${exempt}`);

// 1. Size
if (!exempt) {
  if (files.length > MAX_FILES) fail(`PR touches ${files.length} files (limit ${MAX_FILES}). Split it.`);
  if (changedLines > MAX_LINES) fail(`PR changes ${changedLines} lines (limit ${MAX_LINES}). Split it.`);
}

// 2. Ticket and allowed paths
if (!exempt) {
  const m = BRANCH.match(/^(feat|bug|task)\/((?:FEAT|BUG|TASK)-\d+)/i);
  if (!m) fail(`Branch "${BRANCH}" must be named <feat|bug|task>/<TICKET-ID>-short-name (e.g. feat/FEAT-001-add-stock).`);
  else {
    const id = m[2].toUpperCase();
    const tf = fs.existsSync('tickets') ? fs.readdirSync('tickets').find((f) => f.toUpperCase().startsWith(id) && f.endsWith('.md')) : null;
    if (!tf) fail(`No ticket file tickets/${id}*.md found for branch ${BRANCH}.`);
    else {
      const fm = (fs.readFileSync(path.join('tickets', tf), 'utf8').match(/^---\n([\s\S]*?)\n---/) || [])[1] || '';
      const allowed = flow((fm.match(/^allowed_paths:\s*(.*)$/m) || [])[1]);
      const owner = (fm.match(/^owner:\s*(.*)$/m) || [])[1];
      if (!owner || !owner.trim()) fail(`Ticket ${id} has no owner.`);
      if (!allowed.length) fail(`Ticket ${id} has no allowed_paths.`);
      else {
        const extra = files.filter((f) => f !== `tickets/${tf}` && !matchAny(f, allowed));
        if (extra.length) fail(`Files outside ${id} allowed_paths [${allowed.join(', ')}]:\n  ` + extra.join('\n  '));
      }
    }
  }
}

// 3. Blocked paths: harness, CI and shared context change only via a bootstrap/chore PR, never in ticket work
const blocked = ['.github/**', '.claude/**', 'scripts/**', 'CLAUDE.md', 'REPO_CONTEXT.md', 'docs/knowledge-map.yaml'];
if (!exempt) {
  const hit = files.filter((f) => matchAny(f, blocked));
  if (hit.length) fail(`Blocked paths changed in a ticket PR (use a chore/ PR with owner review):\n  ` + hit.join('\n  '));
}

// 4. Knowledge-map globs must match real files
if (fs.existsSync('docs/knowledge-map.yaml')) {
  const tracked = git(['ls-files']).split('\n').filter(Boolean);
  const text = fs.readFileSync('docs/knowledge-map.yaml', 'utf8').split('\n');
  let comp = '';
  for (const line of text) {
    if (/^\s*#/.test(line)) continue;
    const c = line.match(/^([A-Za-z0-9_-]+):\s*$/); if (c) { comp = c[1]; continue; }
    const f = line.match(/^\s+(code_paths|protected_paths):\s*(.*)$/);
    if (f) for (const g of flow(f[2])) if (!tracked.some((t) => globToRe(g.replace(/^\.?\//, '')).test(t)))
      fail(`knowledge-map ${comp}.${f[1]} glob "${g}" matches no tracked file.`);
  }
}

// 5. Licences (npm lockfiles carry a licence field; NuGet is covered by a separate step once src/backend exists)
const deny = /(^|[^L])GPL|AGPL|SSPL|BUSL|Commons-Clause/i;
for (const lock of git(['ls-files', '**package-lock.json', 'package-lock.json']).split('\n').filter(Boolean)) {
  try {
    const pk = JSON.parse(fs.readFileSync(lock, 'utf8')).packages || {};
    const bad = Object.entries(pk).filter(([, v]) => v.license && deny.test(String(v.license))).map(([k, v]) => `${k || '(root)'} (${v.license})`);
    if (bad.length) fail(`Disallowed licences in ${lock}:\n  ` + bad.slice(0, 20).join('\n  '));
  } catch (e) { fail(`Cannot read ${lock}: ${e.message}`); }
}

if (failures.length) {
  console.error('\npr-guard FAILED:\n');
  failures.forEach((f, i) => console.error(`${i + 1}. ${f}\n`));
  process.exit(1);
}
console.log('pr-guard passed.');
