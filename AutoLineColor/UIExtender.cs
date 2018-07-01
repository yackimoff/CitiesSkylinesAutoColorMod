using ColossalFramework.UI;
using ICities;
using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor
{
    [UsedImplicitly]
    public class UIExtender : LoadingExtensionBase
    {
        private LoadMode mode;

        private UIButton refreshBtn;
        private MouseEventHandler refreshBtnClick;

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
            //Console.Instance.Message("Attaching UI");

            var ptwip = GameObject.Find("UIView").transform.GetComponentInChildren<PublicTransportWorldInfoPanel>();
            var ptwipUiPanel = ptwip.gameObject.GetComponent<UIPanel>();
            var linesOverview = (UIButton)ptwipUiPanel.Find("LinesOverview");
            var buttonPanel = (UIPanel)linesOverview.parent;

            refreshBtn = buttonPanel.Find<UIButton>("RefreshNameAndColor");

            if (refreshBtn == null)
            {
                refreshBtn = buttonPanel.AddUIComponent<UIButton>();
                refreshBtn.name = "RefreshNameAndColor";
            }
            
            refreshBtn.text = "Refresh Name/Color";
            refreshBtn.tooltip = "Reassign the line name and color according to current AutoLineColor Redux settings.";
            refreshBtn.textScale = .75f;
            refreshBtn.font = linesOverview.font;
            refreshBtn.hoveredTextColor = new Color32(7, 132, 255, 255);
            refreshBtn.textPadding = new RectOffset(10, 10, 4, 2);
            refreshBtn.autoSize = true;
            refreshBtn.anchor = UIAnchorStyle.Left | UIAnchorStyle.Right | UIAnchorStyle.CenterVertical;
            refreshBtn.normalBgSprite = "ButtonMenu";
            refreshBtn.hoveredBgSprite = "ButtonMenuHovered";
            refreshBtn.disabledBgSprite = "ButtonMenuDisabled";
            //refreshBtn.focusedBgSprite = "ButtonMenuFocused";
            refreshBtn.pressedBgSprite = "ButtonMenuPressed";

            refreshBtn.SendToBack();
            buttonPanel.ResetLayout();
            
            refreshBtnClick = (x, y) =>
            {
                ushort lineId;

                var method = typeof(PublicTransportWorldInfoPanel)
                    .GetMethod(
                        "GetLineID",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Diagnostics.Debug.Assert(method != null);

                do
                {
                    var result = method.Invoke(ptwip, new object[0]);
                    lineId = (ushort)result;
                } while (lineId == 0);

                ColorMonitor.Instance?.ForceRefreshLine(lineId);
            };

            refreshBtn.eventClick += refreshBtnClick;

            //Console.Instance.Message("Attached UI");
        }

        private void DetachUI()
        {
            //Console.Instance.Message("Detaching UI");

            if (refreshBtn != null)
            {
                refreshBtn.eventClick -= refreshBtnClick;
                refreshBtnClick = null;

                refreshBtn.parent.RemoveUIComponent(refreshBtn);
                UnityEngine.Object.Destroy(refreshBtn.gameObject);
                refreshBtn = null;
            }

            //Console.Instance.Message("Detached UI");
        }
    }
}
