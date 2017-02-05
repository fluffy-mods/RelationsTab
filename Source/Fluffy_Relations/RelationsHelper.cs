// Karel Kroeze
// RelationsHelper.cs
// 2016-12-26

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Fluffy_Relations
{
    public static class RelationsHelper
    {
        #region Fields

        public static float OPINION_THRESHOLD_NEG = -50f;
        public static float OPINION_THRESHOLD_POS = 50f;
        public static DefMap<PawnRelationDef, Color> RELATIONS_COLOR;
        public static DefMap<PawnRelationDef, bool> RELATIONS_VISIBLE;
        public static Dictionary<Pawn, List<string>> ThoughtsAbout = new Dictionary<Pawn, List<string>>();
        private static Dictionary<Pair<Pawn, Pawn>, float> _opinions;

        #endregion Fields

        #region Constructors

        static RelationsHelper()
        {
            RELATIONS_VISIBLE = new DefMap<PawnRelationDef, bool>();
            RELATIONS_COLOR = new DefMap<PawnRelationDef, Color>();

            // give visible relations a sensible default
            List<PawnRelationDef> relations = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            for ( var i = 0; i < relations.Count; i++ )
            {
                PawnRelationDef relation = relations[i];
                RELATIONS_VISIBLE[relation] = relation.opinionOffset > OPINION_THRESHOLD_POS / 2f ||
                                              relation.opinionOffset < OPINION_THRESHOLD_NEG / 2f;
                RELATIONS_COLOR[relation] = Color.HSVToRGB( i / (float)relations.Count, 1f, 1f );
            }
        }

        #endregion Constructors

        #region Properties

        public static IEnumerable<Pawn> Colonists
        {
            get { return Find.ColonistBar.Entries.Select( e => e.pawn ).OfType<Pawn>(); }
        }

        #endregion Properties

        #region Methods

        public static void DrawSocialStatusEffectsSummary( Rect canvas, Pawn pawn )
        {
            GUI.BeginGroup( canvas );

            var curY = 0f;
            var mainDescRect = new Rect( 0f, curY, canvas.width, Settings.RowHeight );
            curY += Settings.RowHeight + Settings.Margin;
            var summaryRect = new Rect( 0f, curY, canvas.width, canvas.height - curY );

            Widgets.Label( mainDescRect, pawn.MainDesc( true ) );
            Widgets.Label( summaryRect,
                           "Fluffy_Relations.SocialThoughsOfOthers".Translate() + ": <i>" +
                           String.Join( ", ", ThoughtsAbout[pawn].ToArray() ) + "</i>" );
            TooltipHandler.TipRegion( mainDescRect, pawn.ageTracker.AgeTooltipString );

            GUI.EndGroup();
        }

        public static List<Pawn> GetDirectlyRelatedPawns( this Pawn pawn )
        {
            return
                pawn.relations.DirectRelations.Where( rel => RELATIONS_VISIBLE[rel.def] )
                    .Select( rel => rel.otherPawn )
                    .ToList();
        }

        public static string GetFactionLabel( this Faction faction )
        {
            if ( faction == null )
                throw new ArgumentNullException( nameof( faction ) );

            return faction == Faction.OfPlayer
                       ? faction.HasName ? faction.Name : "Fluffy.Relations.Colony".Translate()
                       : faction.GetCallLabel();
        }

        // RimWorld.PawnRelationUtility
        public static PawnRelationDef GetMostImportantVisibleRelation( this Pawn me, Pawn other )
        {
            PawnRelationDef def = null;
            foreach ( PawnRelationDef current in me.GetRelations( other ) )
            {
                if ( RELATIONS_VISIBLE[current] && ( def == null || current.importance > def.importance ) )
                {
                    def = current;
                }
            }

            return def;
        }

        public static List<Pawn> GetRelatedPawns( this Pawn pawn, List<Pawn> options, bool selected )
        {
            // direct relations of ALL pawns.
            List<Pawn> relatedPawns = pawn.GetDirectlyRelatedPawns();

            // opinions above threshold
            foreach ( Pawn other in options )
            {
                float maxOpinion = Mathf.Max( pawn.OpinionOfCached( other ), other.OpinionOfCached( pawn ) );
                float minOpinion = Mathf.Min( pawn.OpinionOfCached( other ), other.OpinionOfCached( pawn ) );
                if ( ( selected && ( maxOpinion > 5f || minOpinion < -5f ) ) || maxOpinion > OPINION_THRESHOLD_POS ||
                     minOpinion < OPINION_THRESHOLD_NEG )
                    relatedPawns.Add( other );
            }

            // return list without duplicates
            return relatedPawns.Distinct().ToList();
        }

        public static Color GetRelationColor( PawnRelationDef def, float opinion )
        {
            if ( def != null && RELATIONS_VISIBLE[def] )
                return RELATIONS_COLOR[def];

            return GetRelationColor( opinion );
        }

        public static Color GetRelationColor( float opinion )
        {
            if ( opinion > 0f )
                return Color.Lerp( Resources.ColorNeutral, Resources.ColorFriend, opinion / 100f );
            if ( opinion < 0f )
                return Color.Lerp( Resources.ColorNeutral, Resources.ColorEnemy, Mathf.Abs( opinion ) / 100f );

            return Resources.ColorNeutral;
        }

        public static string GetTooltip( this Pawn pawn, Pawn other )
        {
            var tip = "";
            if ( other != null && other != pawn )
            {
                tip += "Fluffy_Relations.Possesive".Translate( pawn.NameStringShort );
                tip += pawn.relations.OpinionExplanation( other );
                tip += "\n\n";
                tip += "Fluffy_Relations.Possesive".Translate( other.NameStringShort );
                tip += other.relations.OpinionExplanation( pawn );
            }
            tip += "\n\n" + "Fluffy_Relations.NodeInteractionTip".Translate();
            return tip;
        }

        public static string GetTooltip( this Faction faction, Faction other )
        {
            string tip = "Fluffy_Relations.NodeInteractionTip".Translate( faction.GetCallLabel() );
            if ( other != null && other != faction )
            {
                tip += "Fluffy_Relations.Possesive".Translate( other.GetCallLabel() );
                tip += "Fluffy_Relations.OpinionOf".Translate( faction.GetCallLabel(),
                                                               Mathf.RoundToInt( other.GoodwillWith( faction ) ) );
            }
            return tip;
        }

        public static bool IsSocialThought( Thought thought )
        {
            return thought is ISocialThought;
        }

        private static HediffDef _majorHediffDef;
        private static bool _psychologyLoaded = true;

        public static Pawn GetMayor()
        {
            // cop out if we checked the mayor def and it wasn't there
            if ( !_psychologyLoaded )
                return null;

            // if major def is null and we haven't checked yet, check for the def (and thus for psychology being loaded)
            if ( _majorHediffDef == null && _psychologyLoaded)
            {
                _majorHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail( "Mayor" );
                _psychologyLoaded = _majorHediffDef != null;

                // check done, call again
                return GetMayor();
            }

            // if we got here, that means psychology is loaded. return the pawn with the mayor def in the highest pop map, if any.
            var mayors = Colonists.Where( p => p.health.hediffSet.HasHediff( _majorHediffDef ) );
            if ( !mayors.Any() )
                return null;
            return mayors.OrderByDescending( p => p.MapHeld?.mapPawns.ColonistCount ).First();
        }
        
        public static Pawn Leader( this Faction faction )
        {
            // player faction, see if we have a major (Psychology), if not - oldest is leader.
            if ( faction == Faction.OfPlayer )
            {
                var leader = GetMayor();
                if ( leader == null && Colonists.Any() )
                    leader = Colonists.MaxBy( p => p.ageTracker.AgeBiologicalTicks );

                return leader;
            }

            if ( faction.leader == null )
                faction.GenerateNewLeader();

            return faction.leader;
        }

        public static float OpinionOfCached( this Pawn pawn, Pawn other, bool abs = false )
        {
            var pair = new Pair<Pawn, Pawn>( pawn, other );
            if ( !_opinions.ContainsKey( pair ) )
            {
                float opinion = pawn.relations.OpinionOf( other );
                _opinions.Add( pair, opinion );
                return opinion;
            }

            if ( abs )
                return Mathf.Abs( _opinions[pair] );

            return _opinions[pair];
        }

        public static void ResetOpinionCache()
        {
            _opinions = new Dictionary<Pair<Pawn, Pawn>, float>();
        }

        internal static void CreateThoughtList( List<Pawn> pawns )
        {
            // for each pawn...
            ThoughtsAbout = new Dictionary<Pawn, List<string>>();

            foreach ( Pawn pawn in pawns )
            {
                // add list for this pawn
                ThoughtsAbout.Add( pawn, new List<string>() );

                // get thoughts targeted at the pawn by all other pawns...
                foreach ( Pawn other in pawns.Where( p => p != pawn ) )
                {
                    ThoughtHandler thoughts = other.needs.mood.thoughts;

                    // get distinct social thoughts
                    var DistinctSocialThoughtGroups = new List<ISocialThought>();
                    thoughts.GetDistinctSocialThoughtGroups( pawn, DistinctSocialThoughtGroups );
                    foreach ( ISocialThought t in DistinctSocialThoughtGroups )
                    {
                        var thought = (Thought)t;
                        if ( t.OpinionOffset() != 0 )
                            ThoughtsAbout[pawn].Add( thought.LabelCapSocial );
                    }
                }

                // remove duplicates
                if ( !ThoughtsAbout[pawn].NullOrEmpty() )
                    ThoughtsAbout[pawn] = ThoughtsAbout[pawn].Distinct().ToList();
            }
        }

        #endregion Methods
    }
}
