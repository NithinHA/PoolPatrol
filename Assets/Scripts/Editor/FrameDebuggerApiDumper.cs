using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class FrameDebuggerApiDumper
{
    [MenuItem("Tools/Dump Frame Debugger API")]
    public static void DumpApi()
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

        Debug.Log("--- Methods in FrameDebuggerUtility ---");
        foreach(var method in utilityType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            ParameterInfo[] parameters = method.GetParameters();
            string paramStr = "";
            for(int i=0; i<parameters.Length; i++) {
                paramStr += parameters[i].ParameterType.Name + " " + parameters[i].Name;
                if(i < parameters.Length-1) paramStr += ", ";
            }
            Debug.Log($"{method.ReturnType.Name} {method.Name}({paramStr})");
        }
        
        Debug.Log("--- Properties in FrameDebuggerUtility ---");
        foreach(var prop in utilityType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            Debug.Log($"{prop.PropertyType.Name} {prop.Name}");
        }

        Debug.Log("--- Fields ---");
        foreach(var field in utilityType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            Debug.Log($"{field.FieldType.Name} {field.Name}");
        }
    }
}
