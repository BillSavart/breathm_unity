using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FacialRawData_Collect : MonoBehaviour
{
    [Header("User Information")]
    public string user_ID;
    public string StorePath;
    private string _folderPath;
    public enum FileType
    {
        Csv,
        Txt
    }
    public FileType Filetype;
    public enum compareType
    {
        AU6_AU12_EmotionStatus,
        // AU6_vs_AU12,
        // AU12_vs_EmotionStatus
    }
    public compareType CurrentCompareType;
    
    [Header("Facial Data")]
    public float AU6_L;
    public float AU6_R;
    public float AU12_L;
    public float AU12_R;
    public float AU26; //JawDrop
    public float upperlipRaiser_L;
    public float upperlipRaiser_R;

    public float[] smileStrength; //AU6, AU12
    // private string compare1, compare2;

    [Header ("Data Settings")]
    //Emotion Status
    public BarkelyState emotionStatus;
    public QuestPro_SmileDetection smileDetection;
    public bool isRecording = false;

    //public TextWriter tw;
    public float tm;
    
    private List<dataSet> rawData = new List<dataSet>();
    
    public bool isDataSaved = false;
    public float stopfloat;

    
    //--------
    // public float updateInterval = 0.1f;
    // private double lastInterval;
    // private int frames;
    // private float fps;
    // public float time_check = 0.0f;


    void Start()
    {
        smileDetection = FindObjectOfType(typeof(QuestPro_SmileDetection)) as QuestPro_SmileDetection;
        _folderPath = Application.dataPath + StorePath;
        
        // lastInterval = Time.realtimeSinceStartup;
        // frames = 0;

        InvokeRepeating("UpdateEverySec", 0.0f, 0.1f); //Update every 0.1 second

    }


    void Update()
    {
        
    }

    private void UpdateEverySec()
    {
        // ++frames;
        // float timeNow = Time.realtimeSinceStartup;
        // Debug.Log(timeNow - lastInterval);
        // lastInterval = timeNow;

        //update AU values every frame=======================
        AU6_L = smileDetection.cheek_raise_L;
        AU6_R = smileDetection.cheek_raise_R;
        AU12_L = smileDetection.Lip_corner_Up_L;
        AU12_R = smileDetection.Lip_corner_Up_R;
        AU26 = smileDetection.jawDrop;
        upperlipRaiser_L = smileDetection.upper_lip_raise_L;
        upperlipRaiser_R = smileDetection.upper_lip_raise_R;

        //average the AU values==============================
        smileStrength[0] = (AU6_L + AU6_R) / 2; //AU6
        smileStrength[1] = (AU12_L + AU12_R) / 2; //AU12
        smileStrength[2] = (AU26); //AU26
        smileStrength[3] = (upperlipRaiser_L + upperlipRaiser_R) / 2; //upperlipRaiser
        


        if (isRecording == true)
        {
            tm += Time.deltaTime;
            if (CurrentCompareType == compareType.AU6_AU12_EmotionStatus) //AU6 vs AU12 (0.0 - 1.0)
            {
                rawData.Add(new dataSet { y1 = smileStrength[0], y2 = smileStrength[1], y3 = emotionStatus.currStateValue, y4 = smileStrength[2], y5 = smileStrength[3], x = tm });             
            }
        }
        else if (isRecording == false && emotionStatus.currStateValue > stopfloat)
        {
            if (isDataSaved == false)
            {
                saveData();
                isDataSaved = true;
            }
        }
    }
    public void saveData()
    {
        string fileName = user_ID + "_" + CurrentCompareType + "_FacialRawData.csv";
        StreamWriter tw = new StreamWriter(_folderPath + fileName);
        tw.WriteLine("user_ID" + "," +  "AU6_CheekRaiser" + "," + "AU12_LipCornerPuller" + "," + "AU26_JawDrop" + "," + "AU10_UpperLipRaiser" + "," +"EmotionStatus"+ ","  + "Update_Time");

        for (int i = 0; i < rawData.Count; i++)
        {
            tw.WriteLine(user_ID +"," + rawData[i].y1 + "," + rawData[i].y2 + "," + rawData[i].y4 + "," + rawData[i].y5 + "," + rawData[i].y3 + "," + rawData[i].x);
        }
        tw.Flush();
        tw.Close();

    }
}


[System.Serializable]
public class dataSet
{
    public float y1; //AU6
    public float y2; //AU12
    public float y3; //Emotion Status
    public float y4; //AU26
    public float y5; //upperlipRaiser
    public float x; //Time
}