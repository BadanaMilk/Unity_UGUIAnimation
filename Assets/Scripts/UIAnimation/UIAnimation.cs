using System.Collections.Generic;
using UnityEngine;
using TimeLineLayoutBase;
using UnityEngine.Serialization;

namespace UIAnimationTimeLine
{
    [System.Serializable]
    public enum eTrackType : byte
    { 
        Active,
        Position,
        Rotation,
        Scale,
        Color,
        Alpha,
        Position_2D_Bezir,
        End,
    }

    [System.Serializable]
    public enum ePlayMode : byte
    {
        Forward,
        Forward_CurrentAt,
        Backward,
        Backward_CurrentAt,
    }

    [System.Serializable]
    public enum eStopMode
    {
        LastFrame,
        Pause,
        FirstFrame,
    }

    [System.Serializable]
    public enum eWrapMode
    {
        Once,
        Loop,
        PingPong
    }

    public class UIAnimationUtil
    {
        public const float minIntervalTime = 0.01f;
    }

    public class UIAnimation : MonoBehaviour, IDrawerTimeLine
    {
        public float CurrentTime => _currentTime;

        public float Length
        {
            get => _duration;
            set => _duration = value;
        }

        public bool AutoPlay
        {
            get => _autoPlay;
            set => _autoPlay = value;
        }
        
        public bool IsPlaying => _isPlaying;

        public eWrapMode WrapMode
        {
            get => _wrapMode;
            set => _wrapMode = value;
        }

        public ePlayMode PlayMode
        {
            get => _playMode;
            set => _playMode = value;
        }

        public int Count => _listAniGroup.Count;
        public UIAnimationGroup this[int pIndex] => _listAniGroup[pIndex];

        public float TimeScale { get; set; } = 1.0f;

        [HideInInspector][SerializeField]
        eWrapMode _wrapMode = eWrapMode.Once;
        [HideInInspector][SerializeField]
        ePlayMode _playMode = ePlayMode.Forward;

        [HideInInspector][SerializeField]
        bool _autoPlay = false;
        [HideInInspector][SerializeField]
        float _duration = 1f;

        [HideInInspector] [SerializeField]
        List<UIAnimationGroup> _listAniGroup = new List<UIAnimationGroup>();

        [SerializeField]
        bool _isUsingUnscaledDeltaTime = false;
        
        float                _currentTime = 0f;
        bool                 _isPlaying   = false;
        System.Action<float> _calCurrentTime;
        System.Action        _onFinishedEvent;
        ePlayMode    _currentPlayMode;

        float _timeSinceLastUpdate
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    return Time.realtimeSinceStartup - _editModeLastUpdate;
                }
#endif
                return (_isUsingUnscaledDeltaTime 
                           ? Time.unscaledDeltaTime 
                           : Time.deltaTime)
                       * TimeScale;
            }
        }

#if UNITY_EDITOR
        float _editModeLastUpdate;
#endif

        private void OnEnable()
        {
            if (_autoPlay)
            {
                Play(_playMode);
            }
        }

        private void Update()
        {
            _DoUpdate(_timeSinceLastUpdate);
        }
        
        public void SetUnscaledTime(bool pActive)
        {
            _isUsingUnscaledDeltaTime = pActive;
        }

        public void Play(ePlayMode pPlayMode = ePlayMode.Forward, System.Action pOnFinishedEvent = null)
            => Play(pPlayMode, pOnFinishedEvent, _isUsingUnscaledDeltaTime);
        
        public void Play(ePlayMode pPlayMode, System.Action pOnFinishedEvent, bool pIsUsingUnscaledDeltaTime)
        {
#if UNITY_EDITOR
            _editModeLastUpdate = Time.realtimeSinceStartup;
#endif
            _isUsingUnscaledDeltaTime = pIsUsingUnscaledDeltaTime;

            switch (pPlayMode)
            {
                case ePlayMode.Forward:
                    _currentPlayMode = pPlayMode;
                    _currentTime = 0;
                    _calCurrentTime = _CalForwardTime;
                    break;
                case ePlayMode.Forward_CurrentAt:
                    _currentPlayMode = ePlayMode.Forward;
                    _calCurrentTime = _CalForwardTime;
                    break;
                case ePlayMode.Backward:
                    _currentPlayMode = pPlayMode;
                    _currentTime = _duration;
                    _calCurrentTime = _CalBackwardTime;
                    break;
                case ePlayMode.Backward_CurrentAt:
                    _currentPlayMode = ePlayMode.Backward;
                    _calCurrentTime = _CalBackwardTime;
                    break;
            }
            _onFinishedEvent = pOnFinishedEvent;
            _SetTimeLine(_currentTime);
            _isPlaying = true;
        }

        public void PlayOnTime(ePlayMode pPlayMode, float pTime, System.Action pOnFinishedEvent = null, bool pIsUsingUnscaledDeltaTime = true)
        {
            _isUsingUnscaledDeltaTime = pIsUsingUnscaledDeltaTime;
            _currentTime = pTime > _duration ? _duration : pTime;
            switch (pPlayMode)
            {
                case ePlayMode.Forward:
                case ePlayMode.Forward_CurrentAt:
                    _currentPlayMode = ePlayMode.Forward;
                    _calCurrentTime = _CalForwardTime;
                    break;
                case ePlayMode.Backward:
                case ePlayMode.Backward_CurrentAt:
                    _currentPlayMode = ePlayMode.Backward;
                    _calCurrentTime = _CalBackwardTime;
                    break;
            }
            _onFinishedEvent = pOnFinishedEvent;
            _SetTimeLine(_currentTime);
            _isPlaying = true;
        }

        public void Stop(eStopMode pMode = eStopMode.Pause)
        {
            _isPlaying = false;

            for (int lIndex = 0; lIndex < _listAniGroup.Count; lIndex++)
            {
                _listAniGroup[lIndex].StopGroup(pMode);
            }

            switch (pMode)
            {
                case eStopMode.LastFrame:
                    _currentTime = _duration;
                    break;
                case eStopMode.Pause:
                    break;
                case eStopMode.FirstFrame:
                    _currentTime = 0;
                    break;
                default:
                    break;
            }
        }

        public void Rewind()
        {
            _SetTimeLine(0f);
        }

        public void Sampling(float pTime)
        {
            for (int lIndex = 0; lIndex < _listAniGroup.Count; lIndex++)
            {
                _listAniGroup[lIndex].SetTime(pTime);
            }
        }

        public UIAnimationGroup GetGroupData(GameObject pTarget)
        {
            return _listAniGroup.Find(lItem => lItem.target == pTarget);
        }

        void _SetTimeLine(float pTime)
        {
            _currentTime = pTime;
            for (int lIndex = 0; lIndex < _listAniGroup.Count; lIndex++)
            {
                _listAniGroup[lIndex].SetTime(_currentTime);
            }
        }

        void _PlayTimeLine(float pTime)
        {
            _currentTime = pTime;
            for (int lIndex = 0; lIndex < _listAniGroup.Count; lIndex++)
            {
                _listAniGroup[lIndex].PlayGroup(_currentTime);
            }
        }

        void _CalForwardTime(float pDelta)
        {
            if (_currentTime >= _duration)
            {
                Stop(eStopMode.LastFrame);

                if(_onFinishedEvent != null)
                    _onFinishedEvent.Invoke();
                
                switch (_wrapMode)
                {
                    case eWrapMode.Once:
                        break;
                    case eWrapMode.Loop:
                        Play(_playMode, _onFinishedEvent);
                        break;
                    case eWrapMode.PingPong:
                        Play(_currentPlayMode == ePlayMode.Forward ? ePlayMode.Backward : ePlayMode.Forward, _onFinishedEvent);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                _currentTime += pDelta;
                _currentTime = Mathf.Clamp(_currentTime, 0, _duration);
            }
        }

        void _CalBackwardTime(float pDelta)
        {
            if (_currentTime <= 0)
            {
                Stop(eStopMode.FirstFrame);

                if (_onFinishedEvent != null)
                    _onFinishedEvent();

                switch (_wrapMode)
                {
                    case eWrapMode.Once:
                        break;
                    case eWrapMode.Loop:
                        Play(_playMode, _onFinishedEvent);
                        break;
                    case eWrapMode.PingPong:
                        Play(_currentPlayMode == ePlayMode.Forward ? ePlayMode.Backward : ePlayMode.Forward, _onFinishedEvent);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                _currentTime -= pDelta;
                _currentTime = Mathf.Clamp(_currentTime, 0, _duration);
            }
        }

        void _DoUpdate(float pDeltaTime)
        {
            if (_isPlaying == false)
                return;

#if UNITY_EDITOR
            _editModeLastUpdate = Time.realtimeSinceStartup;
#endif

            _PlayTimeLine(_currentTime);
            _calCurrentTime(pDeltaTime);
        }

        #region Editor용 접근 코드
        public UIAnimationGroup AddGroup(GameObject obj)
        {
            UIAnimationGroup newGroup = new UIAnimationGroup();
            newGroup.SetTarget(obj);
            _listAniGroup.Add(newGroup);
            return newGroup;
        }

        public void RemoveGroup(int pIndex)
        {
            _listAniGroup.RemoveAt(pIndex);
        }

        public bool IsContainObject(GameObject pGameObject)
        {
            if (pGameObject == null)
                return false;

            for (int lIndex = 0; lIndex < _listAniGroup.Count; ++lIndex)
            {
                if (_listAniGroup[lIndex].target == pGameObject)
                    return true;
            }
            return false;
        }

        public void SetTimeEditor(float pDeltaTime)
        {
            if (Application.isPlaying == false)
            {
                _SetTimeLine(pDeltaTime);
            }
        }

        public void UpdateFromEditor()
        {
            if (Application.isPlaying == false)
            {
                _DoUpdate(_timeSinceLastUpdate);
            }
        }

        public int FindIndexEditor(UIAnimationGroup pTarget)
        {
            return _listAniGroup.IndexOf(pTarget);
        }
        #endregion
    }

    [System.Serializable]
    public class UIAnimationGroup : IGroup
    {
        //에디터 코드
        [SerializeField] [HideInInspector] public bool mLockEditor = false;
        [SerializeField] [HideInInspector] public bool mViewAll = true;
        [SerializeField] [HideInInspector] public bool mViewBasic = true;
        [SerializeField] [HideInInspector] public bool mViewTracks = true;

        public GameObject target { get { return _target; } }
        public int count { get { return _listTrack.Count; } }
        public UIAnimationTrack this[int lIndex] { get { return _listTrack[lIndex]; } }

        [SerializeField]
        GameObject _target;

        [SerializeField]
        List<UIAnimationTrack> _listTrack = new List<UIAnimationTrack>();

        public void SetTarget(GameObject pTarget)
        {
            _target = pTarget;

            for (int lIndex = 0; lIndex < _listTrack.Count; lIndex++)
            {
                if (_listTrack[lIndex].target != pTarget)
                    _listTrack[lIndex].SetTarget(pTarget.transform);
            }
        }

        public void PlayGroup(float pCurrentTime)
        {
            for (int lIndex = 0; lIndex < _listTrack.Count; lIndex++)
            {
                _listTrack[lIndex].PlayTrack(pCurrentTime);
            }
        }

        public void StopGroup(eStopMode pStopMode)
        {
            for (int lIndex = 0; lIndex < _listTrack.Count; lIndex++)
            {
                _listTrack[lIndex].StopTrack(pStopMode);
            }
        }

        public void SetTime(float pCurrentTime)
        {
            for (int lIndex = 0; lIndex < _listTrack.Count; lIndex++)
            {
                _listTrack[lIndex].SetTrackTime(pCurrentTime);
            }
        }

        public void AddTrack(eTrackType pTrackType)
        {
            if (_listTrack.FindIndex(lItem => lItem.trackType == pTrackType) == -1)
                _listTrack.Add(new UIAnimationTrack(pTrackType, _target.transform));
        }

        public void AddTrack(ITrack pTrack)
        {
            if (pTrack is UIAnimationTrack)
            {
                UIAnimationTrack pResult = pTrack as UIAnimationTrack;
                if(_listTrack.FindIndex(lItem => lItem.trackType == pResult.trackType) == -1)
                    _listTrack.Add(pResult);
            }
        }

        public void RemoveTrack(eTrackType pTrackType)
        {
            int lIndex = _listTrack.FindIndex(lItem => lItem.trackType == pTrackType);
            if (lIndex != -1)
                _listTrack.RemoveAt(lIndex);
        }

        public void RemoveTrack(ITrack pTarget)
        {
            if (pTarget is UIAnimationTrack)
            {
                var lRemove = pTarget as UIAnimationTrack;
                _listTrack.Remove(lRemove);
            }
        }

        public UIAnimationTrack GetAnimationTrack(eTrackType pTrackType)
        {
            return _listTrack.Find(lItem => lItem.trackType == pTrackType);
        }

        public bool IsContainTrack(eTrackType pTrackType)
        {
            bool lIsPositionType = pTrackType == eTrackType.Position || pTrackType == eTrackType.Position_2D_Bezir;
            if (lIsPositionType)
            {
                for (int lIndex = 0; lIndex < _listTrack.Count; lIndex++)
                {
                    if (_listTrack[lIndex].trackType == eTrackType.Position || 
                        _listTrack[lIndex].trackType == eTrackType.Position_2D_Bezir)
                        return true;
                }
                return false;
            }
            else
                return _listTrack.FindIndex(lItem => lItem.trackType == pTrackType) != -1;
        }

        public int FindTrackIndex(UIAnimationTrack pTarget)
        {
            return _listTrack.IndexOf(pTarget);
        }
    }

    [System.Serializable]
    public class UIAnimationTrack : ITrack
    {
        public eTrackType trackType { get { return _trackType; } }

        public int count { get { return _listClips.Count; } }

        public IClip this[int lIndex] { get { return _listClips[lIndex]; } }

        public Transform target { get { return _target; } }

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

            switch (trackType)
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
                    lClip.SetClip(lClip.endTime, _onUpdateEvent);
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
                            var lCurStartTimeDistance = Mathf.Abs(pCurrentTime - lCurClip.startTime);
                            var lCurNextEndTimeDistance = Mathf.Abs(pCurrentTime - lCurClip.endTime);
                            var lCurMin = Mathf.Min(lCurStartTimeDistance, lCurNextEndTimeDistance);

                            var lPrevStartTimeDistance = Mathf.Abs(pCurrentTime - lClip.startTime);
                            var lPrevEndTimeDistance = Mathf.Abs(pCurrentTime - lClip.endTime);
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
                if (pTime >= _listClips[i].startTime && pTime <= _listClips[i].endTime)
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
                if (lCurClip.endTime <= pClip.startTime)
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
                if (lCurClip.startTime >= pClip.endTime)
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
            _target.localPosition = SimpleBezierCurve.Evaluate(lStart, lControlPoint_First, lControlPoint_Second, lEnd, pFactor);
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

    [System.Serializable]
    public class UIAnimationClip : IClip
    {
        public float duration { get { return _duration; } }
        public float startTime { get { return _startTime; } }
        public float endTime { get { return _endTime; } }

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
    
    public class SimpleBezierCurve
    {
        public static Vector3 Evaluate(Vector3 pStart, Vector3 pControl1, Vector3 pControl2, Vector3 pEnd, float pTime)
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
    
        public static Vector3 Evaluate(Vector3 pStart, Vector3 pControl, Vector3 pEnd, float pTime)
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
            return Evaluate(pBezierPoint[lIndex], pBezierPoint[lIndex + 1], pBezierPoint[lIndex + 2], pBezierPoint[lIndex + 3], pCurrentTime);
        }
    }
}


