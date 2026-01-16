using UnityEngine;
using UnityEngine.UI;

public class StartActivationButton : MonoBehaviour
{
    [Tooltip("Optional: if not set, will FindObjectOfType<TowerBuilder>()")]
    public TowerBuilder towerBuilder;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void OnClick()
    {
        if (towerBuilder == null)
        {
            towerBuilder = FindObjectOfType<TowerBuilder>();
        }

        if (towerBuilder != null)
        {
            towerBuilder.StartActivation();
        }
    }
}
