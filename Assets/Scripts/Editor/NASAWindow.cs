using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using Codice.Utils;
using System.Linq;

public class NASAWindow : EditorWindow
{
    bool objectData = false;
    bool makeEphem = true;
    string startDate = "2025-01-01";
    string stopDate = "2025-01-02";
    readonly string ephemType = "VECTORS";

    Vector3 positionData = new();
    Vector3 velocityData = new();

    string responseText = "";

    [MenuItem("Seminarfach/NASA Tool")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(NASAWindow));
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        startDate = EditorGUILayout.TextField("Start Date", startDate);
        stopDate = EditorGUILayout.TextField("Stop Date", stopDate);
        objectData = EditorGUILayout.Toggle("Object Data", objectData);
        makeEphem = EditorGUILayout.Toggle("Make Ephem", makeEphem);
        if (GUILayout.Button("Fetch Data"))
        {
            OnSubmit();
            EditorApplication.QueuePlayerLoopUpdate();
            Repaint();
        }
        if (GUILayout.Button("Parse Data"))
        {
            string positionRaw = string.Join("", responseText.Split("$$SOE")[1].Split("\n").Skip(2).Take(1));
            string velocityRaw = string.Join("", responseText.Split("$$SOE")[1].Split("\n").Skip(3).Take(1));
            float x = float.Parse(positionRaw.Split("X =")[1].Split(" Y")[0]);
            float y = float.Parse(positionRaw.Split("Y =")[1].Split(" Z")[0]);
            float z = float.Parse(positionRaw.Split("Z =")[1]);
            positionData = new Vector3(x, y, z);
            float vx = float.Parse(velocityRaw.Split("VX=")[1].Split("VY")[0]);
            float vy = float.Parse(velocityRaw.Split("VY=")[1].Split("VZ")[0]);
            float vz = float.Parse(velocityRaw.Split("VZ=")[1]);
            velocityData = new Vector3(vx, vy, vz);
        }

        GUILayout.Space(10);
        EditorGUILayout.TextField(responseText, GUILayout.Height(200));
        GUILayout.Space(10);
        EditorGUILayout.Vector3Field("Position", positionData);
        EditorGUILayout.Vector3Field("Velocity", velocityData);
    }

    private async void OnSubmit()
    {
        UriBuilder uriBuilder = new("https://ssd.jpl.nasa.gov/api/horizons.api");

        var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
        queryParams["format"] = "text";
        queryParams["OBJ_DATA"] = objectData ? "YES" : "NO";
        queryParams["MAKE_EPHEM"] = makeEphem ? "YES" : "NO";
        queryParams["EPHEM_TYPE"] = ephemType;
        queryParams["CENTER"] = "@sun";
        queryParams["START_TIME"] = startDate;
        queryParams["STOP_TIME"] = stopDate;
        queryParams["STEP_SIZE"] = "'1 d'";
        queryParams["COMMAND"] = "399"; // Earth

        uriBuilder.Query = queryParams.ToString();

        responseText = "";

        try
        {
            responseText = await GetRequest(uriBuilder.ToString());
        }
        catch (Exception ex)
        {
            responseText = "Error: " + ex.Message;
        }
    }

    private async Task<string> GetRequest(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Delay(100);

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                return "Error: " + request.error;
            }

            return request.downloadHandler.text;
        }
    }
}
