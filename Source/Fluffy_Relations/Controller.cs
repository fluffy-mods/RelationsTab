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
        #region Fields

        private static Controller _instance;

        #endregion Fields

        #region Constructors

        public Controller(ModContentPack content) : base(content)
        {
            _instance = this;
        }

        #endregion Constructors

        #region Properties

        public static Controller Get => _instance;

        #endregion Properties

        #region Methods

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Fluffy.Relations".Translate();
        }

        #endregion Methods

        #region Classes

        [StaticConstructorOnStartup]
        public class Init
        {
            #region Constructors

            static Init()
            {
                Get.GetSettings<Settings>();
            }

            #endregion Constructors
        }

        #endregion Classes
    }
}
