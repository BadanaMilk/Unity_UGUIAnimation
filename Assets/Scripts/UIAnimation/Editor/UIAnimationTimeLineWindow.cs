using UnityEngine;
using UnityEditor;
using UIAnimationTimeLine;
using TimeLineLayoutBase;

public class UIAnimationTimeLineWindow : TimeLineLayoutWindowBase
{
    public static UIAnimationTimeLineWindow targetWindow;

    protected override IDrawerTimeLine targetTimeLine => targetTween;

    public static void ShowWindow(UIAnimation pTween, UIAnimationEditor pTweenEditor)
    {
        if (targetWindow == null)
        {
            targetWindow = EditorWindow.GetWindow(typeof(UIAnimationTimeLineWindow)) as UIAnimationTimeLineWindow;
            targetWindow.minSize = new Vector2(600, 300);
        }

        targetWindow._Initialize(pTween, pTweenEditor);
        targetWindow.Show();
    }

    UIAnimation _targetTween;
    UIAnimationEditor _targetEditor;

    public UIAnimation targetTween => _targetTween;
    public UIAnimationEditor targetEditor => _targetEditor;

    private void OnEnable()
    {
        targetWindow = this;
        EditorApplication.update += _EditorUpdate;
    }

    private void OnDestroy()
    {
        targetWindow = null;
        EditorApplication.update -= _EditorUpdate;
    }

    public bool IsTargetTween(UIAnimation pTween, UIAnimationEditor pTweenEditor)
    {
        return _targetTween == pTween && _targetEditor == pTweenEditor;
    }

    private void _Initialize(UIAnimation pTween, UIAnimationEditor pTweenEditor)
    {
        titleContent = new GUIContent("UI Animation Time Line");

        if (pTweenEditor != null)
        {
            _targetTween = pTween;
            _targetEditor = pTweenEditor;

            if (targetTween != null)
            {
                if (!Application.isPlaying)
                    Stop(eStopMode.Pause);
            }
        }
        _TimeLine.Init(targetTween);
    }

    private void _AddActorGroupSelect()
    {
        GameObject[] lObjects = Selection.gameObjects;
        if (lObjects.Length == 0 || (lObjects.Length == 1 && lObjects[0] == targetTween.gameObject))
        {
            if (targetTween.IsContainObject(lObjects[0]) == false)
            {
                targetTween.AddGroup(null);
            }
        }
        else
        {
            for (int lIndex = 0; lIndex < lObjects.Length; ++lIndex)
            {
                if (targetTween.IsContainObject(lObjects[lIndex]) == false)
                {
                    targetTween.AddGroup(lObjects[lIndex]);
                }
            }
        }
    }

    private void _AddTrack(UIAnimationGroup pGroup, eTrackType pTrackType)
    {
        pGroup.AddTrack(pTrackType);
    }

    private void _ShowRootGroup(Rect pBaseRect, Event pEvent, ref float pNextYPos)
    {
        for (int lIndex = 0; lIndex < targetTween.Count; ++lIndex)
        {
            _ShowGroup(pBaseRect, pEvent, targetTween[lIndex], lIndex, ref pNextYPos);
            pNextYPos += 6;
        }
    }

    private void _ShowGroupsAndTracksList(Rect pBaseRect, Event pEvent)
    {
        float lNextYPos = First_Group_Top_Margin;

        _ShowRootGroup(pBaseRect, pEvent, ref lNextYPos);
        _totalHeight = lNextYPos;

        var lAddButtonY = _totalHeight + Out_Margin + ToolBar_Height + 20;
        var lAddRect = Rect.MinMaxRect(pBaseRect.xMin + 5, lAddButtonY, pBaseRect.xMax - 5, lAddButtonY + 20);
        GUI.color = Color.white;
        if (GUI.Button(lAddRect, "Add Actor Group"))
        {
            targetTween.AddGroup(null);
        }
        lAddButtonY += 24;
        lAddRect = Rect.MinMaxRect(pBaseRect.xMin + 5, lAddButtonY, pBaseRect.xMax - 5, lAddButtonY + 20);
        if (GUI.Button(lAddRect, "Add Group (Selection)"))
        {
            _AddActorGroupSelect();
        }
    }

    private void _ShowGroup(Rect pBaseRect, Event pEvent, UIAnimationGroup pGroup, int pIndex, ref float pNextPosY)
    {
        Rect lGroupRect = _GetRect(pBaseRect, 20, ref pNextPosY);
        _AddCursorRect(lGroupRect, targetEditor.selectGroup == null ? MouseCursor.Link : MouseCursor.MoveArrow);

        ReferenceEquals(pGroup, targetEditor.selectGroup);
        _DrawContentTexture(lGroupRect, _GroupTitleColor, 0);

        bool lViewState = pGroup.mViewAll;
        DrawerBasic.DrawToggleLabel(new Rect(lGroupRect.x, lGroupRect.y, 16, lGroupRect.height), ref pGroup.mViewAll, Color.white);
        if (pGroup.mLockEditor)
        {
            Rect lLockRect = Rect.MinMaxRect(0, 0, 16, 16);
            lLockRect.center = new Vector2(lGroupRect.xMin + 16 + 8, lGroupRect.yMin + 8);
            DrawerBasic.DrawTexture(lLockRect, Color.white, DrawerBasic.TimeLineStyles.lockIcon.image);
        }
        GUI.Label(new Rect(lGroupRect.x + 32, lGroupRect.y, lGroupRect.width - 16, lGroupRect.height), pGroup.target == null ? "Group" : $"Group : {pGroup.target.name}");

        // Plus Button
        bool lPlusClicked = false;
        DrawerBasic.GUIColor(EditorGUIUtility.isProSkin ? Color.white : Color.black);
        Rect lPlusRect = new Rect(lGroupRect.xMax - 14, lGroupRect.y + 5, 8, 8);
        if (GUI.Button(lPlusRect, DrawerBasic.TimeLineStyles.plusIcon, GUIStyle.none))
        {
            lPlusClicked = true;
            pGroup.mViewAll = lViewState;
        }

        bool lPrevEnable = GUI.enabled;
        GUI.enabled = pGroup.mLockEditor == false;
        DrawerBasic.GUIColor(Color.white);
        // Plus Button or Right Click Content
        if (lPlusClicked || (pEvent.type == EventType.ContextClick && lGroupRect.Contains(pEvent.mousePosition)))
        {
            GenericMenu lMenu = new GenericMenu();

            if (!pGroup.mLockEditor)
            {
                lMenu.AddItem(new GUIContent("Editor Lock"), pGroup.mLockEditor, () =>
                {
                    pGroup.mLockEditor = true;
                });
            }
            else
            {
                lMenu.AddItem(new GUIContent("Editor UnLock"), pGroup.mLockEditor, () =>
                {
                    pGroup.mLockEditor = false;
                });
            }

            if (pGroup.target != null)
            {
                for(eTrackType lType = eTrackType.Active; lType < eTrackType.End; lType++)
                {
                    if (pGroup.IsContainTrack(lType) == false && UIAnimationTrack.IsAvailableTrackType(lType, pGroup.target))
                        lMenu.AddItem(new GUIContent(string.Format("Track/{0}", lType.ToString())), false, () => { _AddTrack(pGroup, lType); });
                }
            }

            lMenu.AddSeparator("/");
            lMenu.AddItem(new GUIContent("Delete Group"), false, () => { targetTween.RemoveGroup(pIndex); });

            lMenu.ShowAsContext();
            pEvent.Use();
        }

        if (pGroup.mViewAll)
        {
            // Group
            {
                Rect lTitleRect = _GetRect(pBaseRect, 20, ref pNextPosY);
                _DrawTitle(lTitleRect, _GroupSubTitleColor, ref pGroup.mViewBasic, "Base");

                if (pGroup.mViewBasic)
                {
                    Rect objectRect = _GetRect(pBaseRect, 20, ref pNextPosY);
                    objectRect = _DrawContentTexture(objectRect, _GroupContentColor, 2);
                    GameObject lNewTarget = (GameObject)EditorGUI.ObjectField(objectRect, "Target", pGroup.target, typeof(GameObject), true);
                    if(lNewTarget != pGroup.target)
                        pGroup.SetTarget(lNewTarget);
                }
            }

            // Tracks
            if (pGroup.target != null)
            {
                Rect lTitleRect = _GetRect(pBaseRect, 20, ref pNextPosY);
                lTitleRect = _DrawContentTexture(lTitleRect, _GroupSubTitleColor, 1);
                DrawerBasic.DrawToggleLabel(new Rect(lTitleRect.x, lTitleRect.y, 16, lTitleRect.height), ref pGroup.mViewTracks, Color.white);
                GUI.Label(new Rect(lTitleRect.x + 16, lTitleRect.y, lTitleRect.width - 16, lTitleRect.height), "Tracks");

                lPlusClicked = false;
                DrawerBasic.GUIColor(EditorGUIUtility.isProSkin ? Color.white : Color.black);
                lPlusRect = new Rect(lTitleRect.xMax - 14, lTitleRect.y + 5, 8, 8);
                if (GUI.Button(lPlusRect, DrawerBasic.TimeLineStyles.plusIcon, GUIStyle.none))
                {
                    lPlusClicked = true;
                    pGroup.mViewAll = lViewState;
                }
                if (lPlusClicked || (pEvent.type == EventType.ContextClick && lTitleRect.Contains(pEvent.mousePosition)))
                {
                    GenericMenu lMenu = new GenericMenu();

                    for (eTrackType lType = eTrackType.Active; lType < eTrackType.End; lType++)
                    {
                        if (pGroup.IsContainTrack(lType) == false && UIAnimationTrack.IsAvailableTrackType(lType, pGroup.target))
                        {
                            eTrackType lSelectType = lType;
                            lMenu.AddItem(new GUIContent(string.Format("{0}", lSelectType.ToString())), false, () => { _AddTrack(pGroup, lSelectType); });
                        }
                    }

                    lMenu.ShowAsContext();
                    pEvent.Use();
                }

                if (pGroup.mViewTracks)
                {
                    _ShowTracks(pBaseRect, ref pNextPosY, pEvent, pGroup);
                }
            }

            Rect lEndRect = _GetRect(pBaseRect, 1, ref pNextPosY);
            _DrawContentTexture(lEndRect, _GroupTitleColor, 0);
        }
        DrawerBasic.BackGUIAllColors();
        GUI.enabled = lPrevEnable;
    }

    private void _ShowTracks(Rect pBaseRect, ref float pNextYPos, Event pEvent, UIAnimationGroup pGroup)
    {
        Rect pTrackRect;
        for (int lIndex = 0; lIndex < pGroup.count; ++lIndex)
        {
            UIAnimationTrack pTrack = pGroup[lIndex];
            pTrackRect = _GetRect(pBaseRect, 20, ref pNextYPos);
            Color pTrackBGColor = lIndex % 2 == 0 ? _TrackEvenColor : _TrackOddColor;

            pTrackRect = _DrawContentTexture(pTrackRect, pTrackBGColor, 2);
            GUI.Label(pTrackRect, pTrack.trackType.ToString());

            if (pEvent.type == EventType.ContextClick && pTrackRect.Contains(pEvent.mousePosition))
            {
                GenericMenu lMenu = new GenericMenu();

                lMenu.AddItem(new GUIContent("Remove Track"), false, () => { pGroup.RemoveTrack(pTrack.trackType); });
                lMenu.ShowAsContext();
            }

            _TimeLine.DrawTrack(pTrackRect.yMin, pTrackRect.yMax, pGroup, pTrack, pEvent);
        }
    }

    private void Play(ePlayMode pPlayMode)
    {
        targetTween.Play(pPlayMode);
    }

    private void Stop(eStopMode pStopMode)
    {
        targetTween.Stop(pStopMode);
    }

    private void _EditorUpdate()
    {
        if(_targetEditor != null)
            _targetEditor.editorUpdate();
    }

    public override IClip GetSelectClip()
    {
        if (targetEditor.selectTrack != null)
        {
            return targetEditor.selectTrack.GetClip(targetEditor.selectTime);
        }
        return null;
    }

    protected override void _SelectTrackAndTime(IGroup pGroup = null, ITrack pTrack = null, float pTime = -1)
    {
        targetEditor.SelectTrackTime(pGroup, pTrack, pTime);
    }

    protected override void _OnLinkPevClipEvent(IClip pPrev, IClip pCurrent)
    {
        UIAnimationClip lCurent = pCurrent as UIAnimationClip;
        UIAnimationClip lPrev = pPrev as UIAnimationClip;
        lCurent.SetStartValue(lPrev.endValue);
    }
    protected override void _OnLinkNextClipEvent(IClip pCurrent, IClip pNext)
    {
        UIAnimationClip lCurent = pCurrent as UIAnimationClip;
        UIAnimationClip lNext = pNext as UIAnimationClip;
        lCurent.SetEndValue(lNext.startValue);
    }

    protected override void _DrawContents(Rect pLeftRect, Event pMouseEvent)
    {
        _ShowGroupsAndTracksList(pLeftRect, pMouseEvent);
    }

    protected override bool _IsEnableGUI()
    {
        if (targetTween == null)
        {
            GUILayout.Label("Select to [UI Animation Time Line] and [Edit] Button.");
            return false;
        }
        else
            return true;
    }

    protected override void _OnClickPlayForward()
    {
        Play(ePlayMode.Forward_CurrentAt);
    }

    protected override void _OnClickPlayBackward()
    {
        Play(ePlayMode.Backward_CurrentAt);
    }

    protected override void _OnClickSetLastFrame()
    {
        Stop(eStopMode.LastFrame);
    }

    protected override void _OnClickSetFirstFrame()
    {
        Stop(eStopMode.FirstFrame);
    }

    protected override void _OnClickSetPause()
    {
        Stop(eStopMode.Pause);
    }
}


