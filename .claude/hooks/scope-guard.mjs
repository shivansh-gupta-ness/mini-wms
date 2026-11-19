// PreToolUse (Edit|Write|MultiEdit|NotebookEdit): scope guard.
// Rules:
//  1. Secrets and env files are never edited by anyone.
//  2. When a ticket is active (.claude/state/active-ticket.json written by /start-ticket) or the caller is an
//     editing agent, edits must stay inside the ticket's allowed_paths and must not touch protected paths.
//  3. With no active ticket and no editing agent (setup work by the owner), everything else is allowed.
import { readInput, rel, matchAny, isEditingAgent, activeTicket, block, PROTECTED_PATHS } from './lib.mjs';

const input = readInput();
const target = input.tool_input?.file_path || input.tool_input?.notebook_path;
if (!target) process.exit(0);
const file = rel(target);

const secrets = ['.env', '.env.*', '**/.env', '**/.env.*', '**/secrets/**', '**/*.pfx', '**/*.key', '**/appsettings.*.local.json'];
if (matchAny(file, secrets)) block('scope-guard', `${file} is a secret/env file; agents never edit these.`);

const ticket = activeTicket();
if (!ticket && !isEditingAgent(input)) process.exit(0);

const protectedPaths = [...PROTECTED_PATHS, ...(ticket?.protected_paths || [])];
if (matchAny(file, protectedPaths)) block('scope-guard', `${file} is a protected path (harness, shared context, CI or logs). Ask the owner.`);

if (!ticket) block('scope-guard', `No active ticket. Run /start-ticket <ID> and get the plan approved before editing ${file}.`);
if (ticket.risk === 'high' || ticket.risk === 'blocked') block('scope-guard', `Ticket ${ticket.id} is risk=${ticket.risk}: agents read and propose only; the owner makes the edit.`);
const allowed = ticket.allowed_paths || [];
if (!allowed.length) block('scope-guard', `Ticket ${ticket.id} has no allowed_paths.`);
if (!matchAny(file, allowed)) block('scope-guard', `${file} is outside ${ticket.id} allowed_paths [${allowed.join(', ')}]. Stop and ask the owner to widen scope.`);
process.exit(0);
