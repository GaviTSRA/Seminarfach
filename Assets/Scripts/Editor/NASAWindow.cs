using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using Transform = UnityEngine.Transform;
using System.Text.RegularExpressions;

public class NASAWindow : EditorWindow
{
    int tab = 0;

    // Object Page
    string objectsResponse = "";
    private readonly Dictionary<string, string> objects = new();
    private int selectedObjectNameIndex = 0;

    // Data Page
    bool objectData = true;
    bool makeEphem = true;
    string startDate = "2025-01-01";
    string stopDate = "2025-01-02";
    readonly string ephemType = "VECTORS";
    Vector3d positionData = new(0, 0, 0);
    Vector3d velocityData = new(0, 0, 0);
    double mass = 0;
    double radius = 0;
    string dataResponse = "";

    [MenuItem("Seminarfach/NASA Tool")]
    public static void ShowWindow()
    {
        GetWindow(typeof(NASAWindow));
    }

    async void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, new string[] { "Object", "Data", "Generation" });
        switch (tab)
        {
            case 0:
                if (GUILayout.Button("Fetch Data"))
                {
                    FetchObjects();
                }
                if (GUILayout.Button("Parse Data"))
                {
                    string[] lines = objectsResponse.Split("\n");
                    foreach (string raw in lines)
                    {
                        string line = raw.Trim();
                        if (line.ToCharArray().Length == 0) continue;
                        if (Char.IsDigit(line.ToCharArray()[0])) 
                        {
                            string idTxt = line.Split(" ")[0];
                            int id = int.Parse(idTxt, CultureInfo.InvariantCulture);

                            // Spacecraft & similar
                            if (id < 0) continue;
                            // Barycenters
                            if (id < 10) continue;
                            // TODO check for importance
                            if (id > 1_000_000) continue;
                            if (id % 100 != 99 && id != 10) continue;

                            string name;
                            try
                            {
                                string nameAndId = line.Split("   ")[0];
                                name = nameAndId.Split("  ")[1];
                            }
                            catch (Exception)
                            {
                                // No name
                                continue;
                            }

                            objects.Add(name, idTxt);
                        }
                    }
                }
                GUILayout.Space(10);
                EditorGUILayout.TextField(objectsResponse, GUILayout.Height(200));
                GUILayout.Space(10);
                GUILayout.Label("Parsed objects: " + objects.Count);
                string[] array = objects.Keys.ToArray();
                Array.Sort(array);
                selectedObjectNameIndex = EditorGUILayout.Popup(selectedObjectNameIndex, array);
                break;

            case 1:
                GUILayout.Label("Base Settings", EditorStyles.boldLabel);
                string[] array2 = objects.Keys.ToArray();
                Array.Sort(array2);
                GUILayout.Label("Selected Object: " + array2[selectedObjectNameIndex]);
                startDate = EditorGUILayout.TextField("Start Date", startDate);
                stopDate = EditorGUILayout.TextField("Stop Date", stopDate);
                objectData = EditorGUILayout.Toggle("Object Data", objectData);
                makeEphem = EditorGUILayout.Toggle("Make Ephem", makeEphem);
                if (GUILayout.Button("Fetch Data"))
                {
                    string[] array3 = objects.Keys.ToArray();
                    Array.Sort(array3);
                    await FetchData(objects[array3[selectedObjectNameIndex]]);
                }
                if (GUILayout.Button("Parse Data"))
                {
                    ParseVelocityAndMass();
                }

                GUILayout.Space(10);
                EditorGUILayout.TextField(dataResponse, GUILayout.Height(200));
                GUILayout.Space(10);
                EditorGUILayout.Vector3Field("Position", (Vector3) positionData);
                EditorGUILayout.Vector3Field("Velocity", (Vector3) velocityData);
                EditorGUILayout.DoubleField("Mass", mass);
                break;

            case 2:
                if (GUILayout.Button("Fetch All"))
                {
                    GameObject universe = GameObject.FindGameObjectWithTag("Universe");

                    List<Transform> children = new();
                    foreach (Transform child in universe.transform)
                    {
                        children.Add(child);
                    }
                    foreach (Transform child in children)
                    {
                        DestroyImmediate(child.gameObject);
                    }

                    UnityEngine.Object prefab = Resources.Load("CelestialObject");

                    foreach (KeyValuePair<string, string> entry in objects)
                    {
                        try
                        {
                            dataResponse = "";
                            await FetchData(entry.Value);
                            ParseVelocityAndMass();
                            GameObject newObject = (GameObject) Instantiate(prefab, universe.transform);
                            newObject.name = entry.Key;
                            newObject.transform.position = (Vector3) positionData / Universe.SCALE_FACTOR;
                            CelestialObject component = newObject.GetComponent<CelestialObject>();
                            component.mass = mass;
                            component.radius = (float) radius;
                            component.position = positionData;
                            component.initialVelocity = velocityData;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                            continue;
                        }
                    }
                }
                break;
        }
    }

    private void ParseVelocityAndMass()
    {
        string positionRaw = string.Join("", dataResponse.Split("$$SOE")[1].Split("\n").Skip(2).Take(1));
        string velocityRaw = string.Join("", dataResponse.Split("$$SOE")[1].Split("\n").Skip(3).Take(1));
        double x = double.Parse(positionRaw.Split("X =")[1].Split(" Y")[0], CultureInfo.InvariantCulture) * 1000;
        double y = double.Parse(positionRaw.Split("Y =")[1].Split(" Z")[0], CultureInfo.InvariantCulture) * 1000;
        double z = double.Parse(positionRaw.Split("Z =")[1], CultureInfo.InvariantCulture) * 1000;
        positionData = new Vector3d(x, y, z);
        double vx = double.Parse(velocityRaw.Split("VX=")[1].Split("VY")[0], CultureInfo.InvariantCulture) * 1000;
        double vy = double.Parse(velocityRaw.Split("VY=")[1].Split("VZ")[0], CultureInfo.InvariantCulture) * 1000;
        double vz = double.Parse(velocityRaw.Split("VZ=")[1], CultureInfo.InvariantCulture) * 1000;
        velocityData = new Vector3d(vx, vy, vz);

        string massRegex = @"Mass,? x? ?10\^([0-9]{2}) \(?kg\)?[ ]*=[ ]*~?([0-9.]*)";
        MatchCollection matches = Regex.Matches(dataResponse, massRegex);
        Match match = matches[0];
        string exponent = match.Groups[1].Value;
        string value = match.Groups[2].Value;
        mass = double.Parse(value + "e" + exponent, CultureInfo.InvariantCulture);

        string radiusRegex = @"Vol. [M|m]ean [R|r]adius,? \(?km\)?[ ]*=[ ]*~?([0-9.]*)";
        matches = Regex.Matches(dataResponse, radiusRegex);
        match = matches[0];
        value = match.Groups[1].Value;
        radius = double.Parse(value, CultureInfo.InvariantCulture);
    }

    private async void FetchObjects()
    {
        UriBuilder uriBuilder = new("https://ssd.jpl.nasa.gov/api/horizons.api");
        var queryParams = new Dictionary<string, string>
        {
            ["format"] = "text",
            ["COMMAND"] = "mb"
        };
        List<string> parts = new();
        foreach (var kv in queryParams)
        {
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        }
        uriBuilder.Query = string.Join("&", parts);
        try
        {
            objectsResponse = await GetRequest(uriBuilder.ToString());
            Repaint();
        }
        catch (Exception ex)
        {
            objectsResponse = "Error: " + ex.Message;
        }
    }

    private async Task FetchData(string command)
    {
        UriBuilder uriBuilder = new("https://ssd.jpl.nasa.gov/api/horizons.api");
        var queryParams = new Dictionary<string, string>
        {
            ["format"] = "text",
            ["OBJ_DATA"] = objectData ? "YES" : "NO",
            ["MAKE_EPHEM"] = makeEphem ? "YES" : "NO",
            ["EPHEM_TYPE"] = ephemType,
            ["CENTER"] = "@sun",
            ["START_TIME"] = startDate,
            ["STOP_TIME"] = stopDate,
            ["STEP_SIZE"] = "'1 d'",
            ["COMMAND"] = command
        };
        List<string> parts = new();
        foreach (var kv in queryParams)
        {
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        }
        uriBuilder.Query = string.Join("&", parts);

        dataResponse = "";

        try
        {
            dataResponse = await GetRequest(uriBuilder.ToString());
            Repaint();
        }
        catch (Exception ex)
        {
            dataResponse = "Error: " + ex.Message;
        }
    }

    private async Task<string> GetRequest(string url)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);
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
