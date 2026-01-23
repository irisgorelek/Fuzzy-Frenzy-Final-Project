using System;
using UnityEngine;

public class EconomyContext
{
    public PlayerEconomyState State { get; private set; }

    private const int LifeRegenMinutes = 30;

    // UI can subscribe to this later
    public event Action OnChanged;

    public EconomyContext()
    {
        State = PlayerEconomyStorage.LoadOrCreate();
    }

    public void Save()
    {
        PlayerEconomyStorage.Save(State);
    }

    public void Wipe()
    {
        PlayerEconomyStorage.Wipe();
        State = PlayerEconomyStorage.LoadOrCreate();
        Save();
        OnChanged?.Invoke();
    }

    public void AddCoins(int amount)
    {
        State.AddCoins(amount);
        Save();
        OnChanged?.Invoke();
    }

    public bool SpendCoins(int amount)
    {
        bool ok = State.SpendCoins(amount);
        if (ok)
        {
            Save();
            OnChanged?.Invoke();
        }
        return ok;
    }

    public void AddBooster(BoosterEffectType type, int amount)
    {
        State.AddBooster(type, amount);
        Save();
        OnChanged?.Invoke();
    }

    public void AddPowerNap(int amount)
    {
        State.AddPowerNap(amount);
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Life / energy logic
    /// </summary>
    public void InitializeLivesIfNeeded()
    {
        // First run: set timestamp so regen works
        if (State.lastLifeTimestampUtcSeconds == 0)
            State.lastLifeTimestampUtcSeconds = GetUtcNowSeconds();

        ApplyLifeRegen();
    }

    public void ApplyLifeRegen()
    {
        if (State.currentLives >= State.maxLives)
        {
            // Keep timestamp fresh so it doesn’t “bank” time while full
            State.lastLifeTimestampUtcSeconds = GetUtcNowSeconds();
            Save();
            return;
        }

        long now = GetUtcNowSeconds();
        long elapsedSeconds = now - State.lastLifeTimestampUtcSeconds;
        int regenSeconds = LifeRegenMinutes * 60;

        if (elapsedSeconds < regenSeconds) return;

        int livesToAdd = (int)(elapsedSeconds / regenSeconds);
        int newLives = Mathf.Min(State.maxLives, State.currentLives + livesToAdd);

        if (newLives != State.currentLives)
        {
            State.currentLives = newLives;

            // Advance timestamp by exactly the amount used
            long usedSeconds = (long)livesToAdd * regenSeconds;
            State.lastLifeTimestampUtcSeconds += usedSeconds;

            // If we reached full, reset timestamp to now (optional but common)
            if (State.currentLives >= State.maxLives)
                State.lastLifeTimestampUtcSeconds = now;

            Save();
            OnChanged?.Invoke();
        }
    }

    public bool CanStartLevel()
    {
        ApplyLifeRegen();
        return State.currentLives > 0;
    }

    public bool TrySpendLifeForLevelStart()
    {
        ApplyLifeRegen();
        if (State.currentLives <= 0) return false;

        State.currentLives -= 1;

        // Start regen “clock” when you drop below max
        if (State.currentLives < State.maxLives)
            State.lastLifeTimestampUtcSeconds = GetUtcNowSeconds();

        Save();
        OnChanged?.Invoke();
        return true;
    }

    public bool CanBuyLives()
    {
        ApplyLifeRegen();
        return State.currentLives < State.maxLives;
    }

    public void AddLives(int amount)
    {
        if (amount <= 0) return;
        ApplyLifeRegen();

        State.currentLives = Mathf.Min(State.maxLives, State.currentLives + amount);

        if (State.currentLives >= State.maxLives)
            State.lastLifeTimestampUtcSeconds = GetUtcNowSeconds();

        Save();
        OnChanged?.Invoke();
    }

    private long GetUtcNowSeconds()
    {
        return (long)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

}
