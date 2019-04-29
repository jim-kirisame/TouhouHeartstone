using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using UberLogger;
using System.Text.RegularExpressions;

/// <summary>
/// The console logging frontend.
/// Pulls data from the UberLoggerEditor backend
/// </summary>

public class UberLoggerEditorWindow : EditorWindow, UberLoggerEditor.ILoggerWindow
{
    [MenuItem("Window/Show Uber Console")]
    static public void ShowLogWindow()
    {
        Init();
    }

    static public void Init()
    {
        var window = ScriptableObject.CreateInstance<UberLoggerEditorWindow>();
        window.Show();
        window.position = new Rect(200,200,400,300);
        window.currentBottomPanelHeight = Math.Max(window.reservedBottomPanleHeight, window.position.height * 0.25f);
    }

    public void OnLogChange(LogInfo logInfo)
    {
        Dirty = true;
        // Repaint();
    }

    
    void OnInspectorUpdate()
    {
        // Debug.Log("Update");
        if(Dirty)
        {
            Repaint();
        }
    }

    void OnEnable()
    {
        // Connect to or create the backend
        if(!EditorLogger)
        {
            EditorLogger = UberLogger.Logger.GetLogger<UberLoggerEditor>();
            if(!EditorLogger)
            {
                EditorLogger = UberLoggerEditor.Create();
            }
        }

        // UberLogger doesn't allow for duplicate loggers, so this is safe
        // And, due to Unity serialisation stuff, necessary to do to it here.
        UberLogger.Logger.AddLogger(EditorLogger);
        EditorLogger.AddWindow(this);

// _OR_NEWER only became available from 5.3
#if UNITY_5 || UNITY_5_3_OR_NEWER
        titleContent.text = "Uber Console";
#else
        title = "Uber Console";

#endif
         
        ClearSelectedMessage();

        SmallErrorIcon = EditorGUIUtility.FindTexture( "d_console.erroricon.sml" ) ;
        SmallWarningIcon = EditorGUIUtility.FindTexture( "d_console.warnicon.sml" ) ;
        SmallMessageIcon = EditorGUIUtility.FindTexture( "d_console.infoicon.sml" ) ;
        ErrorIcon = SmallErrorIcon;
        WarningIcon = SmallWarningIcon;
        MessageIcon = SmallMessageIcon;
        Dirty = true;
        Repaint();

    }

    /// <summary>
    /// Converts the entire message log to a multiline string
    /// </summary>
    public string ExtractLogListToString()
    {
        string result = "";
        foreach (CountedLog log in RenderLogs)
        {
            UberLogger.LogInfo logInfo = log.Log;
            result += logInfo.GetRelativeTimeStampAsString() + ": " + logInfo.Severity + ": " + logInfo.Message + "\n";
        }
        return result;
    }

    /// <summary> 
    /// Converts the currently-displayed stack to a multiline string 
    /// </summary> 
    public string ExtractLogDetailsToString() 
    { 
        string result = ""; 
        if (RenderLogs.Count > 0 && SelectedRenderLog >= 0) 
        { 
            var countedLog = RenderLogs[SelectedRenderLog]; 
            var log = countedLog.Log; 
 
            for (int c1 = 0; c1 < log.Callstack.Count; c1++) 
            { 
                var frame = log.Callstack[c1]; 
                var methodName = frame.GetFormattedMethodName(); 
                result += methodName + "\n"; 
            } 
        } 
        return result; 
    } 

    /// <summary> 
    /// Handle "Copy" command; copies log & stacktrace contents to clipboard
    /// </summary> 
    public void HandleCopyToClipboard() 
    { 
        const string copyCommandName = "Copy";

        Event e = Event.current;
        if (e.type == EventType.ValidateCommand && e.commandName == copyCommandName)
        {
            // Respond to "Copy" command

            // Confirm that we will consume the command; this will result in the command being re-issued with type == EventType.ExecuteCommand
            e.Use();
        }
        else if (e.type == EventType.ExecuteCommand && e.commandName == copyCommandName)
        {
            // Copy current message log and current stack to the clipboard 
 
            // Convert all messages to a single long string 
            // It would be preferable to only copy one of the two, but that requires UberLogger to have focus handling 
            // between the message log and stack views 
            string result = ExtractLogListToString(); 
 
            result += "\n"; 
 
            // Convert current callstack to a single long string 
            result += ExtractLogDetailsToString(); 
 
            GUIUtility.systemCopyBuffer = result; 
        } 
    }

    Vector2 DrawPos;
    public void OnGUI()
    {
        //Set up the basic style, based on the Unity defaults
        //A bit hacky, but means we don't have to ship an editor guistyle and can fit in to pro and free skins
        Color defaultLineColor = GUI.backgroundColor;
        GUIStyle unityLogLineEven = null;
        GUIStyle unityLogLineOdd = null;
        GUIStyle unitySmallLogLine = null;
        
        foreach(var style in GUI.skin.customStyles)
        {
            if     (style.name=="CN EntryBackEven")  unityLogLineEven = style;
            else if(style.name=="CN EntryBackOdd")   unityLogLineOdd = style;
            else if(style.name=="CN StatusInfo")   unitySmallLogLine = style;
        }

        EntryStyleBackEven = new GUIStyle(unitySmallLogLine);

        EntryStyleBackEven.normal = unityLogLineEven.normal;
        EntryStyleBackEven.margin = new RectOffset(0,0,0,0);
        EntryStyleBackEven.border = new RectOffset(0,0,0,0);
        EntryStyleBackEven.fixedHeight = 0;

        EntryStyleBackOdd = new GUIStyle(EntryStyleBackEven);
        EntryStyleBackOdd.normal = unityLogLineOdd.normal;
        // EntryStyleBackOdd = new GUIStyle(unityLogLine);


        SizerLineColour = Color.black;
        SelectedItemColor = new Color(0.4f, 0.8f, 1f);

        // GUILayout.BeginVertical(GUILayout.Height(topPanelHeaderHeight), GUILayout.MinHeight(topPanelHeaderHeight));
        ResizeTopPane();
        DrawPos = Vector2.zero;
        DrawToolbar();
        DrawFilter();
        
        DrawChannels();

        float logPanelHeight = CurrentTopPaneHeight-DrawPos.y;
        
        if(Dirty)
        {
            CurrentLogList = EditorLogger.CopyLogInfo();
        }
        DrawLogList(logPanelHeight);

        DrawPos.y += DividerLineHeight;

        DrawLogDetails();

        HandleCopyToClipboard();

        //If we're dirty, do a repaint
        Dirty = false;
        if(MakeDirty)
        {
            Dirty = true;
			MakeDirty = false;
            Repaint();
        }
    }

    //Some helper functions to draw buttons that are only as big as their text
    bool ButtonClamped(string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        size = style.CalcSize(content);
        var rect = new Rect(DrawPos, size);
        return GUI.Button(rect, text, style);
    }

    bool ToggleClamped(bool state, string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        return ToggleClamped(state, content, style, out size);
    }

    bool ToggleClamped(bool state, GUIContent content, GUIStyle style, out Vector2 size)
    {
        size = style.CalcSize(content);
        Rect drawRect = new Rect(DrawPos, size);
        return GUI.Toggle(drawRect, state, content, style);
    }

    void LabelClamped(string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        size = style.CalcSize(content);

        Rect drawRect = new Rect(DrawPos, size);
        GUI.Label(drawRect, text, style);
    }

    /// <summary>
    /// Draws the thin, Unity-style toolbar showing error counts and toggle buttons
    /// </summary>
    void DrawToolbar()
    {
        var toolbarStyle = EditorStyles.toolbarButton;

        Vector2 elementSize;
        if(ButtonClamped("清空", EditorStyles.toolbarButton, out elementSize))
        {
            EditorLogger.Clear();
        }
        DrawPos.x += elementSize.x;
        EditorLogger.ClearOnPlay = ToggleClamped(EditorLogger.ClearOnPlay, "播放时清空", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;
        EditorLogger.PauseOnError  = ToggleClamped(EditorLogger.PauseOnError, "错误时暂停", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;
        var showTimes = ToggleClamped(ShowTimes, "显示时间", EditorStyles.toolbarButton, out elementSize);
        if(showTimes!=ShowTimes)
        {
            MakeDirty = true;
            ShowTimes = showTimes;
        }
        DrawPos.x += elementSize.x;
        var showChannels = ToggleClamped(ShowChannels, "显示分类", EditorStyles.toolbarButton, out elementSize);
        if (showChannels != ShowChannels)
        {
            MakeDirty = true;
            ShowChannels = showChannels;
        }
        DrawPos.x += elementSize.x;
        var collapse = ToggleClamped(Collapse, "折叠日志", EditorStyles.toolbarButton, out elementSize);
        if(collapse!=Collapse)
        {
            MakeDirty = true;
            Collapse = collapse;
            SelectedRenderLog = -1;
        }
        DrawPos.x += elementSize.x;

        ScrollFollowMessages = ToggleClamped(ScrollFollowMessages, "跟随日志", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;

        var errorToggleContent = new GUIContent(EditorLogger.NoErrors.ToString(), SmallErrorIcon);
        var warningToggleContent = new GUIContent(EditorLogger.NoWarnings.ToString(), SmallWarningIcon);
        var messageToggleContent = new GUIContent(EditorLogger.NoMessages.ToString(), SmallMessageIcon);
        var debugToggleContent = new GUIContent(EditorLogger.NoDebugs.ToString(), SmallMessageIcon);
        var traceToggleContent = new GUIContent(EditorLogger.NoTraces.ToString(), SmallMessageIcon);

        float totalErrorButtonWidth = toolbarStyle.CalcSize(errorToggleContent).x + toolbarStyle.CalcSize(warningToggleContent).x + toolbarStyle.CalcSize(messageToggleContent).x +
            toolbarStyle.CalcSize(debugToggleContent).x + toolbarStyle.CalcSize(traceToggleContent).x;

        float errorIconX = position.width-totalErrorButtonWidth;
        if(errorIconX > DrawPos.x)
        {
            DrawPos.x = errorIconX;
        }

        var showErrors = ToggleClamped(ShowErrors, errorToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showWarnings = ToggleClamped(ShowWarnings, warningToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showMessages = ToggleClamped(ShowMessages, messageToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showDebugs = ToggleClamped(ShowDebugs, debugToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showTraces = ToggleClamped(ShowTraces, traceToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;

        DrawPos.y += elementSize.y;
        DrawPos.x = 0;

        //If the errors/warning to show has changed, clear the selected message
        if(showErrors!=ShowErrors || showWarnings!=ShowWarnings || showMessages!=ShowMessages || showDebugs != ShowDebugs || showTraces != ShowTraces)
        {
            ClearSelectedMessage();
            MakeDirty = true;
        }
        ShowWarnings = showWarnings;
        ShowMessages = showMessages;
        ShowErrors = showErrors;
        ShowDebugs = showDebugs;
        ShowTraces = showTraces;
    }

    /// <summary>
    /// Draws the channel selector
    /// </summary>
    void DrawChannels()
    {
        var channels = GetChannels();
        int currentChannelIndex = 0;
        for(int c1=0; c1<channels.Count; c1++)
        {
            if(channels[c1]==CurrentChannel)
            {
                currentChannelIndex = c1;
                break;
            }
        }

        var content = new GUIContent("S");
        var size = GUI.skin.button.CalcSize(content);
        var drawRect = new Rect(DrawPos, new Vector2(position.width, size.y));
        currentChannelIndex = GUI.SelectionGrid(drawRect, currentChannelIndex, channels.ToArray(), channels.Count);
        if(CurrentChannel!=channels[currentChannelIndex])
        {
            CurrentChannel = channels[currentChannelIndex];
            ClearSelectedMessage();
            MakeDirty = true;
        }
        DrawPos.y+=size.y;
    }

    /// <summary>
    /// Based on filter and channel selections, should this log be shown?
    /// </summary>
    bool ShouldShowLog(System.Text.RegularExpressions.Regex regex, LogInfo log)
    {
        if(log.Channel==CurrentChannel || CurrentChannel=="全部分类" || (CurrentChannel=="无分类" && String.IsNullOrEmpty(log.Channel)))
        {
            if((log.Severity==LogSeverity.Message && ShowMessages)
               || (log.Severity==LogSeverity.Warning && ShowWarnings)
               || (log.Severity==LogSeverity.Error && ShowErrors)
               || (log.Severity==LogSeverity.Debug && ShowDebugs)
               || (log.Severity==LogSeverity.Trace && ShowTraces))
            {
                if(regex==null || regex.IsMatch(log.Message))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Converts a given log element into a piece of gui content to be displayed
    /// </summary>
    GUIContent GetLogLineGUIContent(UberLogger.LogInfo log, bool showTimes, bool showChannels)
    {
        var showMessage = log.Message;
        
        //Make all messages single line
        showMessage = showMessage.Replace(UberLogger.Logger.UnityInternalNewLine, " ");

        // Format the message as follows:
        //     [channel] 0.000 : message  <-- Both channel and time shown
        //     0.000 : message            <-- Time shown, channel hidden
        //     [channel] : message        <-- Channel shown, time hidden
        //     message                    <-- Both channel and time hidden
        var showChannel = showChannels && !string.IsNullOrEmpty(log.Channel);
        var channelMessage = showChannel ? string.Format("[{0}]", log.Channel) : "";
        var channelTimeSeparator = (showChannel && showTimes) ? " " : "";
        var timeMessage = showTimes ? string.Format("{0}", log.GetRelativeTimeStampAsString()) : "";
        var prefixMessageSeparator = (showChannel || showTimes) ? " : " : "";
        showMessage = string.Format("{0}{1}{2}{3}{4}",
                channelMessage,
                channelTimeSeparator,
                timeMessage,
                prefixMessageSeparator,
                showMessage
            );

        // add color for other level
        switch(log.Severity)
        {
            case LogSeverity.Debug:
                showMessage = "<color=#000000c0>" + showMessage + "</color>";
                break;
            case LogSeverity.Trace:
                showMessage = "<color=#00000090>" + showMessage + "</color>";
                break;
        }

        var content = new GUIContent(showMessage, GetIconForLog(log));
        return content;
    }

    /// <summary>
    /// Draws the main log panel
    /// </summary>
    public void DrawLogList(float height)
    {
        var oldColor = GUI.backgroundColor;


        float buttonY = 0;
        
        System.Text.RegularExpressions.Regex filterRegex = null;

        if(!String.IsNullOrEmpty(FilterRegex))
        {
            filterRegex = new Regex(FilterRegex);
        }

        var collapseBadgeStyle = EditorStyles.miniButton;
        var logLineStyle = EntryStyleBackEven;

        // If we've been marked dirty, we need to recalculate the elements to be displayed
        if(Dirty)
        {
            LogListMaxWidth = 0;
            LogListLineHeight = 0;
            CollapseBadgeMaxWidth = 0;
            RenderLogs.Clear();

            //When collapsed, count up the unique elements and use those to display
            if(Collapse)
            {
                var collapsedLines = new Dictionary<string, CountedLog>();
                var collapsedLinesList = new List<CountedLog>();

                foreach(var log in CurrentLogList)
                {
                    if(ShouldShowLog(filterRegex, log))
                    {
                        var matchString = log.Message + "!$" + log.Severity + "!$" + log.Channel;

                        CountedLog countedLog;
                        if(collapsedLines.TryGetValue(matchString, out countedLog))
                        {
                            countedLog.Count++;
                        }
                        else
                        {
                            countedLog = new CountedLog(log, 1);
                            collapsedLines.Add(matchString, countedLog);
                            collapsedLinesList.Add(countedLog);
                        }
                    }
                }

                foreach(var countedLog in collapsedLinesList)
                {
                    var content = GetLogLineGUIContent(countedLog.Log, ShowTimes, ShowChannels);
                    RenderLogs.Add(countedLog);
                    var logLineSize = logLineStyle.CalcSize(content);
                    LogListMaxWidth = Mathf.Max(LogListMaxWidth, logLineSize.x);
                    LogListLineHeight = Mathf.Max(LogListLineHeight, logLineSize.y);

                    var collapseBadgeContent = new GUIContent(countedLog.Count.ToString());
                    var collapseBadgeSize = collapseBadgeStyle.CalcSize(collapseBadgeContent);
                    CollapseBadgeMaxWidth = Mathf.Max(CollapseBadgeMaxWidth, collapseBadgeSize.x);
                }
            }
            //If we're not collapsed, display everything in order
            else
            {
                foreach(var log in CurrentLogList)
                {
                    if(ShouldShowLog(filterRegex, log))
                    {
                        var content = GetLogLineGUIContent(log, ShowTimes, ShowChannels);
                        RenderLogs.Add(new CountedLog(log, 1));
                        var logLineSize = logLineStyle.CalcSize(content);
                        LogListMaxWidth = Mathf.Max(LogListMaxWidth, logLineSize.x);
                        LogListLineHeight = Mathf.Max(LogListLineHeight, logLineSize.y);
                    }
                }
            }

            LogListMaxWidth += CollapseBadgeMaxWidth;
        }

        var scrollRect = new Rect(DrawPos, new Vector2(position.width, height));
        float contentHeight = RenderLogs.Count * LogListLineHeight;
        // reserved for vertical scrollbar.
        float reservedSpace = contentHeight > height ? 15 : 0;

        // do not use horizonal scrollbar.
        // float lineWidth = Mathf.Max(LogListMaxWidth, scrollRect.width - reservedSpace); 
        float lineWidth = scrollRect.width - reservedSpace;

        var contentRect = new Rect(0, 0, lineWidth, contentHeight);
        Vector2 lastScrollPosition = LogListScrollPosition;
        LogListScrollPosition = GUI.BeginScrollView(scrollRect, LogListScrollPosition, contentRect);

        //If we're following the messages but the user has moved, cancel following
        if(ScrollFollowMessages)
        {
            if(lastScrollPosition.y - LogListScrollPosition.y > LogListLineHeight)
            {
                UberDebug.UnityLog(String.Format("{0} {1}", lastScrollPosition.y, LogListScrollPosition.y));
                ScrollFollowMessages = false;
            }
        }
        
        float logLineX = CollapseBadgeMaxWidth;

        //Render all the elements
        int firstRenderLogIndex = (int) (LogListScrollPosition.y/LogListLineHeight);
        int lastRenderLogIndex = (int) ((LogListScrollPosition.y + height) / LogListLineHeight) + 1;

        firstRenderLogIndex = Mathf.Clamp(firstRenderLogIndex, 0, RenderLogs.Count);
        lastRenderLogIndex = Mathf.Clamp(lastRenderLogIndex, 0, RenderLogs.Count);
        buttonY = firstRenderLogIndex*LogListLineHeight;

        for(int renderLogIndex=firstRenderLogIndex; renderLogIndex<lastRenderLogIndex; renderLogIndex++)
        {
            var countedLog = RenderLogs[renderLogIndex];
            var log = countedLog.Log;
            logLineStyle = (renderLogIndex%2==0) ? EntryStyleBackEven : EntryStyleBackOdd;
            if(renderLogIndex==SelectedRenderLog)
            {
                GUI.backgroundColor = SelectedItemColor;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }
                
            //Make all messages single line
            var content = GetLogLineGUIContent(log, ShowTimes, ShowChannels);
            var drawRect = new Rect(logLineX, buttonY, contentRect.width, LogListLineHeight);
            if(GUI.Button(drawRect, content, logLineStyle))
            {
                //Select a message, or jump to source if it's double-clicked
                if(renderLogIndex==SelectedRenderLog)
                {
                    if(EditorApplication.timeSinceStartup-LastMessageClickTime<DoubleClickInterval)
                    {
                        LastMessageClickTime = 0;
                        // Attempt to display source code associated with messages. Search through all stackframes,
                        //   until we find a stackframe that can be displayed in source code view
                        for (int frame = 0; frame < log.Callstack.Count; frame++)
                        {
                            if (JumpToSource(log.Callstack[frame]))
                                break;
                        }
                    }
                    else
                    {
                        LastMessageClickTime = EditorApplication.timeSinceStartup;
                    }
                }
                else
                {
                    SelectedRenderLog = renderLogIndex;
                    SelectedCallstackFrame = -1;
                    LastMessageClickTime = EditorApplication.timeSinceStartup;
                }


                //Always select the game object that is the source of this message
                var go = log.Source as GameObject;
                if(go!=null)
                {
                    Selection.activeGameObject = go;
                }
            }

            if(Collapse)
            {
                var collapseBadgeContent = new GUIContent(countedLog.Count.ToString());
                var collapseBadgeSize = collapseBadgeStyle.CalcSize(collapseBadgeContent);
                var collapseBadgeRect = new Rect(0, buttonY, collapseBadgeSize.x, collapseBadgeSize.y);
                GUI.Button(collapseBadgeRect, collapseBadgeContent, collapseBadgeStyle);
            }
            buttonY += LogListLineHeight;
        }

        //If we're following the log, move to the end
        if(ScrollFollowMessages && RenderLogs.Count>0)
        {
            LogListScrollPosition.y = ((RenderLogs.Count+1)*LogListLineHeight)-scrollRect.height;
        }

        GUI.EndScrollView();
        DrawPos.y += height;
        DrawPos.x = 0;
        GUI.backgroundColor = oldColor;
    }

    class GUIContentWithHeight
    {
        public float Height;
        public GUIContent Content;
    }

    /// <summary>
    /// The bottom of the panel - details of the selected log
    /// </summary>
    public void DrawLogDetails()
    {
        var oldColor = GUI.backgroundColor;

        SelectedRenderLog = Mathf.Clamp(SelectedRenderLog, 0, CurrentLogList.Count);

        if(RenderLogs.Count>0 && SelectedRenderLog>=0)
        {
            var countedLog = RenderLogs[SelectedRenderLog];
            var log = countedLog.Log;
            var logLineStyle = EntryStyleBackEven;

            EntryStyleBackOdd.wordWrap = true;
            EntryStyleBackEven.wordWrap = true;

            var sourceStyle = new GUIStyle(GUI.skin.textArea);
            sourceStyle.richText = true;

            var drawRect = new Rect(DrawPos, new Vector2(position.width-DrawPos.x, position.height-DrawPos.y));

            // calculate whether the content will overflow.
            float preCalcHeight = 0;
            for (int c1 = 0; c1 < log.Callstack.Count; c1++)
            {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodNameWithFileName();
                if (!String.IsNullOrEmpty(methodName))
                {
                    var content = new GUIContent(methodName);
                    var contentSize = logLineStyle.CalcSize(content);
                    preCalcHeight += logLineStyle.CalcHeight(content, drawRect.width);
                    if (ShowFrameSource && c1 == SelectedCallstackFrame)
                    {
                        var sourceContent = GetFrameSourceGUIContent(frame);
                        if (sourceContent != null)
                        {
                            var sourceSize = sourceStyle.CalcSize(sourceContent);
                            preCalcHeight += sourceSize.y;
                        }
                    }
                }
            }

            // reserved for vertical scrollbar.
            float reservedSpace = preCalcHeight > drawRect.height ? 15 : 0;
            float lineWidth = drawRect.width - reservedSpace;

            //Work out the content we need to show, and the sizes
            var detailLines = new List<GUIContentWithHeight>();
            float contentHeight = 0;

            for(int c1=0; c1<log.Callstack.Count; c1++)
            {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodNameWithFileName();
                if(!String.IsNullOrEmpty(methodName))
                {
                    var content = new GUIContent(methodName);
                    var contentWitHeight = new GUIContentWithHeight() { Content = content };
                    detailLines.Add(contentWitHeight);

                    var contentSize = logLineStyle.CalcSize(content);
                    contentWitHeight.Height = logLineStyle.CalcHeight(content, lineWidth);

                    contentHeight += contentWitHeight.Height;
                    if(ShowFrameSource && c1==SelectedCallstackFrame)
                    {
                        var sourceContent = GetFrameSourceGUIContent(frame);
                        if(sourceContent!=null)
                        {
                            var sourceSize = sourceStyle.CalcSize(sourceContent);
                            contentHeight += sourceSize.y;
                        }
                    }
                }
            }

            var contentRect = new Rect(0, 0, lineWidth, contentHeight);

            LogDetailsScrollPosition = GUI.BeginScrollView(drawRect, LogDetailsScrollPosition, contentRect);

            float lineY = 0;
            for(int c1=0; c1<detailLines.Count; c1++)
            {
                var lineContent = detailLines[c1];
                if(lineContent!=null)
                {
                    logLineStyle = (c1%2==0) ? EntryStyleBackEven : EntryStyleBackOdd;
                    if(c1==SelectedCallstackFrame)
                    {
                        GUI.backgroundColor = SelectedItemColor;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.white;
                    }
                    
                    var frame = log.Callstack[c1];
                    var lineRect = new Rect(0, lineY, contentRect.width, lineContent.Height);

                    // Handle clicks on the stack frame
                    if(GUI.Button(lineRect, lineContent.Content, logLineStyle))
                    {
                        if(c1==SelectedCallstackFrame)
                        {
                            if(Event.current.button==1)
                            {
                                ToggleShowSource(frame);
                                Repaint();
                            }
                            else
                            {
                                if(EditorApplication.timeSinceStartup-LastFrameClickTime<DoubleClickInterval)
                                {
                                    LastFrameClickTime = 0;
                                    JumpToSource(frame);
                                }
                                else
                                {
                                    LastFrameClickTime = EditorApplication.timeSinceStartup;
                                }
                            }
                            
                        }
                        else
                        {
                            SelectedCallstackFrame = c1;
                            LastFrameClickTime = EditorApplication.timeSinceStartup;
                        }
                    }
                    lineY += lineContent.Height;
                    //Show the source code if needed
                    if(ShowFrameSource && c1==SelectedCallstackFrame)
                    {
                        GUI.backgroundColor = Color.white;

                        var sourceContent = GetFrameSourceGUIContent(frame);
                        if(sourceContent!=null)
                        {
                            var sourceSize = sourceStyle.CalcSize(sourceContent);
                            var sourceRect = new Rect(0, lineY, contentRect.width, sourceSize.y);

                            GUI.Label(sourceRect, sourceContent, sourceStyle);
                            lineY += sourceSize.y;
                        }
                    }
                }
            }
            GUI.EndScrollView();
        }
        GUI.backgroundColor = oldColor;
    }

    Texture2D GetIconForLog(LogInfo log)
    {
        if(log.Severity==LogSeverity.Error)
        {
            return ErrorIcon;
        }
        if(log.Severity==LogSeverity.Warning)
        {
            return WarningIcon;
        }

        return MessageIcon;
    }

    void ToggleShowSource(LogStackFrame frame)
    {
        ShowFrameSource = !ShowFrameSource;
    }

    bool JumpToSource(LogStackFrame frame)
    {
        if (frame.FileName != null)
        {
            var osFileName = UberLogger.Logger.ConvertDirectorySeparatorsFromUnityToOS(frame.FileName);
            var filename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), osFileName);
            if (System.IO.File.Exists(filename))
            {
                if (UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filename, frame.LineNumber))
                    return true;
            }
        }

        return false;
    }

    GUIContent GetFrameSourceGUIContent(LogStackFrame frame)
    {
        var source = GetSourceForFrame(frame);
        if(!String.IsNullOrEmpty(source))
        {
            var content = new GUIContent(source);
            return content;
        }
        return null;
    }


    void DrawFilter()
    {
        Vector2 size;
        LabelClamped("正则表达式过滤", GUI.skin.label, out size);
        DrawPos.x += size.x;
        
        string filterRegex = null;
        bool clearFilter = false;
        if(ButtonClamped("取消过滤", GUI.skin.button, out size))
        {
            clearFilter = true;
            
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }
        DrawPos.x += size.x;

        var drawRect = new Rect(DrawPos, new Vector2(position.width-DrawPos.x, size.y));
        filterRegex = EditorGUI.TextArea(drawRect, FilterRegex);

        if(clearFilter)
        {
            filterRegex = null;
        }
        //If the filter has changed, invalidate our currently selected message
        if(filterRegex!=FilterRegex)
        {
            ClearSelectedMessage();
            FilterRegex = filterRegex;
            MakeDirty = true;
        }
            
        DrawPos.y += size.y;
        DrawPos.x = 0;
    }

    List<string> GetChannels()
    {
        if(Dirty)
        {
            CurrentChannels = EditorLogger.CopyChannels();
        }

        var categories = CurrentChannels;
        
        var channelList = new List<string>();
        channelList.Add("全部分类");
        channelList.Add("无分类");
        channelList.AddRange(categories);
        return channelList;
    }

    /// <summary>
    ///   Handles the split window stuff, somewhat bodgily
    /// </summary>
    private void ResizeTopPane()
    {
        var heightOffset = (DividerHeight - DividerLineHeight) / 2;

        //Set up the resize collision rect
        CursorChangeRect = new Rect(0, CurrentTopPaneHeight - heightOffset, position.width, DividerHeight);
        var splitLineRect = new Rect(0, CurrentTopPaneHeight, position.width, DividerLineHeight);

        var oldColor = GUI.color;
        GUI.color = SizerLineColour; 
        GUI.DrawTexture(splitLineRect, EditorGUIUtility.whiteTexture);
        GUI.color = oldColor;
        EditorGUIUtility.AddCursorRect(CursorChangeRect,MouseCursor.ResizeVertical);
         
        if( Event.current.type == EventType.MouseDown && CursorChangeRect.Contains(Event.current.mousePosition))
        {
            Resize = true;
        }
        
        //If we've resized, store the new size and force a repaint
        if(Resize)
        {
            CurrentTopPaneHeight = Event.current.mousePosition.y;
            CursorChangeRect.Set(CursorChangeRect.x,CurrentTopPaneHeight - heightOffset, CursorChangeRect.width,CursorChangeRect.height);
            Repaint();
        }

        if(Event.current.type == EventType.MouseUp)
            Resize = false;

        CurrentTopPaneHeight = Mathf.Clamp(CurrentTopPaneHeight, reservedBottomPanleHeight, position.height-reservedBottomPanleHeight);
    }

    //Cache for GetSourceForFrame
    string SourceLines;
    LogStackFrame SourceLinesFrame;

    /// <summary>
    ///If the frame has a valid filename, get the source string for the code around the frame
    ///This is cached, so we don't keep getting it.
    /// </summary>
    string GetSourceForFrame(LogStackFrame frame)
    {
        if(SourceLinesFrame==frame)
        {
            return SourceLines;
        }
        

        if(frame.FileName==null)
        {
            return "";
        }

        var osFileName = UberLogger.Logger.ConvertDirectorySeparatorsFromUnityToOS(frame.FileName);
        var filename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), osFileName);
        if (!System.IO.File.Exists(filename))
        {
            return "";
        }

        int lineNumber = frame.LineNumber-1;
        int linesAround = 3;
        var lines = System.IO.File.ReadAllLines(filename);
        var firstLine = Mathf.Max(lineNumber-linesAround, 0);
        var lastLine = Mathf.Min(lineNumber+linesAround+1, lines.Count());

        SourceLines = "";
        if(firstLine!=0)
        {
            SourceLines += "...\n";
        }
        for(int c1=firstLine; c1<lastLine; c1++)
        {
            string str = lines[c1] + "\n";
            if(c1==lineNumber)
            {
                str = "<color=#ff0000ff>"+str+"</color>";
            }
            SourceLines += str;
        }
        if(lastLine!=lines.Count())
        {
            SourceLines += "...\n";
        }

        SourceLinesFrame = frame;
        return SourceLines;
    }

    void ClearSelectedMessage()
    {
        SelectedRenderLog = -1;
        SelectedCallstackFrame = -1;
        ShowFrameSource = false;
    }

    Vector2 LogListScrollPosition;
    Vector2 LogDetailsScrollPosition;

    Texture2D ErrorIcon;
    Texture2D WarningIcon;
    Texture2D MessageIcon;
    Texture2D SmallErrorIcon;
    Texture2D SmallWarningIcon;
    Texture2D SmallMessageIcon;

    bool ShowChannels = true;
    bool ShowTimes = true;
    bool Collapse = false;
    bool ScrollFollowMessages = false;
    readonly float reservedBottomPanleHeight = 50;
    float currentBottomPanelHeight = 50;
    float CurrentTopPaneHeight {
        get { return position.height - currentBottomPanelHeight; }
        set { currentBottomPanelHeight = position.height - value; }
    }
    bool Resize = false;
    Rect CursorChangeRect;
    int SelectedRenderLog = -1;
    bool Dirty=false;
    bool MakeDirty=false;
    float DividerHeight = 5;
    float DividerLineHeight = 1;

    double LastMessageClickTime = 0;
    double LastFrameClickTime = 0;

    const double DoubleClickInterval = 0.3f;

    //Serialise the logger field so that Unity doesn't forget about the logger when you hit Play
    [UnityEngine.SerializeField]
    UberLoggerEditor EditorLogger;

    List<UberLogger.LogInfo> CurrentLogList = new List<UberLogger.LogInfo>();
    HashSet<string> CurrentChannels = new HashSet<string>();

    //Standard unity pro colours
    Color SizerLineColour;
    Color SelectedItemColor;

    GUIStyle EntryStyleBackEven;
    GUIStyle EntryStyleBackOdd;
    string CurrentChannel=null;
    string FilterRegex = null;
    bool ShowErrors = true; 
    bool ShowWarnings = true; 
    bool ShowMessages = true;
    bool ShowDebugs = true;
    bool ShowTraces = true;
    int SelectedCallstackFrame = 0;
    bool ShowFrameSource = false;

    class CountedLog
    {
        public UberLogger.LogInfo Log = null;
        public Int32 Count=1;
        public CountedLog(UberLogger.LogInfo log, Int32 count)
        {
            Log = log;
            Count = count;
        }
    }

    List<CountedLog> RenderLogs = new List<CountedLog>();
    float LogListMaxWidth = 0;
    float LogListLineHeight = 0;
    float CollapseBadgeMaxWidth = 0;



}
