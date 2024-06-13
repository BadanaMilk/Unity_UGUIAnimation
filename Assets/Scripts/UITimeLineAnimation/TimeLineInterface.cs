using System;
using UnityEngine;
using UnityEditor;

namespace TimeLineInterface
{
    /// <summary>
    /// 메인 타임라인 오브젝트 인터페이스
    /// </summary>
    public interface ITimeLineObject
    {
        bool  IsPlaying   { get; }
        float CurrentTime { get; }
        float Length      { get; set; }

        void SetTimeEditor(float time);
    }

    /// <summary>
    /// 그룹 인터페이스
    /// </summary>
    public interface IGroup
    {
        void AddTrack(ITrack pTarget);
        void RemoveTrack(ITrack pTarget);
    }

    /// <summary>
    /// 트랙 인터페이스
    /// </summary>
    public interface ITrack
    {
        IClip this[int lIndex] { get; }
        int Count { get; }

        void AddClip(float pTime);
        void RemoveClip(float pTime);

        IClip GetPrevClip(IClip pTarget);
        IClip GetNextClip(IClip pTarget);
        IClip GetClip(float pTime);
    }

    /// <summary>
    /// 클립 인터페이스
    /// </summary>
    public interface IClip
    {
        float Duration  { get; }
        float StartTime { get; }
        float EndTime   { get; }

        void SetStartTime(float pSecond);
        void SetEndTime(float pSecond);
    }
}
