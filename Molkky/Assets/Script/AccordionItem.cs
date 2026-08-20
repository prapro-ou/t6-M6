using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccordionItem : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private GameObject contentPanel;

    [Header("見出しのタイトルテキスト")]
    [SerializeField] private string titleText = "ルール項目";

    [Header("記号設定")]
    [SerializeField] private string closedIcon = "□ ";
    [SerializeField] private string openedIcon = "ー ";

    private bool isOpen = false;

    private void Awake()
    {
        SetOpenState(false);
    }

    public void Toggle()
    {
        SetOpenState(!isOpen);
    }

    private void SetOpenState(bool open)
    {
        isOpen = open;

        if (contentPanel != null)
        {
            contentPanel.SetActive(isOpen);
        }

        if (headerText != null)
        {
            string icon = isOpen ? openedIcon : closedIcon;
            headerText.text = icon + titleText;
        }

        // 表示切替後にレイアウトを即時再計算
        Canvas.ForceUpdateCanvases();

        // 1. 自分（AccordionItem）のサイズを更新
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(myRect);

        // 2. 親（RuleList）のサイズ・位置を再計算して下へ押し出す
        if (transform.parent != null)
        {
            RectTransform parentRect = transform.parent as RectTransform;
            if (parentRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }
}