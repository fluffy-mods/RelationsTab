// Karel Kroeze
// PawnSlotDrawer.cs
// 2016-12-26

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations
{
    public static class PawnSlotDrawer
    {
        #region Methods

        public static void DrawPawnInSlot(Pawn pawn, Rect slot)
        {
            // get the pawn's graphics set, and make sure it's resolved.
            PawnGraphicSet graphics = pawn.Drawer.renderer.graphics;
            if (!graphics.AllResolved)
                graphics.ResolveAllGraphics();

            // draw base body
            if (graphics.nakedGraphic != null)
            {
                GUI.color = graphics.nakedGraphic.Color;
                GUI.DrawTexture(slot, graphics.nakedGraphic.MatFront.mainTexture);
            }

            // draw apparel
            var drawHair = true;
            if (graphics.apparelGraphics != null)
                foreach (ApparelGraphicRecord apparel in graphics.apparelGraphics)
                {
                    if (apparel.sourceApparel.def.apparel.LastLayer == ApparelLayer.Overhead)
                    {
                        drawHair = false;
                        continue;
                    }

                    GUI.color = apparel.graphic.Color;
                    GUI.DrawTexture(slot, apparel.graphic.MatFront.mainTexture);
                }

            // draw head-related stuff, offset further drawing up
            slot.y -= Constants.SlotSize * 1 / 4f;

            if (graphics.headGraphic != null)
            {
                GUI.color = graphics.headGraphic.Color;
                GUI.DrawTexture(slot, graphics.headGraphic.MatFront.mainTexture);
            }

            // draw hair OR hat
            if (drawHair && graphics.hairGraphic != null)
            {
                GUI.color = graphics.hairGraphic.Color;
                GUI.DrawTexture(slot, graphics.hairGraphic.MatFront.mainTexture);
            }
            if (!drawHair && graphics.apparelGraphics != null)
                foreach (ApparelGraphicRecord apparel in graphics.apparelGraphics)
                {
                    Rect slot2 = slot;
                    if (apparel.sourceApparel.def.apparel.LastLayer == ApparelLayer.Overhead)
                    {
                        GUI.color = apparel.graphic.Color;
                        GUI.DrawTexture(slot2, apparel.graphic.MatFront.mainTexture);
                    }
                }

            // reset color, and then we're done here
            GUI.color = Color.white;
        }

        public static void DrawSlot(this Pawn pawn, Rect slot, bool drawBG = true, bool drawLabel = true,
                                     bool drawLabelBG = true, bool drawHealthBar = true, bool drawStatusIcons = true,
                                     string label = "")
        {
            // catch null pawn
            if (pawn == null)
            {
                Widgets.Label(slot, "NULL");
                return;
            }

            // background square
            Rect bgRect = slot.ContractedBy(Constants.Inset);

            // name rect
            if (label == "")
                label = pawn.NameStringShort;
            Rect labelRect = LabelRect(label, slot);

            // start drawing
            // draw background square
            if (drawBG)
            {
                GUI.DrawTexture(bgRect, TexUI.GrayBg);
                Widgets.DrawBox(bgRect);
            }

            // draw pawn
            DrawPawnInSlot(pawn, slot);

            // draw label
            if (drawLabel)
            {
                Text.Font = GameFont.Tiny;
                if (drawLabelBG)
                    GUI.DrawTexture(labelRect, Resources.SlightlyDarkBG);
                Widgets.Label(labelRect, label);
                Text.Font = GameFont.Small;
            }

            // draw health bar
        }

        public static Rect LabelRect(string name, Rect slot)
        {
            // get the width
            bool WW = Text.WordWrap;
            Text.WordWrap = false;
            Text.Font = GameFont.Tiny;
            float width = Text.CalcSize(name).x;
            Text.Font = GameFont.Small;
            Text.WordWrap = WW;

            // create rect
            var labelRect = new Rect(
                                     (Constants.SlotSize - width) / 2f + slot.xMin,
                                     slot.yMax - Constants.LabelHeight,
                                     width,
                                     Constants.LabelHeight);

            return labelRect;
        }

        #endregion Methods
    }
}
