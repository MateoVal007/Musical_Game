using UnityEngine;
using UnityEngine.EventSystems;

public class HoverGrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float speed = 10f;

    private Vector3 baseScale;
    private Vector3 targetScale;

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = baseScale * hoverScale;
    public void OnPointerExit(PointerEventData eventData) => targetScale = baseScale;
}