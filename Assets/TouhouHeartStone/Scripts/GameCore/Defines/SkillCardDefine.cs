﻿using TouhouCardEngine;
using System;
using System.Reflection;
namespace TouhouHeartstone
{
    public abstract class SkillCardDefine : CardDefine
    {
        public override string type { get; set; } = CardDefineType.SKILL;
        public abstract int cost { get; set; }
        public override T getProp<T>(string propName)
        {
            if (propName == nameof(cost))
                return (T)(object)cost;
            return base.getProp<T>(propName);
        }
        public override void merge(CardDefine newVersion)
        {
            if (newVersion.type != type)
                UberDebug.LogWarning(newVersion + "的类型与" + this + "不同，可能是一次非法的数据合并！");
            if (newVersion is GeneratedCardDefine generated)
            {
                if (generated.hasProp(nameof(cost)))
                    cost = generated.getProp<int>(nameof(cost));
            }
        }
        public override string isUsable(CardEngine engine, Player player, Card card)
        {
            return null;
        }
    }
}