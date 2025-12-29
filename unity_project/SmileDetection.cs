 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Lip;
using UnityEngine.SceneManagement;

public class SmileDetection : MonoBehaviour
{
    public bool NeededToGetData = true;
    private Dictionary<LipShape_v2, float> LipWeightings;
    // public float l_smile;
    // public float r_smile;
    public float smileAvg;
    
    public SmileSceneManager manager;
    public bool check; //chrck for avatar selection
    public bool start_check;

    public bool Smile_Bus_check;

    
    private void Start()
    {
        if (!SRanipal_Lip_Framework.Instance.EnableLip)
        {
            enabled = false;
            return;
        }
        
    }

    public void Update()
    {
        
        if (SRanipal_Lip_Framework.Status != SRanipal_Lip_Framework.FrameworkStatus.WORKING) return;

        if (NeededToGetData)
        {
            SRanipal_Lip_v2.GetLipWeightings(out LipWeightings);
            EmotionRecognition(LipWeightings);
        }
    }

    private void EmotionRecognition(Dictionary<LipShape_v2, float> lipWeightings){
        float rightSmile = lipWeightings[LipShape_v2.Mouth_Smile_Right];
        float leftSmile = lipWeightings[LipShape_v2.Mouth_Smile_Left];
        // for testing 
        // Debug.Log("r: " + rightSmile + " l: " + leftSmile);

        smileAvg = (rightSmile + leftSmile)/2.0f;
        
        Scene scene = SceneManager.GetActiveScene();
        if(smileAvg >= 0.7f)
        {
            // check = true;
            // start_check = true; //StartScene Only----
            // if (scene.name == "RoomScene_Bus" &&smileAvg >= 0.8f)
            //     {
            //         Smile_Bus_check = true;
            //         // manager.busScene_smileAgree = true;
            //     }
             //BusScene Only----
            manager.smile_success = true;  //set smilescene manager to true
        }
        else
        {
            // start_check = false;

            // Smile_Bus_check = false;
            
        }
    }
}
