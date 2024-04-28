using System;
using System.Collections;
using System.Collections.Generic;
using Expansions;
using KSP.UI;
using KSP.UI.Screens.Flight;
using KSP.UI.TooltipTypes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static VesselAutopilot;

namespace FlyByWireSASMode
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    [DefaultExecutionOrder(1)] // our LateUpdate() need to run after VesselAutopilotUI.LateUpdate()
    internal class FlyByWireSASMode : MonoBehaviour
    {
        internal static FlyByWireSASMode instance;

        private static bool init;
        private static Sprite flyByWireSprite;
        private static Sprite parallelPosSprite;
        private static Sprite parallelNegSprite;
        private static AutopilotMode requiredSASMode = AutopilotMode.Maneuver;
        private static float inputSensitivity = 1f;

        private bool started;

        private UIStateToggleButton stabilityAssistButton;
        private UIStateToggleButton flyByWireButton;
        private UIStateToggleButton parallelPosButton;
        private UIStateToggleButton parallelNegButton;

        internal NavBall navBall;
        private GameObject navBallMarker;
        private GameObject navBallArrow;

        private Vessel activeVessel;
        private VesselState activeVesselState;
        private List<VesselState> nonActiveVesselStates = new List<VesselState>();
        private bool isModeAvailableOnActiveVessel;

        private void Awake()
        {
            instance = this;
            if (init)
                return;

            init = true;

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("FLYBYWIRESASMODE");
            if (nodes != null && nodes.Length == 1)
            {
                nodes[0].TryGetValue("InputSensitivity", ref inputSensitivity);

                int requiredSASLevel = 0;
                if (nodes[0].TryGetValue("RequiredSASLevel", ref requiredSASLevel))
                {
                    switch (requiredSASLevel)
                    {
                        case 0: requiredSASMode = AutopilotMode.StabilityAssist; break;
                        case 1: requiredSASMode = AutopilotMode.Prograde; break;
                        case 2: requiredSASMode = AutopilotMode.Normal; break;
                        case 3: requiredSASMode = AutopilotMode.Maneuver; break;
                        default: requiredSASMode = AutopilotMode.Maneuver; break;
                    }
                }
            }

            flyByWireSprite = LoadSprite("FlyByWireSASMode/FlyByWireMarker");
            parallelPosSprite = LoadSprite("FlyByWireSASMode/ParallelPosMarker");
            parallelNegSprite = LoadSprite("FlyByWireSASMode/ParallelNegMarker");
        }

        private static Sprite LoadSprite(string path)
        {
            Texture2D tex = GameDatabase.Instance.GetTexture(path, false);
            if (tex == null)
            {
                Debug.LogError($"[FlyByWireSASMode] Unable to get 'GameData/{path}.png' texture, make sure the plugin is installed in the right folder.");
                return null;
            }

            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private IEnumerator Start()
        {
            while (!FlightGlobals.ready)
                yield return null;

            navBall = FindObjectOfType<NavBall>();
            VesselAutopilotUI autopilotUI = FindObjectOfType<VesselAutopilotUI>();
            UIStateToggleButton[] autopilotbuttons = autopilotUI.modeButtons;
            stabilityAssistButton = autopilotbuttons[0];

            foreach (UIStateToggleButton button in autopilotbuttons)
                button.onClick.AddListener(DeactivateFromUI);

            flyByWireButton = CopySASButton(autopilotUI, AutopilotMode.Maneuver, "FlyByWire", "Fly by wire", 7f, 25f, flyByWireSprite, ActivateFlyByWireFromUI);
            parallelPosButton = CopySASButton(autopilotUI, AutopilotMode.Target, "Parallel", "Parallel", 5f, -25f, parallelPosSprite, ActivateParallelPosFromUI);
            parallelNegButton = CopySASButton(autopilotUI, AutopilotMode.AntiTarget, "AntiParallel", "AntiParallel", 5f, -25f, parallelNegSprite, ActivateParallelNegFromUI);

            GameObject navballFrame = navBall.gameObject.transform.parent.parent.gameObject;
            GameObject navBallVectorsPivot = navballFrame.gameObject.GetChild("NavBallVectorsPivot");

            navBallMarker = new GameObject("FlyByWireMarker");
            navBallMarker.transform.SetParent(navBallVectorsPivot.transform);
            navBallMarker.layer = LayerMask.NameToLayer("UI");

            Image image = navBallMarker.AddComponent<Image>();
            image.sprite = flyByWireSprite;
            image.type = Image.Type.Simple;
            image.color = Color.green;

            ((RectTransform)navBallMarker.transform).sizeDelta = new Vector2(40, 40);

            navBallArrow = Instantiate(navBallVectorsPivot.GetChild("BurnVectorArrow"));
            navBallArrow.name = "FlyByWireArrow";
            navBallArrow.transform.SetParent(navBallVectorsPivot.transform);
            navBallArrow.GetComponent<MeshRenderer>().materials[0].SetColor("_TintColor", Color.green);

            navBallMarker.SetActive(false);
            navBallArrow.SetActive(false);

            started = true;
        }

        private static UIStateToggleButton CopySASButton(VesselAutopilotUI autopilotUI, AutopilotMode original, string name, string tooltip, float xOffset, float yOffset, Sprite sprite, UnityAction onClick)
        {
            GameObject originalButtonObject = autopilotUI.modeButtons[(int)original].gameObject;
            GameObject buttonObject = Instantiate(originalButtonObject);

            UIStateToggleButton button = buttonObject.GetComponent<UIStateToggleButton>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            button.name = name;
            button.SetState(false);
            button.interactable = true;
            float scale = GameSettings.UI_SCALE * GameSettings.UI_SCALE_NAVBALL;
            button.transform.localScale = new Vector3(scale, scale);
            Vector3 pos = originalButtonObject.transform.position;
            pos.x += xOffset * scale;
            pos.y += yOffset * scale;
            button.transform.position = pos;

            buttonObject.GetComponent<TooltipController_Text>().textString = tooltip;
            buttonObject.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
            buttonObject.transform.SetParent(autopilotUI.transform);
            buttonObject.SetActive(false);

            return button;
        }

        private void ActivateFlyByWireFromUI()
        {
            if (activeVesselState != null)
            {
                activeVesselState.sasMode = CustomSASMode.FlyByWire;
                activeVesselState.ResetDirection();
            }
            else
            {
                activeVesselState = new VesselState(FlightGlobals.ActiveVessel, CustomSASMode.FlyByWire);
            }

            SetUIMode(CustomSASMode.FlyByWire);
        }

        private void ActivateParallelPosFromUI()
        {
            if (activeVesselState != null)
                activeVesselState.sasMode = CustomSASMode.ParallelPos;
            else
                activeVesselState = new VesselState(FlightGlobals.ActiveVessel, CustomSASMode.ParallelPos);

            SetUIMode(CustomSASMode.ParallelPos);
        }

        private void ActivateParallelNegFromUI()
        {
            if (activeVesselState != null)
                activeVesselState.sasMode = CustomSASMode.ParallelNeg;
            else
                activeVesselState = new VesselState(FlightGlobals.ActiveVessel, CustomSASMode.ParallelNeg);

            SetUIMode(CustomSASMode.ParallelNeg);
        }

        private void DeactivateFromUI()
        {
            if (activeVesselState == null)
                return;

            activeVesselState.Destroy();
            activeVesselState = null;

            SetUIMode(CustomSASMode.Stock);
        }

        private void SetUIMode(CustomSASMode mode)
        {
            if (mode == CustomSASMode.Stock)
            {
                flyByWireButton.SetState(false);
                parallelPosButton.SetState(false);
                parallelNegButton.SetState(false);
            }
            else
            {
                flyByWireButton.SetState(mode == CustomSASMode.FlyByWire);
                parallelPosButton.SetState(mode == CustomSASMode.ParallelPos);
                parallelNegButton.SetState(mode == CustomSASMode.ParallelNeg);

            }

            navBallMarker.SetActive(false);
            navBallArrow.SetActive(false);
        }

        private static bool IsModeAvailable(Vessel v)
        {
            return v != null 
                   && v.loaded 
                   && !v.isEVA 
                   && v.IsControllable 
                   && v.ActionGroups[KSPActionGroup.SAS] 
                   && requiredSASMode.AvailableAtLevel(v);
        }

        private void LateUpdate()
        {
            if (!started)
                return;

            // check if active vessel has changed
            if (activeVessel != FlightGlobals.ActiveVessel)
            {
                activeVessel = FlightGlobals.ActiveVessel;
                VesselState lastActiveVesselState = activeVesselState;
                activeVesselState = null;

                // check if the new active vessel state is already known
                for (int i = nonActiveVesselStates.Count; i-- > 0;)
                {
                    if (nonActiveVesselStates[i].vessel == activeVessel)
                    {
                        activeVesselState = nonActiveVesselStates[i];
                        nonActiveVesselStates.RemoveAt(i);
                    }
                }

                // add previously active vessel state to non-active list
                if (lastActiveVesselState != null)
                    nonActiveVesselStates.Add(lastActiveVesselState);

                // set UI state
                SetUIMode(activeVesselState?.sasMode ?? CustomSASMode.Stock);
            }

            // check if active vessel can use the custom modes and show/hide the UI button
            bool isModeAvailable = IsModeAvailable(activeVessel);
            if (isModeAvailable != isModeAvailableOnActiveVessel)
            {
                isModeAvailableOnActiveVessel = isModeAvailable;
                flyByWireButton.gameObject.SetActive(isModeAvailable);
                parallelPosButton.gameObject.SetActive(isModeAvailable);
                parallelNegButton.gameObject.SetActive(isModeAvailable);
            }

            bool isTargetAvailable = activeVessel.targetObject?.GetTransform() != null;
            if (isTargetAvailable != parallelPosButton.interactable)
            {
                parallelPosButton.interactable = isTargetAvailable;
                parallelNegButton.interactable = isTargetAvailable;
            }

            // check non-active vessels too
            for (int i = nonActiveVesselStates.Count; i-- > 0;)
            {
                VesselState vesselState = nonActiveVesselStates[i];
                if (!IsModeAvailable(vesselState.vessel))
                {
                    vesselState.Destroy();
                    nonActiveVesselStates.RemoveAt(i);
                }
            }

            // nothing else to do if mode isn't enabled for active vessel
            if (activeVesselState == null)
                return;

            bool isParallelMode = activeVesselState.sasMode == CustomSASMode.ParallelPos || activeVesselState.sasMode == CustomSASMode.ParallelNeg;

            // if active vessel can't use the mode anymore, destroy the state
            if (!isModeAvailableOnActiveVessel || (!isTargetAvailable && isParallelMode))
            {
                activeVesselState.Destroy();
                activeVesselState = null;
                SetUIMode(CustomSASMode.Stock);
                return;
            }

            // continously disable the stability assist button
            stabilityAssistButton.SetState(false);

            if (activeVesselState.sasMode == CustomSASMode.FlyByWire)
            {            
                // set marker position on navball
                Vector3 markerLocalPos = navBall.attitudeGymbal * (activeVesselState.direction * navBall.VectorUnitScale);
                navBallMarker.transform.localPosition = markerLocalPos;

                // if marker is near the edge, or behind the navball, show the direction arrow instead
                if (markerLocalPos.z > 30f)
                {
                    if (!navBallMarker.activeSelf)
                    {
                        navBallMarker.SetActive(true);
                        navBallArrow.SetActive(false);
                    }
                }
                else
                {
                    if (!navBallArrow.activeSelf)
                    {
                        navBallMarker.SetActive(false);
                        navBallArrow.SetActive(true);
                    }

                    Vector3 arrowLocalPos = markerLocalPos - Vector3.Dot(markerLocalPos, Vector3.forward) * Vector3.forward;
                    arrowLocalPos.Normalize();
                    arrowLocalPos *= navBall.VectorUnitScale * 0.6f;
                    navBallArrow.transform.localPosition = arrowLocalPos;

                    float rot = Mathf.Rad2Deg * Mathf.Acos(arrowLocalPos.x / Mathf.Sqrt(arrowLocalPos.x * arrowLocalPos.x + arrowLocalPos.y * arrowLocalPos.y));

                    if (arrowLocalPos.y < 0f)
                        rot += 2f * (180f - rot);

                    if (float.IsNaN(rot))
                        rot = 0f;

                    navBallArrow.transform.localRotation = Quaternion.Euler(rot + 90f, 270f, 90f);
                }
            }
        }
    }
}
