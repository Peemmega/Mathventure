using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using static Utils;
using Random = UnityEngine.Random;

public class ActionGameController : MonoBehaviour
{
    public static ActionGameController current;

    [field:SerializeField] public bool UseEndlessGenerator { get; private set; }

    [field: SerializeField] public List<GameObject> Enemys { get; private set; }

    [field: SerializeField]
    public CharacterSlotsHolder HeroSlotsHolder { get; private set; }

    [field: SerializeField]
    public CharacterSlotsHolder EnemySlotsHolder { get; private set; }

    [field: SerializeField]
    public GameObject PlusHeroSlot { get; private set; }

    [field: SerializeField]
    public GameObject MinusHeroSlot { get; private set; }

    [field: SerializeField]
    public GameObject MultiplyHeroSlot { get; private set; }

    [field: SerializeField]
    public GameObject DivideHeroSlot { get; private set; }

    [field: SerializeField]
    public GameObject BuffHeroSlot { get; private set; }

    private Queue<HeroAttackData> heroAttackQueue = new();
    private HeroAttackData currentHeroAttack;

    private Queue<Enemy> enemiesAttackQueue = new();
    private Enemy currentEnemyAttack;

    private Action onCompleteAttackCallback;
    private Action onWinCallback;

    private void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }
        current = this;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    public void AllCharecterAttack(Action onCompleteAttack, Action onWin)
    {
        onCompleteAttackCallback = onCompleteAttack;
        onWinCallback = onWin;

        HeroAttack();
    }

    private void HeroAttack()
    {
        GameController.current.SetGamestate(GameState.HeroAttack);

        double multiplier = CardPlayGameController.current.Multiplier;
        List<int> operatorOrders = CardPlayGameController.current.OperatorOrders;

        heroAttackQueue.Clear();
        for (int i = 0; i < operatorOrders.Count - 1; i++)
        {
            OperationEnum heroType =
                CardPlayGameController.current
                    .PlayCardSlotList[operatorOrders[i]]
                    .transform.GetChild(0)
                    .GetComponent<Operatorcard>()
                    .operation;

            CardData leftCardData =
                CardPlayGameController.current
                    .PlayCardSlotList[operatorOrders[i] - 1]
                    .transform.GetChild(0)
                    .GetComponent<CardData>();

            CardData rightCardData =
                CardPlayGameController.current
                    .PlayCardSlotList[operatorOrders[i] + 1]
                    .transform.GetChild(0)
                    .GetComponent<CardData>();

            Hero hero = GetHeroByOperation(heroType);

            heroAttackQueue.Enqueue(new HeroAttackData
            {
                hero = hero,
                multiplier = multiplier,
                left = leftCardData,
                right = rightCardData
            });

            if (BuffHeroSlot.transform.GetChild(0) != null)
            {
                
                heroAttackQueue.Enqueue(new HeroAttackData
                {

                    
                    hero = BuffHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
                    multiplier = multiplier,
                    operationEnum = heroType,
                    left = leftCardData,
                    right = rightCardData
                });;
            }
        }

        StartNextHeroAttack();
    }

    private void StartNextHeroAttack()
    {
        while (heroAttackQueue.Count > 0)
        {
            currentHeroAttack = heroAttackQueue.Dequeue();

            if (currentHeroAttack.hero == null)
                continue;

            if (currentHeroAttack.hero.Hp <= 0)
                continue;

            currentHeroAttack.hero.Attack(
                currentHeroAttack.multiplier,
                currentHeroAttack.left,
                currentHeroAttack.right,
                currentHeroAttack.operationEnum
            );
            return;
        }

        OnAllHeroesFinished();
    }

    private void OnAllHeroesFinished()
    {
        if (IsEnemyRemain())
        {
            enemiesAttackQueue = GetEnemyAttackQueue();
            StartNextEnemyAttack();
        }
        else
        {
            onWinCallback?.Invoke();
        }
    }

    public bool IsEnemyRemain()
    {
        foreach (Transform enemySlot in EnemySlotsHolder.transform)
        {
            if (enemySlot.childCount >= 1)
            {
                return true;
            }
        }

        return false;
    }

    private Queue<Enemy> GetEnemyAttackQueue()
    {
        Queue<Enemy> enemyQueue = new Queue<Enemy>();

        foreach (Transform enemyslot in EnemySlotsHolder.transform)
        {
            if (enemyslot.childCount != 0)
            {
                enemyQueue.Enqueue(enemyslot.GetChild(0).GetComponent<Enemy>());
            }
        }

        return enemyQueue;
    }

    private void StartNextEnemyAttack()
    {
        if (enemiesAttackQueue.Count == 0)
        {
            OnAllEnemiesFinished();
            return;
        }

        currentEnemyAttack = enemiesAttackQueue.Dequeue();
        currentEnemyAttack.Attack();
    }

    private void OnAllEnemiesFinished()
    {
        CardPlayGameController.current.Attacking = false;
        onCompleteAttackCallback?.Invoke();
    }

    public void SpawnNextWave()
    {
        if (!GameController.current.IsEndless && GameController.current.Wave <= 3)
        {
            EnemySlotsHolder.SpawnCharacters(GameController.current.LevelDatas[GameController.current.Level - 1].Waves[GameController.current.Wave - 1].Enemies.ToList());
        }
        else if (GameController.current.IsEndless)
        {
            if (UseEndlessGenerator)
            {
                EnemySlotsHolder.SpawnCharacters(EndlessWaveGenerator.GenerateNewEndlessWave());
            } else
            {
                EnemySlotsHolder.SpawnCharacters(GameController.current.EndlessWaveDatas[Random.Range(0, GameController.current.EndlessWaveDatas.Count())].Enemies.ToList());
            }
        }
        else
        {
            EnemySlotsHolder.SpawnCharacters(new List<GameObject>());
        }
    }

    public void ResetHerosHP()
    {
        ResetHeroHP(PlusHeroSlot);
        ResetHeroHP(MinusHeroSlot);
        ResetHeroHP(MultiplyHeroSlot);
        ResetHeroHP(DivideHeroSlot);
        ResetHeroHP(BuffHeroSlot);
    }

    void ResetHeroHP(GameObject slot)
    {
        if (slot != null)
        {
            slot.transform.GetChild(0).GetComponent<Hero>().ResetHP();
        }
    }

    public void Debug_ForceAttack()
    {
        onCompleteAttackCallback = GameController.current.CleanupFornextRound;
        onWinCallback = GameController.current.RounndWin;

        GameController.current.SetGamestate(GameState.HeroAttack);

        double multiplier = 1;
        CardData leftValue = new CardData();
        leftValue.SetFaceValue(5);
        leftValue.SetEffectValue(1);

        CardData rightValue = new CardData();
        rightValue.SetFaceValue(2);
        rightValue.SetEffectValue(1);

        List<OperationEnum> heroTypes = new()
    {
        OperationEnum.Plus,
        OperationEnum.Minus,
        OperationEnum.Multiply
    };

        heroAttackQueue.Clear();

        foreach (OperationEnum heroType in heroTypes)
        {
            Hero hero = GetHeroByOperation(heroType);

            if (hero == null)
                continue;

            heroAttackQueue.Enqueue(new HeroAttackData
            {
                hero = hero,
                multiplier = multiplier,
                left = leftValue,
                right = rightValue
            });

            if (HeroToggler.current.IsBuffHeroOnThisLevel)
            {
                heroAttackQueue.Enqueue(new HeroAttackData
                {
                    hero = BuffHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
                    multiplier = multiplier,
                    operationEnum = heroType,
                    left = leftValue,
                    right = rightValue
                });
            }
        }

        StartNextHeroAttack();
    }


    private Hero GetHeroByOperation(OperationEnum heroType)
    {
        return heroType switch
        {
            OperationEnum.Plus => PlusHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
            OperationEnum.Minus => MinusHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
            OperationEnum.Multiply => MultiplyHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
            OperationEnum.Divide => DivideHeroSlot.transform.GetChild(0).GetComponent<Hero>(),
            _ => null
        };
    }

    private struct HeroAttackData
    {
        public Hero hero;
        public double multiplier;
        public OperationEnum operationEnum;
        public CardData left;
        public CardData right;
    }

    public void OnHeroAnimationFinished()
    {
        StartNextHeroAttack();
    }

    public void OnEnemyAnimationFinished()
    {
        StartNextEnemyAttack();
    }
}