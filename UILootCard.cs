using System;
using System.Collections;
using DG.Tweening;
using MergeMarines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILootCard : MonoBehaviour
{
    private const float MinProgressValue = 0.07f;
    private const float DelayBeforeProgressAnimation = 1f;
    private const float ProgressAnimationDuration = 2f;

    [SerializeField]
    private Image _backgroundImage = null;
    [SerializeField]
    private Image _mainImage = null;

    [Header("Ability Group")]
    [SerializeField]
    private GameObject _abilityGroup = null;
    [SerializeField]
    private Image _abilityBG = null;
    [SerializeField]
    private Image _abilityImage = null;
    
    [Header("Weakness Group")]
    [SerializeField]
    private GameObject _weaknessGroup = null;
    [SerializeField]
    private Image _weaknessBG = null;
    [SerializeField]
    private Image _weaknessImage = null;

    [Header("Sprites")]
    [SerializeField]
    private Sprite _defaultBackground = null;
    [SerializeField]
    private Sprite _progressSprite = null;
    [SerializeField]
    private Sprite _progressSpriteMaxPWR = null;
    
    [Header("ProgressSlider")]
    [SerializeField]
    private Slider _progressSlider = null;
    [SerializeField]
    private TextMeshProUGUI _currentPowerTitle = null;
    [SerializeField]
    private TextMeshProUGUI _lootAmount = null;

    [Header("Notice")]
    [SerializeField]
    private Image _noticeImage = null;
    [SerializeField]
    private Animator _upgradeNoticeEffect = null;
    
    [Header("Animations")]
    [SerializeField]
    private Animation _unlockAnimation;
    
    [Space] 
    [SerializeField] 
    private TextMeshProUGUI _cardTitle = null;
    
    private UnitType[] _resistTypes;
    private UnitClass[] _weaknessClasses;

    public void Setup(RewardItem rewardItem)
    {
        _unlockAnimation.gameObject.SetActive(false);

        bool isUnit = rewardItem.Type == ItemType.Power || rewardItem.Type == ItemType.UnitCard;

        if (rewardItem.Type == ItemType.UnitCard && !LocalConfig.IsNewUnit(rewardItem.UnitType.Value))
        {
            gameObject.SetActive(false);
            return;
        }
        
        _abilityGroup.SetActive(isUnit);
        _weaknessGroup.SetActive(isUnit);

        SetUnitWeaknessAndUnitTypes();

        if (isUnit)
        {
            UnitType unitType = rewardItem.UnitType.Value;
            UserUnit userUnit = User.Current.GetUnit(unitType);

            SetBasicCardVisual(unitType);

            if (rewardItem.Type == ItemType.Power)
                UpdatePowerProgressValues(userUnit, rewardItem);

            else if (rewardItem.Type == ItemType.UnitCard)
            {
                ChangeProgressState(false,false, Strings.NewTitle);
                UnlockAnimation();
            }
        }
        else
        {
            int currencyValue = User.Current.GetCurrencyCount(rewardItem.Type);
            int lootValue = rewardItem.Count;
            int currencyBefore = currencyValue - lootValue;
            
            ChangeProgressState(false,true, currencyBefore.ToString(), $"+{lootValue}");
            StartCoroutine(AnimateText(currencyValue, lootValue, _cardTitle));
            
            _backgroundImage.sprite = _defaultBackground;
            _mainImage.sprite = IconManager.GetCardIcon(rewardItem.Type);
        }
    }

    private void SetBasicCardVisual(UnitType unitType)
    {
        var unitScheme = AssetsManager.Instance.GetUnitScheme(unitType);

        _lootAmount.fontSize = 55;
        _backgroundImage.sprite = unitScheme.CardBackground;
        _mainImage.sprite = unitScheme.UnitCardIcon;
        _abilityImage.sprite = unitScheme.DamageIcon;
        _weaknessImage.sprite = unitScheme.ClassIcon;

        _abilityBG.sprite = IconManager.GetResistBGIcon(_resistTypes.Contains(unitType));
        _weaknessBG.sprite = IconManager.GetWeaknessBGIcon(_weaknessClasses.Contains(unitType.ToUnitClass()));
        
        _abilityImage.SetNativeSize();
        _weaknessImage.SetNativeSize();
    }

    private void UpdatePowerProgressValues(UserUnit userUnit, RewardItem rewardItem)
    {
        _progressSlider.gameObject.SetActive(true);
        _upgradeNoticeEffect.gameObject.SetActive(false);
        _lootAmount.enabled = true;
        _cardTitle.enabled = false;
        _noticeImage.enabled = false;

        int currentPower = userUnit.GetCurrentPower();
        var currentLevel = userUnit.GetCurrentLevel();
        bool canUpgradePower = userUnit.CanUpgradeLevel();
        var powerBeforeLoot = currentPower - rewardItem.Count;
        float progress = GetPower(powerBeforeLoot, currentLevel);

        _progressSlider.value = progress < MinProgressValue ? progress > 0.01f ? MinProgressValue : 0 : progress;

        ChangeUpgradeState(powerBeforeLoot >= currentLevel.PowerMax, powerBeforeLoot);
        
        if(powerBeforeLoot != currentLevel.PowerMax)
            StartCoroutine(AnimateText(currentPower, rewardItem.Count, _currentPowerTitle,true, canUpgradePower));

        DOTween.Sequence()
            .AppendInterval(DelayBeforeProgressAnimation)
            .Append(_progressSlider.DOValue(GetPower(currentPower, currentLevel), ProgressAnimationDuration));

        if (User.Current.GetUnit(rewardItem.UnitType.Value).IsMaxInLevelPower())
            _lootAmount.text = rewardItem.Count > 0 ? $"+{rewardItem.Count}" : "+0";

        else
            _lootAmount.text = $"+{rewardItem.Count}";
    }

    private void ChangeUpgradeState(bool canUpgradePower, int currentPower)
    {
        if (canUpgradePower)
        {
            _currentPowerTitle.text = $"{currentPower} {Strings.Max}";
            _progressSlider.image.sprite = _progressSpriteMaxPWR;
            _noticeImage.enabled = true;
            _upgradeNoticeEffect.gameObject.SetActive(true);
            _upgradeNoticeEffect.Play(0);
        }
        else
        {
            _currentPowerTitle.text = $"{currentPower}";
            _progressSlider.image.sprite = _progressSprite;
            _noticeImage.enabled = false;
            _upgradeNoticeEffect.gameObject.SetActive(false);
        }
    }

    private void ChangeProgressState(bool isSliderEnable, bool isLootEnable, string cardTitle, string lootText = null)
    {
        _noticeImage.enabled = isSliderEnable;
        _progressSlider.gameObject.SetActive(isSliderEnable);
        _cardTitle.enabled = !isSliderEnable;
        _cardTitle.text = cardTitle;
        _lootAmount.enabled = isLootEnable;
        _lootAmount.text = lootText;
    }

    private IEnumerator AnimateText(int currencyValue, int lootValue, TextMeshProUGUI text, bool needUpgrade = false, bool canUpgradePower = false)
    {
        yield return new WaitForSeconds(DelayBeforeProgressAnimation);
        
        float currencyValueBefore = currencyValue - lootValue;
        float time = 0f;
        float currencyDelta = lootValue / (ProgressAnimationDuration / Time.fixedDeltaTime);
        
        while (time < ProgressAnimationDuration)
        {
            currencyValueBefore += currencyDelta;
            
            text.text = currencyValueBefore < currencyValue
                ? Mathf.RoundToInt(currencyValueBefore).ToString()
                : currencyValue.ToString();
            
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (needUpgrade && time >= ProgressAnimationDuration)
            ChangeUpgradeState(canUpgradePower, currencyValue);
    }

    private float GetPower(int power, UnitLevelData currentLevel)
    {
        return (float)(power - currentLevel.PowerMin) / (currentLevel.PowerMax - currentLevel.PowerMin);
    }

    private void UnlockAnimation()
    {
        _unlockAnimation.gameObject.SetActive(true);
        this.InvokeWithDelay(_unlockAnimation.clip.length + 0.0f, () => { _unlockAnimation.gameObject.SetActive(false); });
    }

    private void SetUnitWeaknessAndUnitTypes()
    {
        var currentDungeon = DungeonData.Get(GameManager.Instance.Dungeon);
        var currentStageIndex = GameManager.Instance.DungeonStage;
        var currentStage = currentDungeon.GetStage(currentStageIndex);

        if (TutorialManager.IsTutorial)
        {
            var allDungeons = UserData.Data.Dungeons;
            var dungeonType = allDungeons[0];

            currentDungeon = DungeonData.Get(dungeonType);
            currentStageIndex = User.Current.Progress.GetUserDungeon(dungeonType).CompletedStageIndex - 1;
            if (currentStageIndex > 0)
            {
                currentStageIndex++;
            }

            currentStageIndex = Math.Max(currentStageIndex, 0);

            currentStage = currentDungeon.GetStage(currentStageIndex);
        }

        _resistTypes = currentStage.ResistUnitsTypes;
        _weaknessClasses = currentStage.Weakness;
    }
}
