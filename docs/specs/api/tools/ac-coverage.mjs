// AC coverage check: every LOC-n.n and PRD-n.n acceptance criterion in docs/specs/product.md
// must appear in an x-acceptance-criteria list of the contracts in docs/specs/api/.
// Plain Node, no dependencies. Usage: node docs/specs/api/tools/ac-coverage.mjs (from anywhere).
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(here, '..', '..', '..', '..');
const read = (p) => readFileSync(join(repoRoot, p), 'utf8');
const CONTRACTS = ['docs/specs/api/locations.yaml', 'docs/specs/api/products.yaml'];
const idPattern = /^(?:LOC|PRD)-\d+\.\d+$/;

// product.md: numbered acceptance criteria are list lines that start with the id ("- LOC-1.1 Given ...").
const spec = new Set();
for (const line of read('docs/specs/product.md').split(/\r?\n/)) {
  const m = line.match(/^\s*[-*]\s+((?:LOC|PRD)-\d+\.\d+)\s/);
  if (m) spec.add(m[1]);
}

// contracts: x-acceptance-criteria: [A, B] lines, attributed to the nearest preceding operationId.
const carriers = new Map(); // id -> operations
for (const file of CONTRACTS) {
  let operation = '(unknown)';
  let block = false; // inside a block-style list ("x-acceptance-criteria:" followed by "- ID" lines)
  const add = (id) => {
    if (!carriers.has(id)) carriers.set(id, []);
    carriers.get(id).push(operation);
  };
  for (const line of read(file).split(/\r?\n/)) {
    const op = line.match(/^\s+operationId:\s*(\S+)/);
    if (op) operation = op[1];
    const item = block ? line.match(/^\s+-\s+(\S+)\s*$/) : null;
    if (item) {
      add(item[1]);
      continue;
    }
    block = false;
    const inline = line.match(/^\s+x-acceptance-criteria:\s*\[(.*)\]\s*$/);
    if (inline) {
      for (const id of inline[1].split(',').map((s) => s.trim()).filter(Boolean)) add(id);
    } else if (/^\s+x-acceptance-criteria:\s*$/.test(line)) {
      block = true;
    }
  }
}

const sortIds = (a, b) => a.localeCompare(b, undefined, { numeric: true });
const rows = [...spec].sort(sortIds).map((id) => {
  const ops = carriers.get(id) ?? [];
  return { id, ops: ops.join(', ') || '-', covered: ops.length > 0 ? 'yes' : 'NO' };
});

const w = Math.max(...rows.map((r) => r.ops.length), 10);
console.log(`${'id'.padEnd(9)}${'covered'.padEnd(9)}operations`);
for (const r of rows) console.log(`${r.id.padEnd(9)}${r.covered.padEnd(9)}${r.ops.padEnd(w)}`);

const onlyInContracts = [...carriers.keys()].filter((id) => !spec.has(id)).sort(sortIds);
const uncovered = rows.filter((r) => r.covered === 'NO').map((r) => r.id);
console.log(`\nIds in product.md: ${spec.size}; covered: ${spec.size - uncovered.length}`);
console.log(`In product.md but in no contract: ${uncovered.join(', ') || 'none'}`);
console.log(`In a contract but not a product.md LOC/PRD criterion: ${onlyInContracts.join(', ') || 'none'}`);
for (const id of onlyInContracts) if (!idPattern.test(id)) console.log(`Malformed id in contract: ${id}`);

// A parser that finds nothing must not read as success (for example after a format change in product.md or a contract).
const parsed = spec.size > 0 && carriers.size > 0;
if (!parsed) {
  console.error(`FAILED: parsed ${spec.size} criteria from product.md and ${carriers.size} ids from the contracts; nothing to compare.`);
}
process.exit(parsed && uncovered.length === 0 ? 0 : 1);
