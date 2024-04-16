using UnityEngine;
using TimeLineLayoutBase;
using System.Collections.Generic;

namespace UIAnimationTimeLine
{
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
            if (pTrack is UIAnimationTrack pResult)
            {
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
}