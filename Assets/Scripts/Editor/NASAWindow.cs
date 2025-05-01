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
using Newtonsoft.Json;
using Unity.VisualScripting;

/// <summary>
/// Ein EditorWindow welches Daten von der NASA abruft und daraus Objekte im generiert.
/// </summary>
public class NASAWindow : EditorWindow
{
    int tab = 0;

    // Object Page
    string objectsResponse = "";
    private readonly Dictionary<string, string> objects = new();
    private int selectedObjectNameIndex = 0;
    bool generateMoons = true;

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
        // Das Fenster ist in drei Tabs unterteilt
        tab = GUILayout.Toolbar(tab, new string[] { "Object", "Data", "Generation" });
        switch (tab)
        {
            // Tab 1: Objekte
            // Hier werden die Namen und später nötigen IDs der Objekte ovn der NASA API geladen.
            case 0:
                // Lädt beim Drücken des Knopfes die nötigen Daten von der NASA API
                if (GUILayout.Button("Fetch Data"))
                {
                    FetchObjects();
                }

                // Ob Monde erstellt werden sollen
                generateMoons = EditorGUILayout.Toggle("Generate Moons", generateMoons);

                // Verarbeitet die Daten und speichert alle Objekte mit ihrem Name und der ID
                if (GUILayout.Button("Parse Data"))
                {
                    // Ein Objekt pro Zeile
                    string[] lines = objectsResponse.Split("\n");

                    foreach (string raw in lines)
                    {
                        // Entfernt Leerzeichen am Anfang und Ende der Zeile
                        string line = raw.Trim();

                        // Wenn die Zeile leer ist, ignoriere sie
                        if (line.ToCharArray().Length == 0) continue;

                        // Wenn die Zeile mit einer Zahl beginnt, ist es ein Objekt
                        if (Char.IsDigit(line.ToCharArray()[0])) 
                        {
                            // Zuerst kommt die ID
                            string idTxt = line.Split(" ")[0];
                            int id = int.Parse(idTxt, CultureInfo.InvariantCulture);

                            // Nun wird überprüft ob das Objekt relevant für die Simulation ist
                            // Negative IDs sind für Objekte wir Raumsonden, diese sind nicht relevant
                            if (id < 0) continue;
                            // Barycenters der Planeten, nicht relevant
                            if (id < 10) continue;
                            // Ungewollte Objekte
                            if (id >= 30 && id < 40) continue;
                            // Irrelevante Objekte, wir Asterioden
                            if (id > 1_000) continue;
                            // Wenn keine Monde erstellt werden sollen, überspringe sie
                            if (!generateMoons && id % 100 != 99 && id != 10) continue;

                            // Nach der ID kommt der Name
                            string name;
                            try
                            {
                                string nameAndId = line.Split("   ")[0];
                                name = nameAndId.Split("  ")[1];
                            }
                            catch (Exception)
                            {
                                // Es konnte kein Name gefunden werden, also ignoriere das Objekt
                                continue;
                            }

                            // Speichert das Objekt
                            objects.Add(name, idTxt);
                        }
                    }
                }

                // Zeigt die von der NASA erhaltenen Daten an
                GUILayout.Space(10);
                EditorGUILayout.TextField(objectsResponse, GUILayout.Height(200));
                GUILayout.Space(10);
                
                // Zeigt die Anzahl der Objekte an
                GUILayout.Label("Parsed objects: " + objects.Count);

                // Auswahl des Objektes für den 2. Tab
                string[] array = objects.Keys.ToArray();
                Array.Sort(array);
                selectedObjectNameIndex = EditorGUILayout.Popup(selectedObjectNameIndex, array);
                break;

            // Tab 2: Daten für ein einzelnes Objekt
            case 1:
                // Einstellungen für den API-Request, müssen nicht verändert werden
                GUILayout.Label("Base Settings", EditorStyles.boldLabel);
                string[] array2 = objects.Keys.ToArray();
                Array.Sort(array2);
                GUILayout.Label("Selected Object: " + array2[selectedObjectNameIndex]);
                startDate = EditorGUILayout.TextField("Start Date", startDate);
                stopDate = EditorGUILayout.TextField("Stop Date", stopDate);
                objectData = EditorGUILayout.Toggle("Object Data", objectData);
                makeEphem = EditorGUILayout.Toggle("Make Ephem", makeEphem);

                // Knopf, um die Daten von der NASA zu laden
                if (GUILayout.Button("Fetch Data"))
                {
                    string[] array3 = objects.Keys.ToArray();
                    Array.Sort(array3);
                    await FetchData(objects[array3[selectedObjectNameIndex]]);
                }

                // Knopf, um die Daten zu verarbeiten
                if (GUILayout.Button("Parse Data"))
                {
                    if (!ParseVelocityAndMass())
                    {
                        // Wenn die NASA API keine Daten für Masse oder Radius bereitstellt, nutzen wir eine andere API
                        await ParseMassAndRadiusFallback(array2[selectedObjectNameIndex]);
                    }
                }

                // Zeigt die von der NASA erhaltenen Daten an
                GUILayout.Space(10);
                EditorGUILayout.TextField(dataResponse, GUILayout.Height(200));
                GUILayout.Space(10);

                // Zeigt die erhaltenen Daten an
                EditorGUILayout.Vector3Field("Position", (Vector3) positionData);
                EditorGUILayout.Vector3Field("Velocity", (Vector3) velocityData);
                EditorGUILayout.DoubleField("Mass", mass);
                EditorGUILayout.DoubleField("Radius", radius);
                break;

            // Tab 3: Generierung der Objekte
            case 2:
                // Knopf um alle Objekte zu generieren
                if (GUILayout.Button("Fetch All"))
                {
                    // Alle Objekte sollen unter dem Universums-Objekt erstellt werden
                    GameObject universe = GameObject.FindGameObjectWithTag("Universe");

                    // Enfernt alle bereits vorhanden Objekte
                    List<Transform> children = new();
                    foreach (Transform child in universe.transform)
                    {
                        children.Add(child);
                    }
                    foreach (Transform child in children)
                    {
                        DestroyImmediate(child.gameObject);
                    }

                    // Lädt das Prefab für ein einzelnes Objekt. Dieses enthält die CelestialObject-Komponente
                    UnityEngine.Object prefab = Resources.Load("CelestialObject");

                    // Für jedes in Tab 1 geladene Objekt
                    foreach (KeyValuePair<string, string> entry in objects)
                    {
                        try
                        {
                            // Lade die Daten von der NASA API
                            dataResponse = "";
                            await FetchData(entry.Value);

                            // Verarbeite die Daten
                            if (!ParseVelocityAndMass())
                            {
                                // Falls Masse oder Radius nicht von der NASA bereitgestellt werden, nutze eine andere API
                                await ParseMassAndRadiusFallback(entry.Key);
                                Debug.Log("Using fallback data for " + entry.Key);
                            }

                            // Erstelle ein neues Objekt unter dem Universums-Objekt
                            GameObject newObject = (GameObject) Instantiate(prefab, universe.transform);
                            // Setze den Namen des Objektes
                            newObject.name = entry.Key;
                            // Setze die Position des Objektes
                            newObject.transform.position = (Vector3) positionData / Universe.SCALE_FACTOR;

                            // Referenz zur CelestialObject-Komponente
                            CelestialObject component = newObject.GetComponent<CelestialObject>();
                            // Setze die Werte der Komponente
                            component.mass = mass;
                            component.radius = (float) radius;
                            component.position = positionData;
                            component.initialVelocity = velocityData;

                            // Fügt das Modell hinzu
                            UnityEngine.Object model = Resources.Load("Models/" + entry.Key);
                            GameObject modelObject = (GameObject)Instantiate(model, newObject.transform);
                            modelObject.GetComponentInChildren<Camera>().enabled = false;
                            modelObject.GetComponentInChildren<Light>().enabled = false;
                            modelObject.transform.localScale = Vector3.one * (float)(radius / 2 / Universe.SCALE_FACTOR * Universe.SIZE_MULTIPLIER);
                        }
                        catch (Exception ex)
                        {
                            // Wenn ein Fehler auftritt, wird dieser in der Konsole ausgegeben
                            Debug.LogError(ex);
                            continue;
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Lädt die Position, Geschwindigkeit, Masse und den Radius aus der bereits geladenen Antwort der NASA API
    /// </summary>
    /// <returns>Ob das Verarbeiten erfolgreich war</returns>
    private bool ParseVelocityAndMass()
    {
        // Lädt die Positions- und Geschwindigkeitsdaten aus der API-Response
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

        // Lädt die Masse aus der API-Response
        try
        {
            string massRegex = @"Mass,? x? ?\(?10\^([0-9]{2}) \(?kg ?\)? *= *~?([0-9.]*) ?\(?1?0?\^?([\-0-9]*)\)?";
            MatchCollection matches = Regex.Matches(dataResponse, massRegex);
            Match match = matches[0];
            float exponent = float.Parse(match.Groups[1].Value);
            string value = match.Groups[2].Value;
            if (match.Groups.Count == 4 && match.Groups[3].Value != "")
            {
                exponent += float.Parse(match.Groups[3].Value);
            }
            string optionalExponent = match.Groups[3].Value;
            mass = double.Parse(value + "e" + exponent, CultureInfo.InvariantCulture);
        } catch
        {
            // Masse konnte nicht geladen werden
            // Es wird false zurückgegeben, damit die Fallback-Methode genutzt wird
            Debug.Log("Failed to parse mass");
            return false;
        }

        // Lädt den Radius aus der API-Response
        try
        {
            string radiusRegex = @"(Vol. [M|m]ean )?[R|r]adius,? \(?km\)?[ ]*=[ ]*~?([0-9.]*)";
            MatchCollection matches = Regex.Matches(dataResponse, radiusRegex);
            Match match = matches[0];
            string value = match.Groups[2].Value;
            radius = double.Parse(value, CultureInfo.InvariantCulture);
        } catch
        {
            // Radius konnte nicht geladen werden
            // Es wird false zurückgegeben, damit die Fallback-Methode genutzt wird
            Debug.Log("Failed to parse radius");
            return false;
        }

        // Alles erfolgreich geladen
        return true;
    }

    // Modell für die Fallback-API
    private class Body
    {
        public Mass mass;
        public double meanRadius;
    }

    private class Mass
    {
        public double massValue;
        public int massExponent;
    }

    /// <summary>
    /// Lädt den Radius und die Masse aus der Fallback-API
    /// </summary>
    /// <param name="name">Name des Objektes</param>
    private async Task ParseMassAndRadiusFallback(string name)
    {
        UriBuilder uriBuilder = new("https://api.le-systeme-solaire.net/rest.php/bodies/" + name);
        string json = await GetRequest(uriBuilder.ToString());

        Body body = JsonConvert.DeserializeObject<Body>(json);
        mass = body.mass.massValue * Math.Pow(10, body.mass.massExponent);
        radius = body.meanRadius;
    }

    /// <summary>
    /// Lädt die Objekte (Name und ID) von der NASA API
    /// </summary>
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

    /// <summary>
    /// Lädt Daten für ein spezifisches Objekt von der NASA API
    /// </summary>
    /// <param name="command">ID des Objektes</param>
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

    /// <summary>
    /// Macht einen GET-Request an die gegebene URL und gibt den Inhalt der Antwort zurück
    /// </summary>
    /// <param name="url">URL für die Request</param>
    /// <returns></returns>
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
