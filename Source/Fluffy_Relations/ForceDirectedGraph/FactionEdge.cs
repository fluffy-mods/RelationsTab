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

            // draw lines
            Helpers.DrawArrow( nodeA.position, nodeB.position, RelationsHelper.GetRelationColor( factionA.GoodwillWith( factionB ) ) );
            Helpers.DrawArrow( nodeB.position, nodeA.position, RelationsHelper.GetRelationColor( factionB.GoodwillWith( factionA ) ) );
        }

        #endregion Methods
    }
}