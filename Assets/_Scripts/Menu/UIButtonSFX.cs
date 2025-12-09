using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    public AudioClip hoverSFX;
    public AudioClip clickSFX;
    // this script gets attached by MenuManager to all present buttons in scene
    // below are subscribe events for both keyboard/controller inputs
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }
    public void OnSelect(BaseEventData eventData)
    {
        PlayHover();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }
    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }
    private void PlayHover() // audio playing events
    {
        if (hoverSFX != null)
            AudioManager.PlaySFX(hoverSFX);
    }
    private void PlayClick()
    {
        if (clickSFX != null)
            AudioManager.PlaySFX(clickSFX);
    }
}