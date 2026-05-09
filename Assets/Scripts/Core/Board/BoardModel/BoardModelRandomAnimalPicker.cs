using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class BoardModelRandomAnimalPicker
{
    public static Animal PickRandomAllowedAnimal(List<Animal> allowedAnimals)
    {
        // If there's nothing to pick from, return null
        if (allowedAnimals == null || allowedAnimals.Count == 0)
            return null;

        // Compute the sum of all weights
        float total = 0f;
        for (int i = 0; i < allowedAnimals.Count; i++)
            total += Mathf.Max(0f, allowedAnimals[i]._spawnWeight);

        // If total is 0 (all weights were 0 or negative), fallback to uniform random
        if (total <= 0f)
            return allowedAnimals[Random.Range(0, allowedAnimals.Count)];

        // Pick a random number in [0, total)
        float r = Random.value * total;

        // Walk through the animals, adding weights until we "cross" r
        float cumulative = 0f;
        for (int i = 0; i < allowedAnimals.Count; i++)
        {
            cumulative += Mathf.Max(0f, allowedAnimals[i]._spawnWeight);

            // Select the first animal whose cumulative range contains r
            if (r <= cumulative)
                return allowedAnimals[i];
        }

        return allowedAnimals[allowedAnimals.Count - 1];
    }
}
