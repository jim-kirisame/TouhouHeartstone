﻿using System;

namespace TouhouHeartstone
{
    [Obsolete("除了EventWitness以外的其他Witness都被废除了")]
    [Serializable]
    public struct InitDrawWitness : IWitness
    {
        public int number { get; set; }
        public int playerId { get; }
        public CardInstance[] cards { get; }
        public InitDrawWitness(int playerId, CardInstance[] cards)
        {
            number = 0;
            this.playerId = playerId;
            this.cards = cards;
        }
        public override string ToString()
        {
            string s = "玩家" + playerId + "初始抽" + cards.Length + "张卡：";
            for (int i = 0; i < cards.Length; i++)
            {
                s += cards[i].ToString();
                if (i != cards.Length - 1)
                    s += "，";
                else
                    s += "。";
            }
            return s;
        }
    }
}