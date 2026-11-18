# Toon bot: work log

Status as of 2026-11-18. WindBot deck AI for the 2026 Toon list, run against MDPro4 as an
external client. Builds with `msbuild`; the main line has been observed in real duels, several
cards have not.

## Files

| Path | What |
| --- | --- |
| `Game/AI/Decks/ToonExecutor.cs` | The executor. `[Deck("Toon", "AI_Toon", "NotFinished")]` |
| `Decks/AI_Toon.ydk` | 40 main / 15 extra / 15 side, copied from `~/Downloads/Toon.ydk`. CRLF, all 41 unique ids resolve in `cards.cdb` |

`NotFinished` keeps it out of the random deck pool (`Deck=Toon` still selects it). Change it to
the default level before opening the PR. `BotWrapper/bot.conf` and `Dialogs/` entries are not
written; the user adds those.

## Deck

Counts taken from `Decks/AI_Toon.ydk` resolved through `cards.cdb`.

Main (40): x3 Funny Dark Rabbit, Comic Cat, Perfect Toon World, Ghost Ogre & Snow Rabbit,
Ash Blossom & Joyous Spring, Effect Veiler, Infinite Impermanence, Dominus Impulse, Rainbow
Bridge Bifrost; x2 Dominus Spark; x1 Evil Box, Dark-Eyes Illusionist Faceless Mage, Toon
Bookmark, Toon Table of Contents, Toon Terror, Terraforming, Pot of Prosperity, Mind Scan,
Piri Reis Map, Typhoon, Droll & Lock Bird.

Extra (15, all x1): Odin, Chaos Angel, Fabled Levilazebul, Evilswarm Ophion, Evilswarm Exciton
Knight, Dark Rebellion Xyz Dragon, Arc Rebellion Xyz Dragon, Tornado Dragon, Raider's Knight,
Exosister Karmael, Divine Arsenal AA-ZEUS, Super Starslayer TY-PHON, Sky Striker Ace - Azalea,
I:P Masquerena, S:P Little Knight.

Side (15): x3 Mulcharmy Fuwalos; x2 Mind Crush, Nibiru, Pot of Sloth, Spatial Trunade; x1
Mulcharmy Purulia, Piri Reis Map, Solemn Judgment, Typhoon.

## Main line (implemented)

Source: <https://note.com/uni928928/n/nffec8fab44ef>, corrected against the card Lua scripts
(`ygopro-scripts/c<id>.lua`). The article says Toon Table of Contents adds Comic Cat; it does
not. ToC only adds cards in the `Toon` set (`0x62`): Perfect Toon World, Toon Bookmark, Toon
Terror, itself. Perfect Toon World's search and Toon Bookmark add cards that *mention* Toon
World (Comic Cat, Evil Box, Funny Dark Rabbit). Full write-up with sources, differences from
them and a step-to-code table: [combo/funny-dark-rabbit-main-line.md](combo/funny-dark-rabbit-main-line.md).

1. Funny Dark Rabbit Normal Summoned, places Perfect Toon World from the Deck.
2. Perfect Toon World searches, priority: Comic Cat, Evil Box, Toon Terror, Funny Dark Rabbit,
   Toon Bookmark. Only cards not already in hand or on the field.
3. Funny Dark Rabbit's extra Normal Summon is used on Comic Cat.
4. Comic Cat tributes an opponent monster if Toon World is up, else Funny Dark Rabbit, else
   itself, and Special Summons Evil Box from hand or Deck.
5. Evil Box from hand (needs Toon World) Special Summons itself and Sets Toon Terror, or declines
   the search if Toon Terror is already in hand or Set.
6. Toon Terror responds to an opponent chain while Toon World and a Toon monster are on the field.

A second Perfect Toon World is placed over the first only when it buys another search.
Rainbow Bridge Bifrost fetches a field spell as a last resort (costs three Level 10+ Extra Deck
monsters). Pot of Prosperity banishes TY-PHON, AA-ZEUS and Exciton Knight (keeps Odin, Chaos
Angel and Levilazebul for Bifrost) and takes Perfect Toon World, else the first starter it lacks.

## Registration order

`AddExecutor` order is priority. Current order:

1. Ash Blossom, Effect Veiler, Ghost Ogre, Droll & Lock Bird, Infinite Impermanence, Solemn
   Judgment, Typhoon, Toon Terror
2. Terraforming, Perfect Toon World, Funny Dark Rabbit, Evil Box, Comic Cat, Toon Bookmark,
   Toon Table of Contents, Pot of Prosperity, Rainbow Bridge Bifrost
3. Summon Funny Dark Rabbit, Summon Comic Cat
4. `GenericActivate`, `DefaultMonsterSummon`, `DefaultSpellSet`

`GenericActivate` wraps `DefaultDontChainMyself` but refuses Dominus Impulse, Dominus Spark and
Droll & Lock Bird. Without that wrapper any unregistered card the server offers gets activated.

## Verified in duels

Bot first and second, MDPro4 room with no banlist. Seen working: Toon Table of Contents adds
Toon Terror; Funny Dark Rabbit places Perfect Toon World; the search adds Comic Cat then Evil
Box; the extra Normal Summon; Comic Cat tributing Funny Dark Rabbit for Evil Box; Evil Box from
hand Setting Toon Terror; Toon Terror activating on the opponent's turn; Bifrost finding Perfect
Toon World when no field spell; Ash Blossom, Ghost Ogre and Infinite Impermanence responses.

## Not tested yet

- Pot of Prosperity (executor written, never drawn in a test duel)
- Droll & Lock Bird (executor written after the last duel)
- Mind Scan: not registered; the generic fallback activates it

## Not written

Dark-Eyes Illusionist, Evil Box's graveyard effect, Perfect Toon World's banish reset (currently
declined), Mind Scan, Nibiru, Mulcharmy, Dominus Spark/Impulse (blocked), every Extra Deck
summon. Master Duel Meta's lines end on Cross-Sheep, Firewall and Perfectron, which are not in
this Extra Deck, and their pages are image-only so the step text did not survive scraping.

## Gotchas found

- Perfect Toon World's "banish a Toon monster" prompt reaches the bot through
  `OnSelectEffectYn`, which asks every `Activate` executor for that card. It does **not** call
  `OnSelectYesNo`. Left unhandled it banished Comic Cat mid-resolution and the Evil Box summon
  fizzled. Fixed with a guard on `ActivateDescription == Util.GetStringId(PerfectToonWorld, 3)`.
- `Logger.DebugWriteLine` is `#if DEBUG` only; `wb-build` makes a Release build, so it prints
  nothing. Use `Logger.WriteLine` for temporary diagnostics.
- Ash Blossom in this list is id `14558128`; `DefaultExecutor` knows `14558127`. The Ash helper
  does not check the id, so registration still works.
- `Bot.HasInDeck` counts from the `.ydk` and is safe; `Bot.Deck.Any(...)` is not (see `AGENTS.md`).
- A restricted (banlist) room silently rejects the bot: no error, the log stops after
  `Decks initialized`.

## Run a test duel

1. In MDPro4, host a LAN room with no card restrictions and read off the port (it changes when
   the room is recreated).
2. `wb-build`, then run `wb-run Deck=Toon Host=127.0.0.1 Port=<port>` in the background with
   output to a file. It blocks until the duel ends.
3. Read the `Debug=True` log. Player `0` is the bot, `1` is you. The `Bot Hand` / `Bot Spell` /
   `Bot Monster` dump prints at every Standby Phase, yours included. A missing line means the
   bot declined or has no registered executor for it.

The test room draws 5 cards per turn, so bot hands are large.

## Next

- Duel-test Pot of Prosperity and Droll & Lock Bird
- Register Mind Scan, then Nibiru and the Mulcharmys
- Write the Perfect Toon World banish reset (resets a once-per-turn effect on a Toon monster)
- Dark-Eyes and Evil Box's graveyard effect
- Extra Deck plays (Rank 4 Xyz line, Odin) once their conditions are written
- PR to `IceYGO/windbot:master`: set the deck level, add `bot.conf`/`Dialogs` entries (user),
  keep `.dev/`, `.envrc`, `CLAUDE.md`, `toon.md` and `cards.cdb` out of the branch
