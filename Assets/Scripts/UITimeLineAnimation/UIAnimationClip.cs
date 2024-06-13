using TimeLineInterface;
using UnityEngine;

namespace UITimeLineAnimation
{
    [System.Serializable]
    public class UIAnimationClip : IClip
    {
        public float Duration { get { return _duration; } }
        public float StartTime { get { return _startTime; } }
        public float EndTime { get { return _endTime; } }

        public float _duration { get { return _endTime - _startTime; } }
        public Vector4 startValue { get { return _startValue; } }
        public Vector4 endValue { get { return _endValue; } }

        public AnimationCurve curveData { get { return _curveData; } }

        [SerializeField]
        Vector4 _startValue;
        [SerializeField]
        Vector4 _endValue;

        [SerializeField]
        float _startTime;
        [SerializeField]
        float _endTime;

        [SerializeField]
        AnimationCurve _curveData = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField]
        UnityEngine.Events.UnityEvent _onFinishEvents;

        public UIAnimationClip(float pStartTime)
        {
            _startTime = pStartTime;
            _endTime = _startTime + UIAnimationUtil.minIntervalTime;
        }

        public void SetClip(float pCurrentTime, System.Action<Vector4, Vector4, float> pUpdateEvent)
        {
            pCurrentTime = Mathf.Clamp(pCurrentTime, _startTime, _endTime);

            var lFactor = _curveData.Evaluate((pCurrentTime - _startTime) / _duration);
            pUpdateEvent(_startValue, _endValue, lFactor);

            if (lFactor == 1f)
                _onFinishEvents.Invoke();
        }

        public bool IsOnTimeClip(float pCurrentTime)
        {
            return _startTime <= pCurrentTime && _endTime >= pCurrentTime;
        }

        public void SetStartValue(Vector4 pValue) { _startValue = pValue; }
        public void SetEndValue(Vector4 pValue) { _endValue = pValue; }

        public void SetStartTime(float pValue) 
        {
            if (pValue >= _endTime)
                return;
            _startTime = pValue;
        }
        public void SetEndTime(float pValue)
        {
            if (pValue <= _startTime)
                return;
            _endTime = pValue; 
        }

        public void SetCurveData(AnimationCurve pCurve)
        {
            _curveData = pCurve;
        }
    }
}
