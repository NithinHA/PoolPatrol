using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.IO;
using System.Collections;

public class FrameDebuggerExporter
{
    [MenuItem("Tools/Optimize Draw Calls/Export Frame Debugger Data")]
    public static void ExportData()
    {
        Type utilityType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            utilityType = assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
            if (utilityType != null)
                break;
        }

        if (utilityType == null)
        {
            Debug.LogError("Could not find UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
            return;
        }

        // Check if getting the events is possible
        MethodInfo getFrameEventsMethod = utilityType.GetMethod("GetFrameEvents", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        
        Array eventsArray = null;
        if (getFrameEventsMethod != null)
        {
            eventsArray = getFrameEventsMethod.Invoke(null, null) as Array;
        }

        if (eventsArray == null || eventsArray.Length == 0)
        {
            Debug.LogWarning("No frame events found. Make sure the Frame Debugger is enabled and paused on a frame.");
            return;
        }

        Type eventType = eventsArray.GetValue(0).GetType();
        PropertyInfo[] properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        FieldInfo[] fields = eventType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        string outputPath = Path.Combine(Application.dataPath, "FrameDebuggerExport.txt");
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine($"Frame Debugger Export - {DateTime.Now}");
            writer.WriteLine($"Total Events: {eventsArray.Length}");
            writer.WriteLine(new string('=', 50));

            // We also see GetFrameEventInfoName and GetFrameEventObject
            MethodInfo getInfoNameMethod = utilityType.GetMethod("GetFrameEventInfoName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo getBatchCauseStrsMethod = utilityType.GetMethod("GetBatchBreakCauseStrings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            
            string[] batchCauses = null;
            if (getBatchCauseStrsMethod != null) {
                batchCauses = getBatchCauseStrsMethod.Invoke(null, null) as string[];
            }

            for (int i = 0; i < eventsArray.Length; i++)
            {
                object evt = eventsArray.GetValue(i);
                
                string evtName = "";
                if (getInfoNameMethod != null) {
                    evtName = getInfoNameMethod.Invoke(null, new object[] { i }) as string;
                }

                writer.WriteLine($"Event #{i}: {evtName}");
                
                foreach (var field in fields)
                {
                    writer.WriteLine($"  {field.Name}: {field.GetValue(evt)}");
                }
                
                foreach (var prop in properties)
                {
                    try {
                        writer.WriteLine($"  {prop.Name}: {prop.GetValue(evt)}");
                    } catch { } // Ignored
                }

                // If eventType has a batch break cause index (often called batchBreakCause or similar integer mapping to those strings)
                try {
                    FieldInfo batchCauseIndexField = eventType.GetField("batchBreakCause", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (batchCauseIndexField != null && batchCauses != null) {
                        int index = Convert.ToInt32(batchCauseIndexField.GetValue(evt));
                        if(index >= 0 && index < batchCauses.Length) {
                            writer.WriteLine($"  BatchBreakCause: {batchCauses[index]}");
                        }
                    } else if (batchCauses != null) {
                       // Sometimes the batch break cause is just part of the info string and we don't need to link it
                    }
                } catch { }

                writer.WriteLine(new string('-', 50));
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"[FrameDebuggerExporter] Exported Frame Debugger data to {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }
}
