// Karel Kroeze
// Controller.cs
// 2017-05-13

using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy_Relations
{
    public class Controller : Mod
    {
        #region Constructors

        public Controller(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
        }

        #endregion Constructors
        
        #region Methods

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Fluffy_Relations".Translate();
        }

        #endregion Methods
    }
}
