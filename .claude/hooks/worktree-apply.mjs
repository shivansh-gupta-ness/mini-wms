// Hand-off: carries an editing agent's work from its worktree back into the main working tree (the task branch).
// Usage: node .claude/hooks/worktree-apply.mjs <worktree path> [--remove]
// The agent's work is the difference from the baseline commit made by worktree-create.mjs, so only what the agent changed is
// applied. Nothing is staged or committed; the owner reviews `git diff` / `git status` afterwards.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { PROTECTED_PATHS, matchAny } from './lib.mjs';

const GIT = fs.existsSync('C:\\Program Files\\Git\\cmd\\git.exe') ? 'C:\\Program Files\\Git\\cmd\\git.exe' : 'git';
const git = (args, cwd, input) => execFileSync(GIT, args, { cwd, input, encoding: 'utf8', maxBuffer: 512 * 1024 * 1024, stdio: ['pipe', 'pipe', 'pipe'] });

const args = process.argv.slice(2);
const given = args.find((a) => !a.startsWith('--'));
const wt = given && path.resolve(given);
if (!wt || !/[\\/]\.claude[\\/]worktrees[\\/]/i.test(wt)) {
  console.error('usage: node .claude/hooks/worktree-apply.mjs <agent worktree path> [--remove]');
  process.exit(2);
}

try {
  const main = path.dirname(git(['rev-parse', '--path-format=absolute', '--git-common-dir'], wt).trim());
  // The folder must really be one of this repository's agent worktrees (not just a path that contains the words).
  const inside = path.relative(path.join(main, '.claude', 'worktrees'), wt);
  if (!inside || inside.startsWith('..') || path.isAbsolute(inside)) throw new Error(`${wt} is not an agent worktree of ${main}`);
  git(['add', '-A'], wt); // include files the agent created or deleted
  // prompt_logs/ is rewritten by the token-report hook inside the worktree as well; it is never the agent's work.
  const scope = ['--', '.', ':(exclude)prompt_logs'];
  const names = git(['diff', '--cached', '--name-status', 'HEAD', ...scope], wt).trim();
  if (!names) { console.log('The agent made no changes.'); }
  else {
    // What the agent changed must still be inside the rules the scope guard enforces on the Edit tool: shell commands
    // do not pass that guard, so the hand-off checks again. Protected paths are always refused; with an active ticket
    // anything outside its allowed_paths is refused too. Nothing is applied when a single file is refused.
    const files = git(['diff', '--cached', '--name-only', '--no-renames', '-z', 'HEAD', ...scope], wt).split('\0').filter(Boolean);
    let ticket = null;
    try { ticket = JSON.parse(fs.readFileSync(path.join(main, '.claude/state/active-ticket.json'), 'utf8')); } catch { /* no active ticket */ }
    const allowed = ticket?.allowed_paths || [];
    const refused = files.filter((f) => matchAny(f, [...PROTECTED_PATHS, ...(ticket?.protected_paths || [])]) || (ticket && !matchAny(f, allowed)));
    if (refused.length) {
      console.error(`worktree-apply refused: the agent changed files it may not change, nothing was applied (the worktree is kept for you to inspect):\n  ${refused.join('\n  ')}`);
      process.exit(1);
    }
    const patch = git(['diff', '--cached', '--binary', 'HEAD', ...scope], wt);
    git(['apply', '--check', '--whitespace=nowarn'], main, patch); // fails without changing anything if it does not fit
    git(['apply', '--whitespace=nowarn'], main, patch);
    console.log(`Applied the agent's changes to ${main}:\n${names}`);
  }
  if (args.includes('--remove')) {
    const branch = git(['rev-parse', '--abbrev-ref', 'HEAD'], wt).trim();
    git(['worktree', 'remove', '--force', wt], main);
    if (branch.startsWith('worktree-')) git(['branch', '-D', branch], main);
    console.log(`Removed ${wt} and ${branch}.`);
  }
} catch (e) {
  console.error(`worktree-apply failed: ${(e.stderr || e.message || e).toString().trim()}`);
  process.exit(1);
}
