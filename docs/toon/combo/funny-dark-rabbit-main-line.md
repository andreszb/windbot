# Funny Dark Rabbit main line

The one combo the bot plays today. Starter: Funny Dark Rabbit, or any Toon starter that reaches
Perfect Toon World. Implemented in `Game/AI/Decks/ToonExecutor.cs`; status and the other
findings are in [`../toon.md`](../toon.md).

## Sources

| Source | Used for | Limits |
| --- | --- | --- |
| <https://note.com/uni928928/n/nffec8fab44ef> (uni928, 2026-04-05, machine-translated from Japanese) | The overall shape: Funny Dark Rabbit, repeated Perfect Toon World searches, Comic Cat into Evil Box | Built around a different Extra Deck and cards this deck does not run |
| <https://www.masterduelmeta.com/combo/6a7a38c67c09c37bb6cf0ecf> ("Funny Dark Rabbit - Main Combo") | The target end board and its disruptions | Step text is image-only, so the scrape lost every card name |
| <https://www.masterduelmeta.com/combo/6a7a520d7d70947586ebcfad> ("Toon World the Perfect World - Main Combo") | Same, from a Perfect Toon World start | Same |
| `ygopro-scripts/c<id>.lua` for each card | What each effect actually does, which choices it asks for and in what order | Authoritative; the article was wrong in one place (below) |

The Master Duel Meta lines end on Cross-Sheep, Firewall Dragon and Perfectron, and use
Desavewurm. None are in `Decks/AI_Toon.ydk`, so those lines were not copied.

## The line as implemented

Hand needs Funny Dark Rabbit and a Normal Summon. Perfect Toon World in hand or Deck.

1. **Normal Summon Funny Dark Rabbit.** Its on-summon effect grants one extra Normal Summon for
   a monster that mentions Toon World, once per turn.
2. **Funny Dark Rabbit's effect:** place Perfect Toon World from the Deck face-up in the Field
   Zone (the "place" option, not "add to hand").
3. **Perfect Toon World's search** (once per copy per turn, three times a turn in total). Takes the
   first of: Comic Cat, Evil Box, Toon Terror, Funny Dark Rabbit, Toon Bookmark that the bot
   does not already hold.
4. **Extra Normal Summon:** Comic Cat.
5. **Comic Cat's effect** (Quick Effect, Main Phase, once per turn): tribute a monster, then
   Special Summon a monster that mentions Toon World from hand or Deck. The bot tributes, in
   order, the opponent's best monster (only possible while Toon World is up), Funny Dark Rabbit
   (its effect is already used), or Comic Cat itself. It summons Evil Box.
6. **Evil Box**, if it was in hand when Toon World was up: Special Summon itself and Set Toon
   Terror, or skip the search if Toon Terror is already held.

Optional loop: with the search spent, a second Funny Dark Rabbit effect (a fresh copy, or after a
reset) places a second Perfect Toon World over the first, which has an unused search again. The
bot only does this when its target list is not empty.

### End board

Perfect Toon World, Funny Dark Rabbit, Comic Cat and Evil Box on the field; Toon Terror Set.
Observed in a duel on 2026-11-18 with Infinite Impermanence and Typhoon also Set from hand.

Disruption on the opponent's turn: Toon Terror (negate and destroy a Spell/Trap activation or a
monster effect, needs Toon World and a Toon monster), Comic Cat's Quick Effect (tribute an
opponent monster and summon a Toon), Evil Box's Quick Effect (put a graveyard card on the bottom
of the Deck, **not implemented**), plus whatever hand traps and Set cards remain.

## Differences from the sources

- **Table of Contents does not add Comic Cat.** The article says it does. In the script it only
  adds `0x62` ("Toon" set) cards: Perfect Toon World, Toon Bookmark, Toon Terror, itself. Comic
  Cat, Evil Box and Funny Dark Rabbit come from Perfect Toon World's search or Toon Bookmark,
  which add any card that mentions Toon World. The bot uses Table of Contents to fetch Toon
  Terror (then Toon Bookmark, then a field spell).
- **No Perfect Toon World banish reset.** The article repeatedly banishes a Toon monster with
  Perfect Toon World to reset its once-per-turn effect, getting several Funny Dark Rabbit
  placements. The bot declines that prompt (see Gotchas in `toon.md`), so it gets one extra
  search from a second placement, not the full loop.
- **No Toon Dark Magician, Blue-Eyes Toon Dragon or fusion.** They are not in this list.
- **Evil Box's graveyard effect is not used,** so the article's "return Perfect Toon World to the
  Deck from the graveyard" recovery step is missing.
- **Extra Deck is not played.** The Master Duel Meta lines spend most of their steps there.

## Where each step lives

| Step | Method in `ToonExecutor.cs` | Seen in a duel |
| --- | --- | --- |
| Normal Summon Funny Dark Rabbit / Comic Cat | `AddExecutor(ExecutorType.Summon, ...)` | yes |
| Place Perfect Toon World | `FunnyDarkRabbitActivate` | yes, including the second placement |
| Perfect Toon World search | `PerfectToonWorldActivate`, `PerfectToonWorldTargets` | yes |
| Comic Cat tribute and summon | `ComicCatActivate` | yes (tributing Funny Dark Rabbit) |
| Evil Box from hand, Set Toon Terror | `EvilBoxActivate` | yes |
| Toon Table of Contents / Toon Bookmark | `ToonTableOfContentsActivate`, `ToonBookmarkActivate` | Table of Contents yes, Bookmark no |
| Toon Terror response | `ToonTerrorActivate` | yes |
| Field spell with no Perfect Toon World | `TerraformingActivate`, `PotOfProsperityActivate`, `RainbowBridgeBifrostActivate` | Bifrost yes, Terraforming and Pot not yet |

## Open points

- Add Perfect Toon World's banish reset to get the full multi-search loop.
- Use Evil Box's graveyard effect, and write Dark-Eyes Illusionist's two effects.
- Pick a line for the Extra Deck once its plays are written; the Master Duel Meta pages need to
  be read as images (or retyped) to recover their steps.
