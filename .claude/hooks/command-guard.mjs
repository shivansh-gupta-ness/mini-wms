// PreToolUse (Bash|PowerShell): command guard. Matches command text, so it can be sidestepped;
// permission rules, approval prompts and the pr-guard required check back it up.
// Blocks: force pushes, pushes to main/master, destructive deletes/resets, network downloads, hook bypass.
import { readInput, block } from './lib.mjs';

const input = readInput();
const cmd = String(input.tool_input?.command || '');
if (!cmd) process.exit(0);
const c = cmd.replace(/\s+/g, ' ');

const rules = [
  [/git\s+push\b[^|;&]*(--force|--force-with-lease|\s-f\b|\s\+\S+)/i, 'force push'],
  [/git\s+push\b[^|;&]*\b(main|master)\b/i, 'push to main/master (use a branch and a PR)'],
  [/git\s+reset\s+--hard/i, 'git reset --hard'],
  [/git\s+clean\s+-[a-z]*f/i, 'git clean -f'],
  [/--no-verify|--no-gpg-sign/i, 'bypassing git hooks/signing'],
  [/\brm\s+-[a-z]*r[a-z]*\s+(\/|~|\.\.?\/?(\s|$)|\*)/i, 'recursive delete of a root/parent/wildcard'],
  [/Remove-Item\b[^|;&]*-Recurse[^|;&]*\s(\\|\/|[A-Za-z]:\\?(\s|$)|\.\.?(\s|$)|\*|~)/i, 'recursive Remove-Item of a root/parent/wildcard'],
  [/\b(curl|wget|Invoke-WebRequest|Invoke-RestMethod|iwr|irm)\b/i, 'network download/upload command'],
  [/\b(diskpart|shutdown|Stop-Computer|Restart-Computer)\b/i, 'system-level command'],
  [/gh\s+(pr\s+merge|repo\s+delete)/i, 'gh merge/delete (owner does this)'],
];
for (const [re, why] of rules) if (re.test(c)) block('command-guard', `${why}. Command: ${cmd.slice(0, 160)}`);
process.exit(0);
