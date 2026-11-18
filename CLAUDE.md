# CLAUDE.md (local only — excluded from git, never upstream)

Fork-in-progress of [IceYGO/windbot](https://github.com/IceYGO/windbot), a C#
YGOPro duel bot. **Goal: write a `Toon` deck AI (2026 Toon list)** to practice
against in MDPro4, then upstream it as a PR.

## Read first

`AGENTS.md` is upstream's authoritative contributor guide (written in Chinese).
Read all of it before touching code. The rules that matter most:

- `AddExecutor` registration order **is** priority: first match that returns
  `true` wins. Reordering is a behaviour change.
- Executor conditions answer "is this _worth_ doing?". The server already
  filtered for legality. Check life points before any pay-LP effect.
- The bot sees only public info: opponent hand/deck/face-downs are `Id == 0`.
- No per-duel mutable state in static fields (server mode runs many bots).
- C# 6 style: no pattern matching, inline `out var`, discards or throw
  expressions (old Mono must build it).
- New deck = `[Deck("Toon", "AI_Toon")]` executor in `Game/AI/Decks/` +
  `Decks/AI_Toon.ydk`. Don't write `BotWrapper/bot.conf` or `Dialogs/`
  entries yourself; tell the user to add them.
- No test project exists. "Done" means it builds and a real duel was observed.
- Don't stage, commit or switch branches unless the user asks.

Reference executors: `Game/AI/Decks/` (73 decks; `ElfnoteExecutor.cs` is the
newest and strongest, 8.6k lines). Upstream also offers a template generator
that turns a `.ydk` into a skeleton executor: https://mercury233.me/windbot/gen.html

## Git

- `origin` = fork `andreszb/windbot` (push here); `upstream` = `IceYGO/windbot`.
- Work on branch `toon-ai`; `master` tracks `upstream/master`.
- Sync: `git fetch upstream && git rebase upstream/master` (on `toon-ai`).
- PR: push `toon-ai` to `origin`, open against `IceYGO/windbot:master`.

## Environment (NixOS)

`direnv` loads `.dev/flake.nix` via `.envrc` (`use flake path:.dev`), or run
`nix develop path:.dev` by hand. Provides mono, msbuild, sqlite and:

- `wb-build`: Release build + symlink `bin/Release/cards.cdb` → MDPro4's
  `~/Games/MDPro4-1.0.2/Data/locales/en-US/cards.cdb`
- `wb-run Deck=<name> Host=127.0.0.1 Port=<port>`: runs from `bin/Release`
  with `Chat=False Debug=True` (`Hand=1|2|3` forces RPS)

Use msbuild, not `dotnet build` (old-style .NET Framework 4.8 csproj).
`LD_LIBRARY_PATH` must contain sqlite or `Mono.Data.Sqlite` fails with
`DllNotFoundException: libsqlite3.so.0`; the flake sets it.

**Never commit** `.dev/`, `.envrc`, `.direnv/`, `CLAUDE.md` or `cards.cdb`.
All are in `.git/info/exclude` / `.gitignore`. Keep it that way so the branch
stays upstream-clean.

## Test loop

MDPro4 (installed at `~/Games/MDPro4-1.0.2`, despite the name it's v1.0.8)
compiles its bots into its IL2CPP binary. A local build **cannot** be dropped
into the game. Instead, connect it as an external client:

1. User hosts a LAN room in MDPro4 and reads off the port.
2. `wb-build && wb-run Deck=Toon Host=127.0.0.1 Port=<port>`
3. User duels it in MDPro4; read the `Debug=True` log to see decisions.

### Verified (2026-11-18, `Deck=Blue-Eyes`, full duel played)

- MDPro4's LAN host accepts an external WindBot with no `Version=` needed.
  Use `Host=127.0.0.1` and the port shown in the room (it can change if the
  room is recreated; re-read it).
- **The room must have no card restrictions (no banlist).** A restricted room
  rejects the bot's deck silently: no error, the log just stops after
  `Decks initialized, 73 found.`. Silence there means "rejected", not "waiting".
- `wb-run` blocks until the duel ends (exit 0 at game over, or kill it). Run it
  in the background with output redirected to a file (scratchpad), and stream
  the file with a log monitor. Don't wrap it in `timeout` for real tests.
- The `Debug=True` log is live and has no filtering needed:
  - `Bot Hand` / `Bot Spell` / `Bot Monster` board dump at **every** Standby
    Phase, the user's turn included.
  - One line per game event: moves (`(0 's X from Hand move to Grave)`),
    activations, targets, phase changes, damage and LP.
  - Player `0` = bot, `1` = user. The user's face-down or hidden cards show as
    `UnKnowCard` until revealed (`Confirm ...`).
  - Bot decisions are only visible on the bot's own turn; read the lines
    between `(0 draw N card)` and the next `(Go to Draw)`.
- Bot actions are implicit: a missing line means the bot declined or had no
  registered executor for it. To tell which, check the hand dump and the
  executor's `AddExecutor` list.

Fallbacks if the LAN loop ever breaks: a standalone YGOSharp server, or
[windbot-arena](https://github.com/mercury233/windbot-arena) for bot-vs-bot
win rates.

## Status

- [x] Clone, build under Mono, card DB loads (`Decks initialized, 73 found.`)
- [x] Fork `andreszb/windbot`, branch `toon-ai`
- [x] Confirm MDPro4 LAN loop with an existing bot (`Deck=Blue-Eyes`); see
      "Verified" under Test loop
- [x] Get the user's Toon 2026 `.ydk` → `Decks/AI_Toon.ydk` (copied from
      `~/Downloads/Toon.ydk`; 40 main / 15 extra / 15 side, all 41 unique ids
      resolve in `cards.cdb`; CRLF line endings, untracked, not staged)
- [x] Skeleton `Game/AI/Decks/ToonExecutor.cs` (builds, `Decks initialized, 74
      found.`; `Level` is `NotFinished` so it stays out of the random pool.
      Change to default before the PR). Has CardId consts for all 41 cards,
      hand traps, field/search spells, generic fallbacks. Not yet duel-tested.
- [~] Main combo line written. Duel-verified 2026-11-18 (bot first, no
      interruption): ToC adds Toon Terror, FDR NS places PTW, PTW search adds
      Comic Cat, FDR extra NS Comic Cat, Comic Cat tributes FDR for Evil Box,
      PTW banish prompt declined (guard on `ActivateDescription`; the prompt
      comes through `OnSelectEffectYn` -> `Activate` executors, NOT
      `OnSelectYesNo`). Also verified: Bifrost -> PTW when no field spell,
      Evil Box from hand Sets Toon Terror, Toon Terror responds on the
      opponent's turn. `Logger.DebugWriteLine` is `#if DEBUG` only, so it
      prints nothing in the Release build `wb-build` makes; use
      `Logger.WriteLine` for temporary diagnostics. Line: Funny Dark Rabbit
      NS -> place Perfect Toon World -> its search adds Comic Cat / Evil Box /
      Toon Terror -> Evil Box from hand Sets Toon Terror -> Comic Cat tributes
      and SSes Evil Box. Source: note.com/uni928928/n/nffec8fab44ef, but
      verify against the Lua scripts (ygopro-scripts `c<id>.lua`): the
      article's "Table of Contents adds Comic Cat" is wrong, ToC only adds
      `0x62` cards; PTW and Bookmark add cards that mention Toon World.
      Pot of Prosperity has an executor (banishes TY-PHON/AA-ZEUS/Exciton
      Knight, keeps Odin/Chaos Angel/Levilazebul for Bifrost): builds, never
      drawn in a test duel yet. Still missing: PTW banish reset (declined via
      the `ActivateDescription` guard), Dark-Eyes, Evil Box GY effect, Droll,
      Mind Scan, Nibiru, Mulcharmy, Dominus (blocked in `GenericActivate`),
      all Extra Deck summons (Master Duel Meta lines are image-only and use
      Cross-Sheep/Firewall, not in this list). The test room draws 5 per turn,
      so bot hands are large.
- [ ] PR to IceYGO/windbot

Update this checklist as work progresses.
