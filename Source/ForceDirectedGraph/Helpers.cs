using UnityEngine;
using Verse;

namespace Fluffy_Relations.ForceDirectedGraph {
    public static class Helpers {
        #region Methods

        public static Vector2 DirectionTo(this Node nodeA, Node nodeB) {
            return DirectionTo(nodeA.position, nodeB.position);
        }

        public static Vector2 DirectionTo(this Vector2 posA, Vector2 posB) {
            return (posB - posA).normalized;
        }

        public static float DistanceTo(this Node nodeA, Node nodeB) {
            return nodeA.position.DistanceTo(nodeB.position);
        }

        public static float DistanceTo(this Vector2 posA, Vector2 posB) {
            // make sure distance is never lower than one
            Vector2 diff = posA - posB;
            return Mathf.Max(Mathf.Sqrt(Mathf.Pow(diff.x, 2) + Mathf.Pow(diff.y, 2)), 1f);
        }

        public static void DrawArrow(Vector2 from, Vector2 to, Color color) {
            // get the normalized direction of the line, offset for parallel lines, and directions of arrow head lines
            Vector2 direction = from.DirectionTo( to );
            Vector2 lineOffset = direction.RotatedBy( 90f ) * 2f;
            Vector2 arrowDirectionA = direction.RotatedBy( 145f );
            Vector2 arrowDirectionB = direction.RotatedBy( 215f );

            // start a little away from 'real' start, and offset to avoid overlapping
            from += direction * 40f;
            from += lineOffset;

            // end 40 px away from 'real' end
            to -= direction * 30f;
            to += lineOffset;

            // arrow end points
            Vector2 arrowA = to + (arrowDirectionA * 6f);
            Vector2 arrowB = to + (arrowDirectionB * 6f);

            // draw the lines
            Widgets.DrawLine(from, to, color, 1f);
            Widgets.DrawLine(to, arrowA, color, 1f);
            Widgets.DrawLine(to, arrowB, color, 1f);
        }

        public static void DrawBiDirectionalArrow(Vector2 from, Vector2 to, Color color) {
            // get the normalized direction of the line, offset for parallel lines, and directions of arrow head lines
            Vector2 direction = from.DirectionTo( to );
            Vector2 arrowDirectionA = direction.RotatedBy( 145f );
            Vector2 arrowDirectionB = direction.RotatedBy( 215f );

            // start a little away from 'real' start, and offset to avoid overlapping
            from += direction * 35f;

            // end 40 px away from 'real' end
            to -= direction * 35f;

            // arrow end points
            Vector2 arrowA1 = to + (arrowDirectionA * 6f);
            Vector2 arrowA2 = to + (arrowDirectionB * 6f);
            Vector2 arrowB1 = from - (arrowDirectionA * 6f);
            Vector2 arrowB2 = from - (arrowDirectionB * 6f);

            // draw the lines
            Widgets.DrawLine(from, to, color, 1f);
            Widgets.DrawLine(to, arrowA1, color, 1f);
            Widgets.DrawLine(to, arrowA2, color, 1f);
            Widgets.DrawLine(from, arrowB1, color, 1f);
            Widgets.DrawLine(from, arrowB2, color, 1f);
        }

        public static Vector2 PointOnCircle(int i, int count, Vector2 center, float radius) {
            float spread = 2 * Mathf.PI / count;

            return new Vector2(
                radius * Mathf.Cos(spread * i),
                radius * Mathf.Sin(spread * i)) + center;
        }

        public static Vector2 RandomPoint(this Rect rect) {
            return new Vector2(UnityEngine.Random.Range(rect.xMin, rect.xMax), UnityEngine.Random.Range(rect.yMin, rect.yMax));
        }

        public static Vector2 RandomPointNearCentre(this Rect rect, float range) {
            return new Rect(rect.center.x - (range / 2f), rect.center.y - (range / 2f), range, range).RandomPoint();
        }

        #endregion Methods
    }
}
