using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FlyByWireSASMode
{
    public static class Lib
    {
        /// <summary>
        /// Return a quaternion describing a rotation from a normalized direction vector to another.
        /// </summary>
        /// <param name="fromV">The normalized starting vector</param>
        /// <param name="toV">The normalized end vector</param>
        public static QuaternionD QuaternionDFromToRotation(Vector3d fromV, Vector3d toV)
        {
            double dot = Vector3d.Dot(fromV, toV);

            if (dot >= 1.0)
                return QuaternionD.identity;

            if (dot <= -1.0)
            {
                Vector3d axis = Vector3d.Cross(Vector3d.right, fromV);
                if (axis.sqrMagnitude == 0.0)
                    axis = Vector3d.Cross(Vector3d.up, fromV);

                return new QuaternionD(axis.x, axis.y, axis.z, 0.0);
            }

            double s = Math.Sqrt((1.0 + dot) * 2.0);
            double rs = 1.0 / s;
            Vector3d cross = Vector3d.Cross(fromV, toV);
            return new QuaternionD(cross.x * rs, cross.y * rs, cross.z * rs, s * 0.5);
        }

        private const double halfDegToRad = Math.PI / 180.0 * 0.5;

        /// <summary>
        /// Returns a rotation that rotates z degrees around the z axis,
        /// x degrees around the x axis, and y degrees around the y axis,
        /// applied in that order.
        /// </summary>
        public static QuaternionD QuaternionDFromEuler(double x, double y, double z)
        {
            double sz, cz, sx, cx, sy, cy;

            double halfX = x * halfDegToRad;
            sx = Math.Sin(halfX);
            cx = Math.Cos(halfX);

            double halfY = y * halfDegToRad;
            sy = Math.Sin(halfY);
            cy = Math.Cos(halfY);

            double halfZ = z * halfDegToRad;
            sz = Math.Sin(halfZ);
            cz = Math.Cos(halfZ);

            return new QuaternionD(
                cy * sx * cz + sy * cx * sz,
                sy * cx * cz - cy * sx * sz,
                cy * cx * sz - sy * sx * cz,
                cy * cx * cz + sy * sx * sz);
        }
    }
}
