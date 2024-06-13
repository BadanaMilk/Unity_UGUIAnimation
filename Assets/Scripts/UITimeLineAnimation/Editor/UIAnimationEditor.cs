using System;
using TimeLineInterface;
using UnityEditor;
using UnityEngine;

namespace UITimeLineAnimation.Editor
{
    [CustomEditor(typeof(UIAnimation))]
    public class UIAnimationEditor : UnityEditor.Editor
    {
        private readonly Color _titleColor   = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private readonly float _contentSpace = 30f;
    
        public void ShowUIAnimationWindow()
        {
            _timeLineWindow = UIAnimationTimeLineWindow.ShowWindow(_targetObject, this);
        }

        public void SelectTrackTime(IGroup pGroup = null, ITrack pTrack = null, float pTime = -1)
        {
            _selectGroup = pGroup as UIAnimationGroup;
            _selectTrack = pTrack as UIAnimationTrack;
            _selectTime = pTime;
            Repaint();
        }

        public void SelectClip(IClip pClip)
        {
            _selectClip = pClip as UIAnimationClip;
            Repaint();
        }

        private UIAnimationTimeLineWindow _timeLineWindow;
        private UIAnimation               _targetObject;

        private UIAnimationGroup _selectGroup;
        private UIAnimationTrack _selectTrack;
        private UIAnimationClip  _selectClip;

        private float _selectTime;

        private SerializedProperty _listGroupProperty;
    
        public UIAnimationGroup selectGroup { get { return _selectGroup; } }
        public UIAnimationTrack selectTrack { get { return _selectTrack; } }
        public UIAnimationClip  selectClip  { get { return _selectClip; } }
    
        public float                              selectTime   { get { return _selectTime; } }
        public EditorApplication.CallbackFunction editorUpdate => _OnEditorUpdate;
    
        private void OnEnable()
        {
            var lTargetAnimationObject = (UIAnimation)target;
            _targetObject = lTargetAnimationObject;
            if (UIAnimationTimeLineWindow.targetWindow != null && UIAnimationTimeLineWindow.targetWindow.IsLinkedEditor(_targetObject, this) == false)
            {
                _timeLineWindow = UIAnimationTimeLineWindow.ShowWindow(_targetObject, this);
            }

            _listGroupProperty = serializedObject.FindProperty("_listAniGroup");
            EditorApplication.update += editorUpdate;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= editorUpdate;
        }

        public override void OnInspectorGUI()
        {
            GUI.skin.label.richText = true;
            EditorStyles.label.richText = true;
            EditorStyles.foldout.richText = true;
        
            DrawDefaultInspector();

            GUILayout.Space(10f);

            #region Mode & Time Inspector
        
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            _targetObject.WrapMode = (eWrapMode)EditorGUI.EnumPopup(lRect, "Wrap Mode", _targetObject.WrapMode);

            lRect = EditorGUILayout.GetControlRect(true, 20);
            _targetObject.PlayMode = (ePlayMode)EditorGUI.EnumPopup(lRect, "Play Mode", _targetObject.PlayMode);

            lRect = EditorGUILayout.GetControlRect(true, 20);
            _targetObject.AutoPlay = EditorGUI.Toggle(lRect, "Auto Play", _targetObject.AutoPlay);

            lRect = EditorGUILayout.GetControlRect(true, 20);
            _targetObject.Length = EditorGUI.FloatField(lRect, "Animation Time", _targetObject.Length);
        
            #endregion Mode & Time Inspector
        
            if (EditorApplication.isCompiling)
                return;

            #region Control Inspector

            bool lIsEditing = (UIAnimationTimeLineWindow.targetWindow != null);
            lRect = EditorGUILayout.GetControlRect(true, 20);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.white : Color.grey;
            GUIBasicDrawer.TimeLineStyles.guiStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            GUI.enabled = !lIsEditing;
        
            if (GUI.Button(new Rect(lRect.x, lRect.y, 20, 20), GUIBasicDrawer.TimeLineStyles.editIcon, (GUIStyle)"box"))
            {
                ShowUIAnimationWindow();
            }
        
            Rect subRect = Rect.MinMaxRect(lRect.xMin + 24, lRect.yMin, lRect.xMax, lRect.yMax);
            if (lIsEditing)
            {
                GUI.Label(subRect, "Editing. Show UI Animatioin Time Line Window please.");
            }
            else
            {
                _DrawPlayButtons(subRect);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUIBasicDrawer.BackGUIAllColors();
        
            #endregion Control Inspector

            #region Clip Inspector
            if (_selectClip != null)
            {
                _DrawClipInspector(selectGroup, selectTrack, selectTime);
            }
            #endregion Clip Inspector
        
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                //UIAnimationTimeLineWindow.targetWindow.Repaint();
            }
            GUIBasicDrawer.BackGUIAllColors();
        }

        private void _Play(ePlayMode pPlayMode)
        {
            _targetObject.Play(pPlayMode);
        }

        private void _Stop(eStopMode pStopMode)
        {
            _targetObject.Stop(pStopMode);
        }

        private void _DrawPlayButtons(Rect pRect)
        {
            GUI.backgroundColor = Color.grey;
            GUI.contentColor = Color.white;

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

            Func<byte, Rect> lGetRect = delegate (byte index)
            {
                return new Rect(pRect.x + ((lBaseWidth + 4) * index), pRect.y, lBaseWidth, lBaseHeight);
            };

            byte lIndex = 0;
            if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.stepReverseIcon, (GUIStyle)"box"))
            {
                _Stop(eStopMode.FirstFrame);
            }
            ++lIndex;
            if (_targetObject.IsPlaying)
            {
                Rect tRect = lGetRect(lIndex);
                tRect.width = tRect.width + lBaseWidth + 4;
                if (GUI.Button(tRect, GUIBasicDrawer.TimeLineStyles.pauseIcon, (GUIStyle)"box"))
                {
                    _Stop(eStopMode.Pause);
                }
                ++lIndex;
                ++lIndex;
            }
            else
            {
                if (_targetObject.CurrentTime == 0)
                {
                    //포워드만    
                    Rect tRect = lGetRect(lIndex);
                    tRect.width = tRect.width + lBaseWidth + 4;
                    if (GUI.Button(tRect, GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                    {
                        _Play(ePlayMode.Forward);
                    }
                    ++lIndex;
                    ++lIndex;
                }
                else if (_targetObject.CurrentTime == _targetObject.Length)
                {
                    //백워드만
                    Rect tRect = lGetRect(lIndex);
                    tRect.width = tRect.width + lBaseWidth + 4;
                    GUIBasicDrawer.BeginGUIRotate(180f, tRect);
                    if (GUI.Button(tRect, GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                    {
                        _Play(ePlayMode.Backward);
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
                        _Play(ePlayMode.Backward_CurrentAt);
                    }
                    GUIBasicDrawer.EndGUIRotate();
                    ++lIndex;
                    if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.playIcon, (GUIStyle)"box"))
                    {
                        _Play(ePlayMode.Forward_CurrentAt);
                    }
                    ++lIndex;
                }
            }
            if (GUI.Button(lGetRect(lIndex), GUIBasicDrawer.TimeLineStyles.stepIcon, (GUIStyle)"box"))
            {
                _Stop(eStopMode.LastFrame);
            }

            GUIBasicDrawer.BackGUIAllColors();
        }

        private void _DrawClipInspector(UIAnimationGroup pGroup, UIAnimationTrack pTrack, float pTime)
        {
            if (_selectClip == null)
                return;

            Rect lTitleRect = EditorGUILayout.GetControlRect(true, 20);
            _DrawContentTexture(lTitleRect, _titleColor, pTrack.TrackType.ToString());

            bool lPrevEnable = GUI.enabled;
            GUI.enabled = pGroup.mLockEditor == false;
            switch (pTrack.TrackType)
            {
                case eTrackType.Active:
                    _DrawFromToBool(_selectClip);
                    _DrawTimePoint(pTrack, _selectClip);
                    break;
                case eTrackType.Position:
                    _DrawFromToVector3(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
                case eTrackType.Position_2D_Bezir:
                    _DrawBezier2DData(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
                case eTrackType.Rotation:
                    _DrawFromToVector3(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
                case eTrackType.Scale:
                    _DrawFromToVector3(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
                case eTrackType.Color:
                    _DrawFromToColor(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
                case eTrackType.Alpha:
                    _DrawFromToFloat(_selectClip);
                    _DrawAnimationCurve(_selectClip);
                    _DrawTime(pTrack, _selectClip);
                    break;
            }
            _DrawEvent(pGroup, pTrack, _selectClip);
            GUI.enabled = lPrevEnable;
        }

        #region Draw From To

        private void _DrawFromBool(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            bool lStartValue = _Vector4ToBoolean(pClip.startValue);
            lStartValue = EditorGUI.Toggle(Rect.MinMaxRect(lRect.xMin + _contentSpace, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, lStartValue);
            pClip.SetStartValue(_BooleanToVector4(lStartValue));
        }
        private void _DrawToBool(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            bool lEndValue = _Vector4ToBoolean(pClip.endValue);
            lEndValue = EditorGUI.Toggle(Rect.MinMaxRect(lRect.xMin + _contentSpace, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, lEndValue);
            pClip.SetEndValue(_BooleanToVector4(lEndValue));
        }
        private void _DrawFromToBool(UIAnimationClip pClip)
        {
            _DrawFromBool(pClip, "Start Value");
            _DrawToBool(pClip, "End Value");
        }

        private void _DrawFromFloat(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            float lStartValue = _Vector4ToAlpha(pClip.startValue);
            lStartValue = EditorGUI.FloatField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, lStartValue);
            pClip.SetStartValue(_AlphaToVector4(lStartValue));
        }
        private void _DrawToFloat(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            float lEndValue = _Vector4ToAlpha(pClip.endValue);
            lEndValue = EditorGUI.FloatField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, lEndValue);
            pClip.SetEndValue(_AlphaToVector4(lEndValue));
        }
        private void _DrawFromToFloat(UIAnimationClip pClip)
        {
            _DrawFromFloat(pClip, "Start Value");
            _DrawToFloat(pClip, "End Value");
        }
        private void _DrawFromVector3(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            pClip.SetStartValue(EditorGUI.Vector3Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, pClip.startValue));
        }
        private void _DrawToVector3(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            pClip.SetEndValue(EditorGUI.Vector3Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, pClip.endValue));
        }
        private void _DrawFromToVector3(UIAnimationClip pClip)
        {
            _DrawFromVector3(pClip, "Start Value");
            _DrawToVector3(pClip, "End Value");
        }
        private void _DrawFromColor(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            pClip.SetStartValue(EditorGUI.ColorField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, pClip.startValue));
        }
        private void _DrawToColor(UIAnimationClip pClip, string pTitle = "")
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            pClip.SetEndValue(EditorGUI.ColorField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), pTitle, pClip.endValue));
        }
        private void _DrawFromToColor(UIAnimationClip pClip)
        {
            _DrawFromColor(pClip, "Start Value");
            _DrawToColor(pClip, "End Value");
        }
        private void _DrawBezier2DData(UIAnimationClip pClip)
        {
            Rect lRect = EditorGUILayout.GetControlRect(true, 20);
            Vector2 lStartValue = pClip.startValue;
            lStartValue = (EditorGUI.Vector2Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), "Start", lStartValue));

            lRect = EditorGUILayout.GetControlRect(true, 20);
            Vector2 lEndValue = pClip.endValue;
            lEndValue = (EditorGUI.Vector2Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), "End", lEndValue));

            lRect = EditorGUILayout.GetControlRect(true, 20);
            Vector2 lControlPoint_First = new Vector2(pClip.startValue.z, pClip.endValue.z);
            lControlPoint_First = (EditorGUI.Vector2Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), "Control Value 1", lControlPoint_First));

            lRect = EditorGUILayout.GetControlRect(true, 20);
            Vector2 lControlPoint_Second = new Vector2(pClip.startValue.w, pClip.endValue.w);
            lControlPoint_Second = (EditorGUI.Vector2Field(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMax, lRect.yMax), "Control Value 2", lControlPoint_Second));

            pClip.SetStartValue(new Vector4(lStartValue.x, lStartValue.y, lControlPoint_First.x, lControlPoint_Second.x));
            pClip.SetEndValue(new Vector4(lEndValue.x, lEndValue.y, lControlPoint_First.y, lControlPoint_Second.y));
        }
    
        private bool _Vector4ToBoolean(Vector4 pValue)
        {
            return pValue.x > 0;
        }

        private float _Vector4ToAlpha(Vector4 pValue)
        {
            return pValue.w;
        }

        private Vector4 _BooleanToVector4(bool pValue)
        {
            return new Vector4(Convert.ToInt16(pValue), 0);
        }

        private Vector4 _AlphaToVector4(float pValue)
        {
            return new Vector4(0, 0, 0, pValue);
        }
        #endregion Draw From To

        private void _DrawAnimationCurve(UIAnimationClip pClip)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 20);
            pClip.SetCurveData(EditorGUI.CurveField(rect, "Animation Curve", pClip.curveData));
        }

        private void _DrawTimePoint(UIAnimationTrack pTrack, UIAnimationClip pClip)
        {
            UIAnimationClip lBeforeClip = pTrack.GetPrevClip(pClip);
            UIAnimationClip lNextClip = pTrack.GetNextClip(pClip);

            float lViewMinTime = 0f;
            float lViewMaxTime = _targetObject.Length;

            if (lBeforeClip != null)
            {
                lViewMinTime = lBeforeClip.EndTime;
            }
            if (lNextClip != null)
            {
                lViewMaxTime = lNextClip.StartTime;
            }
            lViewMaxTime = lViewMaxTime - UIAnimationUtil.minIntervalTime;

            float lStartTime = pClip.StartTime;
            float lEndTime = pClip.EndTime;

            Rect lRect = EditorGUILayout.GetControlRect(true, 18);
            GUI.Label(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + (lRect.width / 3), lRect.yMax), "Start");
            GUI.Label(Rect.MinMaxRect(lRect.xMin + lRect.width / 3, lRect.yMin, lRect.xMax - (lRect.width / 3), lRect.yMax), "During");
            GUI.Label(Rect.MinMaxRect(lRect.xMax - (lRect.width / 3), lRect.yMin, lRect.xMax, lRect.yMax), "End");

            lRect = EditorGUILayout.GetControlRect(true, 18);
            lStartTime = EditorGUI.FloatField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + (lRect.width / 3), lRect.yMax), lStartTime);

            float lDuration = lEndTime - lStartTime;
            EditorGUI.LabelField(Rect.MinMaxRect(lRect.xMin + lRect.width / 3, lRect.yMin, lRect.xMax - (lRect.width / 3), lRect.yMax), lDuration.ToString("0.0#"));
            lEndTime = EditorGUI.FloatField(Rect.MinMaxRect(lRect.xMax - (lRect.width / 3), lRect.yMin, lRect.xMax, lRect.yMax), lEndTime);

            lRect = EditorGUILayout.GetControlRect(true, 18);
            lStartTime = EditorGUI.Slider(lRect, lStartTime, lViewMinTime, lViewMaxTime);

            lRect = EditorGUILayout.GetControlRect(true, 18);
            GUI.Label(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + 30, lRect.yMax), lViewMinTime.ToString("0.##"));
            GUI.Label(Rect.MinMaxRect(lRect.xMax - 30, lRect.yMin, lRect.xMax, lRect.yMax), lViewMaxTime.ToString("0.##"));

            if (pClip.StartTime != lStartTime)
            {
                pClip.SetStartTime(lStartTime);
        
                if (lEndTime - lStartTime != UIAnimationUtil.minIntervalTime)
                    pClip.SetEndTime(lStartTime + UIAnimationUtil.minIntervalTime);
            }

            SelectTrackTime(selectGroup, selectTrack,(pClip.StartTime + pClip.EndTime) / 2f);
        }

        private void _DrawTime(UIAnimationTrack pTrack, UIAnimationClip pClip)
        {
            UIAnimationClip lBeforeClip = pTrack.GetPrevClip(pClip);
            UIAnimationClip lNextClip = pTrack.GetNextClip(pClip);

            float lViewMinTime = 0f;
            float lViewMaxTime = _targetObject.Length;

            if (lBeforeClip != null)
            {
                lViewMinTime = lBeforeClip.EndTime;
            }
            if (lNextClip != null)
            {
                lViewMaxTime = lNextClip.StartTime;
            }

            float lStartTime = pClip.StartTime;
            float lEndTime = pClip.EndTime;

            Rect lRect = EditorGUILayout.GetControlRect(true, 18);
            GUI.Label(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + (lRect.width / 3), lRect.yMax), "Start");
            GUI.Label(Rect.MinMaxRect(lRect.xMin + lRect.width / 3, lRect.yMin, lRect.xMax - (lRect.width / 3), lRect.yMax), "During");
            GUI.Label(Rect.MinMaxRect(lRect.xMax - (lRect.width / 3), lRect.yMin, lRect.xMax, lRect.yMax), "End");

            lRect = EditorGUILayout.GetControlRect(true, 18);
            lStartTime = EditorGUI.DelayedFloatField(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + (lRect.width / 3), lRect.yMax), lStartTime);
            lEndTime = EditorGUI.DelayedFloatField(Rect.MinMaxRect(lRect.xMax - (lRect.width / 3), lRect.yMin, lRect.xMax, lRect.yMax), lEndTime);

            float lDuration = lEndTime - lStartTime;
            EditorGUI.LabelField(Rect.MinMaxRect(lRect.xMin + lRect.width / 3, lRect.yMin, lRect.xMax - (lRect.width / 3), lRect.yMax), lDuration.ToString("0.0#"));

            lRect = EditorGUILayout.GetControlRect(true, 18);
            EditorGUI.MinMaxSlider(lRect, ref lStartTime, ref lEndTime, lViewMinTime, lViewMaxTime);
            lStartTime = Mathf.Clamp(lStartTime, lViewMinTime, lViewMaxTime - UIAnimationUtil.minIntervalTime);
            lEndTime = Mathf.Clamp(lEndTime, lViewMinTime + UIAnimationUtil.minIntervalTime, lViewMaxTime);

            lRect = EditorGUILayout.GetControlRect(true, 18);
            GUI.Label(Rect.MinMaxRect(lRect.xMin, lRect.yMin, lRect.xMin + 30, lRect.yMax), lViewMinTime.ToString("0.##"));
            GUI.Label(Rect.MinMaxRect(lRect.xMax - 30, lRect.yMin, lRect.xMax, lRect.yMax), lViewMaxTime.ToString("0.##"));

            bool lChanged = false;
            if (Mathf.Approximately(pClip.StartTime, lStartTime) == false)
            {
                pClip.SetStartTime(lStartTime);
                lChanged = true;
            }
            else if (Mathf.Approximately(pClip.EndTime, lEndTime) == false)
            {
                pClip.SetEndTime(lEndTime);
                lChanged = true;
            }

            if(lChanged && _timeLineWindow != null)
                _timeLineWindow.Repaint();
            SelectTrackTime(selectGroup, selectTrack, (pClip.StartTime + pClip.EndTime) / 2f);
        }

        private void _DrawContentTexture(Rect pRect, Color pBackColor, string pText)
        {
            GUIBasicDrawer.DrawTexture(pRect, pBackColor);

            pText = string.Format("<b>{0}</b>", pText);
            GUI.Label(pRect, pText);
        }

        private void _DrawEvent(UIAnimationGroup pGroup, UIAnimationTrack pTrack, UIAnimationClip pClip)
        {
            EditorGUILayout.Space();

            int lGroupIndex = _targetObject.FindIndexEditor(pGroup);
            int lTrackIndex = pGroup.FindTrackIndex(pTrack);
            int lClipIndex = pTrack.FindClipIndex(pClip);
            if (lGroupIndex > -1 && _listGroupProperty != null && _listGroupProperty.arraySize > lGroupIndex)
            {
                var lGroupProperty = _listGroupProperty.GetArrayElementAtIndex(lGroupIndex);
                var lListTrackProperty = lGroupProperty.FindPropertyRelative("_listTrack");
                if (lTrackIndex != -1 && lListTrackProperty.arraySize > lTrackIndex)
                {
                    var lTrackPropery = lListTrackProperty.GetArrayElementAtIndex(lTrackIndex);
                    var lListClipPropery = lTrackPropery.FindPropertyRelative("_listClips");
                    if (lClipIndex != -1 && lListClipPropery.arraySize > lClipIndex)
                    {
                        var lClipProperty = lListClipPropery.GetArrayElementAtIndex(lClipIndex);
                        var lEventProperty = lClipProperty.FindPropertyRelative("_onFinishEvents");
                        EditorGUILayout.PropertyField(lEventProperty, new GUIContent("On FinishEvent"));
                        if (serializedObject.hasModifiedProperties)
                            serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private void _OnEditorUpdate()
        {
            if (EditorApplication.isPlaying)
                return;

            if (_targetObject == null)
                return;

            if (EditorApplication.isCompiling)
            {
                _Stop(eStopMode.Pause);
                return;
            }

            if (_targetObject.IsPlaying)
            {
                _targetObject.UpdateFromEditor();
                EditorApplication.QueuePlayerLoopUpdate();
            }
        
            if (_timeLineWindow != null)
                _timeLineWindow.Repaint();

            Repaint();
        }
    }
}
