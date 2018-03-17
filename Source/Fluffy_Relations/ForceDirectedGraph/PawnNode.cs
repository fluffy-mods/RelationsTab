using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph
{
    public class PawnNode : Node
    {
        public PawnNode( Pawn pawn, Vector2 position, Graph graph, bool frozen = false, bool secondary = false ) : base( pawn, position, graph, frozen, secondary )
        {
        }

        public override void AttractedTo( Node other )
        {
            if ( frozen )
                return;

            // pull node towards other
            float force = Graph.ATTRACTIVE_CONSTANT * Mathf.Max( this.DistanceTo( other ) - Graph.idealDistance, 0f );

            // increase force if opinion > 1
            float opinion = pawn.OpinionOfCached( other.pawn );
            if ( opinion > 1f )
                force *= Mathf.Sqrt( opinion );

            // apply force in direction of other
            this.velocity += force * this.DirectionTo( other );

#if DEBUG
            Graph.msg.AppendLine( "\t\tOpinion: " + opinion + ", Force: " + force + ", Vector: " + force * this.DirectionTo( other ) );
#endif
        }
    }
}