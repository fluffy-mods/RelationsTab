// GameComponent_Leader.cs
// Copyright Karel Kroeze, 2018-2018

using Verse;

namespace Fluffy_Relations
{
    public class GameComponent_Leader: GameComponent
    {
        private static Pawn _leader;

        public GameComponent_Leader(): base() {}
        public GameComponent_Leader( Game game ) : base() {}

        public static Pawn Leader
        {
            get
            {
                if (_leader.DestroyedOrNull() || _leader.Dead )
                {
                    _leader = null;
                }
                return _leader;
            }
            set => _leader = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look( ref _leader, "Leader", false );
        }
    }
}