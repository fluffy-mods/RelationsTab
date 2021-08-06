// Karel Kroeze
// Resources.cs
// 2016-12-26

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations {
    [StaticConstructorOnStartup]
    public static class Resources {
        #region Fields

        public static Rect baseSlot = new(0f, 0f, Constants.SlotSize, Constants.SlotSize);
        public static Dictionary<FactionDef, Texture2D> baseTextures = new();

        // colors
        public static Color
            ColorNeutral = Color.grey,
            ColorFriend = Color.green,
            ColorEnemy = Color.red,
            ColorLover = GenUI.MouseoverColor,
            ColorExLover = Color.cyan,
            ColorFamily = Color.white;

        public static Texture2D
            SlightlyDarkBG = SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, .4f)),
            Solid = SolidColorMaterials.NewSolidColorTexture(Color.white),

            //// icons
            DotsCircle = ContentFinder<Texture2D>.Get("UI/Icons/DotsCircle"),
            DotsDynamic = ContentFinder<Texture2D>.Get("UI/Icons/DotsDynamic"),
            Cog = ContentFinder<Texture2D>.Get("UI/Icons/Cog"),
            Pin = ContentFinder<Texture2D>.Get("UI/Icons/Pin"),
            Edit = ContentFinder<Texture2D>.Get( "UI/Icons/edit" ),
            FamilyTree = ContentFinder<Texture2D>.Get( "UI/Icons/FamilyTree" ),

            // misc
            Halo = ContentFinder<Texture2D>.Get("UI/Misc/Halo");

        #endregion Fields

        public static void CacheBaseTextures() {
            foreach (FactionDef faction in DefDatabase<FactionDef>.AllDefsListForReading) {
                if (!faction.settlementTexturePath.NullOrEmpty()) {
                    baseTextures.Add(faction, ContentFinder<Texture2D>.Get(faction.settlementTexturePath));
                }
            }
        }
    }
}
