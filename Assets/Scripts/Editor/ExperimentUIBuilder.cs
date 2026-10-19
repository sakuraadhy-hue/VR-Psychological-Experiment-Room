using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Editor tool (Tools > Experiment > Build Experiment UI) that creates the whole
/// ExperimentUI in the open scene, wires every reference and button, and saves it
/// as Assets/Prefabs/ExperimentUI.prefab.
///
/// It builds:
///   - North wall: a world-space Canvas (briefing, survey, completion panels),
///     a 3D TextMeshPro digital clock with score, and the 3D START button.
///   - West wall: the researcher panel with a 3D TextMeshPro status screen and
///     NEUTRAL / CALM / STRESS / RESET push buttons.
///   - An EventSystem if the scene has none (needed for clicking canvas buttons).
///
/// Positions are based on Room_Shell (10 x 10 m room, walls at +/-5 m, 0.2 m thick).
/// Everything can be moved afterwards in the Scene view.
/// This script is in an Editor folder, so it is not included in builds.
public static class ExperimentUIBuilder
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/ExperimentUI.prefab";
    private const string MaterialFolder = "Assets/Materials/UI";

    // Inner faces of the walls (wall centre 5 m, half thickness 0.1 m).
    private const float NorthWallZ = 4.9f;
    private const float WestWallX = -4.9f;

    // Canvas resolution: 1200 x 650 "pixels" shown at 2 mm per pixel = 2.4 m x 1.3 m.
    private static readonly Vector2 CanvasSize = new Vector2(1200f, 650f);
    private const float CanvasScale = 0.002f;

    // Colours.
    private static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.14f, 0.95f);
    private static readonly Color TextColor = new Color(0.93f, 0.95f, 1f);
    private static readonly Color AccentColor = new Color(0.45f, 0.8f, 1f);
    private static readonly Color MutedColor = new Color(0.65f, 0.7f, 0.8f);
    private static readonly Color OptionColor = new Color(0.22f, 0.25f, 0.32f);
    private static readonly Color SubmitColor = new Color(0.2f, 0.65f, 0.35f);

    private static Sprite uiSprite;

    [MenuItem("Tools/Experiment/Build Experiment UI")]
    public static void Build()
    {
        // TextMeshPro needs its essential resources (default font) before any text can be created.
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro resources missing",
                "Import them first:\nWindow > TextMeshPro > Import TMP Essential Resources\n\nThen run this tool again.",
                "OK");
            return;
        }

        // Replace an existing ExperimentUI instead of creating a second one.
        ExperimentManager existing = Object.FindFirstObjectByType<ExperimentManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Experiment UI?",
                    $"The scene already contains '{existing.gameObject.name}'. Replace it?", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---------- Materials (saved as assets so the prefab keeps them) ----------
        Material boardMat   = GetOrCreateMaterial("Board_Dark",   new Color(0.10f, 0.11f, 0.13f));
        Material plateMat   = GetOrCreateMaterial("Button_Plate", new Color(0.30f, 0.30f, 0.33f));
        Material startMat   = GetOrCreateMaterial("Button_Start", new Color(0.20f, 0.75f, 0.35f));
        Material neutralMat = GetOrCreateMaterial("Button_Neutral", new Color(0.85f, 0.85f, 0.85f));
        Material calmMat    = GetOrCreateMaterial("Button_Calm",  new Color(0.95f, 0.65f, 0.25f));
        Material stressMat  = GetOrCreateMaterial("Button_Stress", new Color(0.85f, 0.15f, 0.15f));
        Material resetMat   = GetOrCreateMaterial("Button_Reset", new Color(0.20f, 0.45f, 0.85f));

        // ---------- Root with the logic components ----------
        GameObject root = new GameObject("ExperimentUI");
        Undo.RegisterCreatedObjectUndo(root, "Build Experiment UI");
        ExperimentManager manager = root.AddComponent<ExperimentManager>();
        DataLogger logger = root.AddComponent<DataLogger>();
        manager.dataLogger = logger;

        // Canvas buttons need an EventSystem in the scene.
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>(); // works because Active Input Handling = Both
        }

        // ---------- North wall: participant canvas ----------
        // The canvas hangs 8 cm in front of the wall, covering the 3D clock behind it.
        // The clock is revealed when the canvas is hidden during the trial.
        Canvas canvas = CreateWorldCanvas("ParticipantCanvas", root.transform,
            new Vector3(0f, 2.05f, NorthWallZ - 0.08f), Quaternion.identity);
        Transform canvasT = canvas.transform;

        Image background = CreateUI("Background", canvasT, Vector2.zero, CanvasSize).AddComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        GameObject briefing = BuildBriefingPanel(canvasT);
        SurveyPanel survey = BuildSurveyPanel(canvasT, manager);
        GameObject complete = CreateUI("CompletePanel", canvasT, Vector2.zero, CanvasSize);
        TextMeshProUGUI completeText = CreateUIText("Message", complete.transform, "Trial complete.",
            Vector2.zero, new Vector2(1100f, 400f), 52f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);
        complete.SetActive(false);

        // ---------- North wall: 3D TextMeshPro digital clock ----------
        GameObject clock = new GameObject("TimerDisplay");
        clock.transform.SetParent(root.transform, false);
        clock.transform.position = new Vector3(0f, 3.25f, NorthWallZ - 0.02f);
        CreateCube("Board", clock.transform, Vector3.zero, new Vector3(1.8f, 0.65f, 0.04f), boardMat, false);

        TextMeshPro timerText = Create3DText("TimerText", clock.transform, "00:00.0",
            new Vector3(0f, 0.07f, -0.025f), new Vector2(1.6f, 0.38f), 12f, new Color(0.35f, 1f, 0.55f), FontStyles.Bold);
        FreezeAutoSize(timerText, "01:00.0");

        TextMeshPro scoreText = Create3DText("ScoreText", clock.transform, "Score: 0  |  Errors: 0",
            new Vector3(0f, -0.22f, -0.025f), new Vector2(1.6f, 0.14f), 6f, TextColor, FontStyles.Normal);
        FreezeAutoSize(scoreText, "Score: 00  |  Errors: 00");

        // ---------- North wall: START button ----------
        GameObject startMount = new GameObject("StartButtonMount");
        startMount.transform.SetParent(root.transform, false);
        startMount.transform.position = new Vector3(0f, 1.12f, NorthWallZ - 0.02f);
        CreateCube("Board", startMount.transform, Vector3.zero, new Vector3(1.05f, 0.32f, 0.04f), boardMat, false);
        RemoteButton startButton = Create3DButton("Button_Start", startMount.transform, new Vector3(0f, 0f, -0.02f),
            new Vector2(0.9f, 0.2f), "START EXPERIMENT", startMat, plateMat, Color.white);
        UnityEventTools.AddPersistentListener(startButton.onPressed, manager.StartTrial);

        // ---------- West wall: researcher panel ----------
        GameObject panel = new GameObject("ResearcherPanel");
        panel.transform.SetParent(root.transform, false);
        panel.transform.position = new Vector3(WestWallX + 0.02f, 1.6f, -1.0f);
        // Local +Z points into the west wall, so the panel faces the centre of the room.
        panel.transform.rotation = Quaternion.LookRotation(Vector3.left);
        CreateCube("Board", panel.transform, Vector3.zero, new Vector3(0.9f, 1.4f, 0.04f), boardMat, false);

        Create3DText("Title", panel.transform, "RESEARCHER PANEL",
            new Vector3(0f, 0.6f, -0.025f), new Vector2(0.8f, 0.1f), 4f, AccentColor, FontStyles.Bold);
        TextMeshPro statusText = Create3DText("StatusText", panel.transform,
            "Condition: Neutral\nReady - choose a condition\nParticipant: P001",
            new Vector3(0f, 0.37f, -0.025f), new Vector2(0.8f, 0.3f), 2f, TextColor, FontStyles.Normal);

        RemoteButton neutralButton = Create3DButton("Button_Neutral", panel.transform, new Vector3(0f, 0.12f, -0.02f),
            new Vector2(0.6f, 0.14f), "NEUTRAL", neutralMat, plateMat, new Color(0.1f, 0.1f, 0.1f));
        RemoteButton calmButton = Create3DButton("Button_Calm", panel.transform, new Vector3(0f, -0.08f, -0.02f),
            new Vector2(0.6f, 0.14f), "CALM", calmMat, plateMat, new Color(0.1f, 0.1f, 0.1f));
        RemoteButton stressButton = Create3DButton("Button_Stress", panel.transform, new Vector3(0f, -0.28f, -0.02f),
            new Vector2(0.6f, 0.14f), "STRESS", stressMat, plateMat, Color.white);
        RemoteButton resetButton = Create3DButton("Button_Reset", panel.transform, new Vector3(0f, -0.54f, -0.02f),
            new Vector2(0.6f, 0.14f), "RESET", resetMat, plateMat, Color.white);

        UnityEventTools.AddPersistentListener(neutralButton.onPressed, manager.SelectNeutral);
        UnityEventTools.AddPersistentListener(calmButton.onPressed, manager.SelectCalm);
        UnityEventTools.AddPersistentListener(stressButton.onPressed, manager.SelectStress);
        UnityEventTools.AddPersistentListener(resetButton.onPressed, manager.ResetExperiment);

        // ---------- Wire the manager ----------
        manager.briefingPanel = briefing;
        manager.surveyPanel = survey;
        manager.completePanel = complete;
        manager.completeMessage = completeText;
        manager.timerText = timerText;
        manager.scoreText = scoreText;
        manager.researcherStatusText = statusText;
        manager.startButton = startButton;
        manager.neutralButton = neutralButton;
        manager.calmButton = calmButton;
        manager.stressButton = stressButton;
        manager.resetButton = resetButton;

        // ---------- Save as a prefab ----------
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);

        // ---------- Scene references (stored on the scene instance, not in the prefab) ----------
        // A prefab asset cannot point to objects in a scene, so these are assigned after saving.
        manager.sortingStation = Object.FindFirstObjectByType<SortingStation>(FindObjectsInactive.Include);
        manager.neutralObjects = FindSceneObjects(root.transform, "NeutralSet", "Environment_Neutral");
        manager.calmObjects = FindSceneObjects(root.transform, "CalmSet", "Environment_Calm");
        manager.stressObjects = FindSceneObjects(root.transform, "StressSet", "Environment_Stress", "Stress_ColdLight");
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;

        // ---------- Report ----------
        string report =
            "ExperimentUI created and saved to " + PrefabPath + ".\n\n" +
            "Sorting station: " + (manager.sortingStation != null
                ? manager.sortingStation.name + (manager.sortingStation.gameObject.activeInHierarchy ? "" : " (INACTIVE - activate it!)")
                : "NOT FOUND") + "\n" +
            "Neutral objects: " + Describe(manager.neutralObjects) + "\n" +
            "Calm objects: " + Describe(manager.calmObjects) + "\n" +
            "Stress objects: " + Describe(manager.stressObjects) + "\n\n" +
            "Missing condition objects can be dragged into the ExperimentManager lists later. " +
            "Remember to save the scene.";
        Debug.Log("[ExperimentUIBuilder] " + report);
        EditorUtility.DisplayDialog("Experiment UI built", report, "OK");
    }

    // =====================================================================
    //  Canvas panels
    // =====================================================================

    private static GameObject BuildBriefingPanel(Transform canvas)
    {
        GameObject panel = CreateUI("BriefingPanel", canvas, Vector2.zero, CanvasSize);

        CreateUIText("Title", panel.transform, "Cognitive Sorting Experiment",
            new Vector2(0f, 245f), new Vector2(1100f, 90f), 66f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);

        CreateUIText("Instructions", panel.transform,
            "Sort as many balls as you can into the bin of the same colour before the timer runs out. " +
            "You have 60 seconds. Try to be fast, but avoid mistakes.",
            new Vector2(0f, 105f), new Vector2(1060f, 150f), 36f, FontStyles.Normal, TextAlignmentOptions.Center, TextColor);

        CreateUIText("Controls", panel.transform,
            "<b>W A S D</b>  glide     <b>E / Q</b>  rise / descend     <b>Shift</b>  faster\n" +
            "<b>Hold right mouse</b> (or arrow keys)  look around\n" +
            "<b>Hold left mouse on a ball</b>  pick it up, release to drop     <b>Scroll</b>  nearer / further",
            new Vector2(0f, -85f), new Vector2(1100f, 170f), 28f, FontStyles.Normal, TextAlignmentOptions.Center, MutedColor);

        CreateUIText("StartHint", panel.transform,
            "When you are ready, click the green START button below.",
            new Vector2(0f, -255f), new Vector2(1100f, 60f), 32f, FontStyles.Italic, TextAlignmentOptions.Center, AccentColor);

        return panel;
    }

    private static SurveyPanel BuildSurveyPanel(Transform canvas, ExperimentManager manager)
    {
        GameObject panel = CreateUI("SurveyPanel", canvas, Vector2.zero, CanvasSize);

        CreateUIText("Title", panel.transform, "Post-Trial Questionnaire",
            new Vector2(0f, 270f), new Vector2(1100f, 70f), 52f, FontStyles.Bold, TextAlignmentOptions.Center, TextColor);

        LikertQuestion stress = CreateLikertRow(panel.transform, "Q_Stress",
            "1. How stressed did you feel during this session?", "None", "Extreme", 175f);
        LikertQuestion focus = CreateLikertRow(panel.transform, "Q_Focus",
            "2. How focused were you able to remain?", "Distracted", "Very focused", 30f);
        LikertQuestion comfort = CreateLikertRow(panel.transform, "Q_Comfort",
            "3. How comfortable was the room?", "Uncomfortable", "Very comfortable", -115f);

        Button submit = CreateUIButton("SubmitButton", panel.transform, "SUBMIT ANSWERS",
            new Vector2(0f, -240f), new Vector2(380f, 70f), SubmitColor, 34f);

        TextMeshProUGUI hint = CreateUIText("Hint", panel.transform, "",
            new Vector2(0f, -297f), new Vector2(1000f, 40f), 24f, FontStyles.Italic, TextAlignmentOptions.Center, MutedColor);

        SurveyPanel survey = panel.AddComponent<SurveyPanel>();
        survey.manager = manager;
        survey.stressQuestion = stress;
        survey.focusQuestion = focus;
        survey.comfortQuestion = comfort;
        survey.submitButton = submit;
        survey.hintText = hint;
        return survey;
    }

    /// One questionnaire row: question text, buttons 1-5, and labels under 1 and 5.
    private static LikertQuestion CreateLikertRow(Transform parent, string name, string question,
                                                  string lowLabel, string highLabel, float y)
    {
        GameObject row = CreateUI(name, parent, new Vector2(0f, y), new Vector2(1100f, 140f));

        CreateUIText("Question", row.transform, question, new Vector2(0f, 42f), new Vector2(1100f, 46f),
            32f, FontStyles.Normal, TextAlignmentOptions.Center, TextColor);

        Button[] options = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 160f;
            options[i] = CreateUIButton("Option_" + (i + 1), row.transform, (i + 1).ToString(),
                new Vector2(x, -12f), new Vector2(120f, 60f), OptionColor, 34f);
        }

        CreateUIText("LowLabel", row.transform, lowLabel, new Vector2(-320f, -58f), new Vector2(220f, 28f),
            22f, FontStyles.Normal, TextAlignmentOptions.Center, MutedColor);
        CreateUIText("HighLabel", row.transform, highLabel, new Vector2(320f, -58f), new Vector2(220f, 28f),
            22f, FontStyles.Normal, TextAlignmentOptions.Center, MutedColor);

        LikertQuestion likert = row.AddComponent<LikertQuestion>();
        likert.options = options;
        likert.normalColor = OptionColor;
        return likert;
    }

    // =====================================================================
    //  UI helpers (world-space canvas)
    // =====================================================================

    private static Canvas CreateWorldCanvas(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main; // also re-assigned at runtime by ExperimentManager

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = CanvasSize;
        rt.position = position;
        rt.rotation = rotation;
        rt.localScale = Vector3.one * CanvasScale;

        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
        go.AddComponent<GraphicRaycaster>(); // lets the mouse click buttons on this canvas
        return canvas;
    }

    private static GameObject CreateUI(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    private static TextMeshProUGUI CreateUIText(string name, Transform parent, string text, Vector2 position,
        Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        TextMeshProUGUI tmp = CreateUI(name, parent, position, size).AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false; // text must not block clicks on buttons
        return tmp;
    }

    private static Button CreateUIButton(string name, Transform parent, string label, Vector2 position,
        Vector2 size, Color color, float fontSize)
    {
        GameObject go = CreateUI(name, parent, position, size);
        Image image = go.AddComponent<Image>();
        image.sprite = uiSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateUIText("Label", go.transform, label, Vector2.zero, size,
            fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return button;
    }

    // =====================================================================
    //  3D helpers
    // =====================================================================

    /// A 3D TextMeshPro label (not on a canvas). It is readable when looking along its local +Z.
    private static TextMeshPro Create3DText(string name, Transform parent, string text, Vector3 localPosition,
        Vector2 size, float maxFontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;   // fits the text inside the rectangle below
        tmp.fontSizeMin = 0.05f;
        tmp.fontSizeMax = maxFontSize;
        tmp.rectTransform.sizeDelta = size;
        tmp.rectTransform.localPosition = localPosition;
        return tmp;
    }

    /// Computes the auto-size for the longest expected text, then fixes the font size,
    /// so the timer/score do not change size every time their digits change.
    private static void FreezeAutoSize(TextMeshPro tmp, string longestText)
    {
        string original = tmp.text;
        tmp.text = longestText;
        tmp.ForceMeshUpdate();
        float size = tmp.fontSize;
        tmp.enableAutoSizing = false;
        tmp.fontSize = size;
        tmp.text = original;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale,
        Material material, bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(cube.GetComponent<Collider>());
        return cube;
    }

    /// A physical push button: a fixed base plate, and a moving part (cap + label).
    /// The root holds the click collider and RemoteButton. Local +Z points into the wall.
    private static RemoteButton Create3DButton(string name, Transform parent, Vector3 localPosition, Vector2 size,
        string label, Material capMaterial, Material plateMaterial, Color labelColor)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;

        CreateCube("Plate", root.transform, new Vector3(0f, 0f, -0.01f),
            new Vector3(size.x + 0.04f, size.y + 0.04f, 0.02f), plateMaterial, false);

        GameObject pressable = new GameObject("Pressable");
        pressable.transform.SetParent(root.transform, false);
        GameObject cap = CreateCube("Cap", pressable.transform, new Vector3(0f, 0f, -0.045f),
            new Vector3(size.x, size.y, 0.05f), capMaterial, false);
        Create3DText("Label", pressable.transform, label, new Vector3(0f, 0f, -0.072f),
            new Vector2(size.x * 0.9f, size.y * 0.75f), 3f, labelColor, FontStyles.Bold);

        // Click target covering the cap. It stays still while the cap moves.
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, -0.045f);
        collider.size = new Vector3(size.x, size.y, 0.05f);

        RemoteButton button = root.AddComponent<RemoteButton>();
        button.pressablePart = pressable.transform;
        button.capRenderer = cap.GetComponent<Renderer>();
        button.pressOffset = new Vector3(0f, 0f, 0.02f);
        return button;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateFolder("Assets/Materials", "UI");
        }

        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader);
        material.SetColor("_BaseColor", color); // URP Lit colour
        material.color = color;                 // fallback for other shaders
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // =====================================================================
    //  Scene search
    // =====================================================================

    /// Finds scene objects (active or not) with one of the given names, outside the UI root.
    /// If a parent and its child both match, only the parent is kept.
    private static GameObject[] FindSceneObjects(Transform exclude, params string[] names)
    {
        List<GameObject> found = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (Transform t in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.IsChildOf(exclude) || System.Array.IndexOf(names, t.name) < 0) continue;

                bool parentAlreadyFound = false;
                foreach (GameObject other in found)
                    if (t != other.transform && t.IsChildOf(other.transform)) parentAlreadyFound = true;

                if (!parentAlreadyFound) found.Add(t.gameObject);
            }
        }
        return found.ToArray();
    }

    private static string Describe(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0) return "none found";
        List<string> list = new List<string>();
        foreach (GameObject obj in objects) list.Add(obj.name);
        return string.Join(", ", list);
    }
}
