// Karel Kroeze
// PawnSlotDrawer.cs
// 2016-12-26

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations {
    public static class PawnSlotDrawer {
        #region Methods

        public static void DrawPawnInSlot(Pawn pawn, Rect slot) {
            GUI.DrawTexture(slot, PortraitsCache.Get(pawn, Constants.SlotSizeVector, Rot4.South, default, 1.22f));
        }

        public static void DrawSlot(this Pawn pawn, Rect slot, bool drawBG = true, bool drawLabel = true,
                                     bool drawLabelBG = true, bool drawHealthBar = true, bool drawStatusIcons = true,
                                     string label = "", bool secondary = false) {
            // catch null pawn
            if (pawn == null) {
                Widgets.Label(slot, "NULL");
                return;
            }

            // background square
            Rect bgRect = slot.ContractedBy(Constants.Inset);

            // name rect
            if (label == "") {
                label = pawn.Name.ToStringShort;
            }

            Rect labelRect = LabelRect(label, slot);

            // start drawing
            // draw background square
            if (drawBG) {
                GUI.DrawTexture(bgRect, TexUI.GrayBg);
                Widgets.DrawBox(bgRect);
            }

            // draw pawn
            DrawPawnInSlot(pawn, slot);

            // draw label
            if (drawLabel) {
                Text.Font = GameFont.Tiny;
                if (drawLabelBG) {
                    GUI.DrawTexture(labelRect, Resources.SlightlyDarkBG);
                }

                if (secondary) {
                    GUI.color = Color.grey;
                }
                Widgets.Label(labelRect, label);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        public static void DrawTextureColoured(Rect rect, Texture2D texture, Color colour) {
            Color oldColor = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, texture);
            GUI.color = oldColor;
        }

        public static Rect LabelRect(string name, Rect slot) {
            // get the width
            bool WW = Text.WordWrap;
            Text.WordWrap = false;
            Text.Font = GameFont.Tiny;
            float width = Text.CalcSize(name).x;
            Text.Font = GameFont.Small;
            Text.WordWrap = WW;

            // create rect
            Rect labelRect = new(
                                     ((Constants.SlotSize - width) / 2f) + slot.xMin,
                                     slot.yMax - Constants.LabelHeight,
                                     width,
                                     Constants.LabelHeight);

            return labelRect;
        }

        #endregion Methods
    }
}
