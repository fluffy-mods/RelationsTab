using CommunityCoreLibrary.ColorPicker;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private static Dictionary<Pair<Pawn,Pawn>, float> _opinions;

        #endregion Fields

        #region Constructors

        static RelationsHelper()
        {
            RELATIONS_VISIBLE = new DefMap<PawnRelationDef, bool>();
            RELATIONS_COLOR = new DefMap<PawnRelationDef, Color>();

            var relations = DefDatabase<PawnRelationDef>.AllDefsListForReading;

            for ( int i = 0; i < relations.Count; i++ )
            {
                var relation = relations[i];
                RELATIONS_VISIBLE[relation] = relation.opinionOffset > OPINION_THRESHOLD_POS / 2f || relation.opinionOffset < OPINION_THRESHOLD_NEG / 2f;
                RELATIONS_COLOR[relation] = ColorHelper.HSVtoRGB( (float)i / (float)relations.Count, 1f, 1f );
            }
        }

        #endregion Constructors

        #region Methods

        public static void DrawSocialStatusEffectsSummary( Rect canvas, Pawn pawn )
        {
            GUI.BeginGroup( canvas );

            float curY = 0f;
            Rect mainDesc = new Rect( 0f, curY, canvas.width, Settings.RowHeight );
            curY += Settings.RowHeight;

            Widgets.Label( mainDesc, pawn.MainDesc( true ) );
            TooltipHandler.TipRegion( mainDesc, pawn.ageTracker.AgeTooltipString );

            // TODO: Get all stuff that others think about this pawn, and shortly list it here.

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
            string tip = "Fluffy_Relations.NodeInteractionTip".Translate( pawn.NameStringShort );
            if ( other != null && other != pawn )
            {
                tip += "\n";
                tip += "Fluffy_Relations.Possesive".Translate( other.NameStringShort );
                tip += "Fluffy_Relations.OpinionOf".Translate( pawn.NameStringShort, Mathf.RoundToInt( other.OpinionOfCached( pawn ) ) );
                tip += "\n";
                tip += "Fluffy_Relations.Possesive".Translate( pawn.NameStringShort );
                tip += "Fluffy_Relations.OpinionOf".Translate( other.NameStringShort, Mathf.RoundToInt( pawn.OpinionOfCached( other ) ) );
            }
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
            if ( faction == Faction.OfColony )
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

        #endregion Methods
    }
}