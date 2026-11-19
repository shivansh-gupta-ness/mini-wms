// PreToolUse (Bash|PowerShell): command guard. Matches command text, so it can be sidestepped;
// permission rules, approval prompts and the pr-guard required check back it up.
// Blocks: force pushes, pushes to main/master, destructive deletes/resets, network downloads, hook bypass, gh merges,
// and (while a ticket is active or an editing agent calls) shell writes to protected paths.
import { readInput, block, activeTicket, isEditingAgent, mainRoot, PROTECTED_PATHS } from './lib.mjs';

const input = readInput();
const cmd = String(input.tool_input?.command || '');
if (!cmd) process.exit(0);

// Normal form for matching: no quotes, backticks or carets (so `"git" push` and g`it` still match), one space between words.
const c = cmd.replace(/[`'"^]/g, '').replace(/\s+/g, ' ');
const GIT = 'git(?:\\s+(?:-[cC]\\s+\\S+|--[\\w-]+(?:=\\S+)?))*\\s+'; // git, then any global options (-c x=y, -C dir, --no-pager)
const GH = 'gh(?:\\s+(?:-R|--repo)\\s+\\S+)*\\s+';

const rules = [
  [new RegExp(GIT + 'push\\b[^|;&]*(--force|--mirror|\\s-[a-z]*f[a-z]*(\\s|$)|\\s\\+\\S+)', 'i'), 'force push'],
  [new RegExp(GIT + 'push\\b[^|;&]*(\\b(main|master)\\b|--all\\b|--delete\\b|\\s:\\S+)', 'i'), 'push to main/master, push --all, or deleting a remote ref (use a branch and a PR)'],
  [new RegExp(GIT + 'reset\\s+[^|;&]*--hard', 'i'), 'git reset --hard'],
  [new RegExp(GIT + 'clean\\s+[^|;&]*-[a-z]*f', 'i'), 'git clean -f'],
  [/--no-verify|--no-gpg-sign|core\.hooksPath/i, 'bypassing git hooks/signing'],
  [/\brm\s+(?:-[\w-]+\s+)*(?:-[a-z]*r[a-z]*|--recursive)\s+(?:-[\w-]+\s+)*(\/|~|\.\.?\/?(\s|$)|\*)/i, 'recursive delete of a root/parent/wildcard'],
  [/\b(Remove-Item|ri|rd|rmdir|del|erase)\b[^|;&]*\s-r(ecurse)?\b[^|;&]*\s(\\|\/|[A-Za-z]:\\?(\s|$)|\.\.?(\s|$)|\*|~)/i, 'recursive Remove-Item of a root/parent/wildcard'],
  [/\b(curl|wget|Invoke-WebRequest|Invoke-RestMethod|iwr|irm|Start-BitsTransfer|bitsadmin)\b|Net\.WebClient|DownloadFile|DownloadString|certutil[^|;&]*urlcache/i, 'network download/upload command'],
  [/\b(diskpart|shutdown|Stop-Computer|Restart-Computer)\b/i, 'system-level command'],
  [new RegExp(GH + '(pr\\s+merge|repo\\s+delete|release\\s+delete|api\\b[^|;&]*(-X|--method)\\s*=?\\s*(PUT|DELETE|PATCH|POST))', 'i'), 'gh merge/delete or a changing gh api call (owner does this)'],
];
for (const [re, why] of rules) if (re.test(c)) block('command-guard', `${why}. Command: ${cmd.slice(0, 160)}`);

// Shell writes to protected paths. The scope guard only sees the Edit and Write tools; this covers a shell command that
// would do the same thing (redirects, cp, mv, sed -i, Set-Content, node -e ...). Setup work by the owner (no ticket, no editing agent) is not checked.
if (activeTicket() || isEditingAgent(input)) {
  const root = mainRoot().replace(/\\/g, '/').replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const n = c.replace(/\\/g, '/')
    .replace(new RegExp(root + '/?', 'ig'), '') // absolute paths into the repository become repository-relative
    .replace(/(^|[\s=(])(\.\/)+/g, '$1')
    .replace(/\.claude\/worktrees\/[^/\s]+\//gi, ''); // a path inside an agent worktree is judged as the same path in the repository
  // Root-anchored names: a module file such as src/backend/REPO_CONTEXT.md or src/frontend/scripts/x is not protected.
  const names = ['\\.github', '\\.claude', 'scripts', 'prompt_logs', 'CLAUDE\\.md', 'REPO_CONTEXT\\.md', 'docs/knowledge-map\\.yaml'];
  const touchesProtected = new RegExp('(?:^|[\\s=(;|&>:])(?:' + names.join('|') + ')(?:/|\\b|$)', 'i').test(n);
  // Write signals: a verb that changes files, or a redirect to something other than /dev/null, $null or a stream such as 2>&1.
  const verbWrite = /\b(tee|sed\s+-[a-z]*i|cp|mv|rm|rmdir|del|touch|truncate|install|Set-Content|Add-Content|Out-File|Remove-Item|Copy-Item|Move-Item|New-Item|Rename-Item|Clear-Content|Set-ItemProperty)\b|\bgit\s+(?:\S+\s+)*(checkout|restore|apply|rm|mv|stash)\b|writeFile|appendFile|rmSync|unlinkSync|renameSync|copyFile|cpSync|\[IO\.File\]::(Write|Append|Delete|Copy|Move)/i.test(n);
  const redirectWrite = />>?\s*(?!&|\/dev\/null|\$null|NUL\b)\S/i.test(n);
  if (touchesProtected && (verbWrite || redirectWrite)) {
    block('command-guard', `a shell command that may write to a protected path (${PROTECTED_PATHS.join(', ')}) while a ticket is active. Ask the owner. Command: ${cmd.slice(0, 160)}`);
  }
}
process.exit(0);
