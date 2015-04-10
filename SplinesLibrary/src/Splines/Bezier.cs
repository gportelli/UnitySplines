using UnityEngine;

namespace AClockworkBerry.Splines
{
    /// <summary>
    /// A set of static functions for bezier cubic curves.
    /// This class provides static methods to calculate cubic bezier curves positions, first derivative and second derivative.
    /// A method for numerical integration (whith trapezoidal rule) of the curve. 
    /// And a method for arc length reparameterization based on the inversion of curve integration with root-finding algorithms (bisection and newton).
    /// </summary>
    public static class Bezier
    {
        /// <summary>
        /// The number of integration steps.
        /// </summary>
        public static int integrationSteps = 12;

        /// <summary>
        /// Gets the curve point at t position.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t">The t parameter in [0, 1].</param>
        /// <returns>The point vector</returns>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float OneMinusT = 1f - t;
            return
                OneMinusT * OneMinusT * OneMinusT * p0 +
                3f * OneMinusT * OneMinusT * t * p1 +
                3f * OneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        /// <summary>
        /// Gets the first derivative of the curve at t position.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t">The t parameter in [0, 1].</param>
        /// <returns>The velocity vector.</returns>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        /// <summary>
        /// Gets the second derivative of the curve at t position.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t">The t parameter in [0, 1].</param>
        /// <returns>The acceleration vector.</returns>
        public static Vector3 GetSecondDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                -6f * oneMinusT * (p1 - p0) +
                6f * (1f - 2f * t) * (p2 - p1) +
                6f * t * (p3 - p2);
        }

        /// <summary>
        /// Integrates numerically with trapezoid rule the curve first derivative in [t0, t1] obtaining an arc length.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t0">The t0 left parameter in [0, 1].</param>
        /// <param name="t1">The t1 right parameter in [0, 1].</param>
        /// <returns>The arc length of the curve in [t0, t1].</returns>
        public static float IntegrateTrapezoid(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1)
        {
            float result = 0f, h = (t1 - t0) / integrationSteps;

            result = (GetFirstDerivative(p0, p1, p2, p3, t0).magnitude +
                      GetFirstDerivative(p0, p1, p2, p3, t1).magnitude) * 0.5f;

            for (int i = 1; i < integrationSteps; i++)
                result += GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            return result * h;
        }

        /// <summary>
        /// Integrates numerically with Simpson's rule the curve first derivative in [t0, t1] obtaining an arc length.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t0">The t0 left parameter in [0, 1].</param>
        /// <param name="t1">The t1 right parameter in [0, 1].</param>
        /// <returns>The arc length of the curve in [t0, t1].</returns>
        public static float IntegrateSimpson(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1)
        {
            int steps = integrationSteps - integrationSteps % 2; // make sure steps is an even number

            float result = 0f, h = (t1 - t0) / steps;

            result = (GetFirstDerivative(p0, p1, p2, p3, t0).magnitude +
                      GetFirstDerivative(p0, p1, p2, p3, t1).magnitude);

            for (int i = 1; i < steps; i += 2)
                result += 4 * GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            for (int i = 2; i < steps; i += 2)
                result += 2 * GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            return result * h / 3;
        }

        /// <summary>
        /// Integrates numerically with Simpson's 3/8 rule the curve first derivative in [t0, t1] obtaining an arc length.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t0">The t0 left parameter in [0, 1].</param>
        /// <param name="t1">The t1 right parameter in [0, 1].</param>
        /// <returns>The arc length of the curve in [t0, t1].</returns>
        public static float IntegrateSimpson38(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1)
        {
            int steps = integrationSteps - integrationSteps % 3; // make sure steps is a multiple of three

            float result = 0f, h = (t1 - t0) / steps;

            result = (GetFirstDerivative(p0, p1, p2, p3, t0).magnitude +
                      GetFirstDerivative(p0, p1, p2, p3, t1).magnitude);

            for (int i = 1; i < steps; i += 3)
                result += 3 * GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            for (int i = 2; i < steps; i += 3)
                result += 3 * GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            for (int i = 3; i < steps; i += 3)
                result += 2 * GetFirstDerivative(p0, p1, p2, p3, t0 + i * h).magnitude;

            return result * 3 * h / 8;
        }


        /// <summary>
        /// Legendre-Gauss weights with n=12 (w_i values, defined by a function linked to in the Bezier primer article)
        /// </summary>
        static float[] Wvalues = new float[] {
            0.2491470458134028f,
            0.2491470458134028f,
            0.2334925365383548f,
            0.2334925365383548f,
            0.2031674267230659f,
            0.2031674267230659f,
            0.1600783285433462f,
            0.1600783285433462f,
            0.1069393259953184f,
            0.1069393259953184f,
            0.0471753363865118f,
            0.0471753363865118f
        };

        /// <summary>
        /// Legendre-Gauss abscissae with n=12 (x_i values, defined at i=n as the roots of the nth order Legendre polynomial Pn(x))
        /// </summary>
        static float[] Tvalues = new float[] {
            -0.1252334085114689f,
            0.1252334085114689f,
            -0.3678314989981802f,
            0.3678314989981802f,
            -0.5873179542866175f,
            0.5873179542866175f,
            -0.7699026741943047f,
            0.7699026741943047f,
            -0.9041172563704749f,
            0.9041172563704749f,
            -0.9815606342467192f,
            0.9815606342467192f
        };

        /// <summary>
        /// Integrates numerically with Legendre Gauss Quadrature rule the curve first derivative in [t0, t1] obtaining an arc length.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="t0">The t0 left parameter in [0, 1].</param>
        /// <param name="t1">The t1 right parameter in [0, 1].</param>
        /// <returns>The arc length of the curve in [t0, t1].</returns>
        public static float Integrate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1)
        {
            float c1 = (t1 - t0) / 2, c2 = (t0 + t1) / 2, result = 0;

            for(int i=0; i<Tvalues.Length; i++)
                result += Wvalues[i] * GetFirstDerivative(p0, p1, p2, p3, c1 * Tvalues[i] + c2).magnitude;

            return c1 * result;
        }

        /// <summary>
        /// Gets the curve parameter that gives an arc length of s.
        /// </summary>
        /// <param name="p0">First control point.</param>
        /// <param name="p1">Second control point.</param>
        /// <param name="p2">Third control point.</param>
        /// <param name="p3">Forth control point.</param>
        /// <param name="s">The disired arc length s in [0, curve length].</param>
        /// <param name="t0">The t parameter initial candidate for the algorithm.</param>
        /// <param name="epsilon">The maximum error ds.</param>
        /// <param name="jmax">The maximum number of iterations for the root-finding algorithm.</param>
        /// <returns>The curve parameter.</returns>
        public static float GetArcLengthParameter(
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, 
            float s, float t0, float epsilon, int jmax = 10)
        {
            float dt = t0;
            
            // Initial root-bounding interval for bisection.
            float lower = 0, upper = 1;

            for (int j = 0; j < jmax; j++)
            {
                float F = Integrate(p0, p1, p2, p3, 0, dt) - s;

                if (Mathf.Abs(F) < epsilon)
                {
                    // |F(t)| is close enough to zero, report t as the time at which length s is attained.
                    return dt;
                }

                // Generate a candidate for Newton's method.
                float DF = GetFirstDerivative(p0, p1, p2, p3, dt).magnitude;
                float tCandidate = dt - F / DF;

                // Update the root-bounding interval and test for containment of the candidate.
                if (F > 0)
                {
                    upper = dt;
                    if (tCandidate <= lower)
                    {
                        // Candidate is outside the root-bounding interval. Use bisection instead.
                        dt = 0.5f * (upper + lower);
                    }
                    else
                    {
                        // There is no need to compare to 'upper' because the tangent
                        // line has positive slope, guaranteeing that the t-axis
                        // intercept is smaller than 'upper'.
                        dt = tCandidate;
                    }
                }
                else
                {
                    lower = dt;
                    if (tCandidate >= upper)
                    {
                        // Candidate is outside the root-bounding interval. Use bisection instead.
                        dt = 0.5f * (upper + lower);
                        //Debug.Log("using bisection tCandidate >= upper " + tCandidate + " >= " + upper);
                    }
                    else
                    {
                        // There is no need to compare to 'lower' because the tangent
                        // line has positive slope, guaranteeing that the t-axis
                        // intercept is larger than 'lower'.
                        dt = tCandidate;
                    }
                }
            }

            // A root was not found according to the specified number of iterations
            // and tolerance. You might want to increase iterations or tolerance or
            // integration accuracy. However, in this application it is likely that
            // the time values are oscillating, due to the limited numerical
            // precision of 32-bit floats. It is safe to use the last computed time.
            return dt;
        }
    }
}