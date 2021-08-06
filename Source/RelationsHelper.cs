// Karel Kroeze
// RelationsHelper.cs
// 2016-12-26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations {
    public static class RelationsHelper {
        #region Fields

        public static float OPINION_THRESHOLD_NEG = -50f;

        public static float OPINION_THRESHOLD_POS = 50f;

        public static Dictionary<Pawn, List<string>> ThoughtsAbout = new();

        private static HediffDef _majorHediffDef;

        private static Dictionary<Pair<Pawn, Pawn>, float> _opinions;

        private static bool _psychologyLoaded = true;

        #endregion Fields

        #region Properties

        public static IEnumerable<Pawn> Colonists => Find.ColonistBar.Entries.Select(e => e.pawn).OfType<Pawn>();

        #endregion Properties

        #region Methods

        public static Color ColorInTab(this PawnRelationDef relation) {
            return Color.HSVToRGB(relation.index / (float) DefDatabase<PawnRelationDef>.DefCount, 1f, 1f);
        }

        public static void DrawSocialStatusEffectsSummary(Rect canvas, Pawn pawn) {
            Rect mainDescRect = canvas.TakeRow();
            Widgets.Label(mainDescRect, pawn.MainDesc(true));

            if (ModsConfig.IdeologyActive && pawn.ideo is Pawn_IdeoTracker tracker && pawn.ideo.Ideo is Ideo ideo) {
                Rect ideologyRect = canvas.TakeRow();
                Widgets.Label(ideologyRect, "Fluffy_Relations.BelievesInXWithCertaintyY".Translate(ideo.name.Colorize(ideo.Color), tracker.Certainty.ToStringPercent()).Resolve().CapitalizeFirst());
            }

            Widgets.Label(canvas, "Fluffy_Relations.SocialThoughsOfOthers".Translate() + ": <i>" +
                           string.Join(", ", ThoughtsAbout[pawn].ToArray()) + "</i>");
            TooltipHandler.TipRegion(mainDescRect, pawn.ageTracker.AgeTooltipString);
        }

        public static List<Pawn> GetDirectlyRelatedPawns(this Pawn pawn) {
            return
                pawn.relations.DirectRelations.Where(rel => rel.def.VisibleInTab())
                    .Select(rel => rel.otherPawn)
                    .ToList();
        }

        public static string GetFactionLabel(this Faction faction) {
            if (faction == null) {
                throw new ArgumentNullException(nameof(faction));
            }

            return faction == Faction.OfPlayer
                       ? faction.HasName ? faction.Name : "Fluffy.Relations.Colony".Translate().Resolve()
                       : faction.GetCallLabel();
        }

        public static Pawn GetMayor() {
            // cop out if we checked the mayor def and it wasn't there
            if (!_psychologyLoaded) {
                return null;
            }

            // if major def is null and we haven't checked yet, check for the def (and thus for psychology being loaded)
            if (_majorHediffDef == null && _psychologyLoaded) {
                _majorHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Mayor");
                _psychologyLoaded = _majorHediffDef != null;

                // check done, call again
                return GetMayor();
            }

            // if we got here, that means psychology is loaded. return the pawn with the mayor def in the highest pop map, if any.
            IEnumerable<Pawn> mayors = Colonists.Where(p => p.health.hediffSet.HasHediff(_majorHediffDef));
            if (!mayors.Any()) {
                return null;
            }

            return mayors.OrderByDescending(p => p.MapHeld?.mapPawns.ColonistCount).First();
        }

        // RimWorld.PawnRelationUtility
        public static PawnRelationDef GetMostImportantVisibleRelation(this Pawn me, Pawn other) {
            return me.GetRelations(other)
                     .Where(VisibleInTab)
                     .OrderByDescending(d => d.importance)
                     .FirstOrDefault();
        }

        public static List<Pawn> GetRelatedPawns(this Pawn pawn, List<Pawn> options, bool selected) {
            // direct relations of ALL pawns.
            List<Pawn> relatedPawns = pawn.GetDirectlyRelatedPawns();

            // opinions above threshold
            foreach (Pawn other in options) {
                float maxOpinion = Mathf.Max(pawn.OpinionOfCached(other), other.OpinionOfCached(pawn));
                float minOpinion = Mathf.Min(pawn.OpinionOfCached(other), other.OpinionOfCached(pawn));
                if ((selected && (maxOpinion > 5f || minOpinion < -5f)) || maxOpinion > OPINION_THRESHOLD_POS ||
                     minOpinion < OPINION_THRESHOLD_NEG) {
                    relatedPawns.Add(other);
                }
            }

            // return list without duplicates
            return relatedPawns.Distinct().ToList();
        }

        public static Color GetRelationColor(PawnRelationDef def, float opinion) {
            if (def != null && def.VisibleInTab()) {
                return def.ColorInTab();
            }

            return GetRelationColor(opinion);
        }

        public static Color GetRelationColor(float opinion) {
            if (opinion > 0f) {
                return Color.Lerp(Resources.ColorNeutral, Resources.ColorFriend, opinion / 100f);
            }

            if (opinion < 0f) {
                return Color.Lerp(Resources.ColorNeutral, Resources.ColorEnemy, Mathf.Abs(opinion) / 100f);
            }

            return Resources.ColorNeutral;
        }

        public static string GetTooltip(this Pawn pawn, Pawn other) {
            StringBuilder tip = new();
            tip.Append(pawn.NameFullColored.Resolve());
            if (other != null && other != pawn) {
                tip.Append("\n\n");
                tip.Append("Fluffy_Relations.Possesive".Translate(pawn.NameShortColored).Resolve());
                tip.Append(pawn.relations.OpinionExplanation(other).LowercaseFirst());
                tip.Append("\n\n");
                tip.Append("Fluffy_Relations.Possesive".Translate(other.NameShortColored).Resolve());
                tip.Append(other.relations.OpinionExplanation(pawn).LowercaseFirst());
            }
            tip.Append("\n\n");
            tip.Append("Fluffy_Relations.NodeInteractionTip".Translate().Resolve());
            return tip.ToString();
        }

        public static string GetTooltip(this Faction faction, Faction other) {
            StringBuilder tip = new();
            tip.Append(faction.Name.Colorize(faction.Color));
            if (other != null && faction != other) {
                tip.Append("\n\n");
                tip.Append(GetFactionRelationString(faction, other));
            }
            tip.Append("\n\n");
            tip.Append("Fluffy_Relations.NodeInteractionTip".Translate().Resolve());
            return tip.ToString();
        }

        public static string GetFactionRelationString(this Faction faction, Faction other) {
            if (other == null || other == faction) {
                return "";
            }
            StringBuilder tip = new();
            if (other == Faction.OfPlayer) {
                tip.Append("Fluffy_Relations.Our".Translate().CapitalizeFirst().Resolve());
            } else {
                tip.Append("Fluffy_Relations.Possesive".Translate(other.Name.Colorize(other.Color)).Resolve());
            }
            int goodwill = Mathf.RoundToInt(other.GoodwillWith(faction));
            tip.Append("Fluffy_Relations.OpinionOf".Translate(faction.Name.Colorize(faction.Color), goodwill.ToStringWithSign().Colorize(GetRelationColor(goodwill))).Resolve());
            return tip.ToString();
        }

        public static bool IsSocialThought(Thought thought) {
            return thought is ISocialThought;
        }

        public static Pawn Leader(this Faction faction) {
            // player faction, see if we have a major (Psychology), if not - oldest is leader.
            if (faction == Faction.OfPlayer) {
                Pawn leader = GetMayor();
                if (leader == null) {
                    leader = GameComponent_Leader.Leader;
                }

                if (leader == null && Colonists.Any()) {
                    leader = Colonists.MaxBy(p => p.ageTracker.AgeBiologicalTicks);
                }

                return leader;
            }

            if (faction.leader == null) {
                _ = faction.TryGenerateNewLeader();
            }

            return faction.leader;
        }

        public static float OpinionOfCached(this Pawn pawn, Pawn other, bool abs = false) {
            Pair<Pawn, Pawn> pair = new(pawn, other);
            if (!_opinions.ContainsKey(pair)) {
                float opinion = pawn.relations.OpinionOf(other);
                _opinions.Add(pair, opinion);
                return opinion;
            }

            if (abs) {
                return Mathf.Abs(_opinions[pair]);
            }

            return _opinions[pair];
        }

        public static void ResetOpinionCache() {
            _opinions = new Dictionary<Pair<Pawn, Pawn>, float>();
        }

        public static bool VisibleInTab(this PawnRelationDef relation) {
            if (relation == null) {
                return false;
            }

            return relation.opinionOffset > OPINION_THRESHOLD_POS / 2f ||
                   relation.opinionOffset < OPINION_THRESHOLD_NEG / 2f;
        }

        internal static void CreateThoughtList(List<Pawn> pawns) {
            // for each pawn...
            ThoughtsAbout = new Dictionary<Pawn, List<string>>();

            foreach (Pawn pawn in pawns) {
                // add list for this pawn
                ThoughtsAbout.Add(pawn, new List<string>());

                // get thoughts targeted at the pawn by all other pawns...
                foreach (Pawn other in pawns.Where(p => p != pawn)) {
                    ThoughtHandler thoughts = other.needs?.mood?.thoughts;
                    if (thoughts == null) {
                        continue;
                    }

                    // get distinct social thoughts
                    List<ISocialThought> DistinctSocialThoughtGroups = new();
                    thoughts.GetDistinctSocialThoughtGroups(pawn, DistinctSocialThoughtGroups);
                    foreach (ISocialThought t in DistinctSocialThoughtGroups) {
                        if (t is Thought thought && t.OpinionOffset() != 0) {
                            ThoughtsAbout[pawn].Add(thought.LabelCapSocial.Colorize(GetRelationColor(t.OpinionOffset())));
                        }
                    }
                }

                // remove duplicates
                if (!ThoughtsAbout[pawn].NullOrEmpty()) {
                    ThoughtsAbout[pawn] = ThoughtsAbout[pawn].Distinct().ToList();
                }
            }
        }

        #endregion Methods
    }
}
