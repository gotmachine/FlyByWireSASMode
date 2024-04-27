using UnityEngine;

namespace FlyByWireSASMode
{
    internal class VesselFlyByWireState
    {
        public Vessel vessel;
        public Vector3 direction;

        public VesselFlyByWireState(Vessel vessel)
        {
            this.vessel = vessel;
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

            // clear manual input
            st.pitch = 0f;
            st.yaw = 0f;

            // continuously set mode to stability assist so we know which button to disable
            vessel.Autopilot.mode = VesselAutopilot.AutopilotMode.StabilityAssist;

            // set SAS to follow requested direction
            vessel.Autopilot.SAS.lockedMode = false;
            vessel.Autopilot.SAS.SetTargetOrientation(direction, false);
        }
    }
}