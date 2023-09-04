using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Patcher : IMod
{
    public ModMetadata Metadata => new ModMetadata("Busy Developer", "lorenzofman", "0.1.0");

    public void Init(string path)
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == "MainMenu")
        {
            var propertyInfo = typeof(MainMenu).GetProperty("STATE_TRANSITIONS_TIME", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                return;
            }
            propertyInfo.SetValue(GameObject.FindObjectOfType<MainMenu>(), Convert.ChangeType(0.0f, propertyInfo.PropertyType), null);
        }
    }

}
