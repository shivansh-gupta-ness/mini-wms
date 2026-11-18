// WorktreeRemove hook: removes an agent worktree created by worktree-create.mjs and deletes its throwaway branch.
// Best effort: it never fails the session.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { readInput } from './lib.mjs';

const GIT = fs.existsSync('C:\\Program Files\\Git\\cmd\\git.exe') ? 'C:\\Program Files\\Git\\cmd\\git.exe' : 'git';
const git = (args, cwd) => execFileSync(GIT, args, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
const log = (m) => process.stderr.write(`worktree-remove: ${m}\n`);

try {
  const input = readInput();
  const wt = input.worktree_path || input.path;
  if (!wt || !/[\\/]\.claude[\\/]worktrees[\\/]/i.test(wt)) { log('not an agent worktree, nothing to do'); process.exit(0); }
  const common = git(['rev-parse', '--path-format=absolute', '--git-common-dir'], wt).trim();
  const main = path.dirname(common);
  const branch = git(['rev-parse', '--abbrev-ref', 'HEAD'], wt).trim();
  git(['worktree', 'remove', '--force', wt], main);
  if (branch.startsWith('worktree-')) git(['branch', '-D', branch], main);
  log(`removed ${wt} and ${branch}`);
} catch (e) {
  log(`ignored: ${(e.stderr || e.message || e).toString().trim()}`);
}
process.exit(0);
