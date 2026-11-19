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
// bypass forms (part 4b)
t('force push after a git global option', cg, c('git -c user.name=x push --force origin feat/x'), 2);
t('force push with -C', cg, c('git -C src push -f origin feat/x'), 2);
t('force push, combined flag -uf', cg, c('git push -uf origin feat/x'), 2);
t('force push, --force-if-includes', cg, c('git push --force-if-includes origin feat/x'), 2);
t('force push, +refspec', cg, c('git push origin +feat/x'), 2);
t('quoted git push to main', cg, c('"git" push origin HEAD:main'), 2);
t('push --all', cg, c('git push --all origin'), 2);
t('delete a remote branch by refspec', cg, c('git push origin :feat/x'), 2);
t('reset hard after -C', cg, c('git -C . reset --hard'), 2);
t('hooksPath change', cg, c('git -c core.hooksPath=/dev/null commit -m x'), 2);
t('rm -fr root', cg, c('rm -fr /'), 2);
t('rm --recursive wildcard', cg, c('rm --recursive *'), 2);
t('rm -rf of a build folder stays allowed', cg, c('rm -rf src/frontend/dist'), 0);
t('Remove-Item -r alias flag on a wildcard', cg, c('Remove-Item -r *'), 2);
t('gh pr merge', cg, c('gh pr merge 5'), 2);
t('gh pr merge with --repo', cg, c('gh --repo a/b pr merge 5'), 2);
t('gh api with a changing method', cg, c('gh api -X PUT repos/a/b/pulls/1/merge'), 2);
t('gh api read stays allowed', cg, c('gh api repos/a/b/git/ref/tags/v4'), 0);
t('gh pr create stays allowed', cg, c('gh pr create --base main --title x'), 0);
t('WebClient download', cg, c('(New-Object Net.WebClient).DownloadString("http://x")'), 2);
t('git push origin feature with a colon refspec to a feature branch', cg, c('git push origin HEAD:feat/FEAT-001-x'), 0);

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

console.log('-- command guard, shell writes to protected paths (ticket active)');
t('redirect into .claude', cg, c('echo x > .claude/settings.json'), 2);
t('append to scripts', cg, c('echo x >> scripts/pr-guard.mjs'), 2);
t('cp into .github', cg, c('cp a.yml .github/workflows/pr-guard.yml'), 2);
t('sed -i on CLAUDE.md', cg, c("sed -i 's/a/b/' CLAUDE.md"), 2);
t('PowerShell Set-Content on the root REPO_CONTEXT.md', cg, c('Set-Content -Path REPO_CONTEXT.md -Value x'), 2);
t('PowerShell Out-File into .claude with backslashes', cg, c('"x" | Out-File .claude\\hooks\\lib.mjs'), 2);
t('node -e writing into the knowledge map', cg, c("node -e \"require('fs').writeFileSync('docs/knowledge-map.yaml','')\""), 2);
t('git checkout restoring a protected file', cg, c('git checkout main -- .claude/settings.json'), 2);
t('absolute path into the repository', cg, c(`echo x > ${inP('.claude\\settings.json')}`), 2);
t('rm of prompt_logs', cg, c('rm prompt_logs/logs.md'), 2);
t('reading a protected file is allowed', cg, c('cat .claude/settings.json'), 0);
t('git status excluding prompt_logs is allowed', cg, c('git status --short -- . ":!prompt_logs"'), 0);
t('running a hook script is allowed', cg, c('node .claude/hooks/worktree-apply.mjs "C:\\x\\.claude\\worktrees\\agent-1" --remove'), 0);
t('running the token report is allowed', cg, c('node scripts/token-report.mjs 2>&1 | tail -3'), 0);
t('writing a report file is allowed', cg, c('echo x >> docs/reports/ac-log.md'), 0);
t('module REPO_CONTEXT.md write is allowed', cg, c('echo x >> src/backend/REPO_CONTEXT.md'), 0);
t('a write inside an agent worktree to src is allowed', cg, c(`cp a.cs ${path.join(P, '.claude', 'worktrees', 'agent-x', 'src', 'a.cs')}`), 0);
t('a write inside an agent worktree to scripts is not', cg, c(`cp a.cs ${path.join(P, '.claude', 'worktrees', 'agent-x', 'scripts', 'a.mjs')}`), 2);
t('a redirect to /dev/null with a protected path read is allowed', cg, c('grep x .claude/settings.json > /dev/null'), 0);

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

console.log('-- hardening: case, junctions, hand-off checks, ticket lookup, settings');
// case: NTFS folders are case-insensitive
t('scope guard: a secret file with other letter case is still blocked', sg, f(inP('.ENV')), 2);
setTicket({ id: 'T-9', risk: 'low', allowed_paths: ['src/**', '.claude/**'] });
t('scope guard: protected folder with other letter case is blocked', sg, f(inP('.CLAUDE/hooks/x.mjs')), 2);
t('scope guard: allowed path with other letter case is accepted', sg, f(inP('SRC/a.cs')), 0);
// junction: src/jlink points at .claude, so an edit through it is an edit to a protected folder
fs.mkdirSync(path.join(P, 'src'), { recursive: true });
try { fs.symlinkSync(path.join(P, '.claude'), path.join(P, 'src', 'jlink'), 'junction'); } catch { /* not permitted */ }
if (fs.existsSync(path.join(P, 'src', 'jlink'))) t('scope guard: an edit through a junction to a protected folder is blocked', sg, f(inP('src/jlink/hooks/new.mjs')), 2);
else check('scope guard: junction test skipped (the junction could not be created)', true);
clearTicket();

// worktree-apply refuses protected files and out-of-scope files, and does not apply anything then
const A = path.join(tmp, 'apply-repo');
const gitA = (args, cwd = A) => execFileSync(path.join(GIT_DIR, 'git.exe'), args, { cwd, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
fs.mkdirSync(path.join(A, 'src'), { recursive: true });
fs.writeFileSync(path.join(A, '.gitignore'), '.claude/state/\n.claude/worktrees/\n');
fs.writeFileSync(path.join(A, 'src', 'a.txt'), 'one\n');
gitA(['init', '-q', '-b', 'task/T-2-x']);
gitA(['-c', 'user.name=t', '-c', 'user.email=t@t', 'add', '-A']);
gitA(['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'init']);
fs.mkdirSync(path.join(A, '.claude', 'state'), { recursive: true });
fs.writeFileSync(path.join(A, '.claude', 'state', 'active-ticket.json'), JSON.stringify({ id: 'T-2', risk: 'low', allowed_paths: ['src/**'] }));
const made = (name) => hook('worktree-create.mjs', { name, cwd: A }, A, A).stdout.trim();
const applyIn = (dir) => spawnSync('node', [path.join(HOOKS, 'worktree-apply.mjs'), dir], { cwd: A, encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH } });
const w1 = made('agent-prot');
fs.mkdirSync(path.join(w1, '.github'), { recursive: true });
fs.writeFileSync(path.join(w1, '.github', 'x.yml'), 'evil\n');
fs.writeFileSync(path.join(w1, 'src', 'a.txt'), 'one\nchanged\n');
const r1 = applyIn(w1);
check('apply: a protected file written in the worktree is refused', r1.status === 1 && /\.github\/x\.yml/.test(String(r1.stderr)), `exit ${r1.status}`);
check('apply: nothing is applied when one file is refused', text(path.join(A, 'src', 'a.txt')) === 'one\n' && !fs.existsSync(path.join(A, '.github', 'x.yml')));
check('apply: the refused worktree is kept for inspection', fs.existsSync(w1));
const w2 = made('agent-scope');
fs.writeFileSync(path.join(w2, 'other.txt'), 'outside the ticket\n');
const r2 = applyIn(w2);
check('apply: a file outside the active ticket allowed_paths is refused', r2.status === 1 && /other\.txt/.test(String(r2.stderr)) && !fs.existsSync(path.join(A, 'other.txt')), `exit ${r2.status}`);
const w3 = made('agent-ok');
fs.writeFileSync(path.join(w3, 'src', 'a.txt'), 'one\nok\n');
const r3 = applyIn(w3);
check('apply: an in-scope change still applies', r3.status === 0 && text(path.join(A, 'src', 'a.txt')) === 'one\nok\n', `exit ${r3.status} ${String(r3.stderr).split('\n')[0]}`);
const decoy = path.join(tmp, 'decoy', '.claude', 'worktrees', 'x');
fs.mkdirSync(decoy, { recursive: true });
const r4 = applyIn(decoy);
check('apply: a folder that only looks like a worktree path is refused', r4.status === 1, `exit ${r4.status}`);

// pr-guard: FEAT-1 must not pick up the ticket FEAT-10
const R = path.join(tmp, 'prg-repo');
const gitR = (args, cwd = R) => execFileSync(path.join(GIT_DIR, 'git.exe'), args, { cwd, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] });
fs.mkdirSync(path.join(R, 'tickets'), { recursive: true });
fs.mkdirSync(path.join(R, 'src'), { recursive: true });
fs.writeFileSync(path.join(R, 'tickets', 'FEAT-10.md'), '---\nid: FEAT-10\nowner: me\nallowed_paths: [src/**, tickets/FEAT-10.md]\n---\n');
fs.writeFileSync(path.join(R, 'src', 'a.txt'), 'one\n');
gitR(['init', '-q', '-b', 'main']);
gitR(['-c', 'user.name=t', '-c', 'user.email=t@t', 'add', '-A']);
gitR(['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'init']);
gitR(['checkout', '-q', '-b', 'feat/FEAT-1-x']);
fs.writeFileSync(path.join(R, 'src', 'a.txt'), 'one\ntwo\n');
gitR(['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-am', 'change']);
const guardRun = (branch) => spawnSync('node', [path.join(HOOKS, '..', '..', 'scripts', 'pr-guard.mjs')], { cwd: R, encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH, BASE_REF: 'main', HEAD_REF: 'HEAD', BRANCH: branch } });
const pg1 = guardRun('feat/FEAT-1-x');
check('pr-guard: branch FEAT-1 does not use the ticket FEAT-10', pg1.status === 1 && /No ticket file tickets\/FEAT-1/.test(String(pg1.stderr)), `exit ${pg1.status}`);
const pg2 = guardRun('feat/FEAT-10-x');
check('pr-guard: branch FEAT-10 finds its own ticket', pg2.status === 0, `exit ${pg2.status} ${String(pg2.stderr).split('\n').slice(0, 3).join(' ')}`);

// guards fail closed: a missing or crashing guard script blocks the call instead of letting it through
const guardCmds = settings.hooks.PreToolUse.flatMap((g) => g.hooks.map((h) => h.command));
const noNode = { ...process.env, PATH: 'C:\\Program Files\\Git\\usr\\bin', CLAUDE_PROJECT_DIR: REPO };
check('settings: both PreToolUse guards are wrapped to fail closed', guardCmds.length === 2 && guardCmds.every((c2) => /rc=\$\?/.test(c2) && /exit 2/.test(c2)));
for (const c2 of guardCmds) {
  const r = spawnSync(BASH, ['-c', c2], { input: '{}', encoding: 'utf8', env: noNode });
  check('settings: a guard that cannot run (node missing) blocks with exit 2', r.status === 2, 'exit ' + r.status);
}

// pr-guard (part 4b): chore/ PRs may not change the harness; the ticket is read from the base branch
const gc = (...a) => gitR(['-c', 'user.name=t', '-c', 'user.email=t@t', ...a]);
gc('checkout', '-q', 'main');
gc('checkout', '-q', '-b', 'chore/harness-edit');
fs.mkdirSync(path.join(R, '.claude'), { recursive: true });
fs.writeFileSync(path.join(R, '.claude', 'x.txt'), 'x\n');
gc('add', '-A'); gc('commit', '-q', '-m', 'harness edit');
const pgChore = guardRun('chore/harness-edit');
check('pr-guard: a chore/ PR that changes .claude is refused', pgChore.status === 1 && /Blocked paths changed outside a setup\/ PR/.test(String(pgChore.stderr)), `exit ${pgChore.status}`);
const pgSetup = guardRun('setup/harness-edit');
check('pr-guard: a setup/ PR may change .claude', pgSetup.status === 0, `exit ${pgSetup.status} ${String(pgSetup.stderr).split('\n').slice(0, 3).join(' ')}`);
const pgLabel = spawnSync('node', [path.join(HOOKS, '..', '..', 'scripts', 'pr-guard.mjs')], { cwd: R, encoding: 'utf8', env: { ...process.env, PATH: ENV_PATH, BASE_REF: 'main', HEAD_REF: 'HEAD', BRANCH: 'feat/FEAT-10-x', PR_LABELS: 'bootstrap' } });
check('pr-guard: the bootstrap label does not allow harness changes', pgLabel.status === 1 && /Blocked paths changed outside a setup\/ PR/.test(String(pgLabel.stderr)), `exit ${pgLabel.status}`);
gc('checkout', '-q', 'main');
gc('checkout', '-q', '-b', 'feat/FEAT-10-widen');
fs.writeFileSync(path.join(R, 'tickets', 'FEAT-10.md'), '---\nid: FEAT-10\nowner: me\nallowed_paths: [src/**, docs/**, tickets/FEAT-10.md]\n---\n');
fs.mkdirSync(path.join(R, 'docs'), { recursive: true });
fs.writeFileSync(path.join(R, 'docs', 'y.md'), 'y\n');
gc('add', '-A'); gc('commit', '-q', '-m', 'widen own ticket');
const pgWiden = guardRun('feat/FEAT-10-widen');
check('pr-guard: a PR cannot widen its own ticket allowed_paths', pgWiden.status === 1 && /outside FEAT-10 allowed_paths/.test(String(pgWiden.stderr)), `exit ${pgWiden.status}`);
gc('checkout', '-q', 'main');
gc('checkout', '-q', '-b', 'feat/FEAT-11-x');
const pgNoBase = guardRun('feat/FEAT-11-x');
check('pr-guard: a ticket that is not on the base branch is refused', pgNoBase.status === 1 && /found on main/.test(String(pgNoBase.stderr)), `exit ${pgNoBase.status}`);

// settings: git branch is allowed only in its read-only forms; --output is denied on git diff and git log
const allow = settings.permissions.allow;
const deny = settings.permissions.deny;
check('settings: the allow list has no unrestricted git branch rule', !allow.some((a) => /git branch\*/.test(a)), allow.filter((a) => /git branch/.test(a)).join(' | '));
check('settings: git branch --list and --show-current stay allowed', allow.includes('PowerShell(git branch --list*)') && allow.includes('PowerShell(git branch --show-current*)'));
check('settings: --output is denied on git diff and git log', ['Bash(git diff*--output*)', 'Bash(git log*--output*)', 'PowerShell(git diff*--output*)', 'PowerShell(git log*--output*)'].every((d) => deny.includes(d)));

fs.rmSync(tmp, { recursive: true, force: true });
console.log(bad ? `${bad} FAILED` : 'ALL PASSED');
process.exit(bad ? 1 : 0);
