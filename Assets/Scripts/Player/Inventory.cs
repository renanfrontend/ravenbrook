using System.Collections.Generic;
using UnityEngine;

namespace Ravenbrook
{
    public enum ItemId { None, HerbG, HerbR, HerbM, Spray, AmmoP, AmmoS, AmmoM, KeyCrow, KeyLion, MedalSun, MedalMoon, MedalStar }
    public enum ItemKind { Heal, Ammo, Key, Medal }

    public class ItemInfo
    {
        public string Key; public ItemKind Kind; public int Max; public float Heal; public Color Color;
    }

    public static class ItemDB
    {
        static readonly Dictionary<ItemId, ItemInfo> db = new Dictionary<ItemId, ItemInfo>
        {
            { ItemId.HerbG, new ItemInfo { Key = "it_herb_g", Kind = ItemKind.Heal, Max = 1, Heal = 35, Color = new Color(0.3f, 0.75f, 0.3f) } },
            { ItemId.HerbR, new ItemInfo { Key = "it_herb_r", Kind = ItemKind.Heal, Max = 1, Heal = 0, Color = new Color(0.8f, 0.2f, 0.18f) } },
            { ItemId.HerbM, new ItemInfo { Key = "it_herb_m", Kind = ItemKind.Heal, Max = 1, Heal = 100, Color = new Color(0.75f, 0.6f, 0.2f) } },
            { ItemId.Spray, new ItemInfo { Key = "it_spray", Kind = ItemKind.Heal, Max = 1, Heal = 100, Color = new Color(0.85f, 0.88f, 0.9f) } },
            { ItemId.AmmoP, new ItemInfo { Key = "it_ammo_p", Kind = ItemKind.Ammo, Max = 60, Color = new Color(0.8f, 0.65f, 0.3f) } },
            { ItemId.AmmoS, new ItemInfo { Key = "it_ammo_s", Kind = ItemKind.Ammo, Max = 20, Color = new Color(0.75f, 0.2f, 0.15f) } },
            { ItemId.AmmoM, new ItemInfo { Key = "it_ammo_m", Kind = ItemKind.Ammo, Max = 12, Color = new Color(0.7f, 0.72f, 0.75f) } },
            { ItemId.KeyCrow, new ItemInfo { Key = "it_key_crow", Kind = ItemKind.Key, Max = 1, Color = new Color(0.35f, 0.35f, 0.38f) } },
            { ItemId.KeyLion, new ItemInfo { Key = "it_key_lion", Kind = ItemKind.Key, Max = 1, Color = new Color(0.9f, 0.72f, 0.25f) } },
            { ItemId.MedalSun, new ItemInfo { Key = "it_medal_sun", Kind = ItemKind.Medal, Max = 1, Color = new Color(0.72f, 0.45f, 0.22f) } },
            { ItemId.MedalMoon, new ItemInfo { Key = "it_medal_moon", Kind = ItemKind.Medal, Max = 1, Color = new Color(0.75f, 0.78f, 0.82f) } },
            { ItemId.MedalStar, new ItemInfo { Key = "it_medal_star", Kind = ItemKind.Medal, Max = 1, Color = new Color(0.95f, 0.78f, 0.28f) } },
        };

        public static ItemInfo Get(ItemId id) => db[id];
        public static string Name(ItemId id) => id == ItemId.None ? "" : Loc.Get(db[id].Key);
        public static string Desc(ItemId id) => id == ItemId.None ? "" : Loc.Get(db[id].Key + "_d");
    }

    [System.Serializable]
    public struct Slot
    {
        public ItemId Id; public int Count;
        public bool Empty => Id == ItemId.None || Count <= 0;
        public static Slot None => new Slot();
    }

    /// <summary>Inventário de 8 espaços (estilo survival horror) e o baú compartilhado das salas seguras.</summary>
    public class Inventory
    {
        public Slot[] Slots = new Slot[8];
        public Slot[] Box = new Slot[16];

        /// <summary>Adiciona e devolve o que sobrou.</summary>
        public int Add(ItemId id, int count) => AddTo(Slots, id, count);

        static int AddTo(Slot[] arr, ItemId id, int count)
        {
            int max = ItemDB.Get(id).Max;
            for (int i = 0; i < arr.Length && count > 0; i++)
                if (arr[i].Id == id && arr[i].Count < max)
                {
                    int put = Mathf.Min(max - arr[i].Count, count);
                    arr[i].Count += put; count -= put;
                }
            for (int i = 0; i < arr.Length && count > 0; i++)
                if (arr[i].Empty)
                {
                    int put = Mathf.Min(max, count);
                    arr[i] = new Slot { Id = id, Count = put }; count -= put;
                }
            return count;
        }

        public int Count(ItemId id)
        {
            int n = 0;
            foreach (var s in Slots) if (s.Id == id) n += s.Count;
            return n;
        }

        public bool Has(ItemId id) => Count(id) > 0;

        /// <summary>Remove até <paramref name="count"/> unidades e devolve quantas saíram.</summary>
        public int Take(ItemId id, int count)
        {
            int taken = 0;
            for (int i = Slots.Length - 1; i >= 0 && taken < count; i--)
                if (Slots[i].Id == id)
                {
                    int t = Mathf.Min(Slots[i].Count, count - taken);
                    Slots[i].Count -= t; taken += t;
                    if (Slots[i].Count <= 0) Slots[i] = Slot.None;
                }
            return taken;
        }

        /// <summary>Usa o item do espaço. Devolve a mensagem para mostrar.</summary>
        public string Use(int i, PlayerController p)
        {
            var s = Slots[i];
            if (s.Empty) return null;
            var info = ItemDB.Get(s.Id);
            if (info.Kind != ItemKind.Heal) return ItemDB.Desc(s.Id);
            if (s.Id == ItemId.HerbR) return Loc.Get("need_green");
            if (p.Hp >= p.MaxHp - 0.5f) return Loc.Get("hp_full");
            p.Heal(info.Heal);
            Slots[i] = Slot.None;
            Sfx.Play("pickup", 0.6f);
            return null;
        }

        public bool Combine(int a, int b, out string msg)
        {
            msg = Loc.Get("cant_combine");
            if (a == b || Slots[a].Empty || Slots[b].Empty) return false;
            var x = Slots[a].Id; var y = Slots[b].Id;
            if ((x == ItemId.HerbG && y == ItemId.HerbR) || (x == ItemId.HerbR && y == ItemId.HerbG))
            {
                Slots[a] = new Slot { Id = ItemId.HerbM, Count = 1 };
                Slots[b] = Slot.None;
                msg = Loc.Get("combined");
                return true;
            }
            if (x == y && ItemDB.Get(x).Kind == ItemKind.Ammo)
            {
                int max = ItemDB.Get(x).Max;
                int move = Mathf.Min(max - Slots[a].Count, Slots[b].Count);
                if (move <= 0) return false;
                Slots[a].Count += move; Slots[b].Count -= move;
                if (Slots[b].Count <= 0) Slots[b] = Slot.None;
                msg = Loc.Get("combined");
                return true;
            }
            return false;
        }

        public bool ToBox(int i)
        {
            if (Slots[i].Empty) return false;
            int left = AddTo(Box, Slots[i].Id, Slots[i].Count);
            if (left == Slots[i].Count) return false;
            if (left > 0) Slots[i].Count = left; else Slots[i] = Slot.None;
            return true;
        }

        public bool FromBox(int j)
        {
            if (Box[j].Empty) return false;
            int left = AddTo(Slots, Box[j].Id, Box[j].Count);
            if (left == Box[j].Count) return false;
            if (left > 0) Box[j].Count = left; else Box[j] = Slot.None;
            return true;
        }

        public static void Write(Slot[] arr, out int[] ids, out int[] counts)
        {
            ids = new int[arr.Length]; counts = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++) { ids[i] = (int)arr[i].Id; counts[i] = arr[i].Count; }
        }

        public static void Read(Slot[] arr, int[] ids, int[] counts)
        {
            if (ids == null || counts == null) return;
            for (int i = 0; i < arr.Length && i < ids.Length; i++) arr[i] = new Slot { Id = (ItemId)ids[i], Count = counts[i] };
        }
    }
}
