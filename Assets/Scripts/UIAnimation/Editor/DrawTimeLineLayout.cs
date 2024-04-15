using System;
using UnityEngine;
using UnityEditor;
using TimeLineLayoutBase;

public class DrawTimeLineLayout
{
    private const float kHighMode = 1000000f;

    public IDrawerTimeLine currentTimeLine { get { return _targetTimeLine; } }

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
        get
        { 
            return new Color(0.5f, 0.5f, 0.5f, 1.0f); 
        }
    }
    private Color _colorDotLine
    {
        get 
        {
            return EditorGUIUtility.isProSkin ? new Color(1.0f, 1.0f, 1.0f, 0.1f) : new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }

    private Color _colorIntervalLine
    {
        get
        {
            return EditorGUIUtility.isProSkin ? new Color(1.0f, 1.0f, 1.0f, 0.3f) : new Color(0.0f, 0.0f, 0.0f, 0.5f);
        }
    }

    private readonly Color _selectClipColor = new Color(0.0f, 0.1f, 0.0f, 1.0f);
    private readonly Color _unSelectClipColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    float _minViewTime = 0;
    float _maxViewTime = 1;

    IDrawerTimeLine _targetTimeLine;

    Rect _barRect;
    Rect _timeRect;
    Rect _scrollRect;

    float _lowMod = 0f;
    float _highMod = 0f;

    float _timeInterval = 0f;

    float[] _modulos = new float[] { 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000 };

    float _start;
    float _end;

    float _clickTime = 0f;

    public Action<IGroup, ITrack, float> onSelectTrackAndTime;
    public Func<IClip> onGetSelectClip;
    public Action<IClip, IClip> onLinkPrevClipEvent;
    public Action<IClip, IClip> onLinkNextClipEvent;

    public DrawTimeLineLayout()
    {
    }

    public void Init(IDrawerTimeLine pTarget)
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

    void _SetBaseData()
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

    void _ShowSlider()
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

    void _ShowTimeDot()
    {
        //Dot
        if (_timeRect.width / (_viewTime / _lowMod) > 6)
        {
            for (var lIndex = _start; lIndex <= _end; lIndex += _lowMod)
            {
                if (lIndex < minViewTime)
                    continue;
                if (maxViewTime < lIndex)
                    continue;

                float posX = _TimeToPos(lIndex);

                // Time Dot
                Rect lFrameRect = Rect.MinMaxRect(_timeRect.xMin + posX - 1, _timeRect.yMax - 2, _timeRect.xMin + posX + 1, _timeRect.yMax - 1);
                DrawerBasic.DrawTexture(lFrameRect, _colorDotTime);

                // Time Line
                Rect lLineRect = Rect.MinMaxRect(_scrollRect.xMin + posX, _scrollRect.yMin, _scrollRect.xMin + posX + 1, 100000);
                DrawerBasic.DrawTexture(lLineRect, _colorDotLine);
            }
        }

        //the time interval
        for (var lIndex = _start; lIndex <= _end; lIndex += _timeInterval)
        {
            if (lIndex < minViewTime)
                continue;
            if (maxViewTime < lIndex)
                continue;

            var lPosX = _TimeToPos(lIndex);

            var lRound = Mathf.Round(lIndex * 10) / 10;

            // Time Dot
            var lMarkRect = Rect.MinMaxRect(_timeRect.xMin + lPosX - 2, _timeRect.yMax - 3, _timeRect.xMin + lPosX + 2, _timeRect.yMax - 1);
            DrawerBasic.DrawTexture(lMarkRect, EditorGUIUtility.isProSkin ? Color.white : Color.black);

            // Time Label
            var lTimeText = lRound.ToString("0.00");
            var lSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(lTimeText));
            var lStampRect = new Rect(0, 0, lSize.x, lSize.y);
            lStampRect.center = new Vector2(_timeRect.xMin + lPosX, _timeRect.yMin + _timeRect.height - lSize.y + 4);
            DrawerBasic.GUIColor(lRound % _highMod == 0 ? Color.white : new Color(1, 1, 1, 0.5f));
            GUI.Box(lStampRect, lTimeText, (GUIStyle)"label");
            DrawerBasic.BackGUIColor();

            var lGuidRect = new Rect(lPosX + _scrollRect.x, _scrollRect.y, 1, 100000);
            DrawerBasic.DrawTexture(lGuidRect, _colorIntervalLine);
        }

        //the number showing current time when scubing
        if (_currentTime > minViewTime && _currentTime <= maxViewTime)
        {
            var lPosX = _TimeToPos(_currentTime);
            var lCurrentTimeText = _currentTime.ToString("0.00");
            var lText = "<b><size=17>" + lCurrentTimeText + "</size></b>";
            var lSize = new Vector2(50, 20);
            var lStampRect = new Rect(0, 0, lSize.x, lSize.y);
            lStampRect.center = new Vector2(_timeRect.xMin + lPosX, _timeRect.yMin + _timeRect.height - lSize.y / 2);

            DrawerBasic.GUIBackgroundColor(new Color(0.5f, 0.5f, 0.5f, 0.5f));
            DrawerBasic.DrawLabel(lStampRect, lText, Color.yellow);
            Rect lLineRect = new Rect(_scrollRect.x + lPosX, _scrollRect.y, 1, 100000);
            DrawerBasic.DrawTexture(lLineRect, Color.yellow);
            DrawerBasic.BackGUIAllColors();
        }

        //the length position carret texture and pre-exit length indication
        float lLenghtPos = _TimeToPos(length);
        if (lLenghtPos >= 0 && length < maxViewTime)
        {
            Rect lengthRect = new Rect(0, 0, 16, 16);
            lengthRect.center = new Vector2(_timeRect.xMin + lLenghtPos, _timeRect.yMin + _timeRect.height - 2);
            DrawerBasic.DrawTexture(lengthRect, EditorGUIUtility.isProSkin ? Color.white : Color.black, DrawerBasic.TimeLineStyles.carretIcon.image);
        }
    }

    void _ShowSetMaxTime()
    {
        Rect lRect = Rect.MinMaxRect(_timeRect.xMin - 4, _timeRect.yMax - 5, _timeRect.xMax + 60, _timeRect.yMax + 10);
        float lCurTime = _currentTime;
        lCurTime = EditorGUI.Slider(lRect, lCurTime, minViewTime, maxViewTime);
        if (lCurTime != _currentTime)
            _currentTime = lCurTime;
    }

    void _ShowScroll()
    {
        DrawerBasic.DrawTexture(new Rect(_scrollRect.x, _scrollRect.y, _scrollRect.width, 100000), new Color(0.1f, 0.1f, 0.1f, 0.1f));
    }

    float _TimeToPos(float pTime)
    {
        return (pTime - minViewTime) / _viewTime * _scrollRect.width;
    }

    float _PosToTime(float pPosX)
    {
        return (pPosX - _timeRect.xMin) / _timeRect.width * _viewTime + minViewTime;
    }

    public void DrawTexture(float pMinY, float pMaxY, Color pColor)
    {
        DrawerBasic.DrawTexture(Rect.MinMaxRect(_scrollRect.xMin, pMinY, _scrollRect.xMax, pMaxY), pColor);
    }

    public void DrawTrack(float pMinY, float pMaxX, IGroup pGroup, ITrack pTrack, Event pEvent)
    {
        Rect lTrackRect = Rect.MinMaxRect(_scrollRect.xMin, pMinY, _scrollRect.xMax, pMaxX);
        bool lMouseClip = false;

        for (byte lIndex = 0; lIndex < pTrack.count; ++lIndex)
        {
            lMouseClip = (lMouseClip || DrawClip(pMinY, pMaxX, pGroup, pTrack, pTrack[lIndex], pEvent));
        }

        if (lMouseClip == false && lTrackRect.Contains(pEvent.mousePosition))
        {
            if (pEvent.type == EventType.MouseDown)
            {
                _SelectTrackAndTime(pGroup, pTrack, -1);
            }
            if (pEvent.type == EventType.ContextClick)
            {
                _clickTime = _PosToTime(pEvent.mousePosition.x);

                GenericMenu lMenu = new GenericMenu();
                lMenu.AddItem(new GUIContent("Add Clip"), false, () =>
                {
                    pTrack.AddClip(_clickTime);
                });

                lMenu.AddSeparator("/");

                lMenu.AddItem(new GUIContent("Remove Track"), false, () =>
                {
                    pGroup.RemoveTrack(pTrack);
                });

                lMenu.ShowAsContext();
            }
        }
    }

    public bool DrawClip(float pMinY, float pMaxY, IGroup pGroup, ITrack pTrack, IClip pClip, Event pEvent)
    {
        bool lMouseClip = false;

        float lStartTime = pClip.startTime;
        float lEndTime = pClip.endTime;

        if (lStartTime > maxViewTime)
            return false;
        if (lEndTime < minViewTime)
            return false;

        if (lStartTime < minViewTime)
            lStartTime = minViewTime;

        if (maxViewTime < lEndTime)
            lEndTime = maxViewTime;

        IClip lTargetClip = _GetSelectClip();
        float lStartPos = _TimeToPos(pClip.startTime);
        float lEndPos = _TimeToPos(pClip.endTime);
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

        DrawerBasic.DrawTexture(lClipRect, lIsSelectedClip ? _selectClipColor : _unSelectClipColor);

        if (lClipRect.width > 2)
        {
            DrawerBasic.DrawTexture(Rect.MinMaxRect(lClipRect.xMin + 1, lClipRect.yMin + 2, lClipRect.xMax - 1, lClipRect.yMax - 2), new Color(1.0f, 1.0f, 1.0f, 0.5f));
        }
        switch (pEvent.type)
        {
            case EventType.MouseDown:
                {
                    if (lClipRect.Contains(pEvent.mousePosition) == false)
                        break;
                    lMouseClip = true;
                    _SelectTrackAndTime(pGroup, pTrack, _PosToTime(pEvent.mousePosition.x));
                    GUI.enabled = lPrevEnable;
                }
                break;
            case EventType.MouseDrag:
                {
                    if (lPrevEnable == false || lIsSelectedClip == false || lClipRect.Contains(pEvent.mousePosition) == false)
                        break;

                    lMouseClip = true;
                    IClip lBeforeClip = pTrack.GetPrevClip(pClip);
                    IClip lNextClip = pTrack.GetNextClip(pClip);
                    float lDuration = pClip.duration;

                    float lMouseTime = _PosToTime(pEvent.mousePosition.x);
                    var lTestStart = Mathf.Clamp(lMouseTime - (lDuration * 0.5f), lBeforeClip != null ? lBeforeClip.endTime : 0, lNextClip != null ? lNextClip.startTime - lDuration : length - lDuration);
                    var lTestEnd = Mathf.Clamp(lMouseTime + (lDuration * 0.5f), lTestStart + lDuration, lNextClip != null ? lNextClip.startTime : length);

                    pClip.SetStartTime(lTestStart);
                    pClip.SetEndTime(lTestEnd);
                }
                break;
            case EventType.ContextClick:
                {
                    if (lPrevEnable == false || lClipRect.Contains(pEvent.mousePosition) == false)
                        break;

                    _clickTime = _PosToTime(pEvent.mousePosition.x);
                    lMouseClip = true;

                    GenericMenu lMenu = new GenericMenu();

                    IClip lBeforeClip = pTrack.GetPrevClip(pClip);
                    IClip lNextClip = pTrack.GetNextClip(pClip);

                    if (lBeforeClip != null)
                    {
                        lMenu.AddItem(new GUIContent("Link Prev Clip Time"), false, () =>
                        {
                            pClip.SetStartTime(lBeforeClip.endTime);
                            onLinkPrevClipEvent(lBeforeClip, pClip);
                        });
                    }
                    if (lNextClip != null)
                    {
                        lMenu.AddItem(new GUIContent("Link Next Clip Time"), false, () =>
                        {
                            pClip.SetEndTime(lNextClip.startTime);
                            onLinkNextClipEvent(pClip, lNextClip);
                        });
                    }

                    lMenu.AddItem(new GUIContent("Remove Clip"), false, () =>
                    {
                        pTrack.RemoveClip(_clickTime);
                    });
                    lMenu.ShowAsContext();
                }
                break;
        }
        GUI.enabled = lPrevEnable;
        return lMouseClip;
    }

    private void _SelectTrackAndTime(IGroup group, ITrack track, float time)
    {
        if (onSelectTrackAndTime != null)
        {
            onSelectTrackAndTime(group, track, time);
        }
    }

    private IClip _GetSelectClip()
    {
        if (onGetSelectClip != null)
        {
            return onGetSelectClip();
        }
        return null;
    }    
}

public static class DrawerBasic
{
    public static void GUIColor(Color _color)
    {
        GUI.color = _color;
    }

    public static void BackGUIColor()
    {
        GUI.color = Color.white;
    }

    public static void GUIBackgroundColor(Color _color)
    {
        GUI.backgroundColor = _color;
    }

    public static void BackGUIBackgroundColor()
    {
        GUI.backgroundColor = Color.white;
    }

    public static void GUIContentColor(Color _color)
    {
        GUI.contentColor = _color;
    }

    public static void BackGUIContentColor()
    {
        GUI.contentColor = Color.white;
    }

    public static void BackGUIAllColors()
    {
        BackGUIColor();
        BackGUIBackgroundColor();
        BackGUIContentColor();
    }

    public static void DrawLabel(Rect rect, string text, Color color)
    {
        TimeLineStyles.guiStyle.normal.textColor = color;
        GUI.Label(rect, text, TimeLineStyles.guiStyle);
    }

    public static bool DrawToggleLabel(Rect rect, ref bool isToggled, Color color, string addString = "")
    {
        GUIColor(color);
        bool isClick = false;

        if (isToggled)
        {
            DrawerBasic.BeginGUIRotate(180f, rect);
            if (GUI.Button(Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMin + 20, rect.yMax), TimeLineStyles.dropUpIcon, (GUIStyle)"label"))
            {
                isToggled = !isToggled;
                isClick = true;
            }
            DrawerBasic.EndGUIRotate();
        }
        else
        {
            if (GUI.Button(Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMin + 20, rect.yMax), TimeLineStyles.dropDownIcon, (GUIStyle)"label"))
            {
                isToggled = !isToggled;
                isClick = true;
            }
        }

        if (string.IsNullOrEmpty(addString) == false)
        {
            if (GUI.Button(Rect.MinMaxRect(rect.xMin + 20, rect.yMin, rect.xMax, rect.yMax), addString, (GUIStyle)"label"))
            {
                isToggled = !isToggled;
                isClick = true;
            }
        }
        BackGUIColor();

        return isClick;
    }

    public static void DrawTexture(Rect rect, Color color)
    {
        DrawTexture(rect, color, TimeLineStyles.whiteTexture);
    }

    public static void DrawTexture(Rect rect, Color color, Texture image)
    {
        GUIColor(color);
        GUI.DrawTexture(rect, image);
        BackGUIColor();
    }

    static Matrix4x4 matrixBackup;
    public static void BeginGUIRotate(float pAngle, Rect pRect)
    {
        var pivot = new Vector2(pRect.xMin + pRect.width * 0.5f, pRect.yMin + pRect.height * 0.5f);

        matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(pAngle, pivot);
    }

    public static void EndGUIRotate()
    {
        GUI.matrix = matrixBackup;
    }

    [InitializeOnLoad]
    public static class TimeLineStyles
    {
        public static GUIContent editIcon;
        public static GUIContent dropDownIcon;
        public static GUIContent dropUpIcon;

        public static GUIContent lockIcon;

        public static GUIContent playIcon;
        public static GUIContent stepIcon;
        public static GUIContent stepReverseIcon;
        public static GUIContent pauseIcon;
        public static GUIContent stopIcon;

        public static GUIContent carretIcon;
        public static GUIContent plusIcon;
        public static GUIContent trashIcon;

        //private static GUISkin styleSheet;
        public static GUIStyle guiStyle;

        static TimeLineStyles()
        {
            Load();
        }

        [InitializeOnLoadMethod]
        public static void Load()
        {
            editIcon = EditorGUIUtility.IconContent("CustomTool");
            dropDownIcon = EditorGUIUtility.IconContent("d_icon dropdown");
            dropUpIcon = EditorGUIUtility.IconContent("d_icon dropdown");

            lockIcon = EditorGUIUtility.IconContent("InspectorLock");

            playIcon = EditorGUIUtility.IconContent("Animation.Play");
            stepIcon = EditorGUIUtility.IconContent("Animation.NextKey");
            stepReverseIcon = EditorGUIUtility.IconContent("Animation.PrevKey");
            pauseIcon = EditorGUIUtility.IconContent("d_PauseButton");
            stopIcon = EditorGUIUtility.IconContent("animationdopesheetkeyframe");

            carretIcon = EditorGUIUtility.IconContent("d_icon dropdown");
            plusIcon = EditorGUIUtility.IconContent("d_Toolbar Plus@2x");
            trashIcon = EditorGUIUtility.IconContent("d_Toolbar Minus@2x");

            guiStyle = new GUIStyle();
        }

        ///Get a white 1x1 texture
        public static Texture2D whiteTexture
        {
            get { return EditorGUIUtility.whiteTexture; }
        }
    }
}

public abstract class TimeLineLayoutWindowBase : EditorWindow
{
    protected DrawTimeLineLayout _timeLine;
    protected DrawTimeLineLayout _TimeLine
    {
        get
        {
            if (_timeLine == null || _timeLine.currentTimeLine == null)
            {
                _timeLine = new DrawTimeLineLayout();
                _timeLine.Init(targetTimeLine);
                _timeLine.onSelectTrackAndTime = _SelectTrackAndTime;
                _timeLine.onGetSelectClip = GetSelectClip;
                _timeLine.onLinkPrevClipEvent = _OnLinkPevClipEvent;
                _timeLine.onLinkNextClipEvent = _OnLinkNextClipEvent;
            }
            return _timeLine;
        }
    }

    protected const float Out_Margin = 4;
    protected const float Content_Gab = 2;
    protected const float ToolBar_Height = 40; //top margin AFTER the toolbar
    protected const float First_Group_Top_Margin = 10; //initial top margin

    #region Color
    protected Color _GroupTitleColor
    { get { return new Color(0.50f, 0.50f, 0.50f, 0.5f); } }
    protected Color _GroupSubTitleColor
    { get { return EditorGUIUtility.isProSkin ? new Color(0.10f, 0.10f, 0.10f, 0.5f) : new Color(0.90f, 0.90f, 0.90f, 0.5f); } }
    protected Color _GroupContentColor
    { get { return EditorGUIUtility.isProSkin ? new Color(0.05f, 0.05f, 0.05f, 0.5f) : new Color(0.95f, 0.95f, 0.95f, 0.5f); } }
    protected Color _TrackOddColor
    { get { return EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f, 0.5f) : new Color(0.85f, 0.85f, 0.85f, 0.5f); } }
    protected Color _TrackEvenColor
    { get { return EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.20f, 0.5f) : new Color(0.80f, 0.80f, 0.80f, 0.5f); } }
    #endregion Color

    protected float _ScreenWidth { get { return position.width; } }
    protected float _ScreenHeight { get { return position.height; } }

    protected float _LeftMargin
    {
        get { return _trackListLeftMargin; }
        set { _trackListLeftMargin = Mathf.Clamp(value, 240, Screen.width / 2); }
    }

    protected abstract IDrawerTimeLine targetTimeLine { get; }

    protected bool _isResizingLeftMargin = false;
    protected float _trackListLeftMargin = 300f;
    protected float _totalHeight = 0;
    protected Vector2 _scrollPos = Vector2.zero;

    protected Rect _topLeftRect;   // 플레이 버튼 렉트
    protected Rect _topMiddleRect; // 시간 정보 렉트
    protected Rect _leftRect;      // Group/Track 리스트용 렉트
    protected Rect _centerRect;    // 타임라인 렉트

    /// <summary>
    /// 렉트 초기화 함수
    /// </summary>
    private void _InitRect()
    {
        _topLeftRect = Rect.MinMaxRect(Out_Margin, Out_Margin, Out_Margin + _LeftMargin, ToolBar_Height);
        _centerRect = Rect.MinMaxRect(_topLeftRect.xMax + Content_Gab, _topLeftRect.yMax + Content_Gab, _ScreenWidth - Out_Margin, _ScreenHeight - Out_Margin);

        _leftRect = Rect.MinMaxRect(_topLeftRect.xMin, _topLeftRect.yMax + Content_Gab, _topLeftRect.xMax, _ScreenHeight - Out_Margin);
        _topMiddleRect = Rect.MinMaxRect(_centerRect.xMin, _topLeftRect.yMin, _centerRect.xMax, _topLeftRect.yMax);
    }

    /// <summary>
    /// 사이즈 조절 마우스 이벤트
    /// </summary>
    /// <param name="pEvent"></param>
    private void _MouseEvent(Event pEvent)
    {
        //allow resize list width
        var lScaleRect = new Rect(_leftRect.xMax, _leftRect.yMin, Content_Gab, float.MaxValue);
        DrawerBasic.DrawTexture(lScaleRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        _AddCursorRect(lScaleRect, MouseCursor.ResizeHorizontal);

        if (pEvent.type == EventType.MouseDown &&
            pEvent.button == 0 &&
            lScaleRect.Contains(pEvent.mousePosition))
        {
            _isResizingLeftMargin = true;
            pEvent.Use();
        }
        if (_isResizingLeftMargin)
        {
            _LeftMargin = pEvent.mousePosition.x;
            Repaint();
        }
        if (pEvent.rawType == EventType.MouseUp)
        {
            _isResizingLeftMargin = false;
        }
    }
    protected void _AddCursorRect(Rect pRect, MouseCursor pType)
    {
        EditorGUIUtility.AddCursorRect(pRect, pType);
    }

    /// <summary>
    /// 렉트를 얻는 함수
    /// </summary>
    /// <param name="pBaseRect">기본 렉트</param>
    /// <param name="pHeight">가져올 렉트의 높이 값</param>
    /// <param name="pNextPosY">렉트를 가지고 온 뒤 다음 렉트의 시작 Y값</param>
    /// <returns></returns>
    protected Rect _GetRect(Rect pBaseRect, float pHeight, ref float pNextPosY)
    {
        Rect lReturnRect = Rect.MinMaxRect(pBaseRect.xMin, pBaseRect.yMin + pNextPosY, pBaseRect.xMax, pBaseRect.yMin + pNextPosY + pHeight);
        pNextPosY += (pHeight);
        return lReturnRect;
    }    

    /// <summary>
    /// 컨탠츠의 텍스쳐 색상을 입히는 함수
    /// </summary>
    /// <param name="pTitleRect"></param>
    /// <param name="pBackColor"></param>
    /// <param name="pDepth"></param>
    /// <returns></returns>
    protected Rect _DrawContentTexture(Rect pTitleRect, Color pBackColor, int pDepth)
    {
        Rect lContentRect;
        if (pDepth == 0)
        {
            lContentRect = pTitleRect;
            DrawerBasic.DrawTexture(lContentRect, pBackColor);
        }
        else
        {
            Rect lLineRect = new Rect(pTitleRect.x, pTitleRect.y, 4 * pDepth, pTitleRect.height);
            lContentRect = Rect.MinMaxRect(lLineRect.xMax, pTitleRect.yMin, pTitleRect.xMax, pTitleRect.yMax);

            DrawerBasic.DrawTexture(lLineRect, _GroupTitleColor);
            DrawerBasic.DrawTexture(lContentRect, pBackColor);
        }
        _TimeLine.DrawTexture(pTitleRect.yMin, pTitleRect.yMax, pBackColor);
        return lContentRect;
    }

    /// <summary>
    /// 타이틀 렉트를 그리는 함수
    /// </summary>
    /// <param name="pTitleRect"></param>
    /// <param name="pBackColor"></param>
    /// <param name="pViewer"></param>
    /// <param name="pTitleString"></param>
    /// <returns></returns>
    protected Rect _DrawTitle(Rect pTitleRect, Color pBackColor, ref bool pViewer, string pTitleString)
    {
        Rect lContentRect = _DrawContentTexture(pTitleRect, pBackColor, 1);
        DrawerBasic.DrawToggleLabel(new Rect(lContentRect.x, lContentRect.y, 16, lContentRect.height), ref pViewer, Color.white);
        GUI.Label(new Rect(lContentRect.x + 16, lContentRect.y, lContentRect.width - 16, lContentRect.height), pTitleString);
        return lContentRect;
    }

    /// <summary>
    /// 재생 버튼들을 그리는 함수
    /// </summary>
    /// <param name="pRect"></param>
    /// <param name="pTween"></param>
    protected void _DrawPlayButtons(Rect pRect, IDrawerTimeLine pTween)
    {
        DrawerBasic.GUIBackgroundColor(Color.grey);
        DrawerBasic.GUIContentColor(Color.white);

        float lBaseWidth = 20;
        float lBaseHeight = 20;

        if (pRect.width < lBaseWidth * 5 + 16)
        {
            lBaseWidth = (pRect.width - 16) / 5;
        }
        if (pRect.height < lBaseHeight)
        {
            lBaseHeight = pRect.height;
        }

        Func<int, Rect> lGetRect = delegate (int index)
        {
            return new Rect(pRect.x + ((lBaseWidth + 4) * index), pRect.y, lBaseWidth, lBaseHeight);
        };

        int lIndex = 0;
        if (GUI.Button(lGetRect(lIndex), DrawerBasic.TimeLineStyles.stepReverseIcon, (GUIStyle)"box"))
        {
            _OnClickSetFirstFrame();
        }
        ++lIndex;
        if (pTween.IsPlaying)
        {
            Rect tRect = lGetRect(lIndex);
            tRect.width = tRect.width + lBaseWidth + 4;
            if (GUI.Button(tRect, DrawerBasic.TimeLineStyles.pauseIcon, (GUIStyle)"box"))
            {
                _OnClickSetPause();
            }
            ++lIndex;
            ++lIndex;
        }
        else
        {
            if (pTween.CurrentTime == 0)
            {
                //포워드만    
                Rect tRect = lGetRect(lIndex);
                tRect.width = tRect.width + lBaseWidth + 4;
                if (GUI.Button(tRect, DrawerBasic.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayForward();
                }
                ++lIndex;
                ++lIndex;
            }
            else if (pTween.CurrentTime == pTween.Length)
            {
                //백워드만
                Rect tRect = lGetRect(lIndex);
                tRect.width = tRect.width + lBaseWidth + 4;
                DrawerBasic.BeginGUIRotate(180f, tRect);
                if (GUI.Button(tRect, DrawerBasic.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayBackward();
                }
                DrawerBasic.EndGUIRotate();
                ++lIndex;
                ++lIndex;
            }
            else
            {
                DrawerBasic.BeginGUIRotate(180f, lGetRect(lIndex));
                if (GUI.Button(lGetRect(lIndex), DrawerBasic.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayBackward();
                }
                DrawerBasic.EndGUIRotate();
                ++lIndex;
                if (GUI.Button(lGetRect(lIndex), DrawerBasic.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayForward();
                }
                ++lIndex;
            }
        }
        if (GUI.Button(lGetRect(lIndex), DrawerBasic.TimeLineStyles.stepIcon, (GUIStyle)"box"))
        {
            _OnClickSetLastFrame();
        }

        DrawerBasic.BackGUIAllColors();
    }

    #region Abstract Methods
    /// <summary>
    /// 현재 선택중인 클립 정보를 얻는 함수
    /// </summary>
    /// <returns></returns>
    public abstract IClip GetSelectClip();
    /// <summary>
    /// 타임라인에서 마우스로 트랙 및 시간을 선택했을때 함수
    /// </summary>
    /// <param name="pGroup"></param>
    /// <param name="pTrack"></param>
    /// <param name="pTime"></param>
    protected abstract void _SelectTrackAndTime(IGroup pGroup = null, ITrack pTrack = null, float pTime = -1);
    /// <summary>
    /// 타임라인에서 클립을 우클릭했을때 이전 클립과 연결하는 메뉴 이벤트
    /// </summary>
    /// <param name="pPrev"></param>
    /// <param name="pCurrent"></param>
    protected abstract void _OnLinkPevClipEvent(IClip pPrev, IClip pCurrent);
    /// <summary>
    /// 타임라인에서 클립을 우클릭했을때 다음 클립과 연결하는 메뉴 이벤트
    /// </summary>
    /// <param name="pPrev"></param>
    /// <param name="pCurrent"></param>
    protected abstract void _OnLinkNextClipEvent(IClip pCurrent, IClip pNext);
    /// <summary>
    /// 타임라인 내용을 그리는 함수
    /// </summary>
    /// <param name="pLeftRect"></param>
    /// <param name="pMouseEvent"></param>
    protected abstract void _DrawContents(Rect pLeftRect, Event pMouseEvent);
    /// <summary>
    /// 현재 에디터 UI를 사용할 수 있는지 체크하는 함수
    /// </summary>
    /// <returns></returns>
    protected abstract bool _IsEnableGUI();

    /// <summary>
    /// 플레이 버튼 리스트에서 앞으로 재생 버튼 이벤트
    /// </summary>
    protected abstract void _OnClickPlayForward();
    /// <summary>
    /// 플레이 버튼 리스트에서 뒤로 재생 버튼 이벤트
    /// </summary>
    protected abstract void _OnClickPlayBackward();
    /// <summary>
    /// 플레이 버튼 리스트에서 마지막 프레임으로 이동 버튼 이벤트
    /// </summary>
    protected abstract void _OnClickSetLastFrame();
    /// <summary>
    /// 플레이 버튼 리스트에서 처음 프레임으로 이동 버튼 이벤트
    /// </summary>
    protected abstract void _OnClickSetFirstFrame();
    /// <summary>
    /// 플레이 버튼 리스트에서 일시정지 버튼 이벤트
    /// </summary>
    protected abstract void _OnClickSetPause();

    #endregion Abstract Methods

    private void OnGUI()
    {
        if (EditorApplication.isCompiling)
        {
            GUILayout.Label("Compiling...");
            return;
        }

        if (_IsEnableGUI() == false)
        {
            return;
        }

        var scrollRect1 = Rect.MinMaxRect(0, _centerRect.yMin, _ScreenWidth, _ScreenHeight - 5);
        var scrollRect2 = Rect.MinMaxRect(0, _centerRect.yMin, _ScreenWidth, _totalHeight + 150);

        var cEvent = Event.current;
        _scrollPos = GUI.BeginScrollView(scrollRect1, _scrollPos, scrollRect2);

        _InitRect();

        _MouseEvent(cEvent);

        _DrawContents(_leftRect, cEvent);

        //Timelines
        _TimeLine.SetRect(_topMiddleRect, _centerRect);
        _TimeLine.DrawScroll();

        GUI.EndScrollView();

        _DrawPlayButtons(_topLeftRect, targetTimeLine);

        _TimeLine.DrawTime();

        DrawerBasic.BackGUIAllColors();
    }
}

