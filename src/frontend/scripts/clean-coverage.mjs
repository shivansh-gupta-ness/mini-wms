// Removes the previous coverage report so the gate can never read a stale summary (run before the tests).
import { rmSync } from 'node:fs';

rmSync('coverage', { recursive: true, force: true });
