// Shared helpers for harness hooks. Hooks read one JSON object from stdin.
import fs from 'node:fs';
import path from 'node:path';

export function readInput() {
  try { return JSON.parse(fs.readFileSync(0, 'utf8') || '{}'); } catch { return {}; }
}

// The folder the session or agent works in. Inside an agent worktree this is the worktree folder.
export const root = () => process.env.CLAUDE_PROJECT_DIR || process.cwd();

// Agent worktrees live in <repo>/.claude/worktrees/<name>. Paths there must be judged as if they were in <repo>
// (an agent's edit to <repo>/.claude/worktrees/x/src/a.cs is a change to src/a.cs, not to .claude/**).
const WORKTREE = /[\\/]\.claude[\\/]worktrees[\\/][^\\/]+/i;

// The main repository folder, even when the hook runs inside an agent worktree. Ticket state and the guard log live here.
export const mainRoot = () => {
  const base = root();
  const m = base.match(WORKTREE);
  return m ? base.slice(0, m.index) : base;
};

// Repo-relative path with forward slashes, with any agent-worktree prefix removed.
export const rel = (p) => {
  const abs = path.resolve(root(), p);
  const m = abs.match(WORKTREE);
  const r = m ? abs.slice(m.index + m[0].length) : path.relative(mainRoot(), abs);
  return r.replace(/\\/g, '/').replace(/^\/+/, '');
};

// "**/" = any folders (or none); a trailing "**" = anything; "*" = within one path segment.
export const globToRe = (g) => new RegExp('^' + g.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*\*\//g, '\u0000').replace(/\*\*/g, '\u0001').replace(/\*/g, '[^/]*').replace(/\u0000/g, '(?:.*/)?').replace(/\u0001/g, '.*') + '$');
export const matchAny = (f, globs) => globs.some((g) => globToRe(g.replace(/^\.?\//, '')).test(f));
export const EDITING_AGENTS = ['change-implementer', 'test-writer'];
export const isEditingAgent = (i) => EDITING_AGENTS.includes(i.agent_type || '');

// The active ticket is written by /start-ticket in the main repository (git-ignored, so a worktree does not get it).
export function activeTicket() {
  for (const base of [mainRoot(), root()]) {
    try { return JSON.parse(fs.readFileSync(path.join(base, '.claude/state/active-ticket.json'), 'utf8')); } catch { /* try the next folder */ }
  }
  return null;
}

// Append a line to the guard log (the learning loop reads this: "scope guard or command guard blocks an action").
export function logBlock(kind, detail) {
  try {
    const dir = path.join(mainRoot(), '.claude/state'); fs.mkdirSync(dir, { recursive: true });
    fs.appendFileSync(path.join(dir, 'guard-log.jsonl'), JSON.stringify({ t: new Date().toISOString(), kind, detail }) + '\n');
  } catch { /* logging must never break a hook */ }
}
export function block(kind, message, detail) {
  logBlock(kind, detail || message);
  process.stderr.write(`${kind} blocked: ${message}\n`);
  process.exit(2); // exit 2 = block the tool call and show stderr to the model
}
