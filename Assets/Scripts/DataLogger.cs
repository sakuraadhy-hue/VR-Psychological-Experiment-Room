using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// Everything recorded for one trial (one row of results.csv).
[Serializable]
public struct TrialRecord
{
    public string participantId;
    public ExperimentCondition condition;
    public int score;
    public int errors;
    public float timeRemaining;
    public int stressRating;   // 1 (none) .. 5 (extreme)
    public int focusRating;    // 1 (distracted) .. 5 (very focused)
    public int comfortRating;  // 1 (uncomfortable) .. 5 (very comfortable)
}

/// Appends one row per completed trial to a CSV file that can be opened in Excel, R or Python.
///
/// Where the file is saved:
///   - In the Unity Editor: the project folder (next to Assets/), so it can be committed to Git.
///   - In a built application: Application.persistentDataPath (a folder the app is allowed to write to).
/// The exact path is printed in the Console when Play mode starts.
///
/// Setup: located on the ExperimentUI prefab root, next to ExperimentManager.
public class DataLogger : MonoBehaviour
{
    [Tooltip("Name of the CSV file.")]
    public string fileName = "results.csv";

    /// Column names, written once when the file is created.
    public const string Header =
        "ParticipantID,Condition,Score,Errors,TimeRemaining,StressRating,FocusRating,ComfortRating,Timestamp";

    /// Full path of the CSV file.
    public string FilePath
    {
        get
        {
            // Application.dataPath is ".../Project/Assets", so its parent is the project folder.
            string folder = Application.isEditor
                ? Directory.GetParent(Application.dataPath).FullName
                : Application.persistentDataPath;
            return Path.Combine(folder, fileName);
        }
    }

    void Start()
    {
        Debug.Log($"[DataLogger] Results will be saved to: {FilePath}");
    }

    /// Adds one trial to the CSV. Returns false if the file could not be written
    /// (for example because it is open and locked in Excel).
    public bool LogTrial(TrialRecord record)
    {
        try
        {
            // Write the header only for a new (or empty) file.
            bool writeHeader = !File.Exists(FilePath) || new FileInfo(FilePath).Length == 0;

            // InvariantCulture keeps a dot as the decimal separator, so the commas
            // between columns are never confused with decimal commas (French locale).
            CultureInfo inv = CultureInfo.InvariantCulture;
            string line = string.Join(",",
                record.participantId,
                record.condition.ToString(),
                record.score.ToString(inv),
                record.errors.ToString(inv),
                record.timeRemaining.ToString("0.0", inv),
                record.stressRating.ToString(inv),
                record.focusRating.ToString(inv),
                record.comfortRating.ToString(inv),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", inv));

            // append: true -> add to the end of the file instead of overwriting it.
            using (StreamWriter writer = new StreamWriter(FilePath, append: true))
            {
                if (writeHeader) writer.WriteLine(Header);
                writer.WriteLine(line);
            }

            Debug.Log($"[DataLogger] Saved: {line}");
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"[DataLogger] Could not write {FilePath}: {e.Message}");
            return false;
        }
    }

    /// How many trials are already in the file (all lines except the header).
    /// Used to continue the participant numbering after restarting Play mode.
    public int CountLoggedTrials()
    {
        if (!File.Exists(FilePath)) return 0;

        try
        {
            int lines = 0;
            foreach (string line in File.ReadLines(FilePath))
                if (!string.IsNullOrWhiteSpace(line)) lines++;
            return Mathf.Max(0, lines - 1);
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
