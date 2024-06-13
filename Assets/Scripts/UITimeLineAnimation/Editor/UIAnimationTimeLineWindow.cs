using TimeLineInterface;
using UITimeLineAnimation;
using UITimeLineAnimation.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class UIAnimationTimeLineWindow : TimeLineLayoutWindowBase
{
    public static UIAnimationTimeLineWindow targetWindow;
    protected override ITimeLineObject targetTimeLine => _targetAnimation;

    public static UIAnimationTimeLineWindow ShowWindow(UIAnimation pAnimation, UIAnimationEditor pEditor)
    {
        if (targetWindow == null)
        {
            targetWindow = EditorWindow.GetWindow(typeof(UIAnimationTimeLineWindow)) as UIAnimationTimeLineWindow;
            targetWindow.minSize = new Vector2(600, 300);
        }

        targetWindow._Initialize(pAnimation, pEditor);
        targetWindow.Show();
        targetWindow.Repaint();
        return targetWindow;
    }

    private UIAnimation       _targetAnimation;
    private UIAnimationEditor _targetEditor;
    
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

    public bool IsLinkedEditor(UIAnimation pTween, UIAnimationEditor pTweenEditor)
    {
        return _targetAnimation == pTween && _targetEditor == pTweenEditor;
    }

    private void _Initialize(UIAnimation pTargetObject, UIAnimationEditor pTargetEditor)
    {
        titleContent = new GUIContent("UI Animation Time Line");

        if (pTargetEditor != null)
        {
            _targetAnimation = pTargetObject;
            _targetEditor = pTargetEditor;

            if (_targetAnimation != null)
            {
                if (!Application.isPlaying)
                    Stop(eStopMode.Pause);
            }
        }
        _Core.Init(_targetAnimation);
        _Core.SetBindingMethod(GetSelectClip);
        _Core.SetTrackClickEvent(_SelectTrackAndTime);
        _Core.SetClipClickEvent(_SelectClip);
        
        _Core.SetDragClipEvent((pTrack, pClip, pPositionTime) =>
        {
            IClip lBeforeClip = pTrack.GetPrevClip(pClip);
            IClip lNextClip = pTrack.GetNextClip(pClip);
            float lDuration = pClip.Duration;
            
            var lStartValue = pClip.StartTime + pPositionTime; //pPositionTime - (lDuration * 0.5f);
            var lEndValue = pClip.EndTime + pPositionTime;   //pPositionTime + (lDuration * 0.5f);

            if (pPositionTime < 0)
            {
                var lStartMinValue = lBeforeClip?.EndTime ?? 0;
                lStartValue = lStartValue < lStartMinValue ? lStartMinValue : lStartValue;
                lEndValue = lStartValue + lDuration;
            }
            else
            {
                var lEndMaxValue = lNextClip?.StartTime ?? targetTimeLine.Length;
                lEndValue = lEndValue > lEndMaxValue ? lEndMaxValue : lEndValue;
                lStartValue = lEndValue - lDuration;
            }
            // var lStartMinValue = lBeforeClip?.EndTime ?? 0;
            // var lStartMaxValue = lEndValue - lDuration;
            // lStartValue = Mathf.Clamp(lStartValue, lStartMinValue, lStartMaxValue);
            //
            // var lEndMinValue = lStartValue + lDuration;
            // var lEndMaxValue = lNextClip?.StartTime ?? targetTimeLine.Length;
            // lEndValue = Mathf.Clamp(lEndValue, lEndMinValue, lEndMaxValue);
            
            pClip.SetStartTime(lStartValue);
            pClip.SetEndTime(lEndValue);
            Repaint();
        });
        
        _Core.SetClipContextClickEvent((pTrack, pClip, pPositionTime) =>
        {
            GenericMenu lMenu = new GenericMenu();

            IClip lBeforeClip = pTrack.GetPrevClip(pClip);
            IClip lNextClip = pTrack.GetNextClip(pClip);

            if (lBeforeClip != null)
            {
                lMenu.AddItem(new GUIContent("Link Prev Clip Time"), false, () =>
                {
                    pClip.SetStartTime(lBeforeClip.EndTime);
                });
            }

            if (lNextClip != null)
            {
                lMenu.AddItem(new GUIContent("Link Next Clip Time"), false, () =>
                {
                    pClip.SetEndTime(lNextClip.StartTime);
                });
            }

            lMenu.AddItem(new GUIContent("Remove Clip"), false, () => { pTrack.RemoveClip(pPositionTime); });
            lMenu.ShowAsContext();
        });
    }

    private void _AddActorGroupSelect()
    {
        GameObject[] lObjects = Selection.gameObjects;
        if (lObjects.Length == 0 || (lObjects.Length == 1 && lObjects[0] == _targetAnimation.gameObject))
        {
            if (_targetAnimation.IsContainObject(lObjects[0]) == false)
            {
                _targetAnimation.AddGroup(null);
            }
        }
        else
        {
            for (int lIndex = 0; lIndex < lObjects.Length; ++lIndex)
            {
                if (_targetAnimation.IsContainObject(lObjects[lIndex]) == false)
                {
                    _targetAnimation.AddGroup(lObjects[lIndex]);
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
        for (int lIndex = 0; lIndex < _targetAnimation.Count; ++lIndex)
        {
            _ShowGroup(pBaseRect, pEvent, _targetAnimation[lIndex], lIndex, ref pNextYPos);
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
            _targetAnimation.AddGroup(null);
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
        _AddCursorRect(lGroupRect, _targetEditor.selectGroup == null ? MouseCursor.Link : MouseCursor.MoveArrow);
        _DrawContentTexture(lGroupRect, _GroupTitleColor, 0);

        bool lViewState = pGroup.mViewAll;
        GUIBasicDrawer.DrawToggleLabel(new Rect(lGroupRect.x, lGroupRect.y, 16, lGroupRect.height), ref pGroup.mViewAll, Color.white);
        if (pGroup.mLockEditor)
        {
            Rect lLockRect = Rect.MinMaxRect(0, 0, 16, 16);
            lLockRect.center = new Vector2(lGroupRect.xMin + 16 + 8, lGroupRect.yMin + 8);
            GUIBasicDrawer.DrawTexture(lLockRect, Color.white, GUIBasicDrawer.TimeLineStyles.lockIcon.image);
        }
        GUI.Label(new Rect(lGroupRect.x + 32, lGroupRect.y, lGroupRect.width - 16, lGroupRect.height), pGroup.Target == null ? "Group" : $"Group : {pGroup.Target.name}");

        // Plus Button
        bool lPlusClicked = false;
        GUIBasicDrawer.GUIColor(EditorGUIUtility.isProSkin ? Color.white : Color.black);
        Rect lPlusRect = new Rect(lGroupRect.xMax - 14, lGroupRect.y + 5, 8, 8);
        if (GUI.Button(lPlusRect, GUIBasicDrawer.TimeLineStyles.plusIcon, GUIStyle.none))
        {
            lPlusClicked = true;
            pGroup.mViewAll = lViewState;
        }

        bool lPrevEnable = GUI.enabled;
        GUI.enabled = pGroup.mLockEditor == false;
        GUIBasicDrawer.GUIColor(Color.white);
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

            if (pGroup.Target != null)
            {
                for(eTrackType lType = eTrackType.Active; lType < eTrackType.End; lType++)
                {
                    if (pGroup.IsContainTrack(lType) == false && UIAnimationTrack.IsAvailableTrackType(lType, pGroup.Target))
                        lMenu.AddItem(new GUIContent($"Track/{lType.ToString()}"), false, () => { _AddTrack(pGroup, lType); });
                }
            }

            lMenu.AddSeparator("/");
            lMenu.AddItem(new GUIContent("Delete Group"), false, () => { _targetAnimation.RemoveGroup(pIndex); });

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
                    GameObject lNewTarget = (GameObject)EditorGUI.ObjectField(objectRect, "Target", pGroup.Target, typeof(GameObject), true);
                    if(lNewTarget != pGroup.Target)
                        pGroup.SetTarget(lNewTarget);
                }
            }

            // Tracks
            if (pGroup.Target != null)
            {
                Rect lTitleRect = _GetRect(pBaseRect, 20, ref pNextPosY);
                lTitleRect = _DrawContentTexture(lTitleRect, _GroupSubTitleColor, 1);
                GUIBasicDrawer.DrawToggleLabel(new Rect(lTitleRect.x, lTitleRect.y, 16, lTitleRect.height), ref pGroup.mViewTracks, Color.white);
                GUI.Label(new Rect(lTitleRect.x + 16, lTitleRect.y, lTitleRect.width - 16, lTitleRect.height), "Tracks");

                lPlusClicked = false;
                GUIBasicDrawer.GUIColor(EditorGUIUtility.isProSkin ? Color.white : Color.black);
                lPlusRect = new Rect(lTitleRect.xMax - 14, lTitleRect.y + 5, 8, 8);
                if (GUI.Button(lPlusRect, GUIBasicDrawer.TimeLineStyles.plusIcon, GUIStyle.none))
                {
                    lPlusClicked = true;
                    pGroup.mViewAll = lViewState;
                }
                if (lPlusClicked || (pEvent.type == EventType.ContextClick && lTitleRect.Contains(pEvent.mousePosition)))
                {
                    GenericMenu lMenu = new GenericMenu();

                    for (eTrackType lType = eTrackType.Active; lType < eTrackType.End; lType++)
                    {
                        if (pGroup.IsContainTrack(lType) == false && UIAnimationTrack.IsAvailableTrackType(lType, pGroup.Target))
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
        GUIBasicDrawer.BackGUIAllColors();
        GUI.enabled = lPrevEnable;
    }

    private void _ShowTracks(Rect pBaseRect, ref float pNextYPos, Event pEvent, UIAnimationGroup pGroup)
    {
        Rect pTrackRect;
        for (int lIndex = 0; lIndex < pGroup.Count; ++lIndex)
        {
            UIAnimationTrack pTrack = pGroup[lIndex];
            pTrackRect = _GetRect(pBaseRect, 20, ref pNextYPos);
            Color pTrackBGColor = lIndex % 2 == 0 ? _TrackEvenColor : _TrackOddColor;

            pTrackRect = _DrawContentTexture(pTrackRect, pTrackBGColor, 2);
            GUI.Label(pTrackRect, pTrack.TrackType.ToString());

            if (pEvent.type == EventType.ContextClick && pTrackRect.Contains(pEvent.mousePosition))
            {
                GenericMenu lMenu = new GenericMenu();

                lMenu.AddItem(new GUIContent("Remove Track"), false, () => { pGroup.RemoveTrack(pTrack.TrackType); });
                lMenu.ShowAsContext();
            }

            _Core.DrawTrack(pTrackRect.yMin, pTrackRect.yMax, pGroup, pTrack, pEvent);
        }
    }

    private void Play(ePlayMode pPlayMode)
    {
        _targetAnimation.Play(pPlayMode);
    }

    private void Stop(eStopMode pStopMode)
    {
        _targetAnimation.Stop(pStopMode);
    }

    private void _EditorUpdate()
    {
        if(_targetEditor != null)
            _targetEditor.editorUpdate();
    }

    public override IClip GetSelectClip()
    {
        if (_targetEditor.selectClip != null)
        {
            return _targetEditor.selectClip;
        }
        return null;
    }

    public override IClip GetCurrentTimeClip()
    {
        if (_targetEditor.selectTrack != null)
        {
            return _targetEditor.selectTrack.GetClip(_targetEditor.selectTime);
        }
        return null;
    }

    protected override void _SelectTrackAndTime(IGroup pGroup = null, ITrack pTrack = null, float pTime = -1)
    {
        _targetEditor.SelectTrackTime(pGroup, pTrack, pTime);
        Repaint();
    }

    protected override void _SelectClip(IClip pClip = null)
    {
        _targetEditor.SelectClip(pClip);
        Repaint();
    }
    
    protected override void _DrawContents(Rect pLeftRect, Event pMouseEvent)
    {
        _ShowGroupsAndTracksList(pLeftRect, pMouseEvent);
    }

    protected override bool _IsEnableGUI()
    {
        if (_targetAnimation == null)
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


