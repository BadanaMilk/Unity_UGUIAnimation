using System.Collections.Generic;
using TimeLineInterface;
using UnityEngine;

namespace UITimeLineAnimation
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

    public class UIAnimation : MonoBehaviour, ITimeLineObject
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
            return _listAniGroup.Find(lItem => lItem.Target == pTarget);
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
                if (_listAniGroup[lIndex].Target == pGameObject)
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
}


