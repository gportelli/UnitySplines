using UnityEngine;
using System;
using System.Collections;

namespace AClockworkBerry.Splines
{
    /// <summary>
    /// This class contains functions to create and edit bezier splines. 
    /// It also contains methods to walk the spline (or spline sections) at constant speed based on curve first derivative.
    /// Arc length reparameterization can be performed with a high cost accurate function or with a low cost real-time oriented approximate function.
    /// </summary>
    public class Spline : MonoBehaviour
    {
        [SerializeField]
        private Vector3[] _points;

        /// <summary>
        /// The mode for each control point (corner, aligned, smooth).
        /// </summary>
        [SerializeField]
        private BezierControlPointMode[] _modes;

        /// <summary>
        /// The curve lengths cache.
        /// </summary>
        private float[] _curveLengths;

        /// <summary>
        /// The arc lengths cache, each value represents the length of the arc from spline start to the curve end.
        /// </summary>
        private float[] _arcLengths;

        private Vector3[] _orientationVectors;

        /// <summary>
        /// Gets the total length of the spline.
        /// </summary>
        /// <value>
        /// The spline length.
        /// </value>
        public float length
        {
            get
            {
                _UpdateLengths();

                return _arcLengths[curveCount];
            }
        }

        /// <summary>
        /// Show gizmo in viewport
        /// </summary>
        public bool showGizmo = true;


        /// <summary>
        /// Show velocities in viewport.
        /// </summary>
        public bool showVelocities = false;

        /// <summary>
        /// Show accelerations in viewport.
        /// </summary>
        public bool showAccelerations = false;

        /// <summary>
        /// Show control point numbers in viewport.
        /// </summary>
        public bool showNumbers = false;

        /// <summary>
        /// The spline color showed in the viewport.
        /// </summary>
        public Color color = Color.white;

        /// <summary>
        /// Is this a looping spline
        /// </summary>
        [SerializeField]
        private bool _loop;

        /// <summary>
        /// The distance between each sample to be used in the approximate arc length reparameterization algorithm.
        /// </summary>
        [HideInInspector]
        private float _samplesDistance = 1;

        /// <summary>
        /// Gets or sets the distance between samples to be used in the approximate arc length reparameterization algorithm.
        /// </summary>
        /// <value>
        /// The n samples.
        /// </value>
        public float samplesDistance
        {
            get { return _samplesDistance;  }
            set { _samplesDistance = value; SetDirty(); }
        }

        /// <summary>
        /// The cache of curve parameters to be used in the approximate arc length reparameterization algorithm.
        /// For each curve _nSamples are saved.
        /// </summary>
        private float[] _tSample;


        /// <summary>
        /// The cache of _tSample to _sSample slopes to be used in the approximate arc length reparameterization algorithm.
        /// For each curve _nSamples are saved.
        /// </summary>
        private float[] _tsSlope;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Spline"/> is loop.
        /// If set to true, it welds the first and last point of the spline.
        /// </summary>
        /// <value>
        ///   <c>true</c> if loop; otherwise, <c>false</c>.
        /// </value>
        public bool loop
        {
            get
            {
                return _loop;
            }
            set
            {
                _loop = value;
                if (value == true)
                {
                    _modes[_modes.Length - 1] = _modes[0];
                    SetControlPoint(0, _points[0]);
                }
            }
        }

        void Start()
        {
            _UpdateCache();
        }

        private void _UpdateCache()
        {
            _UpdateLengths();
            _InitSamples();
            _UpdateOrientations();
        }

        void OnDrawGizmos()
        {
            DoGizmos(false);
        }

        void OnDrawGizmosSelected()
        {
            DoGizmos(true);
        }

        /// <summary>
        /// This function renders the spline in the viewport when it is not selected. 
        /// </summary>
        /// <remarks>
        /// It draws the spline with a set of selectable segments. 
        /// A gizmo is included as well to help the user in the selection.
        /// It is compiled only inside the unity editor. It is not included in the exported builds.
        /// </remarks>
        /// <param name="selected">True if the spline is selected.</param>
        void DoGizmos(bool selected)
        {
            Color c = color;
            c.a = selected ? 1.0f : 0.5f;

            if (showGizmo) Gizmos.DrawIcon(GetPointAtIndex(0), "spline-gizmo");
            
            // Draw spline
            Gizmos.color = c;
            for (int i = 0; i < curveCount; i++)
            {
                Vector3 point1 = GetPoint(i, i + 1, 0);
                float step = 1f / 20f;
                for (float t = step; ; t += step)
                {
                    if (t > 1) t = 1;

                    Vector3 point2 = GetPoint(i, i + 1, t);

                    Gizmos.DrawLine(point1, point2);

                    point1 = point2;

                    if (t == 1) break;
                }
            }
        }

        /// <summary>
        /// Creates an empty spline
        /// </summary>
        public static Spline Create()
        {
            return new GameObject("Spline", typeof(Spline)).GetComponent<Spline>();
        }

        /// <summary>
        /// Gets the total length of the curve.
        /// </summary>
        /// <param name="curveIndex">The curve index.</param>
        /// <returns>The length of the curve.</returns>
        private float _CurveLength(int curveIndex)
        {
            _UpdateLengths();

            return _curveLengths[curveIndex];
        }

        /// <summary>
        /// Generates the curve lengths and arc lengths cache.
        /// </summary>
        /// <remarks>
        /// For each bezier curve of the spline its length is stored in _curveLengths and the arc length
        /// from the spline start to the curve end is stored in _arcLenghts.
        /// </remarks>
        private void _UpdateLengths()
        {
            if (_curveLengths != null && _curveLengths.Length != 0) return;

            _curveLengths = new float[curveCount];
            _arcLengths = new float[curveCount + 1];

            float arcLen = 0;

            for (int i = 0; i < curveCount; i++)
            {
                _curveLengths[i] = _GetCurveLength(i);
                _arcLengths[i] = arcLen;
                arcLen += _curveLengths[i];
            }

            _arcLengths[curveCount] = arcLen;
        }

        private void _UpdateOrientations()
        {
            if (_orientationVectors != null) return;

            // Compute initial orientation
            Vector3 tangent = GetDirection(0);
            Vector3 binormal, upVector;

            if (Vector3.Dot(tangent, Vector3.up) < 0.9f)
            {
                binormal = Vector3.Cross(Vector3.up, tangent);
                upVector = Vector3.Cross(tangent, binormal);
            }
            else if (Vector3.Dot(tangent, Vector3.right) < 0.9f)
            {                
                upVector = Vector3.Cross(tangent, Vector3.right);
            }
            else
            {
                upVector = Vector3.Cross(tangent, Vector3.forward);
            }

            int nSamples = (int)(length / samplesDistance);

            _orientationVectors = new Vector3[nSamples + 1];
            _orientationVectors[0] = upVector;

            for (int i = 1; i < nSamples; i++)
            {

            }
        }

        /// <summary>
        /// Gets the control point.
        /// </summary>
        /// <param name="pointIndex">The control point index.</param>
        /// <returns>The control point vector</returns>
        public Vector3 GetControlPoint(int pointIndex)
        {
            return _points[pointIndex];
        }

        /// <summary>
        /// Sets the value for the control point at index position.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        /// <param name="point">The control point value.</param>
        public void SetControlPoint(int index, Vector3 point)
        {
            if (index % 3 == 0)
            {
                Vector3 delta = point - _points[index];
                if (_loop)
                {
                    if (index == 0)
                    {
                        _points[1] += delta;
                        _points[_points.Length - 2] += delta;
                        _points[_points.Length - 1] = point;
                    }
                    else if (index == _points.Length - 1)
                    {
                        _points[0] = point;
                        _points[1] += delta;
                        _points[index - 1] += delta;
                    }
                    else
                    {
                        _points[index - 1] += delta;
                        _points[index + 1] += delta;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        _points[index - 1] += delta;
                    }
                    if (index + 1 < _points.Length)
                    {
                        _points[index + 1] += delta;
                    }
                }
            }
            _points[index] = point;
            _EnforceMode(index);

            SetDirty();
        }

        public void SetControlPointRaw(int index, Vector3 point)
        {
            _points[index] = point;

            SetDirty();
        }

        /// <summary>
        /// Gets the control point mode.
        /// </summary>
        /// <param name="pointIndex">The control point index.</param>
        /// <returns>The control point mode.</returns>
        public BezierControlPointMode GetControlPointMode(int pointIndex)
        {
            return _modes[(pointIndex + 1) / 3];
        }

        /// <summary>
        /// Sets the control point mode at index position.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        /// <param name="mode">The mode.</param>
        public void SetControlPointMode(int index, BezierControlPointMode mode)
        {
            int modeIndex = (index + 1) / 3;
            _modes[modeIndex] = mode;
            if (_loop)
            {
                if (modeIndex == 0)
                {
                    _modes[_modes.Length - 1] = mode;
                }
                else if (modeIndex == _modes.Length - 1)
                {
                    _modes[0] = mode;
                }
            }
            _EnforceMode(index);

            SetDirty();
        }

        /// <summary>
        /// Updates the handles (control points) of pointIndex according to current point mode.
        /// </summary>
        /// <param name="pointIndex">The point index.</param>
        private void _EnforceMode(int pointIndex)
        {
            int modeIndex = (pointIndex + 1) / 3;
            BezierControlPointMode mode = _modes[modeIndex];
            if (mode == BezierControlPointMode.Corner || !_loop && (modeIndex == 0 || modeIndex == _modes.Length - 1))
            {
                return;
            }

            int middleIndex = modeIndex * 3;
            int fixedIndex, enforcedIndex;
            if (pointIndex <= middleIndex)
            {
                fixedIndex = middleIndex - 1;
                if (fixedIndex < 0)
                {
                    fixedIndex = _points.Length - 2;
                }
                enforcedIndex = middleIndex + 1;
                if (enforcedIndex >= _points.Length)
                {
                    enforcedIndex = 1;
                }
            }
            else
            {
                fixedIndex = middleIndex + 1;
                if (fixedIndex >= _points.Length)
                {
                    fixedIndex = 1;
                }
                enforcedIndex = middleIndex - 1;
                if (enforcedIndex < 0)
                {
                    enforcedIndex = _points.Length - 2;
                }
            }

            Vector3 middle = _points[middleIndex];
            Vector3 enforcedTangent = middle - _points[fixedIndex];
            if (mode == BezierControlPointMode.Aligned)
            {
                enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, _points[enforcedIndex]);
            }
            _points[enforcedIndex] = middle + enforcedTangent;
        }

        /// <summary>
        /// Gets the number of bezier curves in the spline.
        /// </summary>
        /// <value>
        /// The curve count.
        /// </value>
        public int curveCount
        {
            get
            {
                return (_points.Length - 1) / 3;
            }
        }

        /// <summary>
        /// Gets the curve vector at t parameter.
        /// </summary>
        /// <param name="t">The t parameter.</param>
        /// <returns>Curve position vector</returns>
        public Vector3 GetPoint(float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = _points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * curveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }

            return transform.TransformPoint(Bezier.GetPoint(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t));
        }

        /// <summary>
        /// Gets the curve vector for the curve section from startIndex to endIndex at t parameter.
        /// </summary>
        /// <param name="startIndex">The start index of the curve section.</param>
        /// <param name="endIndex">The end index of the curve section.</param>
        /// <param name="t">The t parameter for the curve section, t in [0, 1].</param>
        /// <returns>The curve vector position for the curve section at t.</returns>
        public Vector3 GetPoint(int startIndex, int endIndex, float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = (endIndex - 1) * 3;
            }
            else
            {
                t = Mathf.Clamp01(t) * (endIndex - startIndex);
                i = (int)t;
                t -= i;
                i = (startIndex + i) * 3;
            }

            return transform.TransformPoint(Bezier.GetPoint(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t));
        }

        /// <summary>
        /// Gets the velocity of the curve at parameter t.
        /// </summary>
        /// <param name="t">The t parameter.</param>
        /// <returns>The velocity vector at t position.</returns>
        public Vector3 GetVelocity(float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = _points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * curveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetFirstDerivative(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Gets the velocity of the curve section from start index to end index at t parameter.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="t">The t parameter for the curve section, t in [0, 1].</param>
        /// <returns>The velocity vector.</returns>
        public Vector3 GetVelocity(int startIndex, int endIndex, float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = (endIndex - 1) * 3;
            }
            else
            {
                t = Mathf.Clamp01(t) * (endIndex - startIndex);
                i = (int)t;
                t -= i;
                i = (startIndex + i) * 3;
            }

            return transform.TransformPoint(Bezier.GetFirstDerivative(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Gets the curve acceleration at t.
        /// </summary>
        /// <param name="t">The curve position parameter.</param>
        /// <returns>The acceleration vector</returns>
        public Vector3 GetAcceleration(float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = _points.Length - 4;
            }
            else
            {
                t = Mathf.Clamp01(t) * curveCount;
                i = (int)t;
                t -= i;
                i *= 3;
            }
            return transform.TransformPoint(Bezier.GetSecondDerivative(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Gets the acceleration.
        /// </summary>
        /// <param name="startIndex">The start knot index.</param>
        /// <param name="endIndex">The end knot index.</param>
        /// <param name="t">The curve position parameter.</param>
        /// <returns>The acceleration vector.</returns>
        public Vector3 GetAcceleration(int startIndex, int endIndex, float t)
        {
            int i;

            if (t >= 1f)
            {
                t = 1f;
                i = (endIndex - 1) * 3;
            }
            else
            {
                t = Mathf.Clamp01(t) * (endIndex - startIndex);
                i = (int)t;
                t -= i;
                i = (startIndex + i) * 3;
            }

            return transform.TransformPoint(Bezier.GetSecondDerivative(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Gets the direction normalized vector of the curve at t position.
        /// </summary>
        /// <param name="t">The curve parameter t in [0, 1].</param>
        /// <returns>Normalized direction vector.</returns>
        public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        /// <summary>
        /// Gets the direction normalized vector of the curve section from startIndex to endIndex at t position.
        /// </summary>
        /// <param name="startIndex">The start index of the curve section.</param>
        /// <param name="endIndex">The end index of the curve section.</param>
        /// <param name="t">The t parameter for the curve section, t in [0, 1].</param>
        /// <returns>Normalized direction vector.</returns>
        public Vector3 GetDirection(int startIndex, int endIndex, float t)
        {
            return GetVelocity(startIndex, endIndex, t).normalized;
        }

        /// <summary>
        /// Add a point to the spline. 
        /// </summary>
        /// <remarks>
        /// Depending on the current selected point, the new point will be added between two points, or at the end of the curve.
        /// If there's no point selected, or the selected point is the last point, the new point will be added at the end of the curve, at a distance of 1 in the direction of the last point of the spline.
        /// If the selected point is not the last point, the new point will be created between previous and next point. The bezier handles of the new, of the previous and of the next point will be created/modified in order to maintain the same spline shape.
        /// </remarks>
        /// <param name="selectedIndex">The current selected control point index.</param>
        public void AddPoint(int selectedIndex)
        {
            // Add point to the end of the spline
            if (selectedIndex == -1 || selectedIndex == _points.Length - 1 || selectedIndex % 3 != 0)
            {
                Vector3 direction = Bezier.GetFirstDerivative(_points[curveCount * 3-3], _points[curveCount * 3 - 2], _points[curveCount * 3 - 1], _points[curveCount * 3], 1).normalized;
                Vector3 point = _points[_points.Length - 1];
                Array.Resize(ref _points, _points.Length + 3);

                point += direction;
                _points[_points.Length - 3] = point;
                point += direction;
                _points[_points.Length - 2] = point;
                point += direction;
                _points[_points.Length - 1] = point;

                Array.Resize(ref _modes, _modes.Length + 1);
                _modes[_modes.Length - 1] = BezierControlPointMode.Corner;
                _modes[_modes.Length - 2] = BezierControlPointMode.Aligned;
                _EnforceMode(_points.Length - 4);

                if (_loop)
                {
                    _points[_points.Length - 1] = _points[0];
                    _modes[_modes.Length - 1] = _modes[0];
                    _EnforceMode(0);
                }
            }
            // Add point between two points
            else if (selectedIndex % 3 == 0)
            {
                float s = 0;
                for (int i = 0; i < selectedIndex / 3; i++) s += _CurveLength(i);
                s += _CurveLength(selectedIndex / 3) / 2;

                float t0 = GetArcLengthParameter(s) * curveCount - selectedIndex / 3;
                
                Vector3 point = Bezier.GetPoint(_points[selectedIndex], _points[selectedIndex + 1], _points[selectedIndex + 2], _points[selectedIndex + 3], t0);
                Vector3 direction = Bezier.GetFirstDerivative(_points[selectedIndex], _points[selectedIndex + 1], _points[selectedIndex + 2], _points[selectedIndex + 3], t0);

                Array.Resize(ref _points, _points.Length + 3);
                for (int i = _points.Length - 1; i >= selectedIndex + 5; i--)
                    _points[i] = _points[i - 3];

                int newIndex = selectedIndex + 3;

                _points[newIndex] = point;
                _points[newIndex - 1] = point - direction * t0 / 3;
                _points[newIndex - 2] = _points[newIndex - 3] + (_points[newIndex - 2] - _points[newIndex - 3]) * t0;

                _points[newIndex + 1] = point + direction * (1 - t0) / 3;
                _points[newIndex + 2] = _points[newIndex + 3] + (_points[newIndex + 2] - _points[newIndex + 3]) * (1 - t0);

                Array.Resize(ref _modes, _modes.Length + 1);
                for (int i = _modes.Length - 1; i >= selectedIndex / 3 + 1; i--)
                    _modes[i] = _modes[i - 1];

                _modes[selectedIndex / 3] = BezierControlPointMode.Corner;
                _modes[selectedIndex / 3 + 1] = BezierControlPointMode.Aligned;
                _modes[selectedIndex / 3 + 2] = BezierControlPointMode.Corner;
            }

            SetDirty();
        }

        /// <summary>
        /// Resets this instance with an horizontal segment of length 3.
        /// </summary>
        public void Reset()
        {
            _points = new Vector3[] {
			    new Vector3(1f, 2f, 2f),
			    new Vector3(2f, 2f, 2f),
			    new Vector3(3f, 2f, 2f),
			    new Vector3(4f, 2f, 2f)
		    };

            _modes = new BezierControlPointMode[] {
			    BezierControlPointMode.Smooth,
			    BezierControlPointMode.Smooth
		    };
        }

        /// <summary>
        /// Removes a value from an array and returns the new array.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="index">The index value to remove.</param>
        /// <returns>The new array with the removed value.</returns>
        private static T[] _RemoveAt<T>(T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }

        /// <summary>
        /// Removes a range of values from an array and returns the new array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        /// <returns>The new array with the range of values removed.</returns>
        private static T[] _RemoveRange<T>(T[] source, int start, int end)
        {
            if (end < start) return source;

            T[] dest = new T[source.Length - (end - start) - 1];

            if (start > 0)
                Array.Copy(source, 0, dest, 0, start);

            if (end < source.Length - 1)
                Array.Copy(source, end + 1, dest, start, source.Length - end - 1);

            return dest;
        }

        /// <summary>
        /// Deletes the point.
        /// </summary>
        /// <param name="pointIndex">Index of the point to delete.</param>
        public void DeletePoint(int pointIndex)
        {
            if (curveCount < 2) return;

            if (pointIndex != 0 && pointIndex != _points.Length - 1)
                _points = _RemoveRange(_points, pointIndex - 1, pointIndex + 1);
            else if (pointIndex == 0)
                _points = _RemoveRange(_points, pointIndex, pointIndex + 2);
            else
                _points = _RemoveRange(_points, pointIndex - 2, pointIndex);

            _modes = _RemoveAt(_modes, (pointIndex + 1) / 3);

            SetDirty();
        }

        /// <summary>
        /// Empties the caches of curve lenghts and curve samples for arc length reparameterization.
        /// </summary>
        /// <remarks>
        /// The next time that a curve length will be requested, the arc length cache will be regenerated.
        /// The next time that an approximate arc length reparameterizationi will be requested, the arc length samples will be regenerated.
        /// This function is called automatically whenever a change is made to the spline (point added/deleted/moved, ...)
        /// </remarks>
        public void SetDirty(bool lenghts = true, bool orientations = true)
        {
            if (lenghts)
            {
                _curveLengths = null;
                _tSample = null;
            }

            if (orientations)
            {
                _orientationVectors = null;
            }
        }

        /// <summary>
        /// Update curve parameter progress in [0, 1] at constant speed using curve velocity (first derivative).
        /// </summary>
        /// <remarks>
        /// This function is useful for walking on the curve at fixed speed without arc length reparameterization. It must be called for each frame.
        /// </remarks>
        /// <param name="currProgress">The current curve parameter.</param>
        /// <param name="velocity">The velocity.</param>
        /// <param name="direction">The speed direction.</param>
        /// <returns>The new curve parameter</returns>
        public float GetProgressAtSpeed(float currProgress, float velocity, int direction = 1) 
        {
            float step = 1.0f / curveCount;
            float nextT = 0;

            nextT = currProgress + Time.deltaTime * velocity / GetVelocity(currProgress).magnitude * step * direction;

            if (direction == 1)
            {    
                if (nextT > 1) nextT = 1;

                // Curve crossing
                if (nextT != 1 && currProgress % step > nextT % step)
                {
                    int nextCurve = (int)(nextT / step);
                    float currStep = nextCurve * step;
                    float perc = (currStep - currProgress) / (nextT - currProgress);

                    nextT = currStep + Time.deltaTime * (1 - perc) * velocity / GetVelocity(nextCurve, nextCurve+1, 0).magnitude * step;
                }

                return nextT;
            }
            else
            {
                if (nextT < 0) nextT = 0;

                // Curve crossing
                if (nextT != 0 && currProgress % step < nextT % step)
                {
                    int nextCurve = (int)(currProgress / step);
                    float currStep = nextCurve * step;
                    float perc = (currProgress - currStep) / (currProgress - nextT);

                    nextT = currStep - Time.deltaTime * (1 - perc) * velocity / GetVelocity(nextCurve-1, nextCurve, 1).magnitude * step;
                }

                return nextT;
            }
        }

        /// <summary>
        /// This Delegate is called when the spline walk is complete.
        /// </summary>
        public delegate void WalkCompleteFunction();

        /// <summary>
        /// This delegate is called on each frame during a spline walk.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="progress">The progress.</param>
        public delegate void WalkUpdateFunction(Vector3 position, float progress);

        /// <summary>
        /// This is a coroutine for walking a spline section at constant speed in a defined time duration.
        /// </summary>
        /// <remarks>
        /// Based on the specified duration and the length of the specified spline section it calculates the required speed and then calls WalkAtSpeed.
        /// </remarks>
        /// <param name="startIndex">The start index of the spline section. Use start index greater than end index to invert the direction of motion.</param>
        /// <param name="endIndex">The end index of the spline section. Use end index lesser than start index to invert the direction of motion.</param>
        /// <param name="duration">The desired duration to cover the spline section.</param>
        /// <param name="transform">An optional transform object where to apply motion.</param>
        /// <param name="mode">The SplineWalkerMode: once, loop or ping pong.</param>
        /// <param name="lookForward">If set to <c>true</c> the transform rotation is set to curve direction.</param>
        /// <param name="completeFunction">This function will be called upon motion completion (only form SplineWalkerMode.Once).</param>
        /// <param name="updateFunction">This function will be called every frame.</param>
        /// <seealso cref="WalkAtSpeed"/>
        public IEnumerator WalkDuration(
            int startIndex, int endIndex,
            float duration, Transform transform, SplineWalkerMode mode = SplineWalkerMode.Once, Boolean lookForward = true,
            WalkCompleteFunction completeFunction = null, WalkUpdateFunction updateFunction = null)
        {
            _UpdateLengths();

            // Calculate length from startPoint to endPoint
            int a = startIndex, b = endIndex;
            if (a > b)
            {
                int tmp = a;
                a = b;
                b = tmp;
            }

            float l = _arcLengths[b] - _arcLengths[a];

            return WalkAtSpeed(startIndex, endIndex, l / duration, transform, mode, lookForward, completeFunction, updateFunction);
        }

        /// <summary>
        /// This is a coroutine for walking a spline section at constant speed. 
        /// </summary>
        /// <remarks>
        /// It uses the function GetProgressAtSpeed in order to update the curve parameter at each frame mantaining a constant speed.
        /// </remarks>
        /// <param name="startIndex">The start index of the spline section. Use start index greater than end index to invert the direction of motion.</param>
        /// <param name="endIndex">The end index of the spline section. Use end index lesser than start index to invert the direction of motion.</param>
        /// <param name="velocity">The desired velocity.</param>
        /// <param name="transform">An optional transform object where to apply motion.</param>
        /// <param name="mode">The SplineWalkerMode: once, loop or ping pong.</param>
        /// <param name="lookForward">If set to <c>true</c> the transform rotation is set to curve direction.</param>
        /// <param name="completeFunction">This function will be called upon motion completion (only form SplineWalkerMode.Once).</param>
        /// <param name="updateFunction">This function will be called every frame.</param>
        /// <seealso cref="WalkDuration"/>
        public IEnumerator WalkAtSpeed(
            int startIndex, int endIndex,
            float velocity, Transform transform = null, SplineWalkerMode mode = SplineWalkerMode.Once, Boolean lookForward = true, 
            WalkCompleteFunction completeFunction = null, WalkUpdateFunction updateFunction = null)
        {
            float progress = startIndex / (float)curveCount;
            float limit = endIndex / (float)curveCount;

            yield return GetPoint(progress);

            int direction = endIndex > startIndex ? 1 : -1;

            while (true)
            {
                progress = GetProgressAtSpeed(progress, velocity, direction);

                if ( (direction ==  1 && progress >= limit) ||
                     (direction == -1 && progress <= limit) )
                {
                    if(mode == SplineWalkerMode.Once)
                        break;
                    else if (mode == SplineWalkerMode.PingPong)
                    {
                        direction *= -1;

                        if (direction * (endIndex - startIndex) > 0) 
                            limit = endIndex / (float)curveCount;
                        else
                            limit = startIndex / (float)curveCount;

                        continue;
                    }
                    else if (mode == SplineWalkerMode.Loop)
                    {
                        progress -= limit - startIndex / (float)curveCount;

                        continue;
                    }
                }

                Vector3 position = GetPoint(progress);

                if (transform != null)
                {
                    transform.position = position;
                    if (lookForward) transform.LookAt(transform.position + GetDirection(progress));
                }

                if (updateFunction != null) updateFunction(position, progress);

                yield return null;
            }

            if (completeFunction != null) completeFunction();
        }

        /// <summary>
        /// Gets the curve position at the index point.
        /// </summary>
        /// <param name="pointIndex">The index of the control point.</param>
        /// <returns>The curve vector position.</returns>
        /// <seealso cref="GetPoint(float)"/>
        /// <seealso cref="GetPoint(int,int,float)"/>
        public Vector3 GetPointAtIndex(int pointIndex)
        {
            return transform.TransformPoint(_points[pointIndex * 3]);
        }

        /// <summary>
        /// Gets the length of the get curve.
        /// </summary>
        /// <param name="curveIndex">The curve index.</param>
        /// <returns>The curve length.</returns>
        private float _GetCurveLength(int curveIndex)
        {
            int i = curveIndex * 3;
            return Bezier.Integrate(_points[i], _points[i + 1], _points[i + 2], _points[i + 3], 0, 1);
        }

        /// <summary>
        /// Gets the arc length of the arc in the interval [t0, t1].         
        /// </summary>
        /// <remarks>
        /// This function calculates an integration for the first and the last curve pieces of the arc. 
        /// Then adds the precalculated arc lengths for the curves in the middle.
        /// </remarks>
        /// <param name="t0">The start parameter in [0, 1].</param>
        /// <param name="t1">The end parameter in [0, 1].</param>
        /// <returns>The arc length.</returns>
        public float GetArcLength(float t0, float t1)
        {
            _UpdateLengths();

            if (t0 == t1) return 0;

            if (t0 > t1)
            {
                float tmp = t0;
                t0 = t1;
                t1 = tmp;
            }

            int curve0 = (int)(t0 * curveCount);
            int curve1 = t1 == 1 ? curveCount - 1 : (int)(t1 * curveCount);

            t0 = t0 * curveCount - curve0;
            t1 = t1 * curveCount - curve1;

            curve0 *= 3;
            curve1 *= 3;

            if (curve0 == curve1)
                return Bezier.Integrate(_points[curve0], _points[curve0 + 1], _points[curve0 + 2], _points[curve0 + 3], t0, t1);
            else
            {
                float result = 0;

                result += Bezier.Integrate(_points[curve0], _points[curve0 + 1], _points[curve0 + 2], _points[curve0 + 3], t0, 1);

                for (int i = curve0 / 3 + 1; i < curve1 / 3; i++)
                    result += _curveLengths[i];

                result += Bezier.Integrate(_points[curve1], _points[curve1 + 1], _points[curve1 + 2], _points[curve1 + 3], 0, t1);

                return result;
            }
        }

        /// <summary>
        /// Gets the arc length between two knots.         
        /// </summary>
        /// <param name="startKnot">The start knot.</param>
        /// <param name="endKnot">The end knot.</param>
        /// <returns>The arc length.</returns>
        public float GetArcLengthBetweenKnots(int startKnot, int endKnot)
        {
            _UpdateLengths();

            if (startKnot == endKnot) return 0;

            if (startKnot > endKnot)
            {
                int tmp = startKnot;
                startKnot = endKnot;
                endKnot = tmp;
            }

            float result = 0;

            for (int i = startKnot; i < endKnot; i++)
                result += _curveLengths[i];

            return result;
        }

        /// <summary>
        /// Gets the curve parameter corresponding to the requested s arc length.
        /// </summary>
        /// <remarks>
        /// This method performs an accurate arc length reparameterization of the curve. It is computational expensive. Use <see cref="GetArcLengthParameterApproximate"/> for real-time.
        /// It uses numerical integration and root-finding algorithms in order to find the parameter value that gives an arc length of s.
        /// </remarks>
        /// <param name="s">The desired spline arc length 0 &lt;= s &lt;= length.</param>
        /// <param name="epsilon">The maximum error ds for the computed parameter.</param>
        /// <returns>The curve parameter that gives an arc length equal to s.</returns>
        /// <seealso cref="GetArcLengthParameterApproximate"/>
        public float GetArcLengthParameter(float s, float epsilon = 0.0001f)
        {
            _UpdateLengths(); // make sure to calculate lengths

            if (s <= 0) return 0;
            if (s >= length) return 1;

            // find the curve index containing s arc length
            int curveIndex;
            for (curveIndex = 0; curveIndex < curveCount - 1; curveIndex++)
                if (s < _arcLengths[curveIndex + 1])
                    break;

            float length0 = s - _arcLengths[curveIndex]; // the arc length portion inside the curve
            float t0 = length0 / _curveLengths[curveIndex]; // the candidate t parameter inside the curve
            
            int p0 = curveIndex * 3, p1 = p0 + 1, p2 = p1 + 1, p3 = p2 + 1;
            
            return (
                curveIndex + 
                Bezier.GetArcLengthParameter(
                    _points[p0], _points[p1], _points[p2], _points[p3], 
                    length0, t0, epsilon) ) / curveCount;
        }

        /// <summary>
        /// Inits an array of samples (s, t, s-t slope) to be used in the approximate arc length 
        /// reparameterization function: GetArcLengthParameterApproximate.
        /// </summary>
        private void _InitSamples()
        {
            if (_tSample != null && _tSample.Length != 0) return; // Skip if already done.

            _UpdateLengths(); // Make sure we have curve lengths values

            int nSamples = (int) (length / samplesDistance);

            // Allocating arrays. We need nSamples plus a sample for index 0 and one for index nSamples + 1.
            _tSample = new float[nSamples + 2];
            _tsSlope = new float[nSamples + 2];

            // First samples
            _tSample[0] = 0;
            _tsSlope[0] = 0;

            for (int i = 1; i <= nSamples + 1; i++)
            {                
                _tSample[i] = GetArcLengthParameter(i * samplesDistance);
                _tsSlope[i] = (_tSample[i] - _tSample[i - 1]) / samplesDistance;
            }
        }

        /// <summary>
        /// Gets the curve parameter corresponding to the requested s arc length.
        /// </summary>
        /// <remarks>
        /// This method performs an arc length reparameterization of the curve. It is an approximate computation, good for real time use. 
        /// It is based on precomputed samples of s and t obtained with the accurate function GetArcLengthParameter. 
        /// This function interpolates between samplad values to obtain an approximation of the arc length parameter.
        /// </remarks>
        /// <param name="s">The desired spline arc length 0 &lt;= s &lt;= length.</param>
        /// <returns>The curve parameter that gives an arc length near to s.</returns>
        /// /// <seealso cref="GetArcLengthParameter"/>
        public float GetArcLengthParameterApproximate(float s)
        {
            _InitSamples(); // Make sure that samples have been calculated

            if (s <= 0) { return 0; }
            if (s >= length) { return 1; }

            int sampleIndex = (int)(s / samplesDistance);

            // Return linear interpolation between sampleIndex and sampleIndex + 1 
            return _tSample[sampleIndex] + _tsSlope[sampleIndex + 1] * (s % samplesDistance);
        }

        public Vector3[] GetSubdivision(float s0, float s1)
        {
            Vector3[] result = new Vector3[4];

            float t0 = GetArcLengthParameter(s0);
            float t1 = GetArcLengthParameter(s1);

            Vector3 v0 = GetVelocity(t0);
            Vector3 v1 = GetVelocity(t1);

            result[0] = GetPoint(t0);
            result[3] = GetPoint(t1);

            if (Mathf.Floor(t0 * curveCount) == Mathf.Floor(t1 * curveCount))
            {
                result[1] = result[0] + v0 * (t1 - t0) / 3 * curveCount;
                result[2] = result[3] - v1 * (t1 - t0) / 3 * curveCount;
            }
            else
            {
                int kn0 = (int) Mathf.Floor(t0 * curveCount);
                int kn1 = (int) Mathf.Floor(t1 * curveCount);

                float l0 = GetArcLengthBetweenKnots(kn0, kn0 + 1);
                float l1 = GetArcLengthBetweenKnots(kn1, kn1 + 1);

                result[1] = result[0] + v0 / 3 * (s1 - s0) / l0;
                result[2] = result[3] - v1 / 3 * (s1 - s0) / l1;
            }

            return result;
        }

        public Vector3[] GetSubdivisionB(float s0, float s1)
        {
            Vector3[] result = new Vector3[4];

            float t0 = GetArcLengthParameter(s0);
            float t1 = GetArcLengthParameter(s1);

            Vector3 d0 = GetVelocity(t0).normalized;
            Vector3 d1 = -GetVelocity(t1).normalized;

            Vector3 p0 = GetPoint(t0);
            Vector3 p3 = GetPoint(t1) - p0;

            float l = s1 - s0;

            Vector3 pm = GetPoint(GetArcLengthParameter((s0 + s1) / 2)) - p0;

            float k0 = (8 * pm.y - 4 * p3.y - 2 * d1.y * l) / 3 / (d0 - d1).y;
            //float k0 = (8 * pm.x - 4 * p3.x - 2 * d1.x * l) / ((d0.x - d1.x) * 3);
            //float k0 = (8 * pm.z - 4 * p3.z - 2 * d1.z * l) / 3 / (d0 - d1).z;

            float k1 = 2 * l / 3 - k0;

            result[0] = p0;
            result[1] = p0 + d0 * k0;
            result[3] = p3 + p0;
            result[2] = result[3] + d1 * k1;

            Debug.Log(d0 + " " + d1 + " " + p0 + " " + p3 + " " + pm + " " + k0 + " " + k1);

            return result;
        }
    }
}