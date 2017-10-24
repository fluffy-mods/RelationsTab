// Karel Kroeze
// Graph.cs
// 2016-12-26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph
{
    public class Graph
    {
        #region Fields

        public static float MAX_TEMPERATURE = .05f;
        public static float REPULSIVE_CONSTANT = 5000f;
        public static float ATTRACTIVE_CONSTANT = .2f;
        public static float CENTRAL_CONSTANT = .5f;
        public static int MAX_ITERATIONS = 2000;
        public static float THRESHOLD = .02f;

        public bool done;
        private List<Edge> edges = new List<Edge>();
        public static float idealDistance;
        private int iteration = 1;
        public List<Node> nodes = new List<Node>();
        private Dictionary<Node, List<Node>> connections = new Dictionary<Node, List<Node>>();
        public Vector2 size;
        private float temperature = .1f;
        private Dictionary<Pawn, Node> _pawnNodes = new Dictionary<Pawn, Node>();

#if DEBUG
        public static StringBuilder msg = new StringBuilder();
#endif

        #endregion Fields

        public void Restart()
        {
            done = false;
            iteration = 1;
        }

        public Vector2 Center => size / 2f;

        public List<Node> Connections(Node node)
        {
            if (!connections.ContainsKey(node))
                connections.Add(node, new List<Node>());
            return connections[node];
        }

        #region Constructors

        public Graph(Vector2 size)
        {
            this.size = size;
        }

        #endregion Constructors

        #region Methods

        public void AddEdge<T>(Node nodeA, Node nodeB) where T : Edge
        {
            if (
                nodeA != null &&
                nodeB != null &&
                nodeA != nodeB &&
                !Connections(nodeA).Contains(nodeB))
            {
                edges.Add((T)Activator.CreateInstance(typeof(T), nodeA, nodeB));
                Connections(nodeA).Add(nodeB);
                Connections(nodeB).Add(nodeA);
            }
        }

        public void Draw(Rect canvas)
        {
            GUI.BeginGroup(canvas);
            foreach (Edge edge in edges)
                edge.Draw();
            foreach (Node node in nodes)
                node.Draw();

            GUI.EndGroup();
        }

        public Node Node(Pawn pawn)
        {
            if (pawn == null)
                return null;

            Node node;
            if (!_pawnNodes.TryGetValue(pawn, out node))
            {
                node = nodes.FirstOrDefault(n => n.pawn == pawn);
                if (node == null)
                    return node;

                _pawnNodes.Add(pawn, node);
            }

            return node;
        }

        public void ClearEdges()
        {
            edges.Clear();
            connections.Clear();
        }

        public void Update()
        {
            // check if done
            if (done || iteration > MAX_ITERATIONS || !nodes.Any(node => !node.Frozen))
            {
                if (!done)
                    done = true;
                return;
            }

#if DEBUG
            msg = new StringBuilder();
            msg.AppendLine( "Iteration: " + iteration );
#endif

            // prepare iteration global vars, nodes and edges.
            PrepareNextIteration();

            // calculate attractive forces
            foreach (Edge edge in edges)
            {
#if DEBUG
                msg.AppendLine( "\tAttractive force between " + edge.nodeA.pawn.NameStringShort + edge.nodeA.position + " and " + edge.nodeB.pawn.NameStringShort + edge.nodeB.position );
#endif
                edge.nodeA.AttractedTo(edge.nodeB);
                edge.nodeB.AttractedTo(edge.nodeA);
            }

            // calculate repulsive forces
            foreach (Node node in nodes)
            {
#if DEBUG
                msg.AppendLine( "\tRepulsion for " + node.pawn.NameStringShort + node.position );
#endif
                foreach (Node other in nodes)
                    if (node != other)
                        node.RepulsedBy(other);
            }

            // update node positions
            done = true;
            foreach (Node node in nodes)
            {
                // central gravitational force
                node.velocity +=
                    CENTRAL_CONSTANT * // constant
                    (node.position.DistanceTo(Center) + // base term
                      Mathf.Pow(Mathf.Max(node.position.DistanceTo(Center) - size.magnitude, 0f), 2)) *
                    // additional squared force on node far away
                    node.position.DirectionTo(Center); // apply direction

                // dampen velocities
                node.velocity *= temperature;

                // if any magnitude is greater than 1, we're not done yet.
                if (done && node.velocity.sqrMagnitude > THRESHOLD)
                    done = false;

                // physics!
                if (!node.Frozen)
                    node.position += node.velocity;
            }

            // tidy up
            var graphCentre = new Vector2(
                                          (nodes.Where(n => !n.Frozen).Max(node => node.position.x) -
                                            nodes.Where(n => !n.Frozen).Min(node => node.position.x)) / 2f +
                                          nodes.Where(n => !n.Frozen).Min(node => node.position.x),
                                          (nodes.Where(n => !n.Frozen).Max(node => node.position.y) -
                                            nodes.Where(n => !n.Frozen).Min(node => node.position.y)) / 2f +
                                          nodes.Where(n => !n.Frozen).Min(node => node.position.y));
            Vector2 offset = size / 2f - graphCentre;

#if DEBUG
            msg.AppendLine( "Centre: " + graphCentre + ", offset: " + ( size / 2f ) );
#endif
            foreach (Node node in nodes)
            {
                // move to true center
                if (!node.Frozen)
                    node.position += offset;

                // TODO: Better way to handle explosions. Clamping with randomization + reset of velocity?
                // Clamping leads to identical positions, which really fucks up the rest of the algorithm. Central gravitational force should be enough to handle the issue
                //// clamp nodes to be within visible area
                //if ( !node.frozen )
                //    node.Clamp( size );

#if DEBUG
                msg.AppendLine( "\t" + node.pawn.LabelShort + ", velocity: " + node.velocity + ", position: " + node.position );
            }
            Log.Message( msg.ToString() );
#else
            }
#endif
            iteration++;
        }

        private void PrepareNextIteration()
        {
            // set iteration vars
            idealDistance = Mathf.Clamp(Mathf.Sqrt(size.x * size.y) / nodes.Count, Constants.SlotSize,
                                         Constants.SlotSize * 5f);
            temperature = MAX_TEMPERATURE * (1f - 1f / MAX_ITERATIONS * iteration);

#if DEBUG
            msg.AppendLine( "idealDistance: " + idealDistance + ", temperature: " + temperature );
#endif
        }

        #endregion Methods
    }
}
