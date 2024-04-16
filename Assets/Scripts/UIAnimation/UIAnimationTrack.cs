using UnityEngine;
using TimeLineLayoutBase;
using System.Collections.Generic;

namespace UIAnimationTimeLine
{
    
    [System.Serializable]
    public class UIAnimationTrack : ITrack
    {
        public eTrackType TrackType => _trackType;

        public int Count => _listClips.Count;

        public IClip this[int lIndex] => _listClips[lIndex];

        public Transform Target => _target;

        [SerializeField]
        eTrackType _trackType;

        [SerializeField]
        List<UIAnimationClip> _listClips = new List<UIAnimationClip>();

        [SerializeField]
        Transform _target;

        [SerializeField]
        UnityEngine.UI.Graphic _graphicTarget;

        [SerializeField]
        CanvasGroup _alphaTarget;
        RectTransform _targetRT 
        {
            get
            {
                if (_targetRectTransform == null)
                    _targetRectTransform = _target.GetComponent<RectTransform>();
                return _targetRectTransform;
            }
        }

        RectTransform _targetRectTransform;

        System.Action<Vector4, Vector4, float> _updateEvent;
        System.Action<Vector4, Vector4, float> _onUpdateEvent
        {
            get
            {
                if (_updateEvent == null)
                {
                    switch (_trackType)
                    {
                        case eTrackType.Active:
                            _updateEvent = _Vector4ToActive;
                            break;
                        case eTrackType.Position:
                            _updateEvent = _Vector4ToPosition;
                            break;
                        case eTrackType.Position_2D_Bezir:
                            _updateEvent = _Vector4ToPosition_2DBezir;
                            break;
                        case eTrackType.Rotation:
                            _updateEvent = _Vector4ToQuaternion;
                            break;
                        case eTrackType.Scale:
                            _updateEvent = _Vector4ToScale;
                            break;
                        case eTrackType.Color:
                            _updateEvent = _Vector4ToColor;
                            break;
                        case eTrackType.Alpha:
                            _updateEvent = _Vector4ToAlpha;
                            break;
                    }
                }
                return _updateEvent;
            }
        }

        UIAnimationClip _lastClip;

        public UIAnimationTrack(eTrackType pType, Transform pTarget)
        {
            _trackType = pType;
            SetTarget(pTarget);
        }

        public void SetTarget(Transform pTarget)
        {
            _target = pTarget;

            switch (TrackType)
            {
                case eTrackType.Color:
                    _graphicTarget = pTarget.GetComponent<UnityEngine.UI.Graphic>();
                    break;
                case eTrackType.Alpha:
                    _alphaTarget = pTarget.GetComponent<CanvasGroup>();

                    if (_alphaTarget == null)
                        _graphicTarget = pTarget.GetComponent<UnityEngine.UI.Graphic>();
                    break;
                case eTrackType.Position:
                    _targetRectTransform = _target.GetComponent<RectTransform>();
                    break;
            }
        }

        public void PlayTrack(float pCurrentTime)
        {
            UIAnimationClip lClip = GetClip(pCurrentTime);
            if (lClip != null)
            {
                lClip.SetClip(pCurrentTime, _onUpdateEvent);
                _lastClip = lClip;
            }
            else if (_lastClip != null)
            {
                _lastClip.SetClip(pCurrentTime, _onUpdateEvent);
                _lastClip = null;
            }
        }

        public void StopTrack(eStopMode pPlayMode)
        {
            if (pPlayMode == eStopMode.FirstFrame)
            {
                if (_listClips.Count > 0)
                    _listClips[0].SetClip(0, _onUpdateEvent);
            }
            else if (pPlayMode == eStopMode.LastFrame)
            {
                if (_listClips.Count > 0)
                {
                    var lClip = _listClips[_listClips.Count - 1];
                    lClip.SetClip(lClip.EndTime, _onUpdateEvent);
                }
            }
        }

        public void SetTrackTime(float pCurrentTime)
        {
            UIAnimationClip lClip = GetClip(pCurrentTime);
            if (lClip != null)
            {
                lClip.SetClip(pCurrentTime, _onUpdateEvent);
            }
            else
            {
                if (_listClips.Count > 0)
                {
                    for (int lIndex = 0; lIndex < _listClips.Count; lIndex++)
                    {
                        if (lClip != null)
                        {
                            var lCurClip = _listClips[lIndex];
                            var lCurStartTimeDistance = Mathf.Abs(pCurrentTime - lCurClip.StartTime);
                            var lCurNextEndTimeDistance = Mathf.Abs(pCurrentTime - lCurClip.EndTime);
                            var lCurMin = Mathf.Min(lCurStartTimeDistance, lCurNextEndTimeDistance);

                            var lPrevStartTimeDistance = Mathf.Abs(pCurrentTime - lClip.StartTime);
                            var lPrevEndTimeDistance = Mathf.Abs(pCurrentTime - lClip.EndTime);
                            var lPrevMin = Mathf.Min(lPrevStartTimeDistance, lPrevEndTimeDistance);

                            if (lCurMin < lPrevMin)
                                lClip = lCurClip;
                        }
                        else
                        {
                            lClip = _listClips[lIndex];
                        }
                    }
                    lClip.SetClip(pCurrentTime, _onUpdateEvent);
                }
            }
        }

        public void AddClip(float pStartTime)
        {
            var lCreateClip = new UIAnimationClip(pStartTime);

            var lPrevClip = GetPrevClip(lCreateClip);
            if (lPrevClip != null)
                lCreateClip.SetStartValue(lPrevClip.endValue);
            else
                lCreateClip.SetStartValue(_GetInitValue(_trackType));

            var lNextClip = GetNextClip(lCreateClip);
            if (lNextClip != null)
            {
                lCreateClip.SetEndValue(lNextClip.startValue);
                _listClips.Insert(_listClips.IndexOf(lNextClip), lCreateClip);
            }
            else
            {
                lCreateClip.SetEndValue(_GetInitValue(_trackType));
                _listClips.Add(lCreateClip);
            }
        }

        public void RemoveClip(float second)
        {
            RemoveClip(GetClip(second));
        }

        public void RemoveClip(UIAnimationClip pClip)
        {
            _listClips.Remove(pClip);
        }

        public UIAnimationClip GetClip(float pTime)
        {
            int lCount = _listClips.Count;
            for (int i = 0; i < lCount; ++i)
            {
                if (pTime >= _listClips[i].StartTime && pTime <= _listClips[i].EndTime)
                    return _listClips[i];
            }
            return null;
        }

        public UIAnimationClip GetPrevClip(UIAnimationClip pClip)
        {
            UIAnimationClip lPrevClip = null;
            for (int lIndex = 0; lIndex < _listClips.Count; lIndex++)
            {
                var lCurClip = _listClips[lIndex];
                if (lCurClip.EndTime <= pClip.StartTime)
                    lPrevClip = lCurClip;
                else
                    break;
            }
            return lPrevClip;
        }

        public UIAnimationClip GetNextClip(UIAnimationClip pClip)
        {
            UIAnimationClip lNextClip = null;
            for (int lIndex = 0; lIndex < _listClips.Count; lIndex++)
            {
                var lCurClip = _listClips[lIndex];
                if (lCurClip.StartTime >= pClip.EndTime)
                {
                    lNextClip = lCurClip;
                    break;
                }
            }
            return lNextClip;
        }

        public int FindClipIndex(UIAnimationClip pTarget)
        {
            return _listClips.IndexOf(pTarget);
        }

        Vector4 _GetInitValue(eTrackType pType)
        {
            switch (pType)
            {
                case eTrackType.Active:
                    return new Vector4(1, 0);
                case eTrackType.Position:
                    if (_targetRT != null)
                        return _targetRT.anchoredPosition;
                    else
                        return _target.localPosition;
                case eTrackType.Rotation:
                    return _target.localRotation.eulerAngles;
                case eTrackType.Scale:
                    return _target.localScale;
                case eTrackType.Color:
                    return _graphicTarget.color;
                case eTrackType.Alpha:
                    if (_alphaTarget == null)
                        return new Vector4(0, 0, 0, _graphicTarget.color.a);
                    else
                        return new Vector4(0, 0, 0, _alphaTarget.alpha);
                default:
                    return Vector4.zero;
            }
        }

        void _Vector4ToActive(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            _target.gameObject.SetActive(Vector4.Lerp(pStart, pEnd, pFactor).x > 0);
        }

        void _Vector4ToPosition(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            if (_targetRT != null)
                _targetRT.anchoredPosition = Vector3.Lerp(pStart, pEnd, pFactor);
            else
                _target.localPosition = Vector3.Lerp(pStart, pEnd, pFactor);
        }
        
        void _Vector4ToPosition_2DBezir(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            Vector3 lStart = new Vector3(pStart.x, pStart.y, 0);
            Vector3 lControlPoint_First = new Vector3(pStart.z, pEnd.z);
            Vector3 lControlPoint_Second = new Vector3(pStart.w, pEnd.w);
            Vector3 lEnd = new Vector3(pEnd.x, pEnd.y, 0);
            _target.localPosition = UIAnimationUtil.BezierCurveEvaluate(lStart, lControlPoint_First, lControlPoint_Second, lEnd, pFactor);
        }
        void _Vector4ToQuaternion(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            if (_target != null)
                _target.localRotation = Quaternion.Euler(Vector3.Lerp(pStart, pEnd, pFactor));
        }

        void _Vector4ToScale(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            _target.localScale = Vector3.Lerp(pStart, pEnd, pFactor);
        }
        void _Vector4ToColor(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            _graphicTarget.color = Vector4.Lerp(pStart, pEnd, pFactor);
        }
        void _Vector4ToAlpha(Vector4 pStart, Vector4 pEnd, float pFactor)
        {
            if (_alphaTarget != null)
            {
                _alphaTarget.alpha = Mathf.Lerp(pStart.w, pEnd.w, pFactor);
            }
            else if (_graphicTarget != null)
            {
                var lAlphaColor = new Color(_graphicTarget.color.r, _graphicTarget.color.g, _graphicTarget.color.b, Mathf.Lerp(pStart.w, pEnd.w, pFactor));
                _graphicTarget.color = lAlphaColor;
            }
        }
        
        public static bool IsAvailableTrackType(eTrackType pType, GameObject pTarget)
        {
            if (pType == eTrackType.Alpha)
            {
                CanvasGroup lCheck = null;
                if (pTarget.TryGetComponent(out lCheck) == false)
                {
                    UnityEngine.UI.Graphic lGraphicCheck = null;
                    return pTarget.TryGetComponent(out lGraphicCheck);
                }
                else
                    return true;
            }

            if (pType == eTrackType.Color)
            {
                UnityEngine.UI.Graphic lCheck = null;
                return pTarget.TryGetComponent(out lCheck);
            }

            return true;
        }

        public IClip GetPrevClip(IClip pTarget)
        {
            return GetPrevClip(pTarget as UIAnimationClip);
        }

        public IClip GetNextClip(IClip pTarget)
        {
            return GetNextClip(pTarget as UIAnimationClip);
        }

        IClip ITrack.GetClip(float pTime)
        {
            return GetClip(pTime);
        }
    }
}
