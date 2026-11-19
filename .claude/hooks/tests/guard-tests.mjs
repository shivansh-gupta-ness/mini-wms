// Tests for the harness hooks. Run: node .claude/hooks/tests/guard-tests.mjs
// Everything runs in temporary folders, so the real .claude/state and the real repository are never touched.
import { spawnSync, execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const HOOKS = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const GIT_DIR = 'C:\\Program Files\\Git\\cmd';
const ENV_PATH = `${process.env.PATH};${GIT_DIR}`;
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'miniwms-hooks-'));
const P = path.join(tmp, 'project'); // fake project root for the guard tests
fs.mkdirSync(path.join(P, '.claude', 'state'), { recursive: true });

let bad = 0;
const check = (name, ok, extra = '') => { if (!ok) bad++; console.log(`${ok ? 'ok  ' : 'FAIL'} ${name}${extra ? ': ' + extra : ''}`); };
const hook = (script, payload, projectDir, cwd = projectDir) => spawnSync('node', [path.join(HOOKS, script)], {
  cwd, input: JSON.stringify(payload), encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH, CLAUDE_PROJECT_DIR: projectDir },
});
const t = (name, script, payload, want, projectDir = P) => { const r = hook(script, payload, projectDir); check(name, r.status === want, `exit ${r.status} (want ${want})`); };
const setTicket = (ticket) => fs.writeFileSync(path.join(P, '.claude', 'state', 'active-ticket.json'), JSON.stringify(ticket));
const clearTicket = () => fs.rmSync(path.join(P, '.claude', 'state', 'active-ticket.json'), { force: true });

const cg = 'command-guard.mjs', sg = 'scope-guard.mjs';
const c = (command) => ({ tool_input: { command } });
const f = (abs, extra = {}) => ({ ...extra, tool_input: { file_path: abs } });
const inP = (rel) => path.join(P, rel);
// git may convert line endings on Windows (core.autocrlf), so file contents are compared with LF endings.
const text = (p) => fs.readFileSync(p, 'utf8').split('\r\n').join('\n');

console.log('-- command guard');
t('force push', cg, c('git push --force origin feat/x'), 2);
t('force push -f', cg, c('git push -f origin feat/x'), 2);
t('push main', cg, c('git push origin main'), 2);
t('curl', cg, c('curl http://example.com'), 2);
t('Invoke-WebRequest', cg, c('Invoke-WebRequest http://example.com'), 2);
t('reset hard', cg, c('git reset --hard HEAD~1'), 2);
t('no-verify', cg, c('git commit --no-verify -m x'), 2);
t('push feature branch', cg, c('git push -u origin feat/FEAT-001-x'), 0);
t('git status', cg, c('git status'), 0);
t('dotnet test', cg, c('dotnet test src/backend'), 0);

console.log('-- scope guard, main tree');
t('.env edit', sg, f(inP('.env')), 2);
t('normal edit, no ticket', sg, f(inP('src\\a.cs')), 0);
t('editing agent, no ticket', sg, f(inP('src\\a.cs'), { agent_type: 'change-implementer' }), 2);
setTicket({ id: 'FEAT-001', risk: 'medium', allowed_paths: ['src/backend/**'] });
t('ticket: in scope', sg, f(inP('src\\backend\\Api\\A.cs')), 0);
t('ticket: out of scope', sg, f(inP('src\\frontend\\a.ts')), 2);
t('ticket: protected .claude', sg, f(inP('.claude\\settings.json')), 2);
t('ticket: protected CLAUDE.md', sg, f(inP('CLAUDE.md')), 2);
t('ticket: protected scripts', sg, f(inP('scripts\\pr-guard.mjs')), 2);

console.log('-- scope guard, drive-letter and separator forms (the Windows path problem)');
const lower = (p) => p[0].toLowerCase() + p.slice(1);
const upper = (p) => p[0].toUpperCase() + p.slice(1);
t('file path with lowercase drive letter, root uppercase', sg, f(lower(inP('src\\backend\\A.cs'))), 0, upper(P));
t('file path with uppercase drive letter, root lowercase', sg, f(upper(inP('src\\backend\\A.cs'))), 0, lower(P));
t('forward slashes', sg, f(inP('src/backend/A.cs').replace(/\\/g, '/')), 0);
t('out of scope with lowercase drive letter', sg, f(lower(inP('src\\frontend\\a.ts'))), 2, upper(P));

console.log('-- scope guard, inside an agent worktree');
const WT = path.join(P, '.claude', 'worktrees', 'agent-x');
fs.mkdirSync(WT, { recursive: true });
t('worktree path, project dir = main: in scope', sg, f(path.join(WT, 'src', 'backend', 'A.cs'), { agent_type: 'test-writer' }), 0);
t('worktree path, project dir = worktree: in scope (ticket found in main repo)', sg, f(path.join(WT, 'src', 'backend', 'A.cs'), { agent_type: 'test-writer' }), 0, WT);
t('worktree path: out of scope', sg, f(path.join(WT, 'src', 'frontend', 'a.ts'), { agent_type: 'change-implementer' }), 2, WT);
t('worktree path: protected .claude/settings.json', sg, f(path.join(WT, '.claude', 'settings.json'), { agent_type: 'change-implementer' }), 2, WT);
t('worktree path: protected REPO_CONTEXT.md', sg, f(path.join(WT, 'REPO_CONTEXT.md'), { agent_type: 'change-implementer' }), 2, WT);
t('root REPO_CONTEXT.md stays protected', sg, f(inP('REPO_CONTEXT.md'), { agent_type: 'change-implementer' }), 2);
t('module REPO_CONTEXT.md is editable when allowed_paths covers it', sg, f(inP('src\\backend\\REPO_CONTEXT.md'), { agent_type: 'change-implementer' }), 0);
t('worktree path: secret file', sg, f(path.join(WT, '.env'), { agent_type: 'change-implementer' }), 2, WT);
clearTicket();
t('worktree, editing agent, no ticket anywhere', sg, f(path.join(WT, 'src', 'backend', 'A.cs'), { agent_type: 'change-implementer' }), 2, WT);
setTicket({ id: 'BUG-002', risk: 'high', allowed_paths: ['src/backend/**'] });
t('worktree, high risk: in scope still blocked', sg, f(path.join(WT, 'src', 'backend', 'A.cs'), { agent_type: 'change-implementer' }), 2, WT);
const guardLog = path.join(P, '.claude', 'state', 'guard-log.jsonl');
check('blocks from a worktree are logged in the main repo guard log', fs.existsSync(guardLog) && fs.readFileSync(guardLog, 'utf8').includes('scope-guard'));
clearTicket();

console.log('-- worktree create, apply and remove (temporary git repository)');
const G = path.join(tmp, 'repo');
const git = (args, cwd = G, input) => execFileSync(path.join(GIT_DIR, 'git.exe'), args, { cwd, input, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
fs.mkdirSync(path.join(G, 'src'), { recursive: true });
fs.writeFileSync(path.join(G, '.gitignore'), '.claude/state/\n.claude/worktrees/\n');
fs.writeFileSync(path.join(G, 'a.txt'), 'one\n');
fs.writeFileSync(path.join(G, 'gone.txt'), 'delete me\n');
git(['init', '-q', '-b', 'task/T-1-x']);
git(['-c', 'user.name=t', '-c', 'user.email=t@t', 'add', '-A']);
git(['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'init']);
// uncommitted state of the main tree: a modified file, a deleted file, a new file, the active ticket
fs.writeFileSync(path.join(G, 'a.txt'), 'one\nmain-change\n');
fs.rmSync(path.join(G, 'gone.txt'));
fs.writeFileSync(path.join(G, 'src', 'new.txt'), 'main-new\n');
fs.mkdirSync(path.join(G, '.claude', 'state'), { recursive: true });
fs.writeFileSync(path.join(G, '.claude', 'state', 'active-ticket.json'), JSON.stringify({ id: 'T-1', risk: 'low', allowed_paths: ['**'] }));

const created = hook('worktree-create.mjs', { name: 'agent-t1', cwd: lower(G) }, G, lower(G)); // start from a lowercase-drive path on purpose
const wt = created.stdout.trim();
check('create: exits 0', created.status === 0, created.status !== 0 ? created.stderr : '');
check('create: prints one absolute path with an uppercase drive letter', /^[A-Z]:\\/.test(wt) && !wt.includes('\n'), wt);
check('create: path is inside .claude\\worktrees', /\\\.claude\\worktrees\\agent-t1$/.test(wt));
check('create: worktree exists on its own branch', fs.existsSync(wt) && git(['rev-parse', '--abbrev-ref', 'HEAD'], wt).trim() === 'worktree-agent-t1');
check('create: tracked change was carried', text(path.join(wt, 'a.txt')) === 'one\nmain-change\n');
check('create: deletion was carried', !fs.existsSync(path.join(wt, 'gone.txt')));
check('create: new file was carried', text(path.join(wt, 'src', 'new.txt')) === 'main-new\n');
check('create: active ticket was carried', fs.existsSync(path.join(wt, '.claude', 'state', 'active-ticket.json')));
check('create: baseline commit leaves the worktree clean', git(['status', '--porcelain'], wt).trim() === '');

// the agent works in the worktree
fs.writeFileSync(path.join(wt, 'a.txt'), 'one\nmain-change\nagent-change\n');
fs.writeFileSync(path.join(wt, 'agent.txt'), 'agent-new\n');
fs.rmSync(path.join(wt, 'src', 'new.txt'));
// the token-report hook rewrites prompt_logs inside the worktree, and the main tree changed it too: this must not block the hand-off
fs.mkdirSync(path.join(wt, 'prompt_logs'), { recursive: true });
fs.writeFileSync(path.join(wt, 'prompt_logs', 'token_usage.md'), 'worktree version\n');
fs.mkdirSync(path.join(G, 'prompt_logs'), { recursive: true });
fs.writeFileSync(path.join(G, 'prompt_logs', 'token_usage.md'), 'main version\n');
const applied = spawnSync('node', [path.join(HOOKS, 'worktree-apply.mjs'), wt], { cwd: G, encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH } });
check('apply: exits 0', applied.status === 0, applied.status !== 0 ? applied.stderr : '');
check('apply: the agent edit reached the main tree', text(path.join(G, 'a.txt')) === 'one\nmain-change\nagent-change\n');
check('apply: the agent new file reached the main tree', text(path.join(G, 'agent.txt')) === 'agent-new\n');
check('apply: the agent deletion reached the main tree', !fs.existsSync(path.join(G, 'src', 'new.txt')));
check('apply: the main tree own deletion is still deleted', !fs.existsSync(path.join(G, 'gone.txt')));
check('apply: prompt_logs in the worktree is not carried over', text(path.join(G, 'prompt_logs', 'token_usage.md')) === 'main version\n');
const again = spawnSync('node', [path.join(HOOKS, 'worktree-apply.mjs'), wt], { cwd: G, encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH } });
check('apply: a second run is refused instead of applying twice', again.status === 1, `exit ${again.status}`);

const removed = hook('worktree-remove.mjs', { worktree_path: wt }, G, G);
check('remove: exits 0', removed.status === 0);
check('remove: the folder is gone', !fs.existsSync(wt));
check('remove: the throwaway branch is gone', !git(['branch', '--list', 'worktree-agent-t1']).trim());
const outside = hook('worktree-remove.mjs', { worktree_path: path.join(tmp, 'somewhere-else') }, G, G);
check('remove: refuses to touch a folder outside .claude\\worktrees', outside.status === 0 && fs.existsSync(tmp));

console.log('-- hook folder for tests (workRoot): the agent worktree, not the main checkout');
process.env.CLAUDE_PROJECT_DIR = P;
const lib = await import(pathToFileURL(path.join(HOOKS, 'lib.mjs')).href);
const wtDir = path.join(P, '.claude', 'worktrees', 'agent-x');
check('workRoot: input cwd inside a worktree gives the worktree root', lib.workRoot({ cwd: path.join(wtDir, 'src', 'backend') }) === wtDir, lib.workRoot({ cwd: path.join(wtDir, 'src', 'backend') }));
check('workRoot: input cwd at the worktree root gives the worktree root', lib.workRoot({ cwd: wtDir }) === wtDir);
check('workRoot: input cwd in the main checkout gives the project folder', lib.workRoot({ cwd: path.join(P, 'src') }) === P);
check('workRoot: no cwd gives the project folder', lib.workRoot({}) === P);
check('workRoot: lowercase drive letter in a worktree cwd still gives the worktree', lib.workRoot({ cwd: lower(path.join(wtDir, 'src')) }) === lower(wtDir));

console.log('-- settings.json: hook commands do not depend on the current folder');
const REPO = path.resolve(HOOKS, '..', '..');
const settings = JSON.parse(fs.readFileSync(path.join(REPO, '.claude', 'settings.json'), 'utf8'));
const hookCommands = Object.values(settings.hooks).flat().flatMap((g) => g.hooks);
check('settings: every hook is a command hook', hookCommands.every((h) => h.type === 'command'), `${hookCommands.length} hooks`);
check('settings: every hook command starts from "$CLAUDE_PROJECT_DIR"', hookCommands.every((h) => h.command.includes('"$CLAUDE_PROJECT_DIR/')));
check('settings: no hook command uses a path relative to the current folder', hookCommands.every((h) => !/^node (\.claude|scripts)/.test(h.command)));
check('settings: every hook runs through bash explicitly', hookCommands.every((h) => h.shell === 'bash'));
const BASH = 'C:\\Program Files\\Git\\bin\\bash.exe';
const viaSettings = (event, matcherPart, payload, cwd) => {
  const group = settings.hooks[event].find((g) => !matcherPart || (g.matcher || '').includes(matcherPart));
  return spawnSync(BASH, ['-c', group.hooks[0].command], {
    cwd, input: JSON.stringify(payload), encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH, CLAUDE_PROJECT_DIR: REPO },
  });
};
const elsewhere = path.join(tmp, 'some', 'other', 'folder');
fs.mkdirSync(elsewhere, { recursive: true });
const scopeRun = viaSettings('PreToolUse', 'Edit', f(path.join(REPO, '.env')), elsewhere);
check('settings command: scope guard still blocks a secret file when run from an unrelated folder', scopeRun.status === 2, `exit ${scopeRun.status} ${String(scopeRun.stderr).split('\n')[0]}`);
const cmdRun = viaSettings('PreToolUse', 'Bash', c('git push origin main'), elsewhere);
check('settings command: command guard still blocks a push to main when run from an unrelated folder', cmdRun.status === 2, `exit ${cmdRun.status} ${String(cmdRun.stderr).split('\n')[0]}`);
const okRun = viaSettings('PreToolUse', 'Bash', c('git status'), elsewhere);
check('settings command: an allowed command passes from an unrelated folder', okRun.status === 0, `exit ${okRun.status}`);

fs.rmSync(tmp, { recursive: true, force: true });
console.log(bad ? `${bad} FAILED` : 'ALL PASSED');
process.exit(bad ? 1 : 0);
