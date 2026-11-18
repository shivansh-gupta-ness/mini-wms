// WorktreeCreate hook: creates the isolation worktree for an editing agent instead of Claude Code's own git logic.
// Why: on Windows, Claude Code (VS Code extension) refuses its own worktree because it compares the drive letter case-sensitively
// ("c:\..." vs "C:/..."; issue 91618). Here the path is built from git's own answer with an uppercase drive letter.
// Contract (Claude Code docs): read JSON on stdin, print ONLY the absolute worktree path on stdout, everything else to stderr,
// exit non-zero to fail.
//
// The worktree starts from the branch's HEAD plus a snapshot of the current uncommitted state (tracked changes, new files and
// the active ticket file), committed on the throwaway worktree branch as a "baseline". The agent's own work is then exactly
// `git diff baseline` and is carried back by worktree-apply.mjs.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { readInput } from './lib.mjs';

const GIT = fs.existsSync('C:\\Program Files\\Git\\cmd\\git.exe') ? 'C:\\Program Files\\Git\\cmd\\git.exe' : 'git';
const git = (args, cwd, input) => execFileSync(GIT, args, { cwd, input, encoding: 'utf8', maxBuffer: 512 * 1024 * 1024, stdio: ['pipe', 'pipe', 'pipe'] });
const winPath = (p) => { const s = path.resolve(p); return /^[a-z]:/.test(s) ? s[0].toUpperCase() + s.slice(1) : s; };
const log = (m) => process.stderr.write(`worktree-create: ${m}\n`);

let wt = '';
let main = '';
try {
  const input = readInput();
  const start = input.cwd || process.cwd();
  const common = git(['rev-parse', '--path-format=absolute', '--git-common-dir'], start).trim();
  main = winPath(path.dirname(common));

  let name = String(input.name || `agent-${Date.now().toString(36)}`).replace(/[^A-Za-z0-9._-]/g, '-').slice(0, 60);
  if (fs.existsSync(path.join(main, '.claude', 'worktrees', name))) name += `-${Date.now().toString(36)}`;
  wt = winPath(path.join(main, '.claude', 'worktrees', name));
  const branch = `worktree-${name}`;

  git(['worktree', 'add', '-b', branch, wt, 'HEAD'], main);

  // 1. Tracked changes (staged and unstaged, including deletions).
  const diff = git(['diff', 'HEAD', '--binary'], main);
  if (diff.trim()) git(['apply', '--whitespace=nowarn', '--allow-empty'], wt, diff);

  // 2. New files that git does not ignore.
  const untracked = git(['ls-files', '--others', '--exclude-standard', '-z'], main).split('\0').filter(Boolean);
  for (const f of untracked) {
    if (f.startsWith('.claude/worktrees/')) continue;
    const dest = path.join(wt, f);
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    fs.copyFileSync(path.join(main, f), dest);
  }

  // 3. The active ticket (git-ignored local state) so the scope guard also works from inside the worktree.
  const ticket = path.join(main, '.claude', 'state', 'active-ticket.json');
  if (fs.existsSync(ticket)) {
    fs.mkdirSync(path.join(wt, '.claude', 'state'), { recursive: true });
    fs.copyFileSync(ticket, path.join(wt, '.claude', 'state', 'active-ticket.json'));
  }

  // 4. Baseline commit on the throwaway worktree branch (the agent never commits).
  git(['add', '-A'], wt);
  git(['-c', 'user.name=Claude worktree', '-c', 'user.email=noreply@anthropic.com', '-c', 'commit.gpgsign=false',
    'commit', '-q', '--allow-empty', '-m', 'worktree baseline: snapshot of the working tree'], wt);

  log(`created ${wt} on ${branch} (${untracked.length} untracked file(s) copied)`);
  process.stdout.write(wt);
} catch (e) {
  log(`failed: ${(e.stderr || e.message || e).toString().trim()}`);
  if (wt && main) { try { git(['worktree', 'remove', '--force', wt], main); } catch { /* best effort */ } }
  process.exit(1);
}
