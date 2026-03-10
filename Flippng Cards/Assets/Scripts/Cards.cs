using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cards : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private AudioSource cardFlipAudioSource;
    [SerializeField] private AudioClip flipSound;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;

    public bool isSelected;

    public CardsController cardsController;


    void Awake()
    {
        MatchIconToCard();
    }
    void OValidate()
    {
        MatchIconToCard();
    }
    private void MatchIconToCard()
    {
        //iconImage.sprite = iconSprite;
        if (iconImage == null)
            return;

        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        if (cardRect != null && iconRect != null)
        {
            // copy size and anchors to fill the card
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero; // ensures stretched
        }
    }
    public void OnCardClicked()
    {
        cardsController.SetSelectedCard(this);
    }
    public void SetIconSoprite(Sprite sp)
    {
        iconSprite = sp;
    }
    public void ShowIcon()
    {
        if (!this) return;
        StartCoroutine(FlipCard(iconSprite, true));
    }
    public void HideIcon()
    {
        if (!this) return;
        StartCoroutine(FlipCard(hiddenIconSprite, false));
    }
    private IEnumerator FlipCard(Sprite targetSprite, bool isShowing)
    {
        // Play flip sound
        if (cardFlipAudioSource != null && flipSound != null)
        {
            cardFlipAudioSource.PlayOneShot(flipSound);
        }

        float flipDuration = 0.4f;
        float halfDuration = flipDuration * 0.5f;
        float elapsedTime = 0f;

        // First half: rotate to 90 degrees on Y-axis
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float yRotation = Mathf.Lerp(0f, 90f, progress);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            yield return null;
        }

        // Change sprite at midpoint (when card is perpendicular)
        iconImage.sprite = targetSprite;
        isSelected = isShowing;

        // Second half: rotate back to 0 degrees
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            float yRotation = Mathf.Lerp(90f, 0f, progress);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            yield return null;
        }

        // Ensure final rotation is exactly 0
        transform.rotation = Quaternion.identity;
    }
}
