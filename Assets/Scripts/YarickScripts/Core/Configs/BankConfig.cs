using UnityEngine;

namespace Core.Configs
{
    [CreateAssetMenu(menuName = "Configs/BankConfig")]
    public sealed class BankConfig : ScriptableObject
    {
        public int capacity = 0; // 0 = unlimited

        [Header("Auto deposit on win")]
        public int depositOnWin = 50; // сколько перекладываем в банк после победы
    }
}