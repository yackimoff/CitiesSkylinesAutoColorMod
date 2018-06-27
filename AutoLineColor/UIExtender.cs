using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;

namespace AutoLineColor
{
    class UIExtender : LoadingExtensionBase
    {
        private LoadMode mode;

        private static bool ActiveInMode(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.LoadGame:
                    return true;

                default:
                    return false;
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            this.mode = mode;

            if (ActiveInMode(mode))
                AttachUI();
        }

        public override void OnLevelUnloading()
        {
            if (ActiveInMode(mode))
                DetachUI();
        }

        private void AttachUI()
        {

        }

        private void DetachUI()
        {

        }
    }
}
