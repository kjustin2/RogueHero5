using UnityEngine;

namespace RogueHero5
{
    public readonly struct DamageEvent
    {
        public DamageEvent(int amount, ActorTeam sourceTeam, GameObject source, Vector3 point, string moveId)
        {
            Amount = amount;
            SourceTeam = sourceTeam;
            Source = source;
            Point = point;
            MoveId = moveId;
        }

        public int Amount { get; }
        public ActorTeam SourceTeam { get; }
        public GameObject Source { get; }
        public Vector3 Point { get; }
        public string MoveId { get; }
    }
}
