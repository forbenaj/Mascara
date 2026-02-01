using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    public BossController boss;
    public Slider slider;
    public Text label;

    private void OnEnable()
    {
        Attach(boss);
    }

    private void OnDisable()
    {
        if (boss != null)
            boss.onHealthChanged.RemoveListener(OnHealthChanged);
    }

    public void Attach(BossController target)
    {
        if (boss != null)
            boss.onHealthChanged.RemoveListener(OnHealthChanged);

        boss = target;
        if (boss == null || slider == null) return;

        slider.maxValue = boss.maxHealth;
        slider.value = boss.currentHealth;
        boss.onHealthChanged.AddListener(OnHealthChanged);
        UpdateLabel(boss.currentHealth, boss.maxHealth);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (slider != null)
        {
            slider.maxValue = max;
            slider.value = current;
        }
        UpdateLabel(current, max);
    }

    private void UpdateLabel(int current, int max)
    {
        if (label != null)
            label.text = $"{current}/{max}";
    }
}
