#!/usr/bin/env node
// Token usage reporter for Claude Code transcripts (no network, no AI).
// Reads ~/.claude/projects/<slug-of-repo-root>*/**/*.jsonl and writes:
//   prompt_logs/token_usage.md    human-readable tables
//   prompt_logs/token_usage.json  machine-readable rollups
//   prompt_logs/logs.md           a one-line "**Tokens:**" summary (with model) under each logged prompt
// Levels: total, day, session, prompt, model, subagent, tool, branch/ticket.
// Usage: node scripts/token-report.mjs [--out <dir>] [--root <dir>] [--quiet]
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const args = process.argv.slice(2);
const opt = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const quiet = args.includes('--quiet');
const repoRoot = path.resolve(opt('--root', path.join(path.dirname(fileURLToPath(import.meta.url)), '..')));
const outDir = path.resolve(opt('--out', path.join(repoRoot, 'prompt_logs')));
const slug = (p) => p.replace(/[^a-zA-Z0-9]/g, '-');
const prefix = slug(repoRoot); // also matches sessions started in subfolders (e.g. src/...)
const projectsDir = path.join(process.env.CLAUDE_CONFIG_DIR || path.join(os.homedir(), '.claude'), 'projects');

function walk(dir, acc = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p, acc);
    else if (e.name.endsWith('.jsonl')) acc.push(p);
  }
  return acc;
}

const zero = () => ({ calls: 0, input: 0, output: 0, cacheRead: 0, cacheWrite: 0, thinking: 0, models: {} });
const add = (a, u, model) => {
  a.calls += 1; a.input += u.input; a.output += u.output;
  a.cacheRead += u.cacheRead; a.cacheWrite += u.cacheWrite; a.thinking += u.thinking;
  if (model) a.models[model] = (a.models[model] || 0) + 1;
};
const total = (a) => a.input + a.output + a.cacheRead + a.cacheWrite;
const bump = (map, key, u, model) => { if (!map.has(key)) map.set(key, zero()); add(map.get(key), u, model); };
const modelList = (a) => Object.entries(a.models).sort((x, y) => y[1] - x[1]).map(([m, c]) => `${m} (${c})`).join(', ') || '-';

function textOf(msg) {
  const c = msg?.content;
  const parts = typeof c === 'string' ? [c] : Array.isArray(c) ? c.filter((b) => b.type === 'text').map((b) => b.text) : [];
  const real = parts.filter((t) => t && !t.trim().startsWith('<'));
  return (real.length ? real[real.length - 1] : parts[0] || '').replace(/\s+/g, ' ').trim();
}
const isToolResult = (msg) => Array.isArray(msg?.content) && msg.content.length > 0 && msg.content.every((b) => b.type === 'tool_result');

const byMsg = new Map(); // dedupe streamed duplicates: key = message.id
const prompts = new Map(); // promptId -> label/time
if (!fs.existsSync(projectsDir)) { console.error('No transcripts dir: ' + projectsDir); process.exit(0); }
const dirs = fs.readdirSync(projectsDir).filter((d) => d.toLowerCase().startsWith(prefix.toLowerCase()));
for (const d of dirs) {
  for (const file of walk(path.join(projectsDir, d))) {
    const isSubFile = file.split(path.sep).includes('subagents');
    let curPrompt = null;
    for (const line of fs.readFileSync(file, 'utf8').split('\n')) {
      if (!line) continue;
      let r; try { r = JSON.parse(line); } catch { continue; }
      if (r.type === 'user' && r.promptId && !r.isSidechain && !isSubFile) {
        curPrompt = r.promptId;
        if (!isToolResult(r.message) && !r.isMeta) {
          const label = textOf(r.message);
          if (label && !prompts.get(curPrompt)?.label) prompts.set(curPrompt, { label, time: r.timestamp, session: r.sessionId });
        }
      }
      if (r.type !== 'assistant' || !r.message?.usage || !r.message.id) continue;
      if (r.message.model === '<synthetic>') continue;
      const u = r.message.usage;
      const rec = {
        id: r.message.id, session: r.sessionId, time: r.timestamp, model: r.message.model || 'unknown',
        prompt: (r.isSidechain || isSubFile) ? null : curPrompt,
        sub: (r.isSidechain || isSubFile) ? (r.agentId || path.basename(file, '.jsonl')) : null,
        branch: r.gitBranch || '',
        tools: Array.isArray(r.message.content) ? r.message.content.filter((b) => b.type === 'tool_use').map((b) => b.name) : [],
        u: {
          input: u.input_tokens || 0, output: u.output_tokens || 0,
          cacheRead: u.cache_read_input_tokens || 0, cacheWrite: u.cache_creation_input_tokens || 0,
          thinking: u.output_tokens_details?.thinking_tokens || 0,
        },
      };
      const prev = byMsg.get(rec.id);
      if (!prev || rec.u.output >= prev.u.output) byMsg.set(rec.id, rec);
    }
  }
}

const all = [...byMsg.values()].sort((a, b) => a.time.localeCompare(b.time));
const grand = zero(); const day = new Map(); const session = new Map(); const model = new Map();
const sub = new Map(); const tool = new Map(); const branch = new Map(); const prompt = new Map();
const ticketRe = /(FEAT|BUG|TASK)-\d+/i;
for (const m of all) {
  add(grand, m.u, m.model);
  bump(day, m.time.slice(0, 10), m.u, m.model);
  bump(session, m.session, m.u, m.model);
  bump(model, m.model, m.u, m.model);
  const t = (m.branch.match(ticketRe)?.[0] || '').toUpperCase();
  bump(branch, t ? `${t} (${m.branch})` : (m.branch || '(none)'), m.u, m.model);
  if (m.sub) bump(sub, m.sub, m.u, m.model);
  if (m.prompt) bump(prompt, m.prompt, m.u, m.model);
  const names = m.tools.length ? m.tools : ['(text only)'];
  for (const nme of names) {
    if (!tool.has(nme)) tool.set(nme, zero());
    const x = tool.get(nme); x.calls += 1; x.output += Math.round(m.u.output / names.length);
    x.input += Math.round(m.u.input / names.length); x.cacheRead += Math.round(m.u.cacheRead / names.length);
    x.cacheWrite += Math.round(m.u.cacheWrite / names.length); x.models[m.model] = (x.models[m.model] || 0) + 1;
  }
}

const n = (x) => x.toLocaleString('en-US');
const row = (k, a) => `| ${k} | ${modelList(a)} | ${a.calls} | ${n(a.input)} | ${n(a.output)} | ${n(a.thinking)} | ${n(a.cacheRead)} | ${n(a.cacheWrite)} | ${n(total(a))} |`;
const head = (k) => `| ${k} | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |\n|---|---|---:|---:|---:|---:|---:|---:|---:|`;
const sorted = (map, f = (a, b) => total(b[1]) - total(a[1])) => [...map.entries()].sort(f);
const esc = (s) => s.replace(/\|/g, '\\|').slice(0, 90);

const orderedPrompts = [...prompt.entries()].sort((a, b) => (prompts.get(a[0])?.time || '').localeCompare(prompts.get(b[0])?.time || ''));
const lines = [];
lines.push('# Token Usage Report', '', `Generated: ${new Date().toISOString()}  `, `Source: ${dirs.length} transcript folder(s) matching prefix \`${prefix}\`  `,
  'Notes: counts come from the API usage recorded in Claude Code transcripts, deduplicated per API message. "Total" = input + output + cache read + cache write. "Thinking" is already inside Output. Cost is not estimated; apply your own rates.', '');
lines.push('## Grand total', '', head('Scope'), row('All sessions', grand), '');
lines.push('## By day', '', head('Day'), ...sorted(day, (a, b) => a[0].localeCompare(b[0])).map(([k, a]) => row(k, a)), '');
lines.push('## By session', '', head('Session'), ...sorted(session).map(([k, a]) => row(k, a)), '');
lines.push('## By prompt (main conversation, chronological)', '', head('# · time (UTC) · prompt'),
  ...orderedPrompts.map(([k, a], i) => { const p = prompts.get(k); return row(`${i + 1} · ${(p?.time || '').slice(11, 19)} · ${esc(p?.label || k)}`, a); }), '');
lines.push('## By model', '', head('Model'), ...sorted(model).map(([k, a]) => row(k, a)), '');
lines.push('## By subagent / sidechain', '', sub.size ? head('Subagent') : '_No subagent usage recorded yet._', ...sorted(sub).map(([k, a]) => row(k, a)), '');
lines.push('## By tool (tokens of the API call that invoked it, split evenly if several)', '', head('Tool'), ...sorted(tool).map(([k, a]) => row(k, a)), '');
lines.push('## By branch / ticket', '', head('Branch'), ...sorted(branch).map(([k, a]) => row(k, a)), '');

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'token_usage.md'), lines.join('\n') + '\n');
const obj = (map) => Object.fromEntries([...map.entries()].map(([k, a]) => [k, { ...a, total: total(a) }]));
fs.writeFileSync(path.join(outDir, 'token_usage.json'), JSON.stringify({
  generated: new Date().toISOString(), grand: { ...grand, total: total(grand) }, byDay: obj(day), bySession: obj(session),
  byPrompt: Object.fromEntries(orderedPrompts.map(([k, a]) => [k, { label: prompts.get(k)?.label, time: prompts.get(k)?.time, ...a, total: total(a) }])),
  byModel: obj(model), bySubagent: obj(sub), byTool: obj(tool), byBranch: obj(branch),
}, null, 2));

// ---- Sync one-line token summaries into logs.md ------------------------------------------
const logPath = path.join(outDir, 'logs.md');
if (fs.existsSync(logPath)) {
  const norm = (s) => s.toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim();
  // Group transcript prompts with identical text (re-sent/interrupted prompts) and sum them.
  const groups = new Map();
  for (const [k, a] of orderedPrompts) {
    const label = norm(prompts.get(k)?.label || '');
    if (!groups.has(label)) groups.set(label, { label, a: zero() });
    const g = groups.get(label).a;
    for (const f of ['calls', 'input', 'output', 'cacheRead', 'cacheWrite', 'thinking']) g[f] += a[f];
    for (const [m, c] of Object.entries(a.models)) g.models[m] = (g.models[m] || 0) + c;
  }
  const cand = [...groups.values()].map((g) => ({ key: g.label, a: g.a, label: g.label }));
  const used = new Set();
  const summary = (a) => `**Tokens:** ${n(total(a))} total (in ${n(a.input)} · out ${n(a.output)} [thinking ${n(a.thinking)}] · cache-read ${n(a.cacheRead)} · cache-write ${n(a.cacheWrite)}) · ${a.calls} API calls · **Model:** ${modelList(a)}`;
  const raw = fs.readFileSync(logPath, 'utf8').replace(/\r\n/g, '\n');
  const parts = raw.split(/\n(?=## Prompt )/);
  const out = parts.map((entry) => {
    if (!entry.startsWith('## Prompt ')) return entry;
    const header = entry.split('\n', 1)[0];
    const matchLine = entry.match(/^\*\*Match:\*\*\s*(.+)$/m)?.[1];
    const userLine = entry.match(/\*\*User:\*\*\s*\n?([^\n]+)/)?.[1] || '';
    let tokens;
    if (/answers/i.test(header) && !matchLine) {
      tokens = "**Tokens:** n/a — answered inside the previous prompt's turn (AskUserQuestion); counted there.";
    } else {
      const snip = norm(matchLine || userLine).slice(0, 40);
      // An explicit **Match:** line sums every transcript prompt that starts like it (re-sent prompts).
      const hits = !snip ? [] : matchLine ? cand.filter((c) => !used.has(c.key) && c.label.includes(snip))
        : [cand.find((c) => !used.has(c.key) && c.label.includes(snip))].filter(Boolean);
      if (hits.length) {
        const sum = zero();
        for (const h of hits) {
          used.add(h.key);
          for (const f of ['calls', 'input', 'output', 'cacheRead', 'cacheWrite', 'thinking']) sum[f] += h.a[f];
          for (const [m, c] of Object.entries(h.a.models)) sum.models[m] = (sum.models[m] || 0) + c;
        }
        tokens = summary(sum);
      }
      else tokens = '**Tokens:** pending (not in a transcript yet; refreshed at the end of the turn)';
    }
    const body = entry.replace(/\n\*\*Tokens:\*\*[^\n]*/g, '').replace(/\s+$/, '');
    if (/\n---$/.test(body)) return body.replace(/\n---$/, `\n${tokens}\n\n---`);
    return `${body}\n\n${tokens}\n\n---`;
  });
  fs.writeFileSync(logPath, out.join('\n').replace(/\n{3,}/g, '\n\n').replace(/\s*$/, '\n'), 'utf8');
}
if (!quiet) console.log(`Wrote ${path.join(outDir, 'token_usage.md')} — ${all.length} API messages, ${n(total(grand))} total tokens.`);
