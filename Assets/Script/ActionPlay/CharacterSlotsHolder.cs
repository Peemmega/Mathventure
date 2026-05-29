using System.Collections.Generic;
using UnityEngine;

public class CharacterSlotsHolder : MonoBehaviour
{
    [field: SerializeField] public List<Transform> CharacterSlots { get; private set; }
    [field: SerializeField] public List<GameObject> characters = new();

    private void Update()
    {
        //get characters from character in CharacterSlots
        characters.Clear();
        foreach (Transform characterSlot in CharacterSlots)
        {
            if(characterSlot != null && characterSlot.GetComponentInChildren<Character>()!= null)
            {
                characters.Add(characterSlot.GetComponentInChildren<Character>().gameObject);
            }
        }
    }

    public void SpawnCharacters(List<GameObject> prefabs)
    {
        characters.Clear();

        int spawnCount = Mathf.Min(prefabs.Count, CharacterSlots.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            Transform slot = CharacterSlots[i];

            // Destroy old children safely
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }

            if (prefabs[i] == null) continue;

            GameObject instance = Instantiate(prefabs[i], slot, false);
            
            instance.GetComponent<Enemy>().BuffByWave();

            characters.Add(instance);
        }

        UpdateCharacters();
    }

    public void UpdateCharacters()
    {
        for (int i = 0; i < characters.Count; i++)
        {
            characters[i].transform.SetParent(CharacterSlots[i], false);
            characters[i].transform.localPosition = Vector3.zero;
        }
    }

    public List<GameObject> GetFirstCharacterAsList()
    {
        return characters.Count >= 1
        ? new List<GameObject> { characters[0] }
        : new List<GameObject>();
    }

    public List<GameObject> GetFirstTwoCharactersAsList()
    {
        switch (characters.Count)
        {
            case 0:
                return new List<GameObject>(); ;
            case 1:
                return new List<GameObject> { characters[0] };
            default: //more than 2 characters left
                return new List<GameObject> { characters[0], characters[1] };
        }
    }

    public List<GameObject> GetLastCharacterAsList()
    {
        if (characters.Count == 0)
            return new List<GameObject>();

        int startIndex = Mathf.Max(0, characters.Count - 3);
        int randomIndex = Random.Range(startIndex, characters.Count);

        return new List<GameObject> { characters[randomIndex] };
    }

    public List<GameObject> GetAllCharactersAsList()
    {
        return characters.Count >= 1
        ? new List<GameObject>(characters)
        : new List<GameObject>();
    }
}