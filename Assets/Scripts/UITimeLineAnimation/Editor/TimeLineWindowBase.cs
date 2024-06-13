using System;
using TimeLineInterface;
using UnityEditor;
using UnityEngine;
public abstract class TimeLineLayoutWindowBase : EditorWindow
{
    private TimeLineCore _timeLine;

    protected TimeLineCore _Core
    {
        get
        {
            if (_timeLine == null || _timeLine.currentTimeLine == null)
            {
                _timeLine = new TimeLineCore();
                _timeLine.Init(targetTimeLine);
            }
            return _timeLine;
        }
    }

    protected const float Out_Margin             = 4;
    protected const float Content_Gab            = 2;
    protected const float ToolBar_Height         = 40; //top margin AFTER the toolbar
    protected const float First_Group_Top_Margin = 10; //initial top margin

    #region Color

    protected Color _GroupTitleColor => new(0.50f, 0.50f, 0.50f, 0.5f);

    protected Color _GroupSubTitleColor => EditorGUIUtility.isProSkin ? new Color(0.10f, 0.10f, 0.10f, 0.5f) : new Color(0.90f, 0.90f, 0.90f, 0.5f);

    protected Color _GroupContentColor => EditorGUIUtility.isProSkin ? new Color(0.05f, 0.05f, 0.05f, 0.5f) : new Color(0.95f, 0.95f, 0.95f, 0.5f);

    protected Color _TrackOddColor => EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f, 0.5f) : new Color(0.85f, 0.85f, 0.85f, 0.5f);

    protected Color _TrackEvenColor => EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.20f, 0.5f) : new Color(0.80f, 0.80f, 0.80f, 0.5f);

    #endregion Color

    private float _screenWidth  { get { return position.width; } }
    private float _screenHeight { get { return position.height; } }

    private float _LeftMargin
    {
        get { return _trackListLeftMargin; }
        set { _trackListLeftMargin = Mathf.Clamp(value, 240, Screen.width / 2); }
    }

    protected abstract ITimeLineObject targetTimeLine { get; }

    private   bool    _isResizingLeftMargin = false;
    private   float   _trackListLeftMargin  = 300f;
    protected float   _totalHeight          = 0;
    private   Vector2 _scrollPos            = Vector2.zero;

    private Rect _topLeftRect;   // 플레이 버튼 렉트
    private Rect _topMiddleRect; // 시간 정보 렉트
    private Rect _leftRect;      // Group/Track 리스트용 렉트
    private Rect _centerRect;    // 타임라인 렉트

    /// <summary>
    /// 렉트 초기화 함수
    /// </summary>
    private void _InitRect()
    {
        _topLeftRect = Rect.MinMaxRect(Out_Margin, Out_Margin, Out_Margin + _LeftMargin, ToolBar_Height);
        _centerRect = Rect.MinMaxRect(_topLeftRect.xMax + Content_Gab, _topLeftRect.yMax + Content_Gab, _screenWidth - Out_Margin, _screenHeight - Out_Margin);

        _leftRect = Rect.MinMaxRect(_topLeftRect.xMin, _topLeftRect.yMax + Content_Gab, _topLeftRect.xMax, _screenHeight - Out_Margin);
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
        GUIBasicDrawer.DrawTexture(lScaleRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

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
            GUIBasicDrawer.DrawTexture(lContentRect, pBackColor);
        }
        else
        {
            Rect lLineRect = new Rect(pTitleRect.x, pTitleRect.y, 4 * pDepth, pTitleRect.height);
            lContentRect = Rect.MinMaxRect(lLineRect.xMax, pTitleRect.yMin, pTitleRect.xMax, pTitleRect.yMax);

            GUIBasicDrawer.DrawTexture(lLineRect, _GroupTitleColor);
            GUIBasicDrawer.DrawTexture(lContentRect, pBackColor);
        }
        _Core.DrawTexture(pTitleRect.yMin, pTitleRect.yMax, pBackColor);
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
        GUIBasicDrawer.DrawToggleLabel(new Rect(lContentRect.x, lContentRect.y, 16, lContentRect.height), ref pViewer, Color.white);
        GUI.Label(new Rect(lContentRect.x + 16, lContentRect.y, lContentRect.width - 16, lContentRect.height), pTitleString);
        return lContentRect;
    }

    /// <summary>
    /// 재생 버튼들을 그리는 함수
    /// </summary>
    /// <param name="pRect"></param>
    /// <param name="pTargetObject"></param>
    protected void _DrawPlayButtons(Rect pRect, ITimeLineObject pTargetObject)
    {
        GUIBasicDrawer.GUIBackgroundColor(Color.grey);
        GUIBasicDrawer.GUIContentColor(Color.white);

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
        if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.stepReverseIcon, (GUIStyle)"box"))
        {
            _OnClickSetFirstFrame();
        }
        ++lIndex;
        if (pTargetObject.IsPlaying)
        {
            Rect lRect = lGetRect(lIndex);
            lRect.width = lRect.width + lBaseWidth + 4;
            if (GUI.Button(lRect, GUIBasicDrawer.TimeLineStyles.pauseIcon, (GUIStyle)"box"))
            {
                _OnClickSetPause();
            }
            ++lIndex;
            ++lIndex;
        }
        else
        {
            if (pTargetObject.CurrentTime == 0)
            {
                //포워드만    
                Rect tRect = lGetRect(lIndex);
                tRect.width = tRect.width + lBaseWidth + 4;
                if (GUI.Button(tRect, GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayForward();
                }
                ++lIndex;
                ++lIndex;
            }
            else if (pTargetObject.CurrentTime == pTargetObject.Length)
            {
                //백워드만
                Rect tRect = lGetRect(lIndex);
                tRect.width = tRect.width + lBaseWidth + 4;
                GUIBasicDrawer.BeginGUIRotate(180f, tRect);
                if (GUI.Button(tRect, GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayBackward();
                }
                GUIBasicDrawer.EndGUIRotate();
                ++lIndex;
                ++lIndex;
            }
            else
            {
                GUIBasicDrawer.BeginGUIRotate(180f, lGetRect(lIndex));
                if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayBackward();
                }
                GUIBasicDrawer.EndGUIRotate();
                ++lIndex;
                if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                {
                    _OnClickPlayForward();
                }
                ++lIndex;
            }
        }
        if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.stepIcon, (GUIStyle)"box"))
        {
            _OnClickSetLastFrame();
        }

        GUIBasicDrawer.BackGUIAllColors();
    }

    #region Abstract Methods
    /// <summary>
    /// 현재 선택중인 클립 정보를 얻는 함수
    /// </summary>
    /// <returns></returns>
    public abstract IClip GetSelectClip();
    public abstract IClip GetCurrentTimeClip();
    /// <summary>
    /// 타임라인에서 마우스로 트랙 및 시간을 선택했을때 함수
    /// </summary>
    /// <param name="pGroup"></param>
    /// <param name="pTrack"></param>
    /// <param name="pTime"></param>
    protected abstract void _SelectTrackAndTime(IGroup pGroup = null, ITrack pTrack = null, float pTime = -1);

    protected abstract void _SelectClip(IClip pClip = null);
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

        var lScrollRect1 = Rect.MinMaxRect(0, _centerRect.yMin, _screenWidth, _screenHeight - 5);
        var lScrollRect2 = Rect.MinMaxRect(0, _centerRect.yMin, _screenWidth, _totalHeight + 150);

        var lEvent = Event.current;
        _scrollPos = GUI.BeginScrollView(lScrollRect1, _scrollPos, lScrollRect2);

        _InitRect();

        _MouseEvent(lEvent);

        _DrawContents(_leftRect, lEvent);

        _Core.SetRect(_topMiddleRect, _centerRect);
        _Core.DrawScroll();

        GUI.EndScrollView();

        _DrawPlayButtons(_topLeftRect, targetTimeLine);

        _Core.DrawTime();

        GUIBasicDrawer.BackGUIAllColors();
    }
}
