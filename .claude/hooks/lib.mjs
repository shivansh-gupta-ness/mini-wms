// Shared helpers for harness hooks. Hooks read one JSON object from stdin.
import fs from 'node:fs';
import path from 'node:path';

export function readInput() {
  try { return JSON.parse(fs.readFileSync(0, 'utf8') || '{}'); } catch { return {}; }
}
export const root = () => process.env.CLAUDE_PROJECT_DIR || process.cwd();
export const rel = (p) => path.relative(root(), path.resolve(root(), p)).replace(/\\/g, '/');
// "**/" = any folders (or none); a trailing "**" = anything; "*" = within one path segment.
export const globToRe = (g) => new RegExp('^' + g.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*\*\//g, '\u0000').replace(/\*\*/g, '\u0001').replace(/\*/g, '[^/]*').replace(/\u0000/g, '(?:.*/)?').replace(/\u0001/g, '.*') + '$');
export const matchAny = (f, globs) => globs.some((g) => globToRe(g.replace(/^\.?\//, '')).test(f));
export const EDITING_AGENTS = ['change-implementer', 'test-writer'];
export const isEditingAgent = (i) => EDITING_AGENTS.includes(i.agent_type || '');
export function activeTicket() {
  try { return JSON.parse(fs.readFileSync(path.join(root(), '.claude/state/active-ticket.json'), 'utf8')); } catch { return null; }
}
// Append a line to the guard log (the learning loop reads this: "scope guard or command guard blocks an action").
export function logBlock(kind, detail) {
  try {
    const dir = path.join(root(), '.claude/state'); fs.mkdirSync(dir, { recursive: true });
    fs.appendFileSync(path.join(dir, 'guard-log.jsonl'), JSON.stringify({ t: new Date().toISOString(), kind, detail }) + '\n');
  } catch { /* logging must never break a hook */ }
}
export function block(kind, message, detail) {
  logBlock(kind, detail || message);
  process.stderr.write(`${kind} blocked: ${message}\n`);
  process.exit(2); // exit 2 = block the tool call and show stderr to the model
}
