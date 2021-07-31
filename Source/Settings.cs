// Karel Kroeze
// Settings.cs
// 2017-05-13

using System.Collections.Generic;
using System.Globalization;
using Fluffy_Relations.ForceDirectedGraph;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations {
    public class Settings: ModSettings {
        #region Fields

        public static Dictionary<string, object> values = new Dictionary<string, object>();
        private static Vector2 _scrollposition = Vector2.zero;
        private static float _settingsHeight = 999f;
        private static readonly float rowHeight = 24f;
        private static readonly float rowMargin = 6f;

        #endregion Fields

        #region Methods

        public static void DoSettingsWindowContents(Rect canvas) {
            Widgets.BeginScrollView(canvas, ref _scrollposition,
                                     new Rect(0f, 0f, canvas.width - Constants.ScrollbarWidth, _settingsHeight));
            float curY = 0f;

            // RELATIONS OPTIONS
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight * 2),
                           "Fluffy_Relations.RelationOptions".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight * 2;

            // min opinion threshold
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.LowerOpinionThreshold".Translate(),
                             ref RelationsHelper.OPINION_THRESHOLD_NEG,
                             "Fluffy_Relations.LowerOpinionThreshold.Tip".Translate());

            // max opinion threshold
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.UpperOpinionThreshold".Translate(),
                             ref RelationsHelper.OPINION_THRESHOLD_POS,
                             "Fluffy_Relations.UpperOpinionThreshold.Tip".Translate());

            // GRAPH OPTIONS
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, curY, canvas.width, rowHeight * 2),
                           "Fluffy_Relations.GraphOptions".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            curY += rowHeight * 2;

            string disclaimer = "Fluffy_Relations.GraphOptionsInformation".Translate();
            Text.Font = GameFont.Tiny;
            float disclaimerHeight = Text.CalcHeight(disclaimer, canvas.width);
            Widgets.Label(new Rect(0f, curY, canvas.width, disclaimerHeight), disclaimer);
            Text.Font = GameFont.Small;
            curY += disclaimerHeight;

            // max iterations
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.MaxIterations".Translate(),
                             ref Graph.MAX_ITERATIONS,
                             "Fluffy_Relations.Graph.MaxIterationsTip".Translate());

            // movement threshold
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.Threshold".Translate(),
                             ref Graph.THRESHOLD,
                             "Fluffy_Relations.Graph.ThresholdTip".Translate());

            // max temperature
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.MaxTemperature".Translate(),
                             ref Graph.MAX_TEMPERATURE,
                             "Fluffy_Relations.Graph.MaxTemperatureTip".Translate());

            // centre gravitational force
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.CentralConstant".Translate(),
                             ref Graph.CENTRAL_CONSTANT,
                             "Fluffy_Relations.Graph.CentralConstantTip".Translate());

            // attractive force
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.AttractiveConstant".Translate(),
                             ref Graph.ATTRACTIVE_CONSTANT,
                             "Fluffy_Relations.Graph.AttractiveConstantTip".Translate());

            // repulsive force
            DrawLabeledInput(
                             ref curY, canvas,
                             "Fluffy_Relations.Graph.RepulsiveConstant".Translate(),
                             ref Graph.REPULSIVE_CONSTANT,
                             "Fluffy_Relations.Graph.RepulsiveConstantTip".Translate());

            _settingsHeight = curY;

            Widgets.EndScrollView();
        }

        public static void DrawLabeledInput(ref float curY, Rect canvas, string label, ref float value, string tip = "") {
            Widgets.Label(new Rect(0f, curY, canvas.width / 3f * 2f, rowHeight), label);

            if (!values.ContainsKey(label)) {
                values.Add(label, value);
            }

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width / 3f * 2f, curY, canvas.width / 3f, rowHeight),
                                               values[label].ToString());

            if (tip != "") {
                TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);
            }

            if (GUI.GetNameOfFocusedControl() != label && !float.TryParse(values[label].ToString(), out value)) {
                Messages.Message(
                                 "Fluffy_Relations.InvalidFloat".Translate(
                                                                           NumberFormatInfo.CurrentInfo
                                                                                           .NumberDecimalSeparator),
                                 MessageTypeDefOf.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        public static void DrawLabeledInput(ref float curY, Rect canvas, string label, ref int value, string tip = "") {
            Widgets.Label(new Rect(0f, curY, canvas.width / 3f * 2f, rowHeight), label);

            if (!values.ContainsKey(label)) {
                values.Add(label, value);
            }

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width / 3f * 2f, curY, canvas.width / 3f, rowHeight),
                                               values[label].ToString());

            if (tip != "") {
                TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);
            }

            if (GUI.GetNameOfFocusedControl() != label && !int.TryParse(values[label].ToString(), out value)) {
                Messages.Message("Fluffy_Relations.InvalidInteger".Translate(), MessageTypeDefOf.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        public override void ExposeData() {
            base.ExposeData();

            // relation thresholds
            Scribe_Values.Look(ref RelationsHelper.OPINION_THRESHOLD_NEG, "OPINION_THRESHOLD_NEG", -50f);
            Scribe_Values.Look(ref RelationsHelper.OPINION_THRESHOLD_POS, "OPINION_THRESHOLD_POS", 50f);

            // Graph parameters (advanced).
            Scribe_Values.Look(ref Graph.ATTRACTIVE_CONSTANT, "ATTRACTIVE_CONSTANT", .2f);
            Scribe_Values.Look(ref Graph.CENTRAL_CONSTANT, "CENTRAL_CONSTANT", .5f);
            Scribe_Values.Look(ref Graph.MAX_ITERATIONS, "MAX_ITERATIONS", 2000);
            Scribe_Values.Look(ref Graph.MAX_TEMPERATURE, "MAX_TEMPERATURE", .05f);
            Scribe_Values.Look(ref Graph.REPULSIVE_CONSTANT, "REPULSIVE_CONSTANT", 5000f);
            Scribe_Values.Look(ref Graph.THRESHOLD, "THRESHOLD", .02f);
        }

        #endregion Methods
    }
}
