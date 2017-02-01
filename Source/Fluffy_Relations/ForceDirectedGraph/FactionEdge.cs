using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph
{
    public class FactionEdge : Edge
    {
        #region Constructors

        public FactionEdge( FactionNode nodeA, FactionNode nodeB ) : base( nodeA, nodeB )
        {
        }

        #endregion Constructors

        #region Methods

        public override void Draw()
        {
            // get direction between nodes so we can offset our lines perpendicular to the line between nodes.
            var factionA = ( nodeA as FactionNode )?.faction;
            var factionB = ( nodeB as FactionNode )?.faction;

            if ( factionA == null || factionB == null )
            {
                Log.Error( "FactionEdge with non-FactionNode node(s), or null faction." );
                return;
            }

            // get relevant opinion. Opinion between non-player faction is symmetric, but between player and faction IS NOT - while this has no gameplay impact, it's confusing as fuck.
            float opinion;
            if ( factionA == Faction.OfPlayer )
                opinion = factionB.GoodwillWith( factionA );
            else
                opinion = factionA.GoodwillWith( factionB );

            // draw lines
            Helpers.DrawBiDirectionalArrow( nodeA.position, nodeB.position, RelationsHelper.GetRelationColor( opinion ) );
        }

        #endregion Methods
    }
}