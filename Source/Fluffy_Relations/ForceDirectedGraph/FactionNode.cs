using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph
{
    public class FactionNode : Node
    {
        public Faction faction;

        public FactionNode( Faction faction, Vector2 position, Graph graph, bool frozen = false ) : base( faction.Leader(), position, graph, frozen )
        {
            this.faction = faction;
        }

        public override void AttractedTo( Node other )
        {
            if ( frozen )
                return;

            // pull node towards other
            float force = Graph.ATTRACTIVE_CONSTANT * Mathf.Max( this.DistanceTo( other ) - Graph.idealDistance, 0f );

            // increase force if opinion > 1
            var otherFaction = other as FactionNode;
            float opinion = 999;
            if ( otherFaction != null )
            {
                opinion = faction.GoodwillWith( otherFaction.faction );
                if ( opinion > 1f )
                    force *= Mathf.Sqrt( opinion );
            }

            // apply force in direction of other
            this.velocity += force * this.DirectionTo( other );

#if DEBUG
            Graph.msg.AppendLine( "\t\tOpinion: " + opinion + ", Force: " + force + ", Vector: " + force * this.DirectionTo( other ) );
#endif
        }

        public override void Draw()
        {
            // call extra draw handlers
            if ( PreDrawExtras != null )
                PreDrawExtras();

            // draw basic slot, with faction label
            PawnSlotDrawer.DrawSlot( pawn, slot, false, label: faction.GetCallLabel() );

            // call extra draw handlers
            if ( PostDrawExtras != null )
                PostDrawExtras();

            // do interactions, with all their handlers and stuff
            Interactions();
        }
    }
}