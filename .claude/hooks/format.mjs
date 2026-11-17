// PostToolUse (Edit|Write|MultiEdit): format the edited file. Silent no-op until src/ has a project.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { readInput, root } from './lib.mjs';

const input = readInput();
const f = input.tool_input?.file_path;
if (!f) process.exit(0);
const abs = path.resolve(root(), f);
const dotnet = fs.existsSync('C:\\Program Files\\dotnet\\dotnet.exe') ? 'C:\\Program Files\\dotnet\\dotnet.exe' : 'dotnet';
const run = (cmd, args, cwd) => { try { execFileSync(cmd, args, { cwd, stdio: 'ignore', timeout: 60000 }); } catch { /* never block on formatting */ } };

if (abs.endsWith('.cs')) {
  const proj = path.join(root(), 'src', 'backend');
  if (fs.existsSync(proj) && fs.readdirSync(proj).some((x) => /\.(sln|slnx)$/.test(x))) run(dotnet, ['format', '--include', abs, '--no-restore'], proj);
} else if (/\.(ts|html|scss|css|json)$/.test(abs) && abs.includes(`${path.sep}src${path.sep}frontend${path.sep}`)) {
  const fe = path.join(root(), 'src', 'frontend');
  const prettier = path.join(fe, 'node_modules', '.bin', process.platform === 'win32' ? 'prettier.cmd' : 'prettier');
  if (fs.existsSync(prettier)) run(prettier, ['--write', abs], fe);
}
process.exit(0);
