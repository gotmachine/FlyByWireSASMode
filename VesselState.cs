using UnityEngine;

namespace FlyByWireSASMode
{
    internal enum CustomSASMode
    {
        Stock,
        FlyByWire,
        ParallelPos,
        ParallelNeg
    }

    internal class VesselState
    {
        public Vessel vessel;
        public CustomSASMode sasMode;
        public Vector3 direction;

        public VesselState(Vessel vessel, CustomSASMode sasMode)
        {
            this.vessel = vessel;
            this.sasMode = sasMode;
            vessel.OnPreAutopilotUpdate += OnPreAutopilotUpdate;
            ResetDirection();
        }

        public void ResetDirection()
        {
            direction = vessel.GetTransform().up;
        }

        public void Destroy()
        {
            vessel.OnPreAutopilotUpdate -= OnPreAutopilotUpdate;
        }

        private void OnPreAutopilotUpdate(FlightCtrlState st)
        {
            // continuously set mode to stability assist so we know which button to disable
            vessel.Autopilot.mode = VesselAutopilot.AutopilotMode.StabilityAssist;

            if (sasMode == CustomSASMode.FlyByWire)
            {
                // clear manual input
                st.pitch = 0f;
                st.yaw = 0f;

                if (FlightGlobals.ActiveVessel == vessel)
                {
                    // get input manually so this can work when under partial control
                    float pitch, yaw;
                    if (GameSettings.PITCH_DOWN.GetKey())
                        pitch = -1f;
                    else if (GameSettings.PITCH_UP.GetKey())
                        pitch = 1f;
                    else
                        pitch = 0f;

                    if (GameSettings.YAW_LEFT.GetKey())
                        yaw = -1f;
                    else if (GameSettings.YAW_RIGHT.GetKey())
                        yaw = 1f;
                    else
                        yaw = 0f;

                    // increase speed when further away from control direction
                    float speed = Vector3.Dot(vessel.GetTransform().up, direction) - 1.2f;

                    // transform pitch/yaw input into a X/Y translation on the navball
                    Quaternion navballAttitudeGymbal = FlyByWireSASMode.instance.navBall.attitudeGymbal;
                    direction =
                        navballAttitudeGymbal.Inverse()
                        * Quaternion.Euler(pitch * -speed, yaw * -speed, 0f)
                        * navballAttitudeGymbal
                        * direction;

                    direction.Normalize();
                }
            }
            else
            {
                Transform targetTransform = vessel.targetObject?.GetTransform();
                if (targetTransform == null)
                    return;

                direction = vessel.targetObject is Vessel ? targetTransform.up : vessel.targetObject.GetFwdVector();
                if (sasMode == CustomSASMode.ParallelNeg)
                    direction *= -1f;
            }

            // set SAS to follow requested direction
            vessel.Autopilot.SAS.lockedMode = false;
            vessel.Autopilot.SAS.SetTargetOrientation(direction, false);
        }
    }
}