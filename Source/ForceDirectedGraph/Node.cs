using System;
using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph {
    public class Node {
        #region Fields

        protected bool frozen;
        public Pawn pawn;
        public Vector2 position;
        public Vector2 velocity = Vector2.zero;
        public Graph graph;
        private bool wasDragged = false;
        public bool secondary = false;

        public Action OnLeftClick;
        public Action OnRightClick;
        public Action OnHover;
        public Action PostDrawExtras;
        public Action PreDrawExtras;

        #endregion Fields

        #region Constructors

        public Node(Pawn pawn, Vector2 position, Graph graph, bool frozen = false, bool secondary = false) {
            this.pawn = pawn;
            this.position = position;
            this.frozen = frozen;
            this.graph = graph;
            this.secondary = secondary;

            OnRightClick += new Action(() => Frozen = false);

#if DEBUG
            OnHover += new Action(() => Log.Message("OnHover()" + Frozen));
            OnLeftClick += new Action(() => Log.Message("OnLeftClick()" + Frozen));
            OnRightClick += new Action(() => Log.Message("OnRightClick()" + Frozen));
#endif
        }

        #endregion Constructors

        public bool Frozen {
            get => frozen;
            set => frozen = value;
        }

        #region Methods

        public virtual void AttractedTo(Node other) {
            if (frozen) {
                return;
            }

            // pull node towards other
            float force = Graph.ATTRACTIVE_CONSTANT * Mathf.Max( this.DistanceTo( other ) - Graph.idealDistance, 0f );
            velocity += force * this.DirectionTo(other);

#if DEBUG
            Graph.msg.AppendLine("\t\tForce: " + force + ", Vector: " + (force * this.DirectionTo(other)));
#endif
        }

        public virtual void RepulsedBy(Node other) {
            if (frozen) {
                return;
            }

            // remove square?
            float force = -(  Graph.REPULSIVE_CONSTANT / Mathf.Pow( this.DistanceTo( other ), 2 )  * Graph.idealDistance );
            velocity += force * this.DirectionTo(other);

#if DEBUG
            Graph.msg.AppendLine("\t\tRepulsion from " + other.pawn.Name.ToStringShort + other.position + "; Distance: " + this.DistanceTo(other) + " (" + (Mathf.Pow(this.DistanceTo(other), 2) * Graph.idealDistance) + "), Force: " + force + ", Vector: " + (force * this.DirectionTo(other)));
#endif
        }

        public virtual void Clamp(Vector2 size) {
            position.x = Mathf.Clamp(position.x, 0f, size.x);
            position.y = Mathf.Clamp(position.y, 0f, size.y);
        }

        public Rect slot {
            get {
                Rect slot = Resources.baseSlot;
                slot.center = position;
                return slot;
            }
        }

        public virtual void Draw() {
            // call extra draw handlers
            PreDrawExtras?.Invoke();

            // draw basic slot
            pawn.DrawSlot(slot, false, label: pawn?.LabelShort, secondary: secondary);

            // call extra draw handlers
            PostDrawExtras?.Invoke();

            // do interactions, with all their handlers and stuff
            Interactions();
        }

        public virtual void Interactions() {
            // on mouse down, reset the drag check
            if (Event.current.type == EventType.MouseDown) {
                wasDragged = false;
            }

            // hover and drag handlers
            if (Mouse.IsOver(slot)) {
                // hover
                OnHover?.Invoke();

                // on left mouse drag, move node, freeze location, and restart graph so other nodes react
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDrag) {
                    position += Event.current.delta;
                    frozen = true;
                    wasDragged = true;
                    graph.Restart();
                }
            }

            // clicks
            if (Widgets.ButtonInvisible(slot) && !wasDragged) {
                // on right click
                if (Event.current.button == 1 && OnRightClick != null) {
                    OnRightClick();
                }

                // on left click
                if (Event.current.button == 0 && OnLeftClick != null) {
                    OnLeftClick();
                }
            }
        }

        #endregion Methods
    }
}
