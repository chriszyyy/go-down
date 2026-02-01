using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a UI element (e.g., full-screen Panel) to block pointer clicks from passing through.
/// Requires a Graphic (Image) with Raycast Target enabled on the same object.
/// </summary>
public class BlockAllClicks : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerClick(PointerEventData eventData) { }
    public void OnPointerDown(PointerEventData eventData) { }
    public void OnPointerUp(PointerEventData eventData) { }
}
