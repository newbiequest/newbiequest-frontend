using UnityEngine;
using TMPro;

public class NPCNameTag : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 2.3f, 0);

    private Camera mainCamera;
    private TextMeshProUGUI nameText;
    private Transform canvasTransform;

    void Start()
    {
        mainCamera = Camera.main;

        // NameTagCanvas와 NameText 자동으로 찾기
        canvasTransform = transform.Find("NameTagCanvas");
        if (canvasTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: NameTagCanvas 없음");
            return;
        }

        nameText = canvasTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText == null)
        {
            Debug.LogWarning($"{gameObject.name}: TextMeshProUGUI 없음");
            return;
        }

        nameText.text = gameObject.name;
        canvasTransform.localPosition = offset;
    }

    void LateUpdate()
    {
        if (mainCamera == null || canvasTransform == null) return;

        canvasTransform.rotation = Quaternion.LookRotation(
            canvasTransform.position - mainCamera.transform.position
        );
    }
}