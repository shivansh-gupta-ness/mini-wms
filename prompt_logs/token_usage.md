# Token Usage Report

Generated: 2026-11-17T15:48:48.572Z  
Source: 1 transcript folder(s) matching prefix `C--Users-P7120484-Downloads-Claude-Eval`  
Notes: counts come from the API usage recorded in Claude Code transcripts, deduplicated per API message. "Total" = input + output + cache read + cache write. "Thinking" is already inside Output. Cost is not estimated; apply your own rates.

## Grand total

| Scope | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| All sessions | claude-sonnet-5-5 (45), claude-sonnet-4-6 (15) | 60 | 117 | 75,323 | 17,414 | 4,995,055 | 293,195 | 5,363,690 |

## By day

| Day | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 2026-11-17 | claude-sonnet-5-5 (45), claude-sonnet-4-6 (15) | 60 | 117 | 75,323 | 17,414 | 4,995,055 | 293,195 | 5,363,690 |

## By session

| Session | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| d056af0b-88c6-4c97-8f17-3c4ec23e666f | claude-sonnet-5-5 (45), claude-sonnet-4-6 (14) | 59 | 114 | 75,096 | 17,301 | 4,971,153 | 282,685 | 5,329,048 |
| c65f4548-f0b0-4be8-a353-eb589f45bc26 | claude-sonnet-4-6 (1) | 1 | 3 | 227 | 113 | 23,902 | 10,510 | 34,642 |

## By prompt (main conversation, chronological)

| # · time (UTC) · prompt | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 1 ·  · 16cb0bd7-a992-4128-833d-e48038c2feb5 | claude-sonnet-5-5 (6) | 6 | 14 | 8,575 | 1,366 | 529,918 | 13,092 | 551,599 |
| 2 · 10:28:29 · Make sure to write my each prompt in prompt_logs/logs.md file. go through my claude code a | claude-sonnet-4-6 (1) | 1 | 3 | 227 | 113 | 23,902 | 10,510 | 34,642 |
| 3 · 10:30:51 · Make sure to write my each prompt in prompt_logs/logs.md file. go through my claude code a | claude-sonnet-4-6 (14) | 14 | 16 | 6,441 | 2,174 | 965,830 | 69,653 | 1,041,940 |
| 4 · 10:44:35 · Make sure to montior and log my token usage at every level from per prompt to per whatever | claude-sonnet-5-5 (7) | 7 | 14 | 3,315 | 471 | 342,766 | 26,080 | 372,175 |
| 5 · 11:01:33 · gh login done, continue setup | claude-sonnet-5-5 (9) | 9 | 20 | 10,623 | 3,427 | 541,729 | 25,468 | 577,840 |
| 6 · 11:39:17 · go ahead with this format and also do this - prompt_logs/logs.md to carry a one-line token | claude-sonnet-5-5 (2) | 2 | 6 | 3,806 | 2,858 | 162,726 | 689 | 167,227 |
| 7 · 12:00:28 · I want to rename the Claude_Eval folder to MiniWMS folder. And remove existing MiniWMS fol | claude-sonnet-5-5 (12) | 12 | 26 | 29,638 | 5,859 | 1,277,232 | 29,365 | 1,336,261 |
| 8 · 15:44:13 · continue from where you left | claude-sonnet-5-5 (9) | 9 | 18 | 12,698 | 1,146 | 1,150,952 | 118,338 | 1,282,006 |

## By model

| Model | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| claude-sonnet-5-5 | claude-sonnet-5-5 (45) | 45 | 98 | 68,655 | 15,127 | 4,005,323 | 213,032 | 4,287,108 |
| claude-sonnet-4-6 | claude-sonnet-4-6 (15) | 15 | 19 | 6,668 | 2,287 | 989,732 | 80,163 | 1,076,582 |

## By subagent / sidechain

_No subagent usage recorded yet._

## By tool (tokens of the API call that invoked it, split evenly if several)

| Tool | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| PowerShell | claude-sonnet-5-5 (22), claude-sonnet-4-6 (4) | 26 | 56 | 17,777 | 0 | 2,184,068 | 137,279 | 2,339,180 |
| Write | claude-sonnet-5-5 (11), claude-sonnet-4-6 (1) | 12 | 23 | 44,514 | 0 | 1,307,828 | 49,701 | 1,402,066 |
| AskUserQuestion | claude-sonnet-4-6 (3), claude-sonnet-5-5 (1) | 4 | 5 | 2,614 | 0 | 336,406 | 4,386 | 343,411 |
| (text only) | claude-sonnet-5-5 (3), claude-sonnet-4-6 (1) | 4 | 7 | 2,372 | 0 | 320,909 | 5,686 | 328,974 |
| Edit | claude-sonnet-5-5 (2) | 2 | 4 | 1,406 | 0 | 246,339 | 1,653 | 249,402 |
| Read | claude-sonnet-4-6 (3), claude-sonnet-5-5 (1) | 4 | 5 | 743 | 0 | 204,175 | 30,661 | 235,584 |
| Glob | claude-sonnet-4-6 (3), claude-sonnet-5-5 (1) | 4 | 9 | 1,758 | 0 | 149,711 | 42,254 | 193,732 |
| Grep | claude-sonnet-5-5 (2) | 2 | 4 | 521 | 0 | 138,429 | 5,192 | 144,146 |
| Bash | claude-sonnet-5-5 (2) | 2 | 4 | 3,618 | 0 | 107,190 | 16,383 | 127,195 |

## By branch / ticket

| Branch | Model (API calls) | API calls | Input | Output | of which thinking | Cache read | Cache write | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| HEAD | claude-sonnet-5-5 (36), claude-sonnet-4-6 (15) | 51 | 99 | 62,625 | 16,268 | 3,844,103 | 174,857 | 4,081,684 |
| main | claude-sonnet-5-5 (9) | 9 | 18 | 12,698 | 1,146 | 1,150,952 | 118,338 | 1,282,006 |

