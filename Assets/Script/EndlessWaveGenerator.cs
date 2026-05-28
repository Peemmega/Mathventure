using System.Collections.Generic;
using UnityEngine;

public static class EndlessWaveGenerator
{
    

    public static List<GameObject> GenerateNewEndlessWave()
    {
        int wave = GameController.current.Wave;
        int endlessValue = wave;
        List<GameObject> tempEnemys = new List<GameObject>();

        for (int i = 0; i < 1000; i++)
        {
            GameObject randomEnemy = ActionGameController.current.Enemys[Random.Range(0, ActionGameController.current.Enemys.Count)];
            Enemy randomEnemyData = randomEnemy.GetComponent<Enemy>();
           
            if (randomEnemyData.EndlessSpawnableOnWaveMoreThan <= wave && randomEnemyData.EndlessValue <= endlessValue)
            {
                Debug.Log(randomEnemyData.EndlessValue + " " + randomEnemy);
                tempEnemys.Add(randomEnemy);
                endlessValue -= randomEnemyData.EndlessValue;
            }

            if(endlessValue <= 0 || tempEnemys.Count >= 4)
            {
                break;
            }
        }
        Debug.Log(tempEnemys.Count);
        if(tempEnemys.Count == 0)
        {
            tempEnemys.Add(ActionGameController.current.Enemys[0]);
        }

        return tempEnemys;
    }
}
