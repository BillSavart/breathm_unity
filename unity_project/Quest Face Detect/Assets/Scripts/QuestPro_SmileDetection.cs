using UnityEngine;

/// <summary>
/// Quest Pro facial-expression based emotion monitor (Happy / Angry / Sad / Surprise).
/// Keeps your original raw AU-style fields (smileStrength, jawDrop, etc.)
/// and adds smoothed emotion scores + stable detection with hold time.
/// </summary>
public class QuestPro_SmileDetection : MonoBehaviour
{
    [Header("Wiring")]
    public SmileSceneManager manager;
    public bool getData = true;
    public OVRFaceExpressions AvatarFace;

    [Header("Raw Face Weights (for inspection / logging)")]
    public float Lip_corner_Up_L;         // AU12 L
    public float Lip_corner_Up_R;         // AU12 R
    public float cheek_raise_L;           // AU6  L
    public float cheek_raise_R;           // AU6  R
    public float smileStrength;           // (AU12 L+R)/2  (kept)
    public float jawDrop;                 // AU26 (Surprise)
    public float brow_Lowerer_L;          // AU4  L (Angry)
    public float brow_Lowerer_R;          // AU4  R (Angry) - added but safe if enum exists
    public float lip_Corner_Depressor_L;  // AU15 L (Sad)
    public float lip_Corner_Depressor_R;  // AU15 R (Sad)

    [Header("Smoothed Emotion Scores (0~1)")]
    public float happyScore;
    public float angryScore;
    public float sadScore;
    public float surpriseScore;

    [Header("Detection Tuning")]
    [Range(0.01f, 1.0f)] public float emaAlpha = 0.18f;   // smaller = smoother
    public float holdSeconds = 0.20f;

    [Header("Thresholds")]
    public float happyThreshold = 0.55f;
    public float angryThreshold = 0.55f;
    public float sadThreshold = 0.55f;
    public float surpriseThreshold = 0.60f;

    [Header("Neutral Calibration (recommended)")]
    public bool autoCalibrateOnStart = true;
    public float calibrateSeconds = 1.0f;

    // --- internal ---
    float _happyHold, _angryHold, _sadHold, _surpriseHold;

    bool _calibrating;
    float _calibT;
    float _baseAu6, _baseAu12, _baseAu4, _baseAu15, _baseAu26;
    int _calibCount;

    void Start()
    {
        if (autoCalibrateOnStart)
        {
            BeginCalibration();
        }
    }

    public void BeginCalibration()
    {
        _calibrating = true;
        _calibT = 0f;
        _calibCount = 0;
        _baseAu6 = _baseAu12 = _baseAu4 = _baseAu15 = _baseAu26 = 0f;

        // reset holds so you won't "carry" a previous state into the next session
        _happyHold = _angryHold = _sadHold = _surpriseHold = 0f;
    }

    void Update()
    {
        if (!getData || AvatarFace == null) return;

        // --- Read raw weights (only using expressions you've already referenced before) ---
        cheek_raise_L = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.CheekRaiserL);
        cheek_raise_R = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.CheekRaiserR);

        Lip_corner_Up_L = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerPullerL);
        Lip_corner_Up_R = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerPullerR);

        jawDrop = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.JawDrop);

        brow_Lowerer_L = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.BrowLowererL);

        // Some SDK versions may not have BrowLowererR / LipCornerDepressorR.
        // If your project errors here, comment the R lines and the corresponding averages below.
        brow_Lowerer_R = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.BrowLowererR);

        lip_Corner_Depressor_L = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerDepressorL);
        lip_Corner_Depressor_R = AvatarFace.GetWeight(OVRFaceExpressions.FaceExpression.LipCornerDepressorR);

        // Keep your original smileStrength definition
        smileStrength = (Lip_corner_Up_L + Lip_corner_Up_R) * 0.5f;

        // --- Build AU averages ---
        float au6  = (cheek_raise_L + cheek_raise_R) * 0.5f;                // eye/cheek
        float au12 = smileStrength;                                         // mouth corners
        float au26 = jawDrop;                                               // surprise
        float au4  = (brow_Lowerer_L + brow_Lowerer_R) * 0.5f;              // angry
        float au15 = (lip_Corner_Depressor_L + lip_Corner_Depressor_R) * 0.5f; // sad mouth

        // --- Neutral calibration (baseline) ---
        if (_calibrating)
        {
            _calibT += Time.deltaTime;

            _baseAu6  += au6;
            _baseAu12 += au12;
            _baseAu4  += au4;
            _baseAu15 += au15;
            _baseAu26 += au26;
            _calibCount++;

            if (_calibT >= Mathf.Max(0.1f, calibrateSeconds))
            {
                float inv = 1f / Mathf.Max(1, _calibCount);
                _baseAu6  *= inv;
                _baseAu12 *= inv;
                _baseAu4  *= inv;
                _baseAu15 *= inv;
                _baseAu26 *= inv;

                _calibrating = false;
            }
        }

        // Convert to "delta from neutral" (clamped)
        float dAu6  = Mathf.Clamp01(au6  - _baseAu6);
        float dAu12 = Mathf.Clamp01(au12 - _baseAu12);
        float dAu4  = Mathf.Clamp01(au4  - _baseAu4);
        float dAu15 = Mathf.Clamp01(au15 - _baseAu15);
        float dAu26 = Mathf.Clamp01(au26 - _baseAu26);

        // --- HAPPY (eyes can trigger even if mouth doesn't move, but mouth still contributes) ---
        // Dynamic weighting:
        //  - If mouth is small, rely more on eyes (AU6).
        //  - If mouth is large, mouth contributes more.
        float mouthW = Mathf.Lerp(0.15f, 0.45f, dAu12); // always keep a mouth contribution, but never let it dominate
        float eyeW   = 1f - mouthW;

        float happyRaw = Mathf.Clamp01(eyeW * dAu6 + mouthW * dAu12);

        // --- ANGRY / SAD / SURPRISE (kept intuitive, based on your existing signals) ---
        float angryRaw    = Mathf.Clamp01(dAu4);
        float sadRaw      = Mathf.Clamp01(dAu15);
        float surpriseRaw = Mathf.Clamp01(dAu26);

        // --- Smooth (EMA) ---
        happyScore    = Mathf.Lerp(happyScore,    happyRaw,    emaAlpha);
        angryScore    = Mathf.Lerp(angryScore,    angryRaw,    emaAlpha);
        sadScore      = Mathf.Lerp(sadScore,      sadRaw,      emaAlpha);
        surpriseScore = Mathf.Lerp(surpriseScore, surpriseRaw, emaAlpha);

        // --- Stable detection (hold time) ---
        _happyHold    = (happyScore    >= happyThreshold)    ? (_happyHold    + Time.deltaTime) : 0f;
        _angryHold    = (angryScore    >= angryThreshold)    ? (_angryHold    + Time.deltaTime) : 0f;
        _sadHold      = (sadScore      >= sadThreshold)      ? (_sadHold      + Time.deltaTime) : 0f;
        _surpriseHold = (surpriseScore >= surpriseThreshold) ? (_surpriseHold + Time.deltaTime) : 0f;

        // Preserve your existing success flag behavior, but make it stable.
        if (manager != null)
        {
            manager.smile_success = (_happyHold >= holdSeconds);
        }
    }

    public bool IsCalibrating()
    {
        return _calibrating;
    }
}
