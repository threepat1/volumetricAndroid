using prometheus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputTextManager : MonoBehaviour
{
    public GameObject panel;
    public MeshPlayerPlugin meshPlugin;
    InputField input;
    public Text text;

    // Start is called before the first frame update
    void Start()
    {
        meshPlugin = FindObjectOfType<MeshPlayerPlugin>();
        input = FindObjectOfType<InputField>();
        
    }

    // Update is called once per frame
    void Update()
    {
       
        
    }
    public void InsertText()
    {
        meshPlugin.SourceType = SOURCE_TYPE.RTMP;
        meshPlugin.SourceUrl = input.text;
        text.text = input.text; 
        meshPlugin.Initialize();
       
        
    }
    public void ToggleButton()
    {
        panel.SetActive(!panel.activeSelf);
    }
}
