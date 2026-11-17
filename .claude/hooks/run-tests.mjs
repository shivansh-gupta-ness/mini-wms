// Stop / SubagentStop: tests for changed paths must pass before an EDITING agent finishes.
// Does nothing for the owner's own sessions, or before src/ has a project (then there is nothing to test).
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { readInput, root, isEditingAgent, block } from './lib.mjs';

const input = readInput();
if (input.stop_hook_active) process.exit(0); // avoid loops
if (!isEditingAgent(input)) process.exit(0);

const dotnet = fs.existsSync('C:\\Program Files\\dotnet\\dotnet.exe') ? 'C:\\Program Files\\dotnet\\dotnet.exe' : 'dotnet';
const changed = (() => { try { return execFileSync('git', ['status', '--porcelain'], { cwd: root(), encoding: 'utf8' }); } catch { return ''; } })();
const be = path.join(root(), 'src', 'backend');
const fe = path.join(root(), 'src', 'frontend');
const failures = [];
const tail = (e) => String(e.stdout || e.message).split('\n').filter((l) => /error|fail/i.test(l)).slice(0, 25).join('\n');

if (/src\/backend\//.test(changed) && fs.existsSync(be) && fs.readdirSync(be).some((x) => /\.(sln|slnx)$/.test(x))) {
  try { execFileSync(dotnet, ['test', '--nologo', '-v', 'q'], { cwd: be, encoding: 'utf8', timeout: 540000, stdio: ['ignore', 'pipe', 'pipe'] }); }
  catch (e) { failures.push('dotnet test failed:\n' + tail(e)); }
}
if (/src\/frontend\//.test(changed) && fs.existsSync(path.join(fe, 'package.json'))) {
  try { execFileSync(process.platform === 'win32' ? 'npm.cmd' : 'npm', ['test', '--', '--watch=false'], { cwd: fe, encoding: 'utf8', timeout: 540000, stdio: ['ignore', 'pipe', 'pipe'], shell: true }); }
  catch (e) { failures.push('frontend tests failed:\n' + tail(e)); }
}
if (failures.length) block('run-tests', 'Tests for changed paths must pass before you finish.\n' + failures.join('\n'));
process.exit(0);
