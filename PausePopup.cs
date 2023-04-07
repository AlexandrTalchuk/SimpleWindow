using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using MergeMarines._Application.Scripts.UI.Common;
using TMPro;

namespace MergeMarines.UI
{
    public class PausePopup : Window
    {
        [SerializeField]
        private TextMeshProUGUI _mainTitle = null;
        [SerializeField]
        private TextMeshProUGUI _wavesTitle = null;
        [SerializeField]
        private GameObject[] _starsObjects = null;
        [SerializeField]
        private TextMeshProUGUI _lootTitle = null;
        [SerializeField]
        private VerticalLayoutGroup _buttonsGroup = null;

        [Header("Cards")]
        [SerializeField] 
        private UILootCard[] _unitsCards = null;
        [SerializeField]
        private UILootCard[] _lootCards = null;
        [SerializeField] 
        private List<UIMyUnitCardHangar> _cardSlots = null;

        [Header("Play Button")]
        [SerializeField]
        private Button _playButton = null;
        [SerializeField]
        private TextMeshProUGUI _playButtonLabel = null;
        
        [Header("Home Button")]
        [SerializeField]
        private Button _homeButton = null;
        [SerializeField]
        private TextMeshProUGUI _homeButtonLabel = null;

        [Header("Sound Button")]
        [SerializeField]
        private Button _soundButton = null;
        [SerializeField]
        private TextMeshProUGUI _soundButtonLabel = null;
        
        [Header("Music Button")]
        [SerializeField]
        private Button _musicButton = null;
        [SerializeField]
        private TextMeshProUGUI _musicButtonLabel = null;

        [Header("Haptic Button")]
        [SerializeField]
        private Button _hapticButton = null;
        [SerializeField]
        private TextMeshProUGUI _hapticButtonLabel = null;
        
        [Header("SquadBlock")] 
        [SerializeField]
        private UISquadPower _squadPower;

        public static event Action QuitGameConfirmed = delegate { };
        public static event Action<bool> HapticButtonClicked = delegate { };
        
        public override bool IsPopup => true;

        public override bool Preload()
        {
            if ((float) Screen.height / Screen.width < 1.9f)
            {
                _buttonsGroup.spacing *= 0.5f;
                _buttonsGroup.padding.bottom = (int) (0.5f * _buttonsGroup.padding.bottom);
            }
            
            return base.Preload();
        }

        protected override void Start()
        {
            base.Start();
            _playButton.onClick.AddListener(OnPlayButtonClick);
            _homeButton.onClick.AddListener(OnHomeButtonClick);

            _soundButton.onClick.AddListener(OnSoundButtonClick);
            _musicButton.onClick.AddListener(OnMusicButtonClick);
            _hapticButton.onClick.AddListener(OnHapticButtonClick);

            Localize();
        }

        private void Localize()
        {
            _mainTitle.text = Strings.Pause;
            
            _playButtonLabel.text = Strings.Play;
            _homeButtonLabel.text = Strings.Home;
            _lootTitle.text = Strings.Loot + ":";
        }

        protected override void OnShown()
        {
            base.OnShown();

            int currentWave = WavesManager.IsSpawnStarted
                ? Mathf.Min(GameManager.Instance.CompletedWaveIndex + 2, GameManager.Instance.WavesLength)
                : 0;
            
            _wavesTitle.text = string.Format(Strings.ResultWaveFormat, currentWave, GameManager.Instance.WavesLength);

            for (var i = 0; i < _starsObjects.Length; i++)
            {
                _starsObjects[i].SetActive(i < GameManager.Instance.CurrentWaveStars);
            }

            SetupReward();
            UpdateSquadPower();
            
            UITextBubble.ForceHide();
            TutorialUIController.FreeHand();
            UITutorialMessageOverlay.ForceHide();
            UIMessageOverlay.ForceHide();
            UISystem.Instance.DialogueSystem.ForceHide();
        }

        protected override void Refresh()
        {
            base.Refresh();

            _musicButtonLabel.text = $"{Strings.MusicTitle}: {(LocalConfig.IsMusicEnable ? Strings.CommonOn : Strings.CommonOff)}";
            _soundButtonLabel.text = $"{Strings.SoundTitle}: {(LocalConfig.IsSoundEnable ? Strings.CommonOn : Strings.CommonOff)}";
            _hapticButtonLabel.text = $"{Strings.Vibro}: {(LocalConfig.IsHapticEnable ? Strings.CommonOn : Strings.CommonOff)}";
        }

        private void SetupReward()
        {
            // NOTE: skipEmptyRewards - тк на lootcards нужно учитывать юнитов, которым не добавилось павера, из-за того что он max
            var rewardItems = ChestData.Union(GameManager.Instance.PowerObserver.GetRewards()
                .Union(IngameDrop.Current.GetRewardPreview()), skipEmptyRewards: false)
                .ToList();

            foreach (var unit in User.Current.GetSelectedUnits())
            {
                if (!rewardItems.Any(r => (r.Type is ItemType.Power or ItemType.UnitCard)  && r.UnitType == unit))
                {
                    rewardItems.Add(new RewardItem(ItemType.Power, 0, unit));
                }
            }
            
            var itemTypes = new[]
            {
                ItemType.Power, ItemType.UnitCard
            };
            
            rewardItems = rewardItems.OrderBy(r => itemTypes.Contains(r.Type) ? itemTypes.IndexOf(r.Type) : int.MaxValue)
                .ThenBy(r => r.UnitType == null ? 0 : User.Current.GetSelectedUnits().IndexOf(r.UnitType.Value)).ToList();
            
            if (rewardItems.IsNullOrEmpty())
            {
                _unitsCards.Do(i => i.gameObject.SetActive(false));
                return;
            }
            
#if UNITY_EDITOR || FORCE_DEBUG_MENU
            if (rewardItems.Count > 8)
            {
                Debug.LogException(new Exception("Trying to show more then 8 rewards, but maximum is 8"));
            }
#endif
            SetupLootCards(rewardItems);
        }
        
        
         private void SetupLootCards(List<RewardItem> rewardItems)
        {
            int index = 0;
            int unitIndex = 0;
            var listOfRewardItems = rewardItems.ToList();
            var selectedUnits = User.Current.GetSelectedUnits().ToList();

            for (; index < rewardItems.Count && index < _unitsCards.Length; index++)
            {
                _unitsCards[index].gameObject.SetActive(true);
                _cardSlots[index].ChangeBackSprite(false);

                var isTutorialUnitCard = rewardItems[index].Type == ItemType.UnitCard && TutorialManager.IsTutorial;

                if (rewardItems[index].Type == ItemType.Power || isTutorialUnitCard)
                {
                    _unitsCards[index].Setup(rewardItems[index]);
                    listOfRewardItems.Remove(rewardItems[index]);
                    selectedUnits.Remove(rewardItems[index].UnitType.Value);
                }
                else
                {
                    break;
                }
            }

            for (; index < _unitsCards.Length; index++)
            {
                if (selectedUnits.Count <= unitIndex)
                {
                    _unitsCards[index].gameObject.SetActive(false);
                    _cardSlots[index].ChangeBackSprite(true);
                }
            }

            index = 0;
            for (; index < listOfRewardItems.Count && index < _lootCards.Length; index++)
            {
                _lootCards[index].gameObject.SetActive(true);
                _lootCards[index].Setup(listOfRewardItems[index]);
            }

            for (; index < _lootCards.Length; index++)
            {
                _lootCards[index].gameObject.SetActive(false);
            }
        }
        
        private void UpdateSquadPower()
        {
            var currentDungeon = DungeonData.Get(GameManager.Instance.Dungeon);
            var currentStageIndex = GameManager.Instance.DungeonStage;
            var selectedUnits = User.Current.GetSelectedUnits();
            
            _squadPower.RefreshPowerLabels(currentDungeon, currentStageIndex, selectedUnits);
        }

        private void OnPlayButtonClick()
        {
            AudioManager.Play2D(SoundType.Click);

            UISystem.ReturnToPreviousWindow();
        }

        private void OnHomeButtonClick()
        {
            AudioManager.Play2D(SoundType.Click);

            UIPopupOverlay.ShowHome((flag) =>
                {
                    if (flag)
                    {
                        QuitGameConfirmed();
                    }
                });
        }

        private void OnSoundButtonClick()
        {
            AudioManager.Play2D(SoundType.Click);

            LocalConfig.IsSoundEnable = !LocalConfig.IsSoundEnable;
            AudioManager.Play2D(SoundType.Click);

            AudioManager.RefreshConfig();
            Refresh();
        }

        private void OnMusicButtonClick()
        {
            AudioManager.Play2D(SoundType.Click);

            LocalConfig.IsMusicEnable = !LocalConfig.IsMusicEnable;
            AudioManager.Play2D(SoundType.Click);

            AudioManager.RefreshConfig();
            Refresh();
        }

        private void OnHapticButtonClick()
        {
            AudioManager.Play2D(SoundType.Click);

            LocalConfig.IsHapticEnable = !LocalConfig.IsHapticEnable;
            Refresh();

            HapticButtonClicked(LocalConfig.IsHapticEnable);
        }
    }
}