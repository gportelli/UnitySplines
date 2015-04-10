using UnityEngine;

namespace AClockworkBerry.Splines
{
    /// <summary>
    /// Script for moving a game object along a spline.
    /// </summary>
    public class SplineWalker : MonoBehaviour
    {
        /// <summary>
        /// The spline.
        /// </summary>
        public Spline spline;

        /// <summary>
        /// The walk duration.
        /// </summary>
        public float duration = 1;

        /// <summary>
        /// Changes the object orientation to meet curve direction.
        /// </summary>
        public bool lookForward = true;


        /// <summary>
        /// The walk mode (Once, Loop, Pin Pong).
        /// </summary>
        public SplineWalkerMode mode = SplineWalkerMode.Once;

        /// <summary>
        /// The walk progress (curve parameter in [0, 1]).
        /// </summary>
        private float _progress;

        /// <summary>
        /// Flag used for ping pong mode.
        /// </summary>
        private bool _goingForward = true;

        void Update()
        {
            if (_goingForward)
            {
                _progress = spline.GetProgressAtSpeed(_progress, spline.length / duration);
                if (_progress >= 1f)
                {
                    if (mode == SplineWalkerMode.Once)
                    {
                        _progress = 1f;
                    }
                    else if (mode == SplineWalkerMode.Loop)
                    {
                        _progress -= 1f;
                    }
                    else
                    {
                        _progress = 2f - _progress;
                        _goingForward = false;
                    }
                }
            }
            else
            {
                _progress = spline.GetProgressAtSpeed(_progress, spline.length / duration, -1);
                if (_progress <= 0f)
                {
                    _progress = -_progress;
                    _goingForward = true;
                }
            }

            Vector3 position = spline.GetPoint(_progress);
            transform.localPosition = position;
            if (lookForward)
            {
                transform.LookAt(position + spline.GetDirection(_progress));
            }
        }
    }
}