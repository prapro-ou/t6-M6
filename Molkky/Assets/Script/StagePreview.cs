using UnityEngine;
using UnityEngine.UI;

public class StagePreview : MonoBehaviour
{
    public Image previewImage;

    public Sprite stage1Image;
    public Sprite stage2Image;
    public Sprite stage3Image;

    public void ShowStage1()
    {
        previewImage.sprite = stage1Image;
    }

    public void ShowStage2()
    {
        previewImage.sprite = stage2Image;
    }

    public void ShowStage3()
    {
        previewImage.sprite = stage3Image;
    }
}