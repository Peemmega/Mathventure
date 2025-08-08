
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using static Utils;



public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Card Properties")]
    [SerializeField] private double faceValue; //use to calculate equation
    [SerializeField] private double effectValue; //use to calculate effectiveness of the card
    [SerializeField] private EffectType effect;

    [Header("Visual and Feel")]
    public Vector3 deckPosition;

    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private VisualCardsHandler visualHandler;
    private Vector3 offset;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab;
    [HideInInspector] public CardVisual cardVisual;

    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;

    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        if (!instantiateVisual)
            return;

        visualHandler = FindFirstObjectByType<VisualCardsHandler>();
        cardVisual = Instantiate(cardVisualPrefab, deckPosition, new Quaternion(), visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this);

        transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
        CheckColliderWithPlayedCardSlots();
        CheckSelect();
    }

    void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
            transform.localPosition = Vector3.zero;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;

        
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;

        selected = !selected;
        SelectEvent.Invoke(this, selected);

        //if (selected)
        //    transform.localPosition += (cardVisual.transform.up * selectionOffset);
        //else
        //    transform.localPosition = Vector3.zero;
    }

    public void Deselect()
    {
        //if (selected)
        //{
        //    selected = false;
        //    if (selected)
        //        transform.localPosition += (cardVisual.transform.up * 50);
        //    else
        //        transform.localPosition = Vector3.zero;
        //}
    }

    public void CheckSelect()
    {
        if (selected)
        {
            // cardSlots if first parent is "PlayedCard"
            if (!transform.parent.parent.parent.CompareTag("PlayedCard"))
            {
                selected = CheckValidSpaceAndChangeParent(CardPlayGameController.current.playedCardSlots.transform);  
            }
        } else
        {
            // cardSlots if first parent is "CardInHand"
            if (!transform.parent.parent.CompareTag("CardInHand"))
            {
                selected = !CheckValidSpaceAndChangeParent(CardPlayGameController.current.cardInHand.transform);
            }
        }
    }
    

    public bool CheckValidSpaceAndChangeParent(Transform cardSlots)
    {
        foreach (Transform cardSlot in cardSlots)
        {
            if (cardSlot.childCount == 0 && !cardSlot.CompareTag("OperatorSlot"))
            {
                cardSlot.gameObject.SetActive(true);
                transform.SetParent(cardSlot);
                transform.localPosition = Vector3.zero;
                return true;
            }
        }

        return false;
    }


    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    private void OnDestroy()
    {
        if(cardVisual != null)
        Destroy(cardVisual.gameObject);
    }

    private void CheckColliderWithPlayedCardSlots()
    {
        if(isDragging == false)
        {
            foreach(Transform playedCard in CardPlayGameController.current.playedCardSlots.transform)
            {
                if (CardPlayGameController.current.RectOverlaps(transform.GetComponent<RectTransform>(), playedCard.GetComponent<RectTransform>()) && playedCard.CompareTag("Slot"))
                {
                    transform.SetParent(playedCard);
                }
            }
        }
    }

    public double GetFaceValue()
    {
        return faceValue;
    }

    public double GetEffectValue()
    {
        return effectValue;
    }

    public EffectType GetFace()
    {
        return effect;
    }

    public void SetFaceValue(double value)
    {
        faceValue = value;
    }

    public void SetEffectValue(double value)
    {
        effectValue = value;
    }

    public void SetEffect(EffectType value)
    {
        effect = value;
    }

    public void SetCardData(CardData cardData)
    {
        faceValue = cardData.FaceValue;
        effectValue = cardData.EffectValue;
        effect = cardData.Effect;
    }
}
