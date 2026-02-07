using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("File")]
    [SerializeField] private string fileName = "savefile.json"; // The name of the save file
    [SerializeField] private bool prettyPrint = true; // Controls if the save file is written in a human friendly format

    public PowerUpSaveData Data { get; private set; }

    public event Action OnLoaded;
    public event Action OnChanged;

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName); // Get the path the save file was saved in

    private void Awake() // Initialize the save file
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void OnApplicationPause(bool pause) // Save the game when it's paused
    {
        if (pause) Save();
    }

    private void OnApplicationQuit() // Save the game when quitting
    {
        Save();
    }

    public void Load() // Load the data
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<PowerUpSaveData>(json);
        }
        else
        {
            Data = CreateDefault();
            Save(); // create the file on first run
        }

        if (Data == null)
            Data = CreateDefault();

        CorrectNegativeValues();

        OnLoaded?.Invoke();
        OnChanged?.Invoke();
    }

    public void Save() // Save the data
    {
        if (Data == null)
            Data = CreateDefault();

        CorrectNegativeValues();

        string json = JsonUtility.ToJson(Data, prettyPrint);
        SafeWriteAllText(SavePath, json);
    }

    public int GetCount(PowerUpType type) // Get the current amount of the power-ups
    {
        if (Data == null) 
            return 0;

        return type switch
        {
            PowerUpType.ExtraMove => Data.extraMove,
            PowerUpType.Bomb => Data.bomb,
            PowerUpType.TimerBomb => Data.timerBomb,
            _ => 0
        };
    }

    public void Add(PowerUpType type, int amount = 1) // Add X power-ups
    {
        if (amount <= 0) return;
        SetCount(type, GetCount(type) + amount);
    }

    public bool TryUsePowerUp(PowerUpType type, int amount = 1) // Try to use a power-up
    {
        if (amount <= 0) 
            return true;

        int current = GetCount(type);
        if (current < amount) 
            return false;

        SetCount(type, current - amount);
        return true;
    }

    private void SetCount(PowerUpType type, int newCount) // Set the count to the new one (either remove or add power-ups)
    {
        if (Data == null)
            Data = CreateDefault();

        newCount = Mathf.Max(0, newCount);

        switch (type)
        {
            case PowerUpType.ExtraMove: 
                Data.extraMove = newCount; 
                break;

            case PowerUpType.Bomb: 
                Data.bomb = newCount; 
                break;

            case PowerUpType.TimerBomb: 
                Data.timerBomb = newCount; 
                break;
        }

        Save();
        OnChanged?.Invoke();
    }

    private static PowerUpSaveData CreateDefault() // Create a default Power Up Save Data
    {
        return new PowerUpSaveData();
    }

    private void CorrectNegativeValues() // If there's a negative amount of powerups, turn it to 0
    {
        Data.extraMove = Mathf.Max(0, Data.extraMove);
        Data.bomb = Mathf.Max(0, Data.bomb);
        Data.timerBomb = Mathf.Max(0, Data.timerBomb);
    }

    private static void SafeWriteAllText(string path, string contents) // A safe way to write a save file
    {
        string dir = Path.GetDirectoryName(path); // Find the folder part of the path
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) // Make sure the folder exists
            Directory.CreateDirectory(dir);

        string tmp = path + ".tmp"; // Create a temp filename
        File.WriteAllText(tmp, contents); // Write the new JSON to the temp file

        if (File.Exists(path)) // Delete the old save file if it exists
            File.Delete(path);

        File.Move(tmp, path); // Rename the tmp to the real file
    }
}
