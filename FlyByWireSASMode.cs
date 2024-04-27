using System.Collections;
using System.Collections.Generic;
using KSP.UI;
using KSP.UI.Screens.Flight;
using KSP.UI.TooltipTypes;
using UnityEngine;
using UnityEngine.UI;
using static VesselAutopilot;

namespace FlyByWireSASMode
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    [DefaultExecutionOrder(1)] // our LateUpdate() need to run after VesselAutopilotUI.LateUpdate()
    internal class FlyByWireSASMode : MonoBehaviour
    {
        internal static FlyByWireSASMode instance;

        private static Sprite sprite;
        private static bool configParsed;
        private static AutopilotMode requiredSASMode = AutopilotMode.Maneuver;
        private static float inputSensitivity = 1f;

        private bool started;

        private UIStateToggleButton flyByWireButton;
        private UIStateToggleButton stabilityAssistButton;

        internal NavBall navBall;
        private GameObject navBallMarker;
        private GameObject navBallArrow;

        private Vessel activeVessel;
        private VesselFlyByWireState activeVesselState;
        private List<VesselFlyByWireState> nonActiveVesselStates = new List<VesselFlyByWireState>();
        private bool isModeAvailableOnActiveVessel;

        private void Awake()
        {
            instance = this;

            if (!configParsed)
            {
                configParsed = true;
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
            }

            if (sprite == null)
            {
                Texture2D tex = GameDatabase.Instance.GetTexture("FlyByWireSASMode/FlyByWireMarker", false);
                if (tex == null)
                {
                    Debug.LogError($"[FlyByWireSASMode] Unable to get 'FLYBYWIRE' texture, make sure the plugin is installed in 'GameData\\FlyByWireSASMode'");
                    DestroyImmediate(this);
                    return;
                }

                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
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

            GameObject flyByWireButtonObject = Instantiate(autopilotbuttons[9].gameObject);

            flyByWireButton = flyByWireButtonObject.GetComponent<UIStateToggleButton>();
            flyByWireButton.onClick.RemoveAllListeners();
            flyByWireButton.onClick.AddListener(ActivateFromUI);
            flyByWireButton.name = "FlyByWire";
            flyByWireButton.SetState(false);
            flyByWireButton.interactable = true;
            float scale = GameSettings.UI_SCALE * GameSettings.UI_SCALE_NAVBALL;
            flyByWireButton.transform.localScale = new Vector3(scale, scale);
            Vector3 pos = autopilotbuttons[9].transform.position;
            pos.x += 8 * scale;
            pos.y += 24 * scale;
            flyByWireButton.transform.position = pos;

            flyByWireButtonObject.GetComponent<TooltipController_Text>().textString = "Fly by wire";
            flyByWireButtonObject.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
            flyByWireButtonObject.transform.SetParent(autopilotUI.transform);


            GameObject navballFrame = navBall.gameObject.transform.parent.parent.gameObject;
            GameObject navBallVectorsPivot = navballFrame.gameObject.GetChild("NavBallVectorsPivot");

            navBallMarker = new GameObject("FlyByWireMarker");
            navBallMarker.transform.SetParent(navBallVectorsPivot.transform);
            navBallMarker.layer = LayerMask.NameToLayer("UI");

            Image image = navBallMarker.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.green;

            ((RectTransform)navBallMarker.transform).sizeDelta = new Vector2(40, 40);

            navBallArrow = Instantiate(navBallVectorsPivot.GetChild("BurnVectorArrow"));
            navBallArrow.name = "FlyByWireArrow";
            navBallArrow.transform.SetParent(navBallVectorsPivot.transform);
            navBallArrow.GetComponent<MeshRenderer>().materials[0].SetColor("_TintColor", Color.green);

            flyByWireButtonObject.SetActive(false);
            navBallMarker.SetActive(false);
            navBallArrow.SetActive(false);

            started = true;
        }

        private void ActivateFromUI()
        {
            if (activeVesselState != null)
            {
                activeVesselState.ResetDirection();
                return;
            }

            activeVesselState = new VesselFlyByWireState(FlightGlobals.ActiveVessel);

            SetUIState(true);
        }

        private void DeactivateFromUI()
        {
            if (activeVesselState == null)
                return;

            activeVesselState.Destroy();
            activeVesselState = null;

            SetUIState(false);
        }

        private void SetUIState(bool state)
        {
            if (state)
            {
                flyByWireButton.SetState(true);
            }
            else
            {
                flyByWireButton.SetState(false);
                navBallMarker.SetActive(false);
                navBallArrow.SetActive(false);
            }
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
                VesselFlyByWireState lastActiveVesselState = activeVesselState;
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
                SetUIState(activeVesselState != null);
            }

            // check if active vessel can use the fly by wire mode and show/hide the UI button
            bool isModeAvailable = IsModeAvailable(activeVessel);
            if (isModeAvailable != isModeAvailableOnActiveVessel)
            {
                isModeAvailableOnActiveVessel = isModeAvailable;
                flyByWireButton.gameObject.SetActive(isModeAvailable);
            }

            // if active vessel can't use the mode anymore, destroy the state
            if (!isModeAvailableOnActiveVessel && activeVesselState != null)
            {
                activeVesselState.Destroy();
                activeVesselState = null;
                SetUIState(false);
            }

            // check non-active vessels too
            for (int i = nonActiveVesselStates.Count; i-- > 0;)
            {
                VesselFlyByWireState vesselState = nonActiveVesselStates[i];
                if (!IsModeAvailable(vesselState.vessel))
                {
                    vesselState.Destroy();
                    nonActiveVesselStates.RemoveAt(i);
                }
            }

            // nothing else to do if mode isn't enabled for active vessel
            if (activeVesselState == null)
                return;

            // continously disable the stability assist button
            stabilityAssistButton.SetState(false);

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
