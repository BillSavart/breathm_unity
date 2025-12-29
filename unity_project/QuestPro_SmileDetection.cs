using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestPro_SmileDetection : MonoBehaviour
{
    public SmileSceneManager manager;
    public bool getData = true;
    public OVRFaceExpressions AvatarFace;

    [Header("Face Recognition")]
    public float Lip_corner_Up_L; //if user smiling====
    public float Lip_corner_Up_R;
    // public float upper_lip_raise_L;
    // public float upper_lip_raise_R;
    public float cheek_raise_L;
    public float cheek_raise_R;

    public float smileStrength;

    public float jawDrop;
    public float upper_lip_raise_L;
    public float upper_lip_raise_R;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // if ()
        if (getData)
        {
            cheek_raise_L = AvatarFace.expression4;
            cheek_raise_R = AvatarFace.expression5;
            Lip_corner_Up_L = AvatarFace.expression32;
            Lip_corner_Up_R = AvatarFace.expression33;

            jawDrop = AvatarFace.expression24;
            upper_lip_raise_L = AvatarFace.expression61;
            upper_lip_raise_R = AvatarFace.expression62;


            
            smileStrength = (Lip_corner_Up_L+Lip_corner_Up_R)/2;
            
            smileRecognition(smileStrength);
        }
        
    }

    private void smileRecognition(float smileStrength)
    {
       
        //Game Scene=================================================
        if (smileStrength >= 0.5f)
        {
            manager.smile_success = true;
        }

    }


}
