import { Bin, Zone } from '../data/location.models';

/** What a Deactivate button refers to; stage 3 opens the confirm dialog for it. */
export type DeactivateTarget = { kind: 'zone'; zone: Zone } | { kind: 'bin'; bin: Bin };
