// TODO: Waiting for CCL
// using CommunityCoreLibrary.ColorPicker;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Fluffy_Relations
{
    public enum Visible
    {
        visible,
        hidden,
        inapplicable
    }

    public static class RelationsHelper
    {
        #region Fields

        public static float OPINION_THRESHOLD_NEG = -50f;
        public static float OPINION_THRESHOLD_POS = 50f;
        public static DefMap<PawnRelationDef, Color> RELATIONS_COLOR;
        public static DefMap<PawnRelationDef, bool> RELATIONS_VISIBLE;
        public static DefMap<ThoughtDef, Visible> THOUGHTS_SOCIAL;
        public static Dictionary<Pawn, List<string>> ThoughtsAbout = new Dictionary<Pawn, List<string>>();
        private static Dictionary<Pair<Pawn,Pawn>, float> _opinions;

        #endregion Fields

        #region Constructors

        static RelationsHelper()
        {
            RELATIONS_VISIBLE = new DefMap<PawnRelationDef, bool>();
            RELATIONS_COLOR = new DefMap<PawnRelationDef, Color>();
            THOUGHTS_SOCIAL = new DefMap<ThoughtDef, Visible>();

            // give visible relations a sensible default
            var relations = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            for ( int i = 0; i < relations.Count; i++ )
            {
                var relation = relations[i];
                RELATIONS_VISIBLE[relation] = relation.opinionOffset > OPINION_THRESHOLD_POS / 2f || relation.opinionOffset < OPINION_THRESHOLD_NEG / 2f;
                RELATIONS_COLOR[relation] = GenColor.RandomColorOpaque();
                    // TODO: Watiting for CCL ColorHelper.HSVtoRGB( (float)i / (float)relations.Count, 1f, 1f );
            }

            // give visible thoughtdefs a sensible default
            var thoughts = DefDatabase<ThoughtDef>.AllDefsListForReading;
            foreach ( var thought in thoughts )
            {
                // Log.Message( thought.defName + "\t" + ( thought.ThoughtClass?.FullName ?? "null" ) );
                if ( thought.ThoughtClass.GetInterfaces().Contains( typeof( ISocialThought ) ) )
                    THOUGHTS_SOCIAL[thought] = Visible.visible;
                else
                    THOUGHTS_SOCIAL[thought] = Visible.inapplicable;
            }
        }

        #endregion Constructors

        #region Methods

        public static void DrawSocialStatusEffectsSummary( Rect canvas, Pawn pawn )
        {
            GUI.BeginGroup( canvas );

            float curY = 0f;
            Rect mainDescRect = new Rect( 0f, curY, canvas.width, Settings.RowHeight );
            curY += Settings.RowHeight + Settings.Margin;
            Rect summaryRect = new Rect( 0f, curY, canvas.width, canvas.height - curY );

            Widgets.Label( mainDescRect, pawn.MainDesc( true ) );
            Widgets.Label( summaryRect, "Fluffy_Relations.SocialThoughsOfOthers".Translate() + ": <i>" + String.Join( ", ", ThoughtsAbout[pawn].ToArray() ) + "</i>" );
            TooltipHandler.TipRegion( mainDescRect, pawn.ageTracker.AgeTooltipString );

            GUI.EndGroup();
        }

        public static List<Pawn> GetDirectlyRelatedPawns( this Pawn pawn )
        {
            return pawn.relations.DirectRelations.Where( rel => RELATIONS_VISIBLE[rel.def] ).Select( rel => rel.otherPawn ).ToList();
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
            foreach ( var other in options )
            {
                float maxOpinion = Mathf.Max( pawn.OpinionOfCached( other ), other.OpinionOfCached( pawn ) );
                float minOpinion = Mathf.Min( pawn.OpinionOfCached( other ), other.OpinionOfCached( pawn ) );
                if ( ( selected && ( maxOpinion > 5f || minOpinion < -5f ) ) || maxOpinion > OPINION_THRESHOLD_POS || minOpinion < OPINION_THRESHOLD_NEG )
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
            string tip = "";
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
                tip += "Fluffy_Relations.OpinionOf".Translate( faction.GetCallLabel(), Mathf.RoundToInt( other.GoodwillWith( faction ) ) );
            }
            return tip;
        }

        public static Pawn Leader( this Faction faction )
        {
            if ( faction == Faction.OfPlayer )
                return Find.MapPawns.FreeColonists.First();

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

            foreach ( var pawn in pawns )
            {
                // add list for this pawn
                ThoughtsAbout.Add( pawn, new List<string>() );

                // get thoughts targeted at the pawn by all other pawns...
                foreach ( var other in pawns.Where( p => p != pawn ) )
                {
                    ThoughtHandler thoughts = other.needs.mood.thoughts;

                    // get distinct social thoughts
                    foreach ( ISocialThought t in thoughts.DistinctSocialThoughtGroups( pawn ) )
                    {
                        Thought thought = (Thought)t;
                        if ( THOUGHTS_SOCIAL[thought.def] == Visible.visible && t.OpinionOffset() != 0 )
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