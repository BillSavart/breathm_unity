using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

/// <summary>
/// Collects raw facial weights + 4 emotion scores to CSV, and shows a neat HUD.
/// Keeps your original "A or X to toggle recording" behavior.
/// </summary>
public class FacialRawData_Collect : MonoBehaviour
{
    public string user_ID = "Bill";
    private string _folderPath;

    [Header("Data Wiring")]
    public QuestPro_SmileDetection smileDetection;
    public bool isRecording = false;

    [Header("UI")]
    public TMP_Text monitorText;

    [Header("CSV Settings")]
    public string filePrefix = "QuestPro_Emotion";
    public bool writeHeaderIfNew = true;

    private string _filePath;
    private StreamWriter _writer;

    // timer
    private float time = 0f;

    void Start()
    {
        _folderPath = Path.Combine(Application.persistentDataPath, "QuestPro_EmotionLogs");
        if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);

        CreateNewFilePath();
        UpdateHUD(); // initial
    }

    void Update()
    {
        if (smileDetection == null)
        {
            if (monitorText != null) monitorText.text = "<b>QuestPro</b>\n<color=#FF6666>smileDetection 未指定</color>";
            return;
        }

        // Toggle recording: A (right controller) or X (left controller)
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
        {
            ToggleRecording();
        }

        // Optional: Re-calibrate neutral with B/Y (clean + handy, no ugly words)
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four))
        {
            smileDetection.BeginCalibration();
        }

        // record
        if (isRecording)
        {
            time += Time.deltaTime;
            WriteRow();
        }

        UpdateHUD();
    }

    private void ToggleRecording()
    {
        isRecording = !isRecording;

        if (isRecording)
        {
            OpenWriter();
        }
        else
        {
            CloseWriter();
            // next session -> new file
            CreateNewFilePath();
            time = 0f;
        }
    }

    private void CreateNewFilePath()
    {
        string fileName = $"{filePrefix}_{user_ID}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        _filePath = Path.Combine(_folderPath, fileName);
    }

    private void OpenWriter()
    {
        CloseWriter();

        bool fileExists = File.Exists(_filePath);
        _writer = new StreamWriter(_filePath, append: true);

        if (writeHeaderIfNew && !fileExists)
        {
            // Keep things simple & explicit: raw AUs + 4 emotion scores
            _writer.WriteLine("time,au6,au12,au4,au15,au26,happy,angry,sad,surprise");
            _writer.Flush();
        }
    }

    private void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
    }

    private void WriteRow()
    {
        if (_writer == null) return;

        float au6  = (smileDetection.cheek_raise_L + smileDetection.cheek_raise_R) * 0.5f;
        float au12 = smileDetection.smileStrength;
        float au4  = (smileDetection.brow_Lowerer_L + smileDetection.brow_Lowerer_R) * 0.5f;
        float au15 = (smileDetection.lip_Corner_Depressor_L + smileDetection.lip_Corner_Depressor_R) * 0.5f;
        float au26 = smileDetection.jawDrop;

        _writer.WriteLine(
            $"{time:F3},{au6:F4},{au12:F4},{au4:F4},{au15:F4},{au26:F4}," +
            $"{smileDetection.happyScore:F4},{smileDetection.angryScore:F4},{smileDetection.sadScore:F4},{smileDetection.surpriseScore:F4}"
        );
        _writer.Flush();
    }

    private void UpdateHUD()
    {
        if (monitorText == null) return;

        // neat mini dashboard (no "口罩" wording)
        string rec = isRecording ? "<color=#FF4D4D><b>● REC</b></color>" : "<color=#A0A0A0>REC off</color>";
        string cal = smileDetection.IsCalibrating() ? "<color=#FFD166>Calibrating…</color>" : "<color=#7CFF7C>Ready</color>";

        float au6  = (smileDetection.cheek_raise_L + smileDetection.cheek_raise_R) * 0.5f;
        float au12 = smileDetection.smileStrength;
        float au4  = (smileDetection.brow_Lowerer_L + smileDetection.brow_Lowerer_R) * 0.5f;
        float au15 = (smileDetection.lip_Corner_Depressor_L + smileDetection.lip_Corner_Depressor_R) * 0.5f;
        float au26 = smileDetection.jawDrop;

        // Use monospace alignment via TMP <mspace>
        monitorText.text =
            "<size=34><b>Emotion Monitor</b></size>\n" +
            $"{rec}   {cal}\n" +
            "<color=#5DADE2>────────────────────────</color>\n" +

            "<size=28><b>Scores</b></size>\n" +
            $"<mspace=0.6em>Happy   {smileDetection.happyScore,5:F2}   Angry   {smileDetection.angryScore,5:F2}</mspace>\n" +
            $"<mspace=0.6em>Sad     {smileDetection.sadScore,5:F2}   Surprise{smileDetection.surpriseScore,5:F2}</mspace>\n" +

            "<color=#5DADE2>────────────────────────</color>\n" +
            "<size=28><b>Signals</b></size>\n" +
            $"<mspace=0.6em>AU6  {au6,6:F3}   AU12 {au12,6:F3}   AU4  {au4,6:F3}</mspace>\n" +
            $"<mspace=0.6em>AU15 {au15,6:F3}   AU26 {au26,6:F3}</mspace>\n" +

            "<color=#5DADE2>────────────────────────</color>\n" +
            "<size=24><color=#B8B8B8>A/X: REC  •  B/Y: Recalibrate</color></size>";
    }

    void OnDestroy()
    {
        CloseWriter();
    }
}
