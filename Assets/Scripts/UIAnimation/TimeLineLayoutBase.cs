namespace TimeLineLayoutBase
{
    public interface IGroup
    {
        void AddTrack(ITrack pTarget);
        void RemoveTrack(ITrack pTarget);
    }

    public interface ITrack
    {
        IClip this[int lIndex] { get; }
        int count { get; }

        void AddClip(float pTime);
        void RemoveClip(float pTime);

        IClip GetPrevClip(IClip pTarget);
        IClip GetNextClip(IClip pTarget);
        IClip GetClip(float pTime);
    }

    public interface IClip
    {
        float duration { get; }
        float startTime { get; }
        float endTime { get; }

        void SetStartTime(float pSecond);
        void SetEndTime(float pSecond);
    }

    public interface IDrawerTimeLine
    {
        bool IsPlaying { get; }
        float CurrentTime { get; }
        float Length { get; set; }

        void SetTimeEditor(float time);
    }
}

