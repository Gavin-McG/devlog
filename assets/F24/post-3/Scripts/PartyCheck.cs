//randomly determine whether a given check is succeeded by the party
bool IsSuccess(float strength, float difficulty, float sensitivity)
{
    float offset = strength - difficulty;
    float score = offset * sensitivity;
    float requirement = NormalDistribution(0, 1);

    return score > requirement;
}

//caluclate numerical strength of party
public float GetPartyStrength()
{
    float warriorMultiplier = 1;
    float archerMultiplier = 1;
    float mageMultiplier = 1;

    float skillScore = 1;
    float strengthScore = 1;
    float teamworkScore = 1;

    int adventurerCount = 0;

    for (int i=0; i<4; ++i)
    {
        if (adventurers[i] != null && adventurers[i].state != AdventurerState.Dead)
        {
            //class add
            switch (adventurers[i].info.classType)
            {
                case ClassType.Warrior: 
                    warriorMultiplier += 1;
                    break;
                case ClassType.Archer:
                    archerMultiplier += 1;
                    break;
                case ClassType.Mage:
                    mageMultiplier += 1;
                    break;
            }

            //stats
            skillScore += adventurers[i].skills.skill;
            strengthScore += adventurers[i].skills.strength;
            teamworkScore += adventurers[i].skills.teamwork;

            //adventurer count
            ++adventurerCount;
        }
    }

    //zero party
    if (adventurerCount == 0)
    {
        return 0;
    }

    //calculate averages
    float divisor = Mathf.Sqrt(adventurerCount);
    skillScore /= divisor;
    strengthScore /= divisor;
    teamworkScore /= divisor;

    return warriorMultiplier * archerMultiplier * mageMultiplier * skillScore * strengthScore * teamworkScore;
}

float NormalDistribution(float mean, float std)
{
    // Generate a standard normal distribution with mean 0 and standard deviation 1
    float u1 = 1.0f - Random.value; // uniform(0,1] random values
    float u2 = 1.0f - Random.value;
    float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

    // Scale to our desired spread and center around the target
    return mean + randStdNormal * std;
}