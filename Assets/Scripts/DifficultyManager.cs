using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Difficulty { Easy, Hard}
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager instance;
    public Difficulty difficulty = Difficulty.Easy; 
    void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
}
