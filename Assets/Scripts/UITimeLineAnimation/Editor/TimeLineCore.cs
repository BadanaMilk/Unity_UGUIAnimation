using System;
using TimeLineInterface;
using UnityEditor;
using UnityEngine;

public class TimeLineCore
{
    private const float kHighMode = 1000000f;

    public ITimeLineObject currentTimeLine
    {
        get { return _targetTimeLine; }
    }

    public float length
    {
        get { return _targetTimeLine.Length; }
        set { _targetTimeLine.Length = value; }
    }

    public float maxTime
    {
        get { return Mathf.Max(maxViewTime, length); }
    }

    public float minViewTime
    {
        get { return _minViewTime; }
        private set
        {
            float view = Mathf.Min(maxViewTime - 0.01f, value);
            _minViewTime = Mathf.Max(view, 0);
        }
    }

    public float maxViewTime
    {
        get { return _maxViewTime; }
        private set { _maxViewTime = Mathf.Max(minViewTime + 0.01f, value); }
    }


    private float _currentTime
    {
        get { return _targetTimeLine.CurrentTime; }
        set { _targetTimeLine.SetTimeEditor(value); }
    }

    private float _viewTime
    {
        get { return maxViewTime - minViewTime; }
    }

    private Color _colorDotTime
    {
        get { return new Color(0.5f, 0.5f, 0.5f, 1.0f); }
    }

    private Color _colorDotLine
    {
        get
        {
            return EditorGUIUtility.isProSkin
                ? new Color(1.0f, 1.0f, 1.0f, 0.1f)
                : new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }

    private Color _colorIntervalLine
    {
        get
        {
            return EditorGUIUtility.isProSkin
                ? new Color(1.0f, 1.0f, 1.0f, 0.3f)
                : new Color(0.0f, 0.0f, 0.0f, 0.5f);
        }
    }

    private readonly Color _selectClipColor   = new Color(0.0f, 0.1f, 0.0f, 1.0f);
    private readonly Color _unSelectClipColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    private float _minViewTime = 0;
    private float _maxViewTime = 1;

    private ITimeLineObject _targetTimeLine;

    private Rect _barRect;
    private Rect _timeRect;
    private Rect _scrollRect;

    private float _lowMod  = 0f;
    private float _highMod = 0f;

    private float _timeInterval = 0f;

    private readonly float[] _modulos = new float[]
        { 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000 };

    private float _start;
    private float _end;

    private float _clickTime = 0f;

    private Func<IClip> _funcGetSelectClip;

    public void SetBindingMethod(Func<IClip> pFuncGetSelectClip)
    {
        _funcGetSelectClip = pFuncGetSelectClip;
    }

    public TimeLineCore()
    {
    }

    public void Init(ITimeLineObject pTarget)
    {
        _targetTimeLine = pTarget;
    }

    public void SetRect(Rect pTimeInfoRect, Rect pScrollRect)
    {
        _barRect = new Rect(pTimeInfoRect.x, pTimeInfoRect.y,
            pTimeInfoRect.width, (pTimeInfoRect.height / 2) - 1);
        _timeRect = new Rect(pTimeInfoRect.x, pTimeInfoRect.y + (pTimeInfoRect.height / 2) + 1,
            pTimeInfoRect.width, (pTimeInfoRect.height / 2) - 1);
        _scrollRect = pScrollRect;
        _SetBaseData();
    }

    public void DrawTime()
    {
        _ShowSlider();
        _ShowTimeDot();
        _ShowSetMaxTime();
    }

    public void DrawScroll()
    {
        _ShowScroll();
    }

    /// <summary>
    /// 기본 데이터 세팅
    /// </summary>
    private void _SetBaseData()
    {
        _timeInterval = kHighMode;
        _highMod = kHighMode;
        _lowMod = 0.01f;

        for (var lIndex = 0; lIndex < _modulos.Length; lIndex++)
        {
            var lCount = _viewTime / _modulos[lIndex];

            //50 is approx width of label
            if (_scrollRect.width / lCount > 50)
            {
                _timeInterval = _modulos[lIndex];
                _lowMod = lIndex > 0 ? _modulos[lIndex - 1] : _lowMod;
                _highMod = lIndex < _modulos.Length - 1 ? _modulos[lIndex + 1] : kHighMode;
                break;
            }
        }

        _start = (float)Mathf.FloorToInt(minViewTime / _timeInterval) * _timeInterval;
        _end = (float)Mathf.CeilToInt(maxViewTime / _timeInterval) * _timeInterval;
        _start = Mathf.Round(_start * 10) / 10;
        _end = Mathf.Round(_end * 10) / 10;
    }

    /// <summary>
    /// 타임라인 슬라이더 세팅
    /// </summary>
    private void _ShowSlider()
    {
        // Time Min Max Slider
        float lMinViewTime = minViewTime;
        float lMaxViewTime = maxViewTime;
        var lSliderRect = Rect.MinMaxRect(_barRect.xMin + 5, _barRect.yMin, _barRect.xMax - 5, _barRect.yMin + 18);
        EditorGUI.MinMaxSlider(lSliderRect, ref lMinViewTime, ref lMaxViewTime, 0, maxTime);
        minViewTime = lMinViewTime;
        maxViewTime = lMaxViewTime;
        if (lSliderRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
        {
            minViewTime = 0;
            maxViewTime = length;
        }
    }

    /// <summary>
    /// 타임라인 도트 세팅
    /// </summary>
    private void _ShowTimeDot()
    {
        if (_timeRect.width / (_viewTime / _lowMod) > 6)
        {
            for (var lIndex = _start; lIndex <= _end; lIndex += _lowMod)
            {
                if (lIndex < minViewTime)
                    continue;
                if (maxViewTime < lIndex)
                    continue;

                float posX = _TimeToPosition(lIndex);

                Rect lFrameRect = Rect.MinMaxRect(_timeRect.xMin + posX - 1, _timeRect.yMax - 2,
                    _timeRect.xMin + posX + 1, _timeRect.yMax - 1);
                GUIBasicDrawer.DrawTexture(lFrameRect, _colorDotTime);

                Rect lLineRect = Rect.MinMaxRect(_scrollRect.xMin + posX, _scrollRect.yMin,
                    _scrollRect.xMin + posX + 1, 100000);
                GUIBasicDrawer.DrawTexture(lLineRect, _colorDotLine);
            }
        }

        for (var lIndex = _start; lIndex <= _end; lIndex += _timeInterval)
        {
            if (lIndex < minViewTime)
                continue;
            if (maxViewTime < lIndex)
                continue;

            var lPosX = _TimeToPosition(lIndex);

            var lRound = Mathf.Round(lIndex * 10) / 10;

            var lMarkRect = Rect.MinMaxRect(_timeRect.xMin + lPosX - 2, _timeRect.yMax - 3,
                _timeRect.xMin + lPosX + 2, _timeRect.yMax - 1);
            GUIBasicDrawer.DrawTexture(lMarkRect, EditorGUIUtility.isProSkin ? Color.white : Color.black);

            var lTimeText = lRound.ToString("0.00");
            var lSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(lTimeText));
            var lStampRect = new Rect(0, 0, lSize.x, lSize.y);
            lStampRect.center =
                new Vector2(_timeRect.xMin + lPosX, _timeRect.yMin + _timeRect.height - lSize.y + 4);
            GUIBasicDrawer.GUIColor(lRound % _highMod == 0 ? Color.white : new Color(1, 1, 1, 0.5f));
            GUI.Box(lStampRect, lTimeText, (GUIStyle)"label");
            GUIBasicDrawer.BackGUIColor();

            var lGuidRect = new Rect(lPosX + _scrollRect.x, _scrollRect.y, 1, 100000);
            GUIBasicDrawer.DrawTexture(lGuidRect, _colorIntervalLine);
        }

        if (_currentTime > minViewTime && _currentTime <= maxViewTime)
        {
            var lPosX = _TimeToPosition(_currentTime);
            var lCurrentTimeText = _currentTime.ToString("0.00");
            var lText = "<b><size=17>" + lCurrentTimeText + "</size></b>";
            var lSize = new Vector2(50, 20);
            var lStampRect = new Rect(0, 0, lSize.x, lSize.y);
            lStampRect.center =
                new Vector2(_timeRect.xMin + lPosX, _timeRect.yMin + _timeRect.height - lSize.y / 2);

            GUIBasicDrawer.GUIBackgroundColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUIBasicDrawer.DrawLabel(lStampRect, lText, Color.yellow);
            Rect lLineRect = new Rect(_scrollRect.x + lPosX, _scrollRect.y, 1, 100000);
            GUIBasicDrawer.DrawTexture(lLineRect, Color.yellow);
            GUIBasicDrawer.BackGUIAllColors();
        }

        float lLengthPos = _TimeToPosition(length);
        if (lLengthPos >= 0 && length < maxViewTime)
        {
            Rect lengthRect = new Rect(0, 0, 16, 16);
            lengthRect.center = new Vector2(_timeRect.xMin + lLengthPos, _timeRect.yMin + _timeRect.height - 2);
            GUIBasicDrawer.DrawTexture(lengthRect, EditorGUIUtility.isProSkin ? Color.white : Color.black,
                GUIBasicDrawer.TimeLineStyles.carretIcon.image);
        }
    }

    /// <summary>
    /// 최대시간 세팅
    /// </summary>
    private void _ShowSetMaxTime()
    {
        Rect lRect = Rect.MinMaxRect(_timeRect.xMin - 4, _timeRect.yMax - 5, _timeRect.xMax + 60,
            _timeRect.yMax + 10);
        float lCurTime = _currentTime;
        lCurTime = EditorGUI.Slider(lRect, lCurTime, minViewTime, maxViewTime);
        if (lCurTime != _currentTime)
            _currentTime = lCurTime;
    }

    /// <summary>
    /// 스크롤 세팅
    /// </summary>
    private void _ShowScroll()
    {
        GUIBasicDrawer.DrawTexture(new Rect(_scrollRect.x, _scrollRect.y, _scrollRect.width, 100000),
            new Color(0.1f, 0.1f, 0.1f, 0.1f));
    }

    /// <summary>
    /// 시간을 Position으로
    /// </summary>
    /// <param name="pTime"></param>
    /// <returns></returns>
    private float _TimeToPosition(float pTime)
    {
        return (pTime - minViewTime) / _viewTime * _scrollRect.width;
    }

    /// <summary>
    /// Position을 시간으로
    /// </summary>
    /// <param name="pPosX"></param>
    /// <returns></returns>
    private float _PositionToTime(float pPosX)
    {
        return (pPosX - _timeRect.xMin) / _timeRect.width * _viewTime + minViewTime;
    }

    /// <summary>
    /// 텍스쳐 그리기
    /// </summary>
    /// <param name="pMinY"></param>
    /// <param name="pMaxY"></param>
    /// <param name="pColor"></param>
    public void DrawTexture(float pMinY, float pMaxY, Color pColor)
    {
        GUIBasicDrawer.DrawTexture(Rect.MinMaxRect(_scrollRect.xMin, pMinY, _scrollRect.xMax, pMaxY), pColor);
    }

    /// <summary>
    /// 트랙 UI 그리기
    /// </summary>
    /// <param name="pMinY"></param>
    /// <param name="pMaxX"></param>
    /// <param name="pGroup"></param>
    /// <param name="pTrack"></param>
    /// <param name="pEvent"></param>
    public void DrawTrack(float pMinY, float pMaxX, IGroup pGroup, ITrack pTrack, Event pEvent)
    {
        Rect lTrackRect = Rect.MinMaxRect(_scrollRect.xMin, pMinY, _scrollRect.xMax, pMaxX);
        bool lMouseClip = false;

        for (byte lIndex = 0; lIndex < pTrack.Count; ++lIndex)
        {
            lMouseClip = (lMouseClip || DrawClip(pMinY, pMaxX, pGroup, pTrack, pTrack[lIndex], pEvent));
        }

        if (lMouseClip == false && lTrackRect.Contains(pEvent.mousePosition))
        {
            if (pEvent.type == EventType.MouseDown)
            {
                _OnTrackClickEvent(pGroup, pTrack,-1);
            }

            if (pEvent.type == EventType.ContextClick)
            {
                _clickTime = _PositionToTime(pEvent.mousePosition.x);

                GenericMenu lMenu = new GenericMenu();
                lMenu.AddItem(new GUIContent("Add Clip"), false, () => { pTrack.AddClip(_clickTime); });

                lMenu.AddSeparator("/");

                lMenu.AddItem(new GUIContent("Remove Track"), false, () => { pGroup.RemoveTrack(pTrack); });

                lMenu.ShowAsContext();
            }
        }
    }

    private bool _onDragFlag = false;

    /// <summary>
    /// 클립 UI 그리기
    /// </summary>
    /// <param name="pMinY"></param>
    /// <param name="pMaxY"></param>
    /// <param name="pGroup"></param>
    /// <param name="pTrack"></param>
    /// <param name="pClip"></param>
    /// <param name="pEvent"></param>
    /// <returns></returns>
    public bool DrawClip(float pMinY, float pMaxY, IGroup pGroup, ITrack pTrack, IClip pClip, Event pEvent)
    {
        bool lMouseClip = false;

        float lStartTime = pClip.StartTime;
        float lEndTime = pClip.EndTime;

        if (lStartTime > maxViewTime)
            return false;
        if (lEndTime < minViewTime)
            return false;

        if (lStartTime < minViewTime)
            lStartTime = minViewTime;

        if (maxViewTime < lEndTime)
            lEndTime = maxViewTime;

        IClip lTargetClip = _funcGetSelectClip();
        float lStartPos = _TimeToPosition(pClip.StartTime);
        float lEndPos = _TimeToPosition(pClip.EndTime);
        bool lIsSelectedClip = (lTargetClip == pClip);
        Rect lClipRect = Rect.MinMaxRect(_scrollRect.xMin + lStartPos, pMinY, _scrollRect.xMin + lEndPos, pMaxY);
        if (lClipRect.xMax > _scrollRect.xMax)
        {
            lClipRect.xMax = _scrollRect.xMax;
        }

        if (lClipRect.xMin < _scrollRect.xMin)
        {
            lClipRect.xMin = _scrollRect.xMin;
        }

        bool lPrevEnable = GUI.enabled;
        GUI.enabled = true;

        if (lClipRect.width == 0)
        {
            lClipRect.xMin -= 1;
            lClipRect.xMax += 1;
        }
        GUIBasicDrawer.DrawTexture(lClipRect, lIsSelectedClip ? _selectClipColor : _unSelectClipColor);
        GUIBasicDrawer.DrawTexture(Rect.MinMaxRect(lClipRect.xMin + 1, lClipRect.yMin + 2, lClipRect.xMax - 1, lClipRect.yMax - 2),
            new Color(1.0f, 1.0f, 1.0f, 0.5f));

        switch (pEvent.type)
        {
            case EventType.MouseDown:
            {
                if (lClipRect.Contains(pEvent.mousePosition) == false)
                    break;
                lMouseClip = true;
                _OnClipClick(pGroup, pTrack, pClip);
                GUI.enabled = lPrevEnable;
            }
                break;
            case EventType.MouseDrag:
            {
                if (lPrevEnable == false || lIsSelectedClip == false)
                    break;

                if (lClipRect.Contains(pEvent.mousePosition))
                    _onDragFlag = true;

                if (_onDragFlag)
                {
                    lMouseClip = true;
                    float lMouseTime = _PositionToTime(pEvent.mousePosition.x);
                    _OnClipDrag(pTrack, pClip, pEvent.delta.x * 0.0005f);
                }
            }
                break;
            case EventType.MouseUp:
            {
                if (lIsSelectedClip)
                    _onDragFlag = false;
            }
                break;
            case EventType.ContextClick:
            {
                if (lPrevEnable == false || lClipRect.Contains(pEvent.mousePosition) == false)
                    break;

                _clickTime = _PositionToTime(pEvent.mousePosition.x);
                lMouseClip = true;

                _OnClipContextClick(pTrack, pClip, _clickTime);
               
            }
                break;
        }

        GUI.enabled = lPrevEnable;
        return lMouseClip;
    }
    
    #region Clip Event

    private Action<IClip>                _onClickClip;
    private Action<ITrack, IClip, float> _onDragClip;
    private Action<ITrack, IClip, float> _onContextClick;

    void _OnClipClick(IGroup pGroup, ITrack pTrack, IClip pClip)
    {
        _onClickTrack?.Invoke(pGroup, pTrack, pClip.StartTime);
        _onClickClip?.Invoke(pClip);
    }

    void _OnClipDrag(ITrack pTrack, IClip pClip, float pDragPositionTime)
    {
        _onDragClip?.Invoke(pTrack, pClip, pDragPositionTime);
    }

    void _OnClipContextClick(ITrack pTrack, IClip pClip, float pDragPositionTime)
    {
        _onContextClick?.Invoke(pTrack, pClip, pDragPositionTime);
    }

    public void SetClipClickEvent(Action<IClip> pEvent)
    {
        _onClickClip = pEvent;
    }

    public void SetDragClipEvent(Action<ITrack, IClip, float> pEvent)
    {
        _onDragClip = pEvent;
    }
    
    public void SetClipContextClickEvent(Action<ITrack, IClip, float> pEvent)
    {
        _onContextClick = pEvent;
    }
    #endregion Clip Event
    
    #region Track Event
    
    private Action<IGroup, ITrack, float> _onClickTrack;
    
    void _OnTrackClickEvent(IGroup pGroup, ITrack pTrack, float pTime)
    {
        _onClickTrack?.Invoke(pGroup, pTrack, pTime);
    }

    public void SetTrackClickEvent(Action<IGroup, ITrack, float> pEvent)
    {
        _onClickTrack = pEvent;
    }
    
    #endregion Track Event
}