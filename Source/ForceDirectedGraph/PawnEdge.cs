using RimWorld;

namespace Fluffy_Relations.ForceDirectedGraph {
    public class PawnEdge: Edge {
        #region Constructors

        public PawnEdge(Node nodeA, Node nodeB) : base(nodeA, nodeB) {
        }

        #endregion Constructors

        #region Methods

        public override void Draw() {
            PawnRelationDef relation = nodeA.pawn.GetMostImportantVisibleRelation( nodeB.pawn );

            // draw lines
            Helpers.DrawArrow(nodeA.position, nodeB.position, RelationsHelper.GetRelationColor(relation, nodeA.pawn.OpinionOfCached(nodeB.pawn)));
            Helpers.DrawArrow(nodeB.position, nodeA.position, RelationsHelper.GetRelationColor(relation, nodeB.pawn.OpinionOfCached(nodeA.pawn)));
        }

        #endregion Methods
    }
}
