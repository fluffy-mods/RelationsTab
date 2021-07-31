using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Fluffy_Relations {
    internal struct LabelPars {
        private readonly float width;
        private readonly string message;
        private readonly GameFont font;
        private static readonly Dictionary<LabelPars, float> _labelHeightCache = new Dictionary<LabelPars, float>();

        public LabelPars(float width, string message, GameFont font) {
            this.width = width;
            this.message = message;
            this.font = font;
        }

        public float Height {
            get {
                if (_labelHeightCache.TryGetValue(this, out float height)) {
                    return height;
                }

                Text.Font = font;
                height = Text.CalcHeight(message, width);
                Text.Font = GameFont.Small;
                _labelHeightCache.Add(this, height);
                return height;
            }
        }

        public override bool Equals(object obj) {
            return obj is LabelPars other
                && font == other.font
                && Math.Abs(width - other.width) < 1e-4
                && message == other.message;
        }

        public override int GetHashCode() {
            return width.GetHashCode() ^ message.GetHashCode() ^ font.GetHashCode();
        }
    }
    public static class Utilities {
        public static void Label(ref Vector2 pos, float width, string message, Color? color = null, GameFont font = GameFont.Small) {
            LabelPars pars = new LabelPars( width, message, font );
            float height = pars.Height;
            Rect rect = new Rect( pos.x, pos.y, width, height );
            GUI.color = color ?? Color.white;
            Text.Font = font;
            Widgets.Label(rect, message);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            pos.y += height;
        }

        public static bool ButtonLabel(ref Vector2 pos, float width, string message, Color? color = null) {
            Vector2 startPos = pos;
            Label(ref pos, width, message, color);
            Rect rect = new Rect( startPos.x, startPos.y, width, pos.y - startPos.y );
            Widgets.DrawHighlightIfMouseover(rect);
            return Widgets.ButtonInvisible(rect);
        }
    }
}
