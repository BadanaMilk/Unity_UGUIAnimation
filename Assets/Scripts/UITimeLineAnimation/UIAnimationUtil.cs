using System.Collections.Generic;
using UnityEngine;

namespace UITimeLineAnimation
{
    public class UIAnimationUtil
    {
        public const float minIntervalTime = 0.01f;

        public static Vector3 BezierCurveEvaluate(Vector3 pStart, Vector3 pControl1, Vector3 pControl2, Vector3 pEnd,
            float pTime)
        {
            float lOneMinusT = 1f - pTime;

            //Layer 1
            Vector3 lQ = lOneMinusT * pStart + pTime * pControl1;
            Vector3 lR = lOneMinusT * pControl1 + pTime * pControl2;
            Vector3 lS = lOneMinusT * pControl2 + pTime * pEnd;

            //Layer 2
            Vector3 lP = lOneMinusT * lQ + pTime * lR;
            Vector3 lT = lOneMinusT * lR + pTime * lS;

            //Final interpolated position
            Vector3 lU = lOneMinusT * lP + pTime * lT;
            return lU;
        }

        public static Vector3 BezierCurveEvaluate(Vector3 pStart, Vector3 pControl, Vector3 pEnd, float pTime)
        {
            var lP0 = Vector3.Lerp(pStart, pControl, pTime);
            var lP1 = Vector3.Lerp(pControl, pEnd, pTime);

            var lPt = Vector3.Lerp(lP0, lP1, pTime);

            return lPt;
        }

        private static Vector3 GetBezierPoint(float pCurrentTime, List<Vector3> pBezierPoint)
        {
            var lCurveCount = (pBezierPoint.Count - 1) / 3;
            int lIndex;
            if (pCurrentTime >= 1f)
            {
                pCurrentTime = 1f;
                lIndex = pBezierPoint.Count - 4;
            }
            else
            {
                pCurrentTime = Mathf.Clamp01(pCurrentTime) * lCurveCount;
                lIndex = (int)pCurrentTime;
                pCurrentTime -= lIndex;
                lIndex *= 3;
            }

            return BezierCurveEvaluate(pBezierPoint[lIndex], pBezierPoint[lIndex + 1], pBezierPoint[lIndex + 2],
                pBezierPoint[lIndex + 3], pCurrentTime);
        }
    }
}