using UnityEditor;
using UnityEngine;


public static class GUIBasicDrawer
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
            GUIBasicDrawer.BeginGUIRotate(180f, rect);
            if (GUI.Button(Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMin + 20, rect.yMax), TimeLineStyles.dropUpIcon, (GUIStyle)"label"))
            {
                isToggled = !isToggled;
                isClick = true;
            }
            GUIBasicDrawer.EndGUIRotate();
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
