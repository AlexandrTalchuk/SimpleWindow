using System;
using MergeMarines.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeMarines._Application.Scripts.UI.Common
{
    public class UISquadPower: MonoBehaviour
    {
        [Header("My Squad")]
        [SerializeField]
        private TextMeshProUGUI _squadPowerLabel = null;
        [SerializeField]
        private TextMeshProUGUI _enemiesPowerLabel = null;
        [SerializeField] 
        private TextMeshProUGUI _starsLabel = null;
        [SerializeField] 
        private TextMeshProUGUI _powerLabel = null;
        [SerializeField]
        private Image _powerImage = null;
        [SerializeField]
        private Sprite _activeStarSprite = null;
        [SerializeField]
        private Sprite _inactiveStarSprite = null;
        [SerializeField]
        private Image[] _powerStars = Array.Empty<Image>();

        [Header("Colors")] 
        [SerializeField] 
        private Color _redColor = default; 
        [SerializeField] 
        private Color _greenColor = default;
        
        private DungeonData _currentDungeonData = null;
        private int _currentStageIndex = 0;
        private int _necessaryPower;
        private int _recommendedPower;

        public void Localize()
        {
            _starsLabel.text = Strings.BonusLabel;
        }

        public void RefreshPowerLabels(DungeonData currentDungeonData, int currentStageIndex, UnitType[] myUnits)
        {
            int spawnMergeLevel = 0;
            _currentDungeonData = currentDungeonData;
            _currentStageIndex = currentStageIndex;
            int squadPower = UserManager.Instance.CountUnitsPower(myUnits);
            _necessaryPower = _currentDungeonData.GetNecessaryPower(_currentStageIndex);
            _recommendedPower = _currentDungeonData.GetRecommendedPower(_currentStageIndex);
            
            _squadPowerLabel.text = squadPower.ToPrettyString();
            
            if (squadPower >= _recommendedPower && _recommendedPower > 0)
                spawnMergeLevel = BattleData.Data.DefaultUnitStars;
            else if (squadPower >= _necessaryPower)
                spawnMergeLevel = BattleData.Data.DefaultUnitStars - 1;

            for (int i = 0; i < _powerStars.Length; i++)
            {
                _powerStars[i].gameObject.SetActive(i < BattleData.Data.DefaultUnitStars);
                _powerStars[i].sprite = i < spawnMergeLevel ? _activeStarSprite : _inactiveStarSprite;
            }

            if (squadPower < _necessaryPower)
                UpdatePowerLabels(_redColor, _redColor, true);
            
            else if (squadPower >= _necessaryPower && squadPower < _recommendedPower)
                UpdatePowerLabels(Color.white, Color.white, false);

            else
                UpdatePowerLabels(_greenColor, _greenColor, false);
        }

        private void UpdatePowerLabels(Color imageColor, Color textColor, bool isRequired)
        {
            if (isRequired)
            {
                _enemiesPowerLabel.text = $"{_necessaryPower.ToPrettyString()}";
                _powerLabel.text = $"{Strings.SquadPowerTitle}/{Strings.RequiredTitle}";
            }
            else
            {
                _enemiesPowerLabel.text = $"{_recommendedPower.ToPrettyString()}";
                _powerLabel.text = $"{Strings.SquadPowerTitle}/{Strings.RecomendedTitle}";
            }

            _squadPowerLabel.color = imageColor;
            _powerImage.color = textColor;
        }
    }
}