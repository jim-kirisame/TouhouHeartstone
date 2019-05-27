﻿namespace TouhouHeartstone.Frontend.Model.Witness
{
    public class OnUse : WitnessHandler
    {
        public override string Name => "onUse";

        public override bool HandleWitness(EventWitness witness, DeckController deck, GenericAction callback = null)
        {
            int playerIndex = witness.getVar<int>("playerIndex");
            int cardRID = witness.getVar<int>("cardRID");
            int cardDID = witness.getVar<int>("cardDID");
            int targetPosition = witness.getVar<int>("targetPosition");
            int targetCardRID = witness.getVar<int>("targetCardRID");

            UseCardEventArgs args;
            if (targetPosition == -1)
            {
                if (targetCardRID == 0)
                {
                    args = new UseCardEventArgs();
                }
                else
                {
                    args = new UseCardWithTargetArgs(targetCardRID);
                }
            }
            else
            {
                if (targetCardRID == 0)
                {
                    args = new UseCardWithPositionArgs(targetPosition);
                }
                else
                {
                    args = new UseCardWithTargetPositionArgs(targetPosition, targetCardRID);
                }
            }
            args.CardRID = cardRID;
            args.CardDID = cardDID;
            args.PlayerID = playerIndex;

            deck.RecvEvent(args, callback);
            return false;
        }
    }
}