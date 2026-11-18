// Hand-off: carries an editing agent's work from its worktree back into the main working tree (the task branch).
// Usage: node .claude/hooks/worktree-apply.mjs <worktree path> [--remove]
// The agent's work is the difference from the baseline commit made by worktree-create.mjs, so only what the agent changed is
// applied. Nothing is staged or committed; the owner reviews `git diff` / `git status` afterwards.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const GIT = fs.existsSync('C:\\Program Files\\Git\\cmd\\git.exe') ? 'C:\\Program Files\\Git\\cmd\\git.exe' : 'git';
const git = (args, cwd, input) => execFileSync(GIT, args, { cwd, input, encoding: 'utf8', maxBuffer: 512 * 1024 * 1024, stdio: ['pipe', 'pipe', 'pipe'] });

const args = process.argv.slice(2);
const wt = args.find((a) => !a.startsWith('--'));
if (!wt || !/[\\/]\.claude[\\/]worktrees[\\/]/i.test(wt)) {
  console.error('usage: node .claude/hooks/worktree-apply.mjs <agent worktree path> [--remove]');
  process.exit(2);
}

try {
  const main = path.dirname(git(['rev-parse', '--path-format=absolute', '--git-common-dir'], wt).trim());
  git(['add', '-A'], wt); // include files the agent created or deleted
  // prompt_logs/ is rewritten by the token-report hook inside the worktree as well; it is never the agent's work.
  const scope = ['--', '.', ':(exclude)prompt_logs'];
  const names = git(['diff', '--cached', '--name-status', 'HEAD', ...scope], wt).trim();
  if (!names) { console.log('The agent made no changes.'); }
  else {
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
