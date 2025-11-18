using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
{
    public AudioClip hoverSFX;
    public AudioClip clickSFX;

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
    private void PlayHover()
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