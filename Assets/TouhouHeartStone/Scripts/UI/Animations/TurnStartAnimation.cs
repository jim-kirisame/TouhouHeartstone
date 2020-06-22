﻿using TouhouHeartstone;
using UnityEngine;
using BJSYGameCore;
using UI;
namespace Game
{
    class TurnStartAnimation : EventAnimation<THHGame.TurnStartEventArg>
    {
        Timer _timer = new Timer() { duration = 2 };
        public override bool update(TableManager table, THHGame.TurnStartEventArg eventArg)
        {
            if (!_timer.isStarted)
            {
                table.ui.TurnTipImage.display();
                if (eventArg.player == table.player)
                {
                    table.ui.TurnTipText.text = "你的回合";
                    table.canControl = true;
                }
                else
                {
                    table.ui.TurnTipText.text = "对手的回合";
                    table.canControl = false;
                }
                table.ui.TurnTipImage.GetComponent<Animator>().Play("Display");
                foreach (var card in eventArg.player.field)
                {
                    var servant = table.getServant(card);
                    table.setServant(servant, card);
                }
                _timer.start();
            }
            if (!_timer.isExpired())
                return false;
            table.ui.TurnTipImage.hide();
            return true;
        }
    }
}
