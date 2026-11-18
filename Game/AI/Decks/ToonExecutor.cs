using System.Collections.Generic;
using System.Linq;
using WindBot;
using WindBot.Game;
using WindBot.Game.AI;
using YGOSharp.OCGWrapper.Enums;

namespace WindBot.Game.AI.Decks
{
    // Work in progress. Line implemented so far:
    //   Normal Summon Funny Dark Rabbit -> place Perfect Toon World -> Perfect Toon World searches
    //   Comic Cat / Evil Box -> Evil Box from hand sets Toon Terror -> Comic Cat tributes and
    //   Special Summons Evil Box. Not written yet: the Perfect Toon World banish reset, Dark-Eyes,
    //   Evil Box's graveyard effect, Droll & Lock Bird, Nibiru, Mulcharmy, Dominus, Pot of Prosperity
    //   and every Extra Deck summon.
    [Deck("Toon", "AI_Toon", "NotFinished")]
    public class ToonExecutor : DefaultExecutor
    {
        public class CardId
        {
            // Main deck
            public const int FunnyDarkRabbit = 45536531;
            public const int ComicCat = 72921536;
            public const int DarkEyesIllusionistFacelessMage = 34314989;
            public const int EvilBox = 8915275;
            public const int AshBlossom = 14558128;
            public const int GhostOgreAndSnowRabbit = 59438930;
            public const int DrollAndLockBird = 94145021;
            public const int EffectVeiler = 97268402;
            public const int PerfectToonWorld = 7293697;
            public const int ToonWorld = 15259703;
            public const int ToonTableOfContents = 89997728;
            public const int ToonBookmark = 91500017;
            public const int RainbowBridgeBifrost = 32236916;
            public const int Terraforming = 73628505;
            public const int PotOfProsperity = 84211599;
            public const int MindScan = 34298391;
            public const int DominusImpulse = 40366667;
            public const int DominusSpark = 6325660;
            public const int InfiniteImpermanence = 10045474;
            public const int ToonTerror = 53094821;

            // Side deck
            public const int MulcharmyFuwalos = 42141493;
            public const int MulcharmyPurulia = 84192580;
            public const int NibiruThePrimalBeing = 27204311;
            public const int SolemnJudgment = 41420027;
            public const int Typhoon = 14883228;
            public const int PiriReisMap = 33907039;
            public const int SpatialTrunade = 2729965;
            public const int MindCrush = 15800838;
            public const int PotOfSloth = 98476659;

            // Extra deck
            public const int OdinFatherOfTheAesir = 93483212;
            public const int ChaosAngel = 22850702;
            public const int FabledLevilazebul = 21281085;
            public const int EvilswarmOphion = 91279700;
            public const int DarkRebellionXyzDragon = 90091224;
            public const int ArcRebellionXyzDragon = 64276752;
            public const int TornadoDragon = 6983839;
            public const int RaidersKnight = 28781003;
            public const int ExosisterKarmael = 77675029;
            public const int EvilswarmExcitonKnight = 46772449;
            public const int DivineArsenalAAZeus = 90448279;
            public const int SuperStarslayerTYPhon = 93039339;
            public const int SkyStrikerAceAzalea = 98462037;
            public const int IPMasquerena = 65741786;
            public const int SPLittleKnight = 29301450;
        }

        public ToonExecutor(GameAI ai, Duel duel)
            : base(ai, duel)
        {
            // Responses first: hand traps and interaction.
            AddExecutor(ExecutorType.Activate, CardId.AshBlossom, DefaultAshBlossomAndJoyousSpring);
            AddExecutor(ExecutorType.Activate, CardId.EffectVeiler, DefaultEffectVeiler);
            AddExecutor(ExecutorType.Activate, CardId.GhostOgreAndSnowRabbit, DefaultGhostOgreAndSnowRabbit);
            AddExecutor(ExecutorType.Activate, CardId.DrollAndLockBird, DrollAndLockBirdActivate);
            AddExecutor(ExecutorType.Activate, CardId.InfiniteImpermanence, DefaultInfiniteImpermanence);
            AddExecutor(ExecutorType.Activate, CardId.SolemnJudgment, DefaultSolemnJudgment);
            AddExecutor(ExecutorType.Activate, CardId.Typhoon, DefaultMysticalSpaceTyphoon);
            AddExecutor(ExecutorType.Activate, CardId.ToonTerror, ToonTerrorActivate);

            // Main phase line. Perfect Toon World comes before Funny Dark Rabbit so an unused search
            // is taken before Funny Dark Rabbit replaces the field spell.
            AddExecutor(ExecutorType.Activate, CardId.Terraforming, TerraformingActivate);
            AddExecutor(ExecutorType.Activate, CardId.PerfectToonWorld, PerfectToonWorldActivate);
            AddExecutor(ExecutorType.Activate, CardId.FunnyDarkRabbit, FunnyDarkRabbitActivate);
            AddExecutor(ExecutorType.Activate, CardId.EvilBox, EvilBoxActivate);
            AddExecutor(ExecutorType.Activate, CardId.ComicCat, ComicCatActivate);
            AddExecutor(ExecutorType.Activate, CardId.ToonBookmark, ToonBookmarkActivate);
            AddExecutor(ExecutorType.Activate, CardId.ToonTableOfContents, ToonTableOfContentsActivate);
            AddExecutor(ExecutorType.Activate, CardId.PotOfProsperity, PotOfProsperityActivate);
            AddExecutor(ExecutorType.Activate, CardId.RainbowBridgeBifrost, RainbowBridgeBifrostActivate);
            AddExecutor(ExecutorType.Summon, CardId.FunnyDarkRabbit);
            AddExecutor(ExecutorType.Summon, CardId.ComicCat);

            // Generic fallbacks.
            AddExecutor(ExecutorType.Activate, GenericActivate);
            AddExecutor(ExecutorType.Summon, DefaultMonsterSummon);
            AddExecutor(ExecutorType.SpellSet, DefaultSpellSet);
        }

        private bool DrollAndLockBirdActivate()
        {
            // The server only offers this when the opponent has just added a card from the Deck to
            // their hand outside the Draw Phase. It also stops the bot's own searches and draws for the
            // turn, so keep it for the opponent's turn and skip it once a lock is already active.
            return Duel.Player == 1 && DefaultCheckWhetherEnemyCanDraw();
        }

        private bool PotOfProsperityActivate()
        {
            if (Bot.ExtraDeck.Count < 3 || Bot.Deck.Count < 6)
                return false;

            // The cost is Extra Deck cards of our choice. Banish the ones this deck has no plan for and
            // keep Odin, Chaos Angel and Fabled Levilazebul, which Rainbow Bridge Bifrost needs.
            AI.SelectOption(0); // banish 3, not 6
            AI.SelectCard(
                CardId.SuperStarslayerTYPhon,
                CardId.DivineArsenalAAZeus,
                CardId.EvilswarmExcitonKnight,
                CardId.TornadoDragon,
                CardId.ExosisterKarmael,
                CardId.RaidersKnight,
                CardId.SPLittleKnight,
                CardId.IPMasquerena,
                CardId.SkyStrikerAceAzalea);

            // Then pick the best card out of the excavated ones.
            List<int> pick = new List<int>();
            if (NeedFieldSpell())
                pick.Add(CardId.PerfectToonWorld);
            foreach (int id in new[]
            {
                CardId.FunnyDarkRabbit,
                CardId.ComicCat,
                CardId.EvilBox,
                CardId.ToonBookmark,
                CardId.ToonTableOfContents,
                CardId.ToonTerror,
                CardId.Terraforming
            })
            {
                if (Missing(id))
                    pick.Add(id);
            }
            AI.SelectNextCard(pick);
            return true;
        }

        private bool RainbowBridgeBifrostActivate()
        {
            // Costs three Level 10+ Extra Deck monsters, so it is the last way to find a field spell.
            if (!NeedFieldSpell())
                return false;
            AI.SelectCard(CardId.PerfectToonWorld);
            return true;
        }

        private bool GenericActivate()
        {
            // The Dominus cards lock the bot out of whole attributes when activated from hand,
            // so they stay off until they have real conditions.
            if (Card.IsCode(CardId.DominusImpulse, CardId.DominusSpark, CardId.DrollAndLockBird))
                return false;
            return DefaultDontChainMyself();
        }

        private bool ToonWorldOnField()
        {
            ClientCard field = Bot.GetFieldSpellCard();
            return field != null && field.IsFaceup() && field.IsCode(CardId.PerfectToonWorld, CardId.ToonWorld);
        }

        private bool HasToonMonster()
        {
            return Bot.GetMonsters().Any(card => card.IsFaceup() && card.IsCode(
                CardId.FunnyDarkRabbit, CardId.ComicCat, CardId.EvilBox, CardId.DarkEyesIllusionistFacelessMage));
        }

        // A card we still hold in the deck and have neither in hand nor on the field.
        private bool Missing(int id)
        {
            return Bot.HasInDeck(id) && !Bot.HasInHand(id) && !Bot.HasInMonstersZone(id) && !Bot.HasInSpellZone(id);
        }

        private bool NeedFieldSpell()
        {
            return Bot.GetFieldSpellCard() == null && !Bot.HasInHand(CardId.PerfectToonWorld)
                && Bot.HasInDeck(CardId.PerfectToonWorld);
        }

        private List<int> PerfectToonWorldTargets()
        {
            int[] priority =
            {
                CardId.ComicCat,
                CardId.EvilBox,
                CardId.ToonTerror,
                CardId.FunnyDarkRabbit,
                CardId.ToonBookmark
            };
            return priority.Where(Missing).ToList();
        }

        private bool TerraformingActivate()
        {
            // Terraforming adds a Field Spell; only worth it if we have none in hand, on the field or to place.
            return Bot.GetFieldSpellCard() == null && !Bot.HasInHand(CardId.PerfectToonWorld);
        }

        private bool PerfectToonWorldActivate()
        {
            if (Card.Location == CardLocation.Hand)
                return Bot.GetFieldSpellCard() == null;

            // The prompt to banish a Toon monster (description 3) is routed through this executor
            // too. Decline it until that part of the line is written.
            if (ActivateDescription == Util.GetStringId(CardId.PerfectToonWorld, 3))
                return false;

            List<int> targets = PerfectToonWorldTargets();
            if (targets.Count == 0)
                return false;
            AI.SelectCard(targets);
            return true;
        }

        private bool FunnyDarkRabbitActivate()
        {
            if (Card.Location != CardLocation.MonsterZone || !Bot.HasInDeck(CardId.PerfectToonWorld))
                return false;
            // A second Perfect Toon World replaces the first, so only do it when it buys another search.
            if (ToonWorldOnField() && PerfectToonWorldTargets().Count == 0)
                return false;
            AI.SelectCard(CardId.PerfectToonWorld);
            AI.SelectOption(1); // place on the field instead of adding to the hand
            return true;
        }

        private bool EvilBoxActivate()
        {
            if (Card.Location != CardLocation.Hand || !ToonWorldOnField())
                return false;
            if (Missing(CardId.ToonTerror))
            {
                AI.SelectYesNo(true);
                AI.SelectCard(CardId.ToonTerror);
                AI.SelectOption(1); // Set it instead of adding it to the hand
            }
            else
            {
                AI.SelectYesNo(false);
            }
            return true;
        }

        private bool ComicCatActivate()
        {
            if (Card.Location != CardLocation.MonsterZone)
                return false;
            if (Bot.HasInMonstersZone(CardId.EvilBox) || !(Bot.HasInHand(CardId.EvilBox) || Bot.HasInDeck(CardId.EvilBox)))
                return false;

            ClientCard tribute = null;
            if (ToonWorldOnField())
                tribute = Util.GetBestEnemyMonster();
            if (tribute == null)
                tribute = Bot.GetMonsters().FirstOrDefault(card => card.IsCode(CardId.FunnyDarkRabbit));
            if (tribute == null)
                tribute = Card;
            AI.SelectCard(tribute);
            AI.SelectNextCard(CardId.EvilBox);
            return true;
        }

        private bool ToonBookmarkActivate()
        {
            List<int> targets = new List<int>();
            if (NeedFieldSpell())
                targets.Add(CardId.PerfectToonWorld);
            foreach (int id in new[] { CardId.ComicCat, CardId.EvilBox, CardId.FunnyDarkRabbit })
            {
                if (Missing(id))
                    targets.Add(id);
            }
            if (targets.Count == 0)
                return false;
            AI.SelectCard(targets);
            return true;
        }

        private bool ToonTableOfContentsActivate()
        {
            List<int> targets = new List<int>();
            if (Missing(CardId.ToonTerror))
                targets.Add(CardId.ToonTerror);
            if (Missing(CardId.ToonBookmark))
                targets.Add(CardId.ToonBookmark);
            if (NeedFieldSpell())
                targets.Add(CardId.PerfectToonWorld);
            if (targets.Count == 0)
                return false;
            AI.SelectCard(targets);
            return true;
        }

        private bool ToonTerrorActivate()
        {
            return Duel.LastChainPlayer == 1 && ToonWorldOnField() && HasToonMonster();
        }
    }
}
